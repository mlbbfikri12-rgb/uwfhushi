using Hotel.Api.Configurations;
using Hotel.Api.Data;
using Hotel.Api.DTOs;
using Hotel.Api.Entities.Master;
using Hotel.Api.Entities.Tenant;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;
using System.Data;

namespace Hotel.Api.Services;

public interface IBookingService
{
    Task<Booking> CreateBookingAsync(
        Guid roomId,
        string customerName,
        string customerEmail,
        string customerPhone,
        DateTime checkIn,
        DateTime checkOut,
        int adult,
        int child,
        string? paymentMethod,
        string? notes);

    Task<Booking> CreateBookingForCustomerAsync(
        Guid customerGlobalId,
        Guid roomId,
        DateTime checkIn,
        DateTime checkOut,
        int adult,
        int child,
        string? paymentMethod,
        string? notes);

    Task<CheckoutOrderResponseDto> CheckoutFromOrderAsync(
        Guid customerGlobalId,
        CheckoutOrderDto dto);
}


public class NoopDistributedLockService : IDistributedLockService
{
    public Task<IAsyncDisposable> AcquireAsync(string key, TimeSpan? expiry = null)
    {
        return Task.FromResult<IAsyncDisposable>(new NoopHandle());
    }

    private class NoopHandle : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            // do nothing
            return ValueTask.CompletedTask;
        }
    }
}

public class NoopEmailQueue : IEmailQueue
{
    public void Enqueue(Func<CancellationToken, Task> job)
    {
        // langsung execute (biar test tetap jalan)
        _ = job(CancellationToken.None);
    }
}



public class BookingService : IBookingService
{
    private readonly ITenantDbFactory _tenantDbFactory;
    private readonly MasterDbContext _masterDb;
    private readonly ICacheService _cache;
    private readonly IBookingEmailService _bookingEmailService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly BookingValidationSettings _validationSettings;
    private readonly ILogger<BookingService> _logger;
    private readonly IEmailQueue _emailQueue;

    private readonly IDistributedLockService _lockService;
    private readonly bool _skipBranchValidation;

    // 🔹 1. TEST / FALLBACK CONSTRUCTOR
    public BookingService(ITenantDbFactory tenantDbFactory, MasterDbContext masterDb)
        : this(
            tenantDbFactory,
            masterDb,
            new NoopCacheService(),
            new NoopBookingEmailService(),
            new HttpContextAccessor(),
            Options.Create(new BookingValidationSettings()),
            NullLogger<BookingService>.Instance,
            new NoopDistributedLockService(),
            new NoopEmailQueue(),
            skipBranchValidation: true)
    {
    }

    // 🔹 2. PRODUCTION CONSTRUCTOR (DI)
    public BookingService(
        ITenantDbFactory tenantDbFactory,
        MasterDbContext masterDb,
        ICacheService cache,
        IBookingEmailService bookingEmailService,
        IHttpContextAccessor httpContextAccessor,
        IOptions<BookingValidationSettings> validationSettings,
        ILogger<BookingService> logger,
        IDistributedLockService lockService,
        IEmailQueue emailQueue) // ✅ TAMBAH
        : this(
            tenantDbFactory,
            masterDb,
            cache,
            bookingEmailService,
            httpContextAccessor,
            validationSettings,
            logger,
            lockService,
            emailQueue,
            skipBranchValidation: false)
    {
    }

