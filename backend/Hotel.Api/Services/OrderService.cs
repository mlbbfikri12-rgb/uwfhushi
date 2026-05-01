using Hotel.Api.Data;
using Hotel.Api.DTOs;
using Hotel.Api.Entities.Tenant;
using Microsoft.EntityFrameworkCore;

namespace Hotel.Api.Services;

public interface IOrderService
{
    Task<OrderCurrentDto> AddAsync(Guid customerGlobalId, AddOrderItemDto dto, CancellationToken cancellationToken = default);
    Task<OrderCurrentDto> GetCurrentAsync(Guid customerGlobalId, CancellationToken cancellationToken = default);
    Task<OrderCurrentDto> DeleteItemAsync(Guid customerGlobalId, Guid orderItemId, CancellationToken cancellationToken = default);
}

public class OrderService : IOrderService
{
    private readonly AppDbContext _db;

    public OrderService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<OrderCurrentDto> AddAsync(Guid customerGlobalId, AddOrderItemDto dto, CancellationToken cancellationToken = default)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.GlobalCustomerId == customerGlobalId, cancellationToken);
        if (customer == null)
            throw new Exception("Customer not found in this branch");

        var checkIn = DateTime.SpecifyKind(dto.CheckIn.Date, DateTimeKind.Utc);
        var checkOut = DateTime.SpecifyKind(dto.CheckOut.Date, DateTimeKind.Utc);
        if (checkOut <= checkIn)
            throw new Exception("Invalid date range");

        if (dto.TotalRooms < 1)
            throw new Exception("Total rooms must be at least 1");

        var roomType = await _db.RoomTypes.FirstOrDefaultAsync(rt => rt.Id == dto.RoomTypeId, cancellationToken);
        if (roomType == null)
            throw new Exception("Room type not found");

        var ratePlan = await _db.RatePlans.FirstOrDefaultAsync(rp => rp.Id == dto.RatePlanId && rp.RoomTypeId == dto.RoomTypeId && rp.IsActive, cancellationToken);
        if (ratePlan == null)
            throw new Exception("Rate plan not found");

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

        return await GetCurrentAsync(customerGlobalId, cancellationToken);
    }

    public async Task<OrderCurrentDto> GetCurrentAsync(Guid customerGlobalId, CancellationToken cancellationToken = default)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.GlobalCustomerId == customerGlobalId, cancellationToken);
        if (customer == null)
            throw new Exception("Customer not found in this branch");

        var draft = await _db.OrderDrafts
            .AsNoTracking()
            .Include(o => o.Items)
            .ThenInclude(i => i.RoomType)
            .Include(o => o.Items)
            .ThenInclude(i => i.RatePlan)
            .FirstOrDefaultAsync(o => o.CustomerId == customer.Id && o.Status == "draft", cancellationToken);

        if (draft == null)
        {
            return new OrderCurrentDto
            {
                OrderDraftId = Guid.Empty,
                Items = Array.Empty<OrderItemDto>(),
                GrandTotal = 0m
            };
        }

        var items = draft.Items
            .OrderBy(i => i.CreatedAt)
            .Select(i => new OrderItemDto
            {
                Id = i.Id,
                RoomTypeId = i.RoomTypeId,
                RatePlanId = i.RatePlanId,
                RoomTypeName = i.RoomType?.Name ?? string.Empty,
                RatePlanName = i.RatePlan?.Name ?? string.Empty,
                CheckIn = i.CheckIn,
                CheckOut = i.CheckOut,
                TotalRooms = i.TotalRooms,
                PricePerNight = i.PricePerNight,
                TotalPrice = i.TotalPrice
            })
            .ToList();

        return new OrderCurrentDto
        {
            OrderDraftId = draft.Id,
            Items = items,
            GrandTotal = items.Sum(i => i.TotalPrice)
        };
    }

    public async Task<OrderCurrentDto> DeleteItemAsync(Guid customerGlobalId, Guid orderItemId, CancellationToken cancellationToken = default)
    {
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

        return await GetCurrentAsync(customerGlobalId, cancellationToken);
    }
}
