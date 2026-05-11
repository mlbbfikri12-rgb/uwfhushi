using Hotel.Api.Data;
using Hotel.Api.DTOs;
using Hotel.Api.Entities.Tenant;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Hotel.Api.Services;

public interface IOrderService
{
    Task<OrderCurrentDto> AddAsync(Guid customerGlobalId, AddOrderItemDto dto, CancellationToken cancellationToken = default);
    Task<OrderCurrentDto> GetCurrentAsync(Guid customerGlobalId, CancellationToken cancellationToken = default);
    Task<OrderCurrentDto> DeleteItemAsync(Guid customerGlobalId, Guid orderItemId, CancellationToken cancellationToken = default);
}

public class OrderService : IOrderService
{
    private readonly ITenantDbFactory _tenantDbFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly MasterDbContext _masterDb;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        ITenantDbFactory tenantDbFactory,
        IHttpContextAccessor httpContextAccessor,
        MasterDbContext masterDb,
        ILogger<OrderService> logger)
    {
        _tenantDbFactory = tenantDbFactory;
        _httpContextAccessor = httpContextAccessor;
        _masterDb = masterDb;
        _logger = logger;
    }

    private string GetBranchCode()
    {
        var branchCode = _httpContextAccessor.HttpContext?.Request.Headers["X-Branch-Code"].ToString();

        if (string.IsNullOrWhiteSpace(branchCode))
            throw new Exception("X-Branch-Code header is missing");

        return branchCode.Trim().ToUpperInvariant();
    }

    public async Task<OrderCurrentDto> AddAsync(Guid customerGlobalId, AddOrderItemDto dto, CancellationToken cancellationToken = default)
    {
        var branchCode = GetBranchCode();

        await using var _db = await _tenantDbFactory.CreateAsync(branchCode, cancellationToken);

        var checkIn = DateTime.SpecifyKind(dto.CheckIn.Date, DateTimeKind.Utc);
        var checkOut = DateTime.SpecifyKind(dto.CheckOut.Date, DateTimeKind.Utc);

        if (checkOut <= checkIn)
            throw new Exception("Invalid date range");

        if (dto.TotalRooms < 1)
            throw new Exception("Total rooms must be at least 1");

        var roomType = await _db.RoomTypes.FirstOrDefaultAsync(rt => rt.Id == dto.RoomTypeId, cancellationToken);
        if (roomType == null)
            throw new Exception("Room type not found");

        var ratePlan = await _db.RatePlans.FirstOrDefaultAsync(
            rp => rp.Id == dto.RatePlanId &&
                  rp.RoomTypeId == dto.RoomTypeId &&
                  rp.IsActive,
            cancellationToken);

        if (ratePlan == null)
            throw new Exception("Rate plan not found");

        var customer = await GetOrCreateTenantCustomerAsync(_db, customerGlobalId, branchCode, cancellationToken);

        var draft = await _db.OrderDrafts
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.CustomerId == customer.Id && o.Status == "draft", cancellationToken);

        if (draft == null)
        {
            draft = new OrderDraft
            {
                Id = Guid.NewGuid(),
                CustomerId = customer.Id,
                Status = "draft",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.OrderDrafts.Add(draft);
        }

        var nights = (checkOut - checkIn).Days;
        var totalPrice = ratePlan.Price * nights * dto.TotalRooms;

        _db.OrderItems.Add(new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderDraftId = draft.Id,
            RoomTypeId = roomType.Id,
            RatePlanId = ratePlan.Id,
            CheckIn = checkIn,
            CheckOut = checkOut,
            TotalRooms = dto.TotalRooms,
            PricePerNight = ratePlan.Price,
            TotalPrice = totalPrice,
            CreatedAt = DateTime.UtcNow
        });

        draft.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Order draft item added. BranchCode={BranchCode}, CustomerId={CustomerId}, RoomTypeId={RoomTypeId}, RatePlanId={RatePlanId}, TotalRooms={TotalRooms}",
            branchCode,
            customer.Id,
            dto.RoomTypeId,
            dto.RatePlanId,
            dto.TotalRooms);

        return await GetCurrentAsync(customerGlobalId, cancellationToken);
    }

    public async Task<OrderCurrentDto> GetCurrentAsync(Guid customerGlobalId, CancellationToken cancellationToken = default)
    {
        var branchCode = GetBranchCode();

        await using var _db = await _tenantDbFactory.CreateAsync(branchCode, cancellationToken);

        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.GlobalCustomerId == customerGlobalId, cancellationToken);
        if (customer == null)
        {
            _logger.LogInformation(
                "Order current requested before tenant customer exists. BranchCode={BranchCode}, CustomerGlobalId={CustomerGlobalId}",
                branchCode,
                customerGlobalId);

            return EmptyOrder();
        }

        var draft = await _db.OrderDrafts
            .AsNoTracking()
            .Where(o => o.CustomerId == customer.Id && o.Status == "draft")
            .Select(o => new
            {
                o.Id,
                Items = o.Items
                    .OrderBy(i => i.CreatedAt)
                    .Select(i => new OrderItemDto
                    {
                        Id = i.Id,
                        RoomTypeId = i.RoomTypeId,
                        RatePlanId = i.RatePlanId,
                        RoomTypeName = i.RoomType != null ? i.RoomType.Name : string.Empty,
                        RatePlanName = i.RatePlan != null ? i.RatePlan.Name : string.Empty,
                        CheckIn = i.CheckIn,
                        CheckOut = i.CheckOut,
                        Image = i.RoomType != null ? i.RoomType.ImageUrl : string.Empty,
                        TotalRooms = i.TotalRooms,
                        PricePerNight = i.PricePerNight,
                        TotalPrice = i.TotalPrice
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (draft == null)
        {
            return EmptyOrder();
        }

        return new OrderCurrentDto
        {
            OrderDraftId = draft.Id,
            Items = draft.Items,
            GrandTotal = draft.Items.Sum(i => i.TotalPrice)
        };
    }

    public async Task<OrderCurrentDto> DeleteItemAsync(Guid customerGlobalId, Guid orderItemId, CancellationToken cancellationToken = default)
    {
        var branchCode = GetBranchCode();

        await using var _db = await _tenantDbFactory.CreateAsync(branchCode, cancellationToken);

        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.GlobalCustomerId == customerGlobalId, cancellationToken);
        if (customer == null)
            throw new Exception("Customer not found in this branch");

        var draft = await _db.OrderDrafts
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.CustomerId == customer.Id && o.Status == "draft", cancellationToken);

        if (draft == null)
            throw new Exception("Order draft not found");

        var item = draft.Items.FirstOrDefault(i => i.Id == orderItemId);
        if (item == null)
            throw new Exception("Order item not found");

        _db.OrderItems.Remove(item);
        draft.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Order draft item deleted. BranchCode={BranchCode}, CustomerId={CustomerId}, OrderItemId={OrderItemId}",
            branchCode,
            customer.Id,
            orderItemId);

        return await GetCurrentAsync(customerGlobalId, cancellationToken);
    }

    private async Task<Customer> GetOrCreateTenantCustomerAsync(
        AppDbContext db,
        Guid customerGlobalId,
        string branchCode,
        CancellationToken cancellationToken)
    {
        var existing = await db.Customers
            .FirstOrDefaultAsync(c => c.GlobalCustomerId == customerGlobalId, cancellationToken);

        if (existing != null)
        {
            return existing;
        }

        var globalCustomer = await _masterDb.CustomersGlobal
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == customerGlobalId, cancellationToken);

        if (globalCustomer == null)
        {
            throw new Exception("Customer account not found");
        }

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            GlobalCustomerId = globalCustomer.Id,
            Name = globalCustomer.Name,
            Email = globalCustomer.Email,
            Phone = globalCustomer.Phone,
            IsVerified = globalCustomer.IsVerified,
            CreatedAt = DateTime.UtcNow
        };

        db.Customers.Add(customer);

        try
        {
            await db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Tenant customer auto-created from order flow. BranchCode={BranchCode}, CustomerId={CustomerId}, CustomerGlobalId={CustomerGlobalId}",
                branchCode,
                customer.Id,
                customer.GlobalCustomerId);

            return customer;
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            db.Entry(customer).State = EntityState.Detached;

            _logger.LogWarning(
                ex,
                "Tenant customer auto-create raced with another request. BranchCode={BranchCode}, CustomerGlobalId={CustomerGlobalId}",
                branchCode,
                customerGlobalId);

            return await db.Customers.FirstAsync(c => c.GlobalCustomerId == customerGlobalId, cancellationToken);
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        return ex.InnerException is PostgresException postgresException &&
               postgresException.SqlState == PostgresErrorCodes.UniqueViolation;
    }

    private static OrderCurrentDto EmptyOrder()
    {
        return new OrderCurrentDto
        {
            OrderDraftId = Guid.Empty,
            Items = Array.Empty<OrderItemDto>(),
            GrandTotal = 0m
        };
    }
}
