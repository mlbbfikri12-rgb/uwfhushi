using Hotel.Api.Configurations;
using Hotel.Api.Data;
using Hotel.Api.DTOs;
using Hotel.Api.Entities.Master;
using Hotel.Api.Entities.Tenant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using Npgsql;
using System.Data;

namespace Hotel.Api.Services;

public interface IBookingService
{
    Task<Booking> CreateBookingAsync(
        Guid roomTypeId,
        Guid ratePlanId,
        string customerName,
        string customerEmail,
        string customerPhone,
        DateTime checkIn,
        DateTime checkOut,
        int adult,
        int child,
        string? paymentMethod,
        string? notes,
        Guid? bookingGroupId = null);

    Task<Booking> CreateBookingForCustomerAsync(
        Guid customerGlobalId,
        Guid roomTypeId,
        Guid ratePlanId,
        DateTime checkIn,
        DateTime checkOut,
        int adult,
        int child,
        string? paymentMethod,
        string? notes,
        Guid? bookingGroupId = null);

    Task<CheckoutOrderResponseDto> CheckoutFromOrderAsync(
        Guid customerGlobalId,
        CheckoutOrderDto dto);

    Task<GuestCheckoutResponseDto> GuestCheckoutAsync(GuestCheckoutDto dto);
}

public class NoopDistributedLockService : IDistributedLockService
{
    public Task<IAsyncDisposable> AcquireAsync(string key, TimeSpan? expiry = null)
    {
        return Task.FromResult<IAsyncDisposable>(new NoopHandle());
    }

    private class NoopHandle : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

public class NoopEmailQueue : IEmailQueue
{
    public void Enqueue(Func<CancellationToken, Task> job)
    {
        _ = job(CancellationToken.None);
    }
}

public class BookingService : IBookingService
{
    private readonly ITenantDbFactory _tenantDbFactory;
    private readonly MasterDbContext _masterDb;
    private readonly ICacheService _cache;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly BookingValidationSettings _validationSettings;
    private readonly IDistributedLockService _lockService;
    private readonly bool _skipBranchValidation;

    public BookingService(ITenantDbFactory tenantDbFactory, MasterDbContext masterDb)
        : this(
            tenantDbFactory,
            masterDb,
            new NoopCacheService(),
            new HttpContextAccessor(),
            Options.Create(new BookingValidationSettings()),
            new NoopDistributedLockService(),
            skipBranchValidation: true)
    {
    }

    public BookingService(
        ITenantDbFactory tenantDbFactory,
        MasterDbContext masterDb,
        ICacheService cache,
        IHttpContextAccessor httpContextAccessor,
        IOptions<BookingValidationSettings> validationSettings,
        IDistributedLockService lockService)
        : this(
            tenantDbFactory,
            masterDb,
            cache,
            httpContextAccessor,
            validationSettings,
            lockService,
            skipBranchValidation: false)
    {
    }

    private BookingService(
        ITenantDbFactory tenantDbFactory,
        MasterDbContext masterDb,
        ICacheService cache,
        IHttpContextAccessor httpContextAccessor,
        IOptions<BookingValidationSettings> validationSettings,
        IDistributedLockService lockService,
        bool skipBranchValidation)
    {
        _tenantDbFactory = tenantDbFactory;
        _masterDb = masterDb;
        _cache = cache;
        _httpContextAccessor = httpContextAccessor;
        _validationSettings = validationSettings.Value;
        _lockService = lockService;
        _skipBranchValidation = skipBranchValidation;
    }

    public Task<Booking> CreateBookingAsync(
        Guid roomTypeId,
        Guid ratePlanId,
        string customerName,
        string customerEmail,
        string customerPhone,
        DateTime checkIn,
        DateTime checkOut,
        int adult,
        int child,
        string? paymentMethod,
        string? notes,
        Guid? bookingGroupId = null)
    {
        return CreateBookingCoreAsync(
            null,
            roomTypeId,
            ratePlanId,
            customerName,
            customerEmail,
            customerPhone,
            checkIn,
            checkOut,
            adult,
            child,
            paymentMethod,
            notes,
            bookingGroupId);
    }

