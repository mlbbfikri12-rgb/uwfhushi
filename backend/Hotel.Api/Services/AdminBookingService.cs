using Hotel.Api.Data;
using Hotel.Api.DTOs;
using Hotel.Api.Entities.Tenant;
using Microsoft.EntityFrameworkCore;

namespace Hotel.Api.Services;

public interface IAdminBookingService
{
    Task<AdminPagedResultDto<AdminBookingGroupListItemDto>> GetBookingGroupsAsync(
        AdminBookingGroupQueryDto query,
        CancellationToken cancellationToken = default);

    Task<AdminBookingGroupDetailDto?> GetBookingGroupByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<AdminPagedResultDto<AdminPaymentEventDto>> GetPaymentEventsAsync(
        AdminPaymentEventQueryDto query,
        CancellationToken cancellationToken = default);
}

public class AdminBookingService : IAdminBookingService
{
    private const int MaxPageSize = 100;

    private readonly ITenantDbFactory _tenantDbFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AdminBookingService> _logger;

    public AdminBookingService(
        ITenantDbFactory tenantDbFactory,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AdminBookingService> logger)
    {
        _tenantDbFactory = tenantDbFactory;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<AdminPagedResultDto<AdminBookingGroupListItemDto>> GetBookingGroupsAsync(
        AdminBookingGroupQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var branchCode = GetBranchCode();
        await using var db = await _tenantDbFactory.CreateAsync(branchCode, cancellationToken);

        var page = NormalizePage(query.Page);
        var pageSize = NormalizePageSize(query.PageSize);

        var groups = db.BookingGroups
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var keyword = query.Q.Trim();
            groups = groups.Where(g =>
                EF.Functions.ILike(g.GroupCode, $"%{keyword}%") ||
                EF.Functions.ILike(g.Customer.Name, $"%{keyword}%") ||
                EF.Functions.ILike(g.Customer.Email, $"%{keyword}%"));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = query.Status.Trim().ToLowerInvariant();
            groups = groups.Where(g => g.Status.ToLower() == status);
        }

        if (query.From.HasValue)
        {
            groups = groups.Where(g => g.CreatedAt >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            groups = groups.Where(g => g.CreatedAt <= query.To.Value);
        }

        var totalItems = await groups.CountAsync(cancellationToken);
        groups = ApplyBookingGroupSort(groups, query.SortBy, query.SortDirection);

        var items = await groups
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(g => new AdminBookingGroupListItemDto
            {
                Id = g.Id,
                GroupCode = g.GroupCode,
                CustomerName = g.Customer.Name,
                CustomerEmail = g.Customer.Email,
                Status = g.Status,
                PaymentStatus = g.Bookings
                    .Select(b => b.PaymentStatus ?? "pending")
                    .FirstOrDefault() ?? "pending",
                BookingCount = g.Bookings.Count,
                TotalAmount = g.TotalAmount,
                HoldUntilUtc = g.HoldUntilUtc,
                PaidAt = g.PaidAt,
                CreatedAt = g.CreatedAt
            })
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "Admin booking groups listed. BranchCode={BranchCode}, Page={Page}, PageSize={PageSize}, TotalItems={TotalItems}",
            branchCode,
            page,
            pageSize,
            totalItems);

        return new AdminPagedResultDto<AdminBookingGroupListItemDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            Items = items
        };
    }

    public async Task<AdminBookingGroupDetailDto?> GetBookingGroupByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var branchCode = GetBranchCode();
        await using var db = await _tenantDbFactory.CreateAsync(branchCode, cancellationToken);

        var detail = await db.BookingGroups
            .AsNoTracking()
            .Where(g => g.Id == id)
            .Select(g => new AdminBookingGroupDetailDto
            {
                Id = g.Id,
                GroupCode = g.GroupCode,
                Status = g.Status,
                PaymentStatus = g.Bookings
                    .Select(b => b.PaymentStatus ?? "pending")
                    .FirstOrDefault() ?? "pending",
                TotalAmount = g.TotalAmount,
                HoldUntilUtc = g.HoldUntilUtc,
                PaidAt = g.PaidAt,
                CreatedAt = g.CreatedAt,
                Customer = new AdminBookingCustomerDto
                {
                    Id = g.Customer.Id,
                    GlobalCustomerId = g.Customer.GlobalCustomerId,
                    Name = g.Customer.Name,
                    Email = g.Customer.Email,
                    Phone = g.Customer.Phone
                },
                Bookings = g.Bookings
                    .OrderBy(b => b.CheckIn)
                    .ThenBy(b => b.CreatedAt)
                    .Select(b => new AdminBookingItemDto
                    {
                        Id = b.Id,
                        BookingCode = b.BookingCode,
                        RoomTypeId = b.RoomTypeId,
                        RoomTypeName = b.RoomType.Name,
                        RoomId = b.RoomId,
                        RoomNumber = b.Room == null ? null : b.Room.RoomNumber,
                        CheckIn = b.CheckIn,
                        CheckOut = b.CheckOut,
                        AdultCount = b.AdultCount,
                        ChildCount = b.ChildCount,
                        BasePrice = b.BasePrice,
                        Tax = b.Tax,
                        TotalPrice = b.TotalPrice,
                        Status = b.Status,
                        PaymentStatus = b.PaymentStatus ?? "pending",
                        PaymentMethod = b.PaymentMethod,
                        PaidAt = b.PaidAt,
                        ConfirmedAtUtc = b.ConfirmedAtUtc,
                        CreatedAt = b.CreatedAt
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (detail == null)
        {
            return null;
        }

        var events = await db.PaymentEvents
            .AsNoTracking()
            .Where(e => e.OrderId == detail.GroupCode)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new AdminPaymentEventDto
            {
                Id = e.Id,
                OrderId = e.OrderId,
                TransactionId = e.TransactionId,
                PaymentType = e.PaymentType,
                TransactionStatus = e.TransactionStatus,
                MappedStatus = e.MappedStatus,
                GrossAmount = e.GrossAmount,
                ProcessingStatus = e.ProcessingStatus,
                ErrorMessage = e.ErrorMessage,
                CreatedAt = e.CreatedAt
            })
            .ToListAsync(cancellationToken);

        detail.PaymentEvents = events;

        _logger.LogInformation(
            "Admin booking group detail loaded. BranchCode={BranchCode}, BookingGroupId={BookingGroupId}, BookingGroupCode={BookingGroupCode}",
            branchCode,
            detail.Id,
            detail.GroupCode);

        return detail;
    }

    public async Task<AdminPagedResultDto<AdminPaymentEventDto>> GetPaymentEventsAsync(
        AdminPaymentEventQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var branchCode = GetBranchCode();
        await using var db = await _tenantDbFactory.CreateAsync(branchCode, cancellationToken);

        var page = NormalizePage(query.Page);
        var pageSize = NormalizePageSize(query.PageSize);

        var events = db.PaymentEvents
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var keyword = query.Q.Trim();
            events = events.Where(e =>
                EF.Functions.ILike(e.OrderId, $"%{keyword}%") ||
                EF.Functions.ILike(e.TransactionId, $"%{keyword}%"));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = query.Status.Trim().ToLowerInvariant();
            events = events.Where(e => e.MappedStatus.ToLower() == status || e.ProcessingStatus.ToLower() == status);
        }

        if (query.From.HasValue)
        {
            events = events.Where(e => e.CreatedAt >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            events = events.Where(e => e.CreatedAt <= query.To.Value);
        }

        var totalItems = await events.CountAsync(cancellationToken);
        var items = await events
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new AdminPaymentEventDto
            {
                Id = e.Id,
                OrderId = e.OrderId,
                TransactionId = e.TransactionId,
                PaymentType = e.PaymentType,
                TransactionStatus = e.TransactionStatus,
                MappedStatus = e.MappedStatus,
                GrossAmount = e.GrossAmount,
                ProcessingStatus = e.ProcessingStatus,
                ErrorMessage = e.ErrorMessage,
                CreatedAt = e.CreatedAt
            })
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "Admin payment events listed. BranchCode={BranchCode}, Page={Page}, PageSize={PageSize}, TotalItems={TotalItems}",
            branchCode,
            page,
            pageSize,
            totalItems);

        return new AdminPagedResultDto<AdminPaymentEventDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            Items = items
        };
    }

    private string GetBranchCode()
    {
        var branchCode = _httpContextAccessor.HttpContext?.Request.Headers["X-Branch-Code"].ToString();
        if (string.IsNullOrWhiteSpace(branchCode))
        {
            throw new Exception("X-Branch-Code header is missing");
        }

        return branchCode.Trim().ToUpperInvariant();
    }

    private static IQueryable<BookingGroup> ApplyBookingGroupSort(
        IQueryable<BookingGroup> query,
        string? sortBy,
        string? sortDirection)
    {
        var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);

        return (sortBy ?? "createdAt").Trim().ToLowerInvariant() switch
        {
            "code" or "groupcode" => descending
                ? query.OrderByDescending(g => g.GroupCode)
                : query.OrderBy(g => g.GroupCode),
            "status" => descending
                ? query.OrderByDescending(g => g.Status)
                : query.OrderBy(g => g.Status),
            "total" or "totalamount" => descending
                ? query.OrderByDescending(g => g.TotalAmount)
                : query.OrderBy(g => g.TotalAmount),
            "hold" or "holduntil" => descending
                ? query.OrderByDescending(g => g.HoldUntilUtc)
                : query.OrderBy(g => g.HoldUntilUtc),
            _ => descending
                ? query.OrderByDescending(g => g.CreatedAt)
                : query.OrderBy(g => g.CreatedAt)
        };
    }

    private static int NormalizePage(int page) => Math.Max(1, page);

    private static int NormalizePageSize(int pageSize) => Math.Clamp(pageSize, 1, MaxPageSize);
}