    // 🔹 3. INTERNAL CONSTRUCTOR (SOURCE OF TRUTH)
    private BookingService(
        ITenantDbFactory tenantDbFactory,
        MasterDbContext masterDb,
        ICacheService cache,
        IBookingEmailService bookingEmailService,
        IHttpContextAccessor httpContextAccessor,
        IOptions<BookingValidationSettings> validationSettings,
        ILogger<BookingService> logger,
        IDistributedLockService lockService,
        IEmailQueue emailQueue, // ✅ WAJIB ADA DI SINI
        bool skipBranchValidation)
    {
        _tenantDbFactory = tenantDbFactory;
        _masterDb = masterDb;
        _cache = cache;
        _bookingEmailService = bookingEmailService;
        _httpContextAccessor = httpContextAccessor;
        _validationSettings = validationSettings.Value;
        _logger = logger;
        _lockService = lockService ?? throw new ArgumentNullException(nameof(lockService));
        _emailQueue = emailQueue ?? throw new ArgumentNullException(nameof(emailQueue));
        _skipBranchValidation = skipBranchValidation;
    }

    private async Task<AppDbContext> CreateDbAsync(CancellationToken ct = default)
    {
        var branchCode = GetBranchCode();
        return await _tenantDbFactory.CreateAsync(branchCode, ct);
    }
    public async Task<Booking> CreateBookingAsync(
        Guid roomId,
        string customerName,
        string customerEmail,
        string customerPhone,
        DateTime checkIn,
        DateTime checkOut,
        int adult,
        int child,
        string? paymentMethod,
        string? notes)
    {
        return await CreateBookingCoreAsync(
            null,
            roomId,
            customerName,
            customerEmail,
            customerPhone,
            checkIn,
            checkOut,
            adult,
            child,
            paymentMethod,
            notes);
    }

    public async Task<Booking> CreateBookingForCustomerAsync(
        Guid customerGlobalId,
        Guid roomId,
        DateTime checkIn,
        DateTime checkOut,
        int adult,
        int child,
        string? paymentMethod,
        string? notes)
    {
        return await CreateBookingCoreAsync(
            customerGlobalId,
            roomId,
            string.Empty,
            string.Empty,
            string.Empty,
            checkIn,
            checkOut,
            adult,
            child,
            paymentMethod,
            notes);
    }