    public Task<Booking> CreateBookingForCustomerAsync(
        Guid customerGlobalId,
        Guid roomTypeId,
        Guid ratePlanId,
        DateTime checkIn,
        DateTime checkOut,
        int adult,
        int child,
        string? paymentMethod,
        string? notes,
        Guid? bookingGroupId = null)
    {
        return CreateBookingCoreAsync(
            customerGlobalId,
            roomTypeId,
            ratePlanId,
            string.Empty,
            string.Empty,
            string.Empty,
            checkIn,
            checkOut,
            adult,
            child,
            paymentMethod,
            notes,
            bookingGroupId);
    }

    public async Task<CheckoutOrderResponseDto> CheckoutFromOrderAsync(Guid customerGlobalId, CheckoutOrderDto dto)
    {
        if (dto.AdultCount < 1)
            throw new Exception("At least one adult guest is required");

        var globalCustomer = await _masterDb.CustomersGlobal
            .FirstOrDefaultAsync(c => c.Id == customerGlobalId)
            ?? throw new Exception("Customer not found");

        var branchCode = GetBranchCode();
        await using var db = await _tenantDbFactory.CreateAsync(branchCode);

        var customer = await db.Customers
            .FirstOrDefaultAsync(c => c.GlobalCustomerId == customerGlobalId)
            ?? throw new Exception("Customer not found in this branch");

        var draft = await db.OrderDrafts
            .Include(o => o.Items).ThenInclude(i => i.RatePlan)
            .Include(o => o.Items).ThenInclude(i => i.RoomType)
            .FirstOrDefaultAsync(o => o.CustomerId == customer.Id && o.Status == "draft");

        if (draft == null || draft.Items.Count == 0)
            throw new Exception("Order draft is empty");

        var globalLockKey = $"checkout:{customer.Id}:{draft.Id}";
        await using var lockHandle = await _lockService.AcquireAsync(globalLockKey, TimeSpan.FromSeconds(20));

        using var masterTx = await _masterDb.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await using var tenantTx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            var resultItems = new List<CheckoutBookingItemDto>();
            var holdUntilUtc = DateTime.UtcNow.AddMinutes(_validationSettings.PendingHoldMinutes);
            var bookingGroup = new BookingGroup
            {
                Id = Guid.NewGuid(),
                GroupCode = await GenerateBookingGroupCodeAsync(db),
                CustomerId = customer.Id,
                Status = "pending",
                HoldUntilUtc = holdUntilUtc,
                CreatedAt = DateTime.UtcNow
            };
            db.BookingGroups.Add(bookingGroup);

            foreach (var item in draft.Items.OrderBy(i => i.CreatedAt))
            {
                var checkInDate = DateTime.SpecifyKind(item.CheckIn.Date, DateTimeKind.Utc);
                var checkOutDate = DateTime.SpecifyKind(item.CheckOut.Date, DateTimeKind.Utc);
                var nights = (checkOutDate - checkInDate).Days;

                if (nights <= 0)
                    throw new InvalidOperationException("Invalid date range");

                for (var i = 0; i < item.TotalRooms; i++)
                {
                    await EnsureRoomTypeInventoryAsync(db, item.RoomTypeId, checkInDate, checkOutDate, 1);

                    var basePrice = item.PricePerNight * nights;
                    var tax = Math.Round(basePrice * 0.11m, 2);
                    var total = basePrice + tax;

                    var booking = new Booking
                    {
                        Id = Guid.NewGuid(),
                        CustomerId = customer.Id,
                        BookingGroupId = bookingGroup.Id,
                        RoomTypeId = item.RoomTypeId,
                        RoomId = null,
                        CheckIn = checkInDate,
                        CheckOut = checkOutDate,
                        AdultCount = dto.AdultCount,
                        ChildCount = dto.ChildCount,
                        BasePrice = basePrice,
                        Tax = tax,
                        TotalPrice = total,
                        BookingCode = await GenerateBookingCodeAsync(db),
                        Status = "pending",
                        PaymentMethod = dto.PaymentMethod,
                        PaymentStatus = "pending",
                        HoldUntilUtc = holdUntilUtc,
                        Notes = dto.Notes,
                        CreatedAt = DateTime.UtcNow
                    };

                    db.Bookings.Add(booking);

                    resultItems.Add(new CheckoutBookingItemDto
                    {
                        BookingId = booking.Id,
                        BookingCode = booking.BookingCode,
                        RoomTypeId = item.RoomTypeId,
                        RoomTypeName = item.RoomType?.Name ?? string.Empty,
                        RatePlanName = item.RatePlan?.Name ?? string.Empty,
                        CheckIn = checkInDate,
                        CheckOut = checkOutDate,
                        TotalPrice = total
                    });
                }
            }

            draft.Status = "checked_out";
            draft.UpdatedAt = DateTime.UtcNow;
            bookingGroup.TotalAmount = resultItems.Sum(x => x.TotalPrice);

            await db.SaveChangesAsync();
            await tenantTx.CommitAsync();
            await masterTx.CommitAsync();
            await InvalidateBookingRelatedCachesAsync(branchCode);

            return new CheckoutOrderResponseDto
            {
                Message = "Success",
                BookingGroupCode = bookingGroup.GroupCode,
                OrderDraftId = draft.Id,
                GrandTotal = resultItems.Sum(x => x.TotalPrice),
                Bookings = resultItems
            };
        }
        catch
        {
            await tenantTx.RollbackAsync();
            await masterTx.RollbackAsync();
            throw;
        }
    }

    public async Task<GuestCheckoutResponseDto> GuestCheckoutAsync(GuestCheckoutDto dto)
    {
        if (dto.AdultCount < 1)
            throw new Exception("At least one adult guest is required");

        if (dto.Items == null || dto.Items.Count == 0)
            throw new Exception("Checkout items are required");

        var normalizedEmail = dto.CustomerEmail.Trim().ToLowerInvariant();
        var normalizedName = dto.CustomerName.Trim();
        var normalizedPhone = dto.CustomerPhone.Trim();

        if (string.IsNullOrWhiteSpace(normalizedName))
            throw new Exception("Customer name is required");

        if (string.IsNullOrWhiteSpace(normalizedEmail))
            throw new Exception("Customer email is required");

        var branchCode = GetBranchCode();
        await using var db = await _tenantDbFactory.CreateAsync(branchCode);

        await using var masterTx = await _masterDb.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await using var tenantTx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var lockKey = $"guest-checkout:{branchCode}:{normalizedEmail}";
        await using var lockHandle = await _lockService.AcquireAsync(lockKey, TimeSpan.FromSeconds(30));

        try
        {
            var globalCustomer = await _masterDb.CustomersGlobal
                .FirstOrDefaultAsync(c => c.Email == normalizedEmail);

            if (globalCustomer == null)
            {
                globalCustomer = new CustomerGlobal
                {
                    Id = Guid.NewGuid(),
                    Name = normalizedName,
                    Email = normalizedEmail,
                    Phone = normalizedPhone,
                    CreatedAt = DateTime.UtcNow
                };
                _masterDb.CustomersGlobal.Add(globalCustomer);
                await _masterDb.SaveChangesAsync();
            }

            var customer = await db.Customers
                .FirstOrDefaultAsync(c => c.GlobalCustomerId == globalCustomer.Id);

            if (customer == null)
            {
                customer = new Customer
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
            }

            var holdUntilUtc = DateTime.UtcNow.AddMinutes(_validationSettings.PendingHoldMinutes);
            var bookingGroup = new BookingGroup
            {
                Id = Guid.NewGuid(),
                GroupCode = await GenerateBookingGroupCodeAsync(db),
                CustomerId = customer.Id,
                Status = "pending",
                HoldUntilUtc = holdUntilUtc,
                CreatedAt = DateTime.UtcNow
            };
            db.BookingGroups.Add(bookingGroup);

            var resultItems = new List<CheckoutBookingItemDto>();

            foreach (var item in dto.Items)
            {
                if (item.TotalRooms < 1)
                    throw new Exception("Total rooms must be at least 1");

                for (var i = 0; i < item.TotalRooms; i++)
                {
                    var booking = await CreateBookingCoreAsync(
                        authenticatedCustomerGlobalId: globalCustomer.Id,
                        roomTypeId: item.RoomTypeId,
                        ratePlanId: item.RatePlanId,
                        customerName: globalCustomer.Name,
                        customerEmail: globalCustomer.Email,
                        customerPhone: globalCustomer.Phone,
                        checkIn: item.CheckIn,
                        checkOut: item.CheckOut,
                        adult: dto.AdultCount,
                        child: dto.ChildCount,
                        paymentMethod: dto.PaymentMethod,
                        notes: dto.Notes,
                        bookingGroupId: bookingGroup.Id,
                        existingDb: db,
                        existingMasterTransaction: masterTx,
                        existingTenantTransaction: tenantTx,
                        skipCommit: true);

                    resultItems.Add(new CheckoutBookingItemDto
                    {
                        BookingId = booking.Id,
                        BookingCode = booking.BookingCode,
                        RoomId = booking.RoomId,
                        RoomNumber = booking.Room?.RoomNumber,
                        RoomTypeId = booking.RoomTypeId,
                        RoomTypeName = booking.RoomType?.Name ?? string.Empty,
                        RatePlanName = string.Empty,
                        CheckIn = booking.CheckIn,
                        CheckOut = booking.CheckOut,
                        TotalPrice = booking.TotalPrice
                    });
                }
            }

            bookingGroup.TotalAmount = resultItems.Sum(x => x.TotalPrice);
            await db.SaveChangesAsync();
            await tenantTx.CommitAsync();
            await masterTx.CommitAsync();
            await InvalidateBookingRelatedCachesAsync(branchCode);

            return new GuestCheckoutResponseDto
            {
                Message = "Guest checkout success",
                BookingGroupCode = bookingGroup.GroupCode,
                GrandTotal = bookingGroup.TotalAmount,
                Bookings = resultItems
            };
        }
        catch
        {
            await tenantTx.RollbackAsync();
            await masterTx.RollbackAsync();
            throw;
        }
    }