    public async Task<CheckoutOrderResponseDto> CheckoutFromOrderAsync(
        Guid customerGlobalId,
        CheckoutOrderDto dto)
    {
        if (dto.AdultCount < 1)
            throw new Exception("At least one adult guest is required");

        var globalCustomer = await _masterDb.CustomersGlobal
            .FirstOrDefaultAsync(c => c.Id == customerGlobalId)
            ?? throw new Exception("Customer not found");

        await using var db = await CreateDbAsync();

        var customer = await db.Customers
            .FirstOrDefaultAsync(c => c.GlobalCustomerId == customerGlobalId)
            ?? throw new Exception("Customer not found in this branch");

        var draft = await db.OrderDrafts
            .Include(o => o.Items).ThenInclude(i => i.RatePlan)
            .Include(o => o.Items).ThenInclude(i => i.RoomType)
            .FirstOrDefaultAsync(o => o.CustomerId == customer.Id && o.Status == "draft");

        if (draft == null || draft.Items.Count == 0)
            throw new Exception("Order draft is empty");

        // 🔥 GLOBAL LOCK (FIX UTAMA)
        var globalLockKey = $"checkout:{customer.Id}:{draft.Id}";
        await using var handle = await _lockService.AcquireAsync(globalLockKey, TimeSpan.FromSeconds(10));

        using var masterTx = await _masterDb.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        using var tenantTx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            var resultItems = new List<CheckoutBookingItemDto>();

            var roomTypeIds = draft.Items.Select(i => i.RoomTypeId).Distinct().ToList();

            var allRooms = await db.Rooms
                .Where(r => roomTypeIds.Contains(r.RoomTypeId) && r.Status == "available")
                .Select(r => new
                {
                    r.Id,
                    r.RoomTypeId,
                    r.RoomNumber,
                    RoomTypeName = r.RoomType.Name,
                    r.RoomType.MaxAdults,
                    r.RoomType.MaxChildren
                })
                .ToListAsync();

            var roomsByType = allRooms
                .GroupBy(r => r.RoomTypeId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var allRoomIds = allRooms.Select(r => r.Id).ToList();

            var allDates = draft.Items
                .SelectMany(i =>
                    Enumerable.Range(0, (i.CheckOut.Date - i.CheckIn.Date).Days)
                        .Select(d => i.CheckIn.Date.AddDays(d)))
                .Distinct()
                .ToList();

            var allAvailabilities = await db.RoomAvailabilities
                .Where(a => allRoomIds.Contains(a.RoomId) && allDates.Contains(a.Date))
                .ToListAsync();

            var availabilityLookup = allAvailabilities
                .GroupBy(a => a.RoomId)
                .ToDictionary(g => g.Key, g => g.ToDictionary(x => x.Date));

            foreach (var item in draft.Items.OrderBy(i => i.CreatedAt))
            {
                var checkInDate = item.CheckIn.Date;
                var checkOutDate = item.CheckOut.Date;

                var nights = (checkOutDate - checkInDate).Days;

                var dates = Enumerable.Range(0, nights)
                    .Select(offset => checkInDate.AddDays(offset))
                    .ToList();

                if (!roomsByType.TryGetValue(item.RoomTypeId, out var candidateRooms))
                    throw new InvalidOperationException("Room not available");

                candidateRooms = candidateRooms
                    .Where(r => dto.AdultCount <= r.MaxAdults && dto.ChildCount <= r.MaxChildren)
                    .ToList();

                if (candidateRooms.Count < item.TotalRooms)
                    throw new InvalidOperationException("Room not available");

                var unavailableRoomIds = new HashSet<Guid>();

                foreach (var room in candidateRooms)
                {
                    if (!availabilityLookup.TryGetValue(room.Id, out var roomDates))
                        continue;

                    if (dates.Any(d => roomDates.TryGetValue(d, out var a) && !a.IsAvailable))
                    {
                        unavailableRoomIds.Add(room.Id);
                    }
                }

                var selectedRooms = candidateRooms
                    .Where(r => !unavailableRoomIds.Contains(r.Id))
                    .Take(item.TotalRooms)
                    .ToList();

                if (selectedRooms.Count < item.TotalRooms)
                    throw new InvalidOperationException("Room not available");

                foreach (var room in selectedRooms)
                {
                    var basePrice = item.PricePerNight * nights;
                    var tax = Math.Round(basePrice * 0.11m, 2);
                    var total = basePrice + tax;

                    var booking = new Booking
                    {
                        Id = Guid.NewGuid(),
                        RoomId = room.Id,
                        CustomerId = customer.Id,
                        CheckIn = checkInDate,
                        CheckOut = checkOutDate,
                        AdultCount = dto.AdultCount,
                        ChildCount = dto.ChildCount,
                        BasePrice = basePrice,
                        Tax = tax,
                        TotalPrice = total,
                        BookingCode = await GenerateBookingCodeAsync(),
                        Status = "pending",
                        PaymentStatus = "pending",
                        CreatedAt = DateTime.UtcNow
                    };

                    db.Bookings.Add(booking);

                    if (!availabilityLookup.ContainsKey(room.Id))
                        availabilityLookup[room.Id] = new();

                    foreach (var date in dates)
                    {
                        availabilityLookup[room.Id][date] = new RoomAvailability
                        {
                            Id = Guid.NewGuid(),
                            RoomId = room.Id,
                            Date = date,
                            IsAvailable = false,
                            CreatedAt = DateTime.UtcNow
                        };

                        db.RoomAvailabilities.Add(availabilityLookup[room.Id][date]);
                    }

                    resultItems.Add(new CheckoutBookingItemDto
                    {
                        BookingId = booking.Id,
                        RoomId = room.Id,
                        RoomNumber = room.RoomNumber,
                        RoomTypeName = room.RoomTypeName,
                        CheckIn = checkInDate,
                        CheckOut = checkOutDate,
                        TotalPrice = total
                    });
                }
            }

            draft.Status = "checked_out";

            await db.SaveChangesAsync();
            await tenantTx.CommitAsync();
            await masterTx.CommitAsync();

            return new CheckoutOrderResponseDto
            {
                Message = "Success",
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
    private async Task<Booking> CreateBookingCoreAsync(
    Guid? authenticatedCustomerGlobalId,
    Guid roomId,
    string customerName,
    string customerEmail,
    string customerPhone,
    DateTime checkIn,
    DateTime checkOut,
    int adult,
    int child,
    string? paymentMethod,
    string? notes)
    {
        var checkInDate = DateTime.SpecifyKind(checkIn.Date, DateTimeKind.Utc);
        var checkOutDate = DateTime.SpecifyKind(checkOut.Date, DateTimeKind.Utc);

        var isAuthenticated = authenticatedCustomerGlobalId.HasValue;

        ValidateBookingRequest(
            checkInDate,
            checkOutDate,
            customerEmail,
            adult,
            child,
            isAuthenticated);

        string normalizedEmail;
        string normalizedName;
        string normalizedPhone;

        CustomerGlobal? globalCustomer = null;

        // 🔥 FIX: ambil customer hanya sekali
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
        else
        {
            normalizedEmail = customerEmail.Trim().ToLowerInvariant();
            normalizedName = customerName.Trim();
            normalizedPhone = customerPhone.Trim();

            globalCustomer = await _masterDb.CustomersGlobal
                .FirstOrDefaultAsync(c => c.Email == normalizedEmail);
        }

        var branchCode = _skipBranchValidation ? "TEST" : GetBranchCode();
        var branchName = "Test Hotel";

        if (!_skipBranchValidation)
        {
            var branch = await _masterDb.Branches
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Code == branchCode && b.IsActive);

            if (branch == null)
                throw new Exception("Branch not found");

            branchName = branch.Name;
        }

        await using var db = await CreateDbAsync();

        using var masterTransaction = await _masterDb.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        using var tenantTransaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            // 🔥 hanya create jika benar-benar belum ada
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

            // 🔥 LOCK RANGE (INDEX FRIENDLY)
            await LockRoomAvailabilityAsync(db, roomId, checkInDate, checkOutDate);

            // 🔥 CEK AVAILABILITY (NO CONTAINS)
            var unavailable = await db.RoomAvailabilities
                .Where(a =>
                    a.RoomId == roomId &&
                    a.Date >= checkInDate &&
                    a.Date < checkOutDate &&
                    !a.IsAvailable)
                .AnyAsync();

            if (unavailable)
                throw new InvalidOperationException("Room not available");

            var room = await db.Rooms
                .Include(r => r.RoomType)
                .FirstOrDefaultAsync(r => r.Id == roomId);

            if (room == null)
                throw new Exception("Room not found in this branch");

            if (room.Status != "available")
                throw new Exception("Room is not available for booking");

            if (adult > room.RoomType.MaxAdults || child > room.RoomType.MaxChildren)
                throw new Exception("Guest count exceeds room capacity");

            var days = (checkOutDate - checkInDate).Days;
            var basePrice = room.RoomType.BasePrice * days;
            var tax = Math.Round(basePrice * 0.11m, 2);
            var totalPrice = basePrice + tax;

            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                RoomId = roomId,
                CustomerId = customer.Id,
                CheckIn = checkInDate,
                CheckOut = checkOutDate,
                AdultCount = adult,
                ChildCount = child,
                BasePrice = basePrice,
                Tax = tax,
                TotalPrice = totalPrice,
                BookingCode = await GenerateBookingCodeAsync(),
                Status = "pending",
                PaymentMethod = paymentMethod,
                PaymentStatus = "pending",
                Notes = notes,
                CreatedAt = DateTime.UtcNow
            };

            db.Bookings.Add(booking);

            // 🔥 tetap butuh ini untuk loop insert/update
            var dates = Enumerable.Range(0, (checkOutDate - checkInDate).Days)
    .Select(offset => checkInDate.AddDays(offset))
    .ToList();

            // 🔥 ambil existing availability (range-based, index friendly)
            var existingAvailabilities = await db.RoomAvailabilities
                .Where(a =>
                    a.RoomId == roomId &&
                    a.Date >= checkInDate &&
                    a.Date < checkOutDate)
                .ToDictionaryAsync(a => a.Date);

            // 🔥 update / insert
            foreach (var date in dates)
            {
                if (existingAvailabilities.TryGetValue(date, out var availability))
                {
                    availability.IsAvailable = false;
                }
                else
                {
                    db.RoomAvailabilities.Add(new RoomAvailability
                    {
                        Id = Guid.NewGuid(),
                        RoomId = roomId,
                        Date = date,
                        IsAvailable = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }



            await db.SaveChangesAsync();

            await tenantTransaction.CommitAsync();
            await masterTransaction.CommitAsync();
            await InvalidateBookingRelatedCachesAsync(branchCode);

            var loadedBooking = await db.Bookings
                .AsNoTracking()
                .Include(b => b.Customer)
                .Include(b => b.Room)
                .ThenInclude(r => r.RoomType)
                .FirstOrDefaultAsync(b => b.Id == booking.Id);

            if (loadedBooking != null)
            {
                try
                {
                    _emailQueue.Enqueue(ct =>
    _bookingEmailService.SendBookingCreatedAsync(loadedBooking, branchName, ct));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Booking email failed after booking was created. BookingId={BookingId}", booking.Id);
                }
            }

            return booking;
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            await tenantTransaction.RollbackAsync();
            await masterTransaction.RollbackAsync();
            throw new Exception("Room not available");
        }
        catch (Exception ex) when (IsSerializationFailure(ex))
        {
            await tenantTransaction.RollbackAsync();
            await masterTransaction.RollbackAsync();
            throw new Exception("Room not available, please try another date");
        }
        catch (InvalidOperationException ex) when (IsTransientFailure(ex))
        {
            await tenantTransaction.RollbackAsync();
            await masterTransaction.RollbackAsync();
            throw new Exception("Room not available, please try another date");
        }
        catch
        {
            await tenantTransaction.RollbackAsync();
            await masterTransaction.RollbackAsync();
            throw;
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return FindPostgresException(exception)?.SqlState == PostgresErrorCodes.UniqueViolation;
    }


    private async Task LockRoomAvailabilityAsync(
        AppDbContext db,
        Guid roomId,
        DateTime checkInDate,
        DateTime checkOutDate)
    {
        await db.Database.ExecuteSqlRawAsync(@"
        SELECT 1
        FROM ""RoomAvailabilities""
        WHERE ""RoomId"" = {0}
        AND ""Date"" >= {1}
        AND ""Date"" < {2}
        FOR UPDATE SKIP LOCKED
    ", roomId, checkInDate, checkOutDate);
    }
    private static bool IsSerializationFailure(Exception exception)
    {
        var postgresException = FindPostgresException(exception);

        return postgresException?.SqlState is
            PostgresErrorCodes.SerializationFailure or
            PostgresErrorCodes.DeadlockDetected;
    }

    private static bool IsTransientFailure(Exception exception)
    {
        return exception.Message.Contains("transient failure", StringComparison.OrdinalIgnoreCase);
    }

    private static PostgresException? FindPostgresException(Exception exception)
    {
        var current = exception;
        while (current != null)
        {
            if (current is PostgresException postgresException)
                return postgresException;

            current = current.InnerException;
        }

        return null;
    }

    private async Task<string> GenerateBookingCodeAsync()
    {
        await using var db = await CreateDbAsync();
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var code = $"BK-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(100000, 999999)}";
            var exists = await db.Bookings.AnyAsync(b => b.BookingCode == code);

            if (!exists)
                return code;
        }

        throw new Exception("Unable to generate unique booking code");
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