    private async Task<Booking> CreateBookingCoreAsync(
        Guid? authenticatedCustomerGlobalId,
        Guid roomTypeId,
        Guid ratePlanId,
        string customerName,
        string customerEmail,
        string customerPhone,
        DateTime checkIn,
        DateTime checkOut,
        int adult,
        int child,
        string? paymentMethod,
        string? notes,
        Guid? bookingGroupId,
        AppDbContext? existingDb = null,
        IDbContextTransaction? existingMasterTransaction = null,
        IDbContextTransaction? existingTenantTransaction = null,
        bool skipCommit = false)
    {
        var checkInDate = DateTime.SpecifyKind(checkIn.Date, DateTimeKind.Utc);
        var checkOutDate = DateTime.SpecifyKind(checkOut.Date, DateTimeKind.Utc);

        var isAuthenticated = authenticatedCustomerGlobalId.HasValue;
        ValidateBookingRequest(checkInDate, checkOutDate, customerEmail, adult, child, isAuthenticated);

        var normalizedEmail = customerEmail.Trim().ToLowerInvariant();
        var normalizedName = customerName.Trim();
        var normalizedPhone = customerPhone.Trim();

        CustomerGlobal? globalCustomer = null;
        if (authenticatedCustomerGlobalId.HasValue)
        {
            globalCustomer = await _masterDb.CustomersGlobal
                .FirstOrDefaultAsync(c => c.Id == authenticatedCustomerGlobalId.Value);

            if (globalCustomer == null)
                throw new Exception("Customer not found");

            normalizedEmail = globalCustomer.Email;
            normalizedName = globalCustomer.Name;
            normalizedPhone = globalCustomer.Phone;
        }
        else if (!string.IsNullOrWhiteSpace(normalizedEmail))
        {
            globalCustomer = await _masterDb.CustomersGlobal
                .FirstOrDefaultAsync(c => c.Email == normalizedEmail);
        }

        var branchCode = _skipBranchValidation ? "TEST" : GetBranchCode();
        if (!_skipBranchValidation)
        {
            var branch = await _masterDb.Branches
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Code == branchCode && b.IsActive);

            if (branch == null)
                throw new Exception("Branch not found");
        }

        var db = existingDb ?? await _tenantDbFactory.CreateAsync(branchCode);

        var masterTransaction = existingMasterTransaction
            ?? await _masterDb.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);

        var tenantTransaction = existingTenantTransaction
            ?? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            if (globalCustomer == null)
            {
                globalCustomer = new CustomerGlobal
                {
                    Id = Guid.NewGuid(),
                    Name = normalizedName,
                    Email = normalizedEmail,
                    Phone = normalizedPhone,
                    CreatedAt = DateTime.UtcNow
                };

                _masterDb.CustomersGlobal.Add(globalCustomer);
                await _masterDb.SaveChangesAsync();
            }

            var customer = await db.Customers
                .FirstOrDefaultAsync(c => c.GlobalCustomerId == globalCustomer.Id);

            if (customer == null)
            {
                customer = new Customer
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
            }

            var roomType = await db.RoomTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(rt => rt.Id == roomTypeId);

            if (roomType == null)
                throw new Exception("Room type not found");

            if (adult > roomType.MaxAdults || child > roomType.MaxChildren)
                throw new Exception("Guest count exceeds room capacity");

            var ratePlan = await db.RatePlans
                .AsNoTracking()
                .FirstOrDefaultAsync(rp =>
                    rp.Id == ratePlanId &&
                    rp.RoomTypeId == roomTypeId &&
                    rp.IsActive);

            if (ratePlan == null)
                throw new Exception("Rate plan not found");

            var lockKey = $"booking:{branchCode}:{roomTypeId}:{checkInDate:yyyyMMdd}:{checkOutDate:yyyyMMdd}";
            await using var lockHandle = await _lockService.AcquireAsync(lockKey, TimeSpan.FromSeconds(10));

            await EnsureRoomTypeInventoryAsync(db, roomTypeId, checkInDate, checkOutDate, 1);

            var days = (checkOutDate - checkInDate).Days;
            var basePrice = ratePlan.Price * days;
            var tax = Math.Round(basePrice * 0.11m, 2);
            var totalPrice = basePrice + tax;
            var holdUntilUtc = DateTime.UtcNow.AddMinutes(_validationSettings.PendingHoldMinutes);

            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                CustomerId = customer.Id,
                BookingGroupId = bookingGroupId,
                RoomTypeId = roomTypeId,
                RoomId = null,
                CheckIn = checkInDate,
                CheckOut = checkOutDate,
                AdultCount = adult,
                ChildCount = child,
                BasePrice = basePrice,
                Tax = tax,
                TotalPrice = totalPrice,
                BookingCode = await GenerateBookingCodeAsync(db),
                Status = "pending",
                PaymentMethod = paymentMethod,
                PaymentStatus = "pending",
                HoldUntilUtc = holdUntilUtc,
                Notes = notes,
                CreatedAt = DateTime.UtcNow
            };

            db.Bookings.Add(booking);
            await db.SaveChangesAsync();

            if (!skipCommit)
            {
                await tenantTransaction.CommitAsync();
                await masterTransaction.CommitAsync();
                await InvalidateBookingRelatedCachesAsync(branchCode);
            }
            return booking;
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            if (existingTenantTransaction == null)
                await tenantTransaction.RollbackAsync();
            if (existingMasterTransaction == null)
                await masterTransaction.RollbackAsync();
            throw new Exception("Room not available");
        }
        catch (Exception ex) when (IsSerializationFailure(ex))
        {
            if (existingTenantTransaction == null)
                await tenantTransaction.RollbackAsync();
            if (existingMasterTransaction == null)
                await masterTransaction.RollbackAsync();
            throw new Exception("Room not available, please try another date");
        }
        catch
        {
            if (existingTenantTransaction == null)
                await tenantTransaction.RollbackAsync();
            if (existingMasterTransaction == null)
                await masterTransaction.RollbackAsync();
            throw;
        }
        finally
        {
            if (existingDb == null)
            {
                await db.DisposeAsync();
            }

            if (existingMasterTransaction == null)
            {
                await masterTransaction.DisposeAsync();
            }

            if (existingTenantTransaction == null)
            {
                await tenantTransaction.DisposeAsync();
            }
        }
    }

    private async Task EnsureRoomTypeInventoryAsync(
        AppDbContext db,
        Guid roomTypeId,
        DateTime checkInDate,
        DateTime checkOutDate,
        int requestedRooms)
    {
        var candidateRoomIds = await db.Rooms
            .AsNoTracking()
            .Where(r =>
                r.RoomTypeId == roomTypeId &&
                r.Status == "available" &&
                r.OperationalStatus == RoomOperationalStatuses.Clean &&
                !db.RoomAvailabilities.Any(a =>
                    a.RoomId == r.Id &&
                    a.Date >= checkInDate &&
                    a.Date < checkOutDate &&
                    !a.IsAvailable))
            .Select(r => r.Id)
            .ToListAsync();

        var totalSellableRooms = candidateRoomIds.Count;
        if (totalSellableRooms <= 0)
            throw new InvalidOperationException("Room not available");

        var activeBookingsCount = await db.Bookings
            .AsNoTracking()
            .CountAsync(b =>
                b.RoomTypeId == roomTypeId &&
                (
                    b.Status == "paid" ||
                    b.Status == "confirmed" ||
                    (b.Status == "pending" && b.HoldUntilUtc > DateTime.UtcNow)
                ) &&
                b.CheckIn < checkOutDate &&
                b.CheckOut > checkInDate);

        if (totalSellableRooms - activeBookingsCount < requestedRooms)
            throw new InvalidOperationException("Room not available");
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return FindPostgresException(exception)?.SqlState == PostgresErrorCodes.UniqueViolation;
    }

    private static bool IsSerializationFailure(Exception exception)
    {
        var postgresException = FindPostgresException(exception);
        return postgresException?.SqlState is
            PostgresErrorCodes.SerializationFailure or
            PostgresErrorCodes.DeadlockDetected;
    }

    private static PostgresException? FindPostgresException(Exception exception)
    {
        var current = exception;
        while (current != null)
        {
            if (current is PostgresException postgresException)
                return postgresException;

            current = current.InnerException!;
        }

        return null;
    }

    private async Task<string> GenerateBookingCodeAsync(AppDbContext db)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var code = $"BK-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(100000, 999999)}";
            var exists = await db.Bookings.AnyAsync(b => b.BookingCode == code);
            if (!exists)
                return code;
        }

        throw new Exception("Unable to generate unique booking code");
    }

    private async Task<string> GenerateBookingGroupCodeAsync(AppDbContext db)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var code = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(100000, 999999)}";
            var exists = await db.BookingGroups.AnyAsync(g => g.GroupCode == code);
            if (!exists)
                return code;
        }

        throw new Exception("Unable to generate unique booking group code");
    }

    private void ValidateBookingRequest(
        DateTime checkInDate,
        DateTime checkOutDate,
        string? customerEmail,
        int adult,
        int child,
        bool isAuthenticated)
    {
        var today = DateTime.UtcNow.Date;

        if (checkInDate < today)
            throw new Exception("Check-in cannot be in the past");

        if (checkOutDate <= checkInDate)
            throw new Exception("Invalid date range");

        if ((checkOutDate - checkInDate).Days > _validationSettings.MaxStayNights)
            throw new Exception($"Maximum stay duration is {_validationSettings.MaxStayNights} nights");

        if ((checkInDate - today).Days > _validationSettings.MaxAdvanceBookingDays)
            throw new Exception($"Check-in cannot be more than {_validationSettings.MaxAdvanceBookingDays} days ahead");

        if (!isAuthenticated && string.IsNullOrWhiteSpace(customerEmail))
            throw new Exception("Customer email is required");

        if (adult < 1)
            throw new Exception("At least one adult guest is required");

        if (child < 0)
            throw new Exception("Child guest count cannot be negative");
    }

    private string GetBranchCode()
    {
        var branchCode = _httpContextAccessor.HttpContext?.Request.Headers["X-Branch-Code"].ToString();
        if (string.IsNullOrWhiteSpace(branchCode))
            throw new Exception("X-Branch-Code header is missing");

        return branchCode.Trim().ToUpperInvariant();
    }

    private async Task InvalidateBookingRelatedCachesAsync(string branchCode)
    {
        await _cache.RemoveByPrefixAsync($"availability:{branchCode}:");
        await _cache.RemoveByPrefixAsync($"hotel:full:{branchCode}:");
    }
}
