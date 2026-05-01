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



public class BookingService : IBookingService
{
    private readonly AppDbContext _db;
    private readonly MasterDbContext _masterDb;
    private readonly ICacheService _cache;
    private readonly IBookingEmailService _bookingEmailService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly BookingValidationSettings _validationSettings;
    private readonly ILogger<BookingService> _logger;
    private readonly bool _skipBranchValidation;

    public BookingService(AppDbContext db, MasterDbContext masterDb)
        : this(
            db,
            masterDb,
            new NoopCacheService(),
            new NoopBookingEmailService(),
            new HttpContextAccessor(),
            Options.Create(new BookingValidationSettings()),
            NullLogger<BookingService>.Instance,
            skipBranchValidation: true)
    {
    }

    public BookingService(
        AppDbContext db,
        MasterDbContext masterDb,
        ICacheService cache,
        IBookingEmailService bookingEmailService,
        IHttpContextAccessor httpContextAccessor,
        IOptions<BookingValidationSettings> validationSettings,
        ILogger<BookingService> logger)
        : this(db, masterDb, cache, bookingEmailService, httpContextAccessor, validationSettings, logger, skipBranchValidation: false)
    {
    }

    private BookingService(
        AppDbContext db,
        MasterDbContext masterDb,
        ICacheService cache,
        IBookingEmailService bookingEmailService,
        IHttpContextAccessor httpContextAccessor,
        IOptions<BookingValidationSettings> validationSettings,
        ILogger<BookingService> logger,
        bool skipBranchValidation)
    {
        _db = db;
        _masterDb = masterDb;
        _cache = cache;
        _bookingEmailService = bookingEmailService;
        _httpContextAccessor = httpContextAccessor;
        _validationSettings = validationSettings.Value;
        _logger = logger;
        _skipBranchValidation = skipBranchValidation;
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
        if (dto.ChildCount < 0)
            throw new Exception("Child guest count cannot be negative");

        var globalCustomer = await _masterDb.CustomersGlobal
            .FirstOrDefaultAsync(c => c.Id == customerGlobalId);
        if (globalCustomer == null)
            throw new Exception("Customer not found");

        var customer = await _db.Customers
            .FirstOrDefaultAsync(c => c.GlobalCustomerId == customerGlobalId);
        if (customer == null)
            throw new Exception("Customer not found in this branch");

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

        var draft = await _db.OrderDrafts
            .Include(o => o.Items)
            .ThenInclude(i => i.RatePlan)
            .Include(o => o.Items)
            .ThenInclude(i => i.RoomType)
            .FirstOrDefaultAsync(o => o.CustomerId == customer.Id && o.Status == "draft");

        if (draft == null || draft.Items.Count == 0)
            throw new Exception("Order draft is empty");

        using var masterTransaction = await _masterDb.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        using var tenantTransaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            var resultItems = new List<CheckoutBookingItemDto>();

            foreach (var item in draft.Items.OrderBy(i => i.CreatedAt))
            {
                var checkInDate = DateTime.SpecifyKind(item.CheckIn.Date, DateTimeKind.Utc);
                var checkOutDate = DateTime.SpecifyKind(item.CheckOut.Date, DateTimeKind.Utc);

                ValidateBookingRequest(checkInDate, checkOutDate, globalCustomer.Email, dto.AdultCount, dto.ChildCount, isAuthenticated: true);

                var nights = (checkOutDate - checkInDate).Days;
                var dates = Enumerable.Range(0, nights)
                    .Select(offset => checkInDate.AddDays(offset))
                    .ToList();

                var candidateRooms = await _db.Rooms
                    .Where(r =>
                        r.RoomTypeId == item.RoomTypeId &&
                        r.Status == "available" &&
                        dto.AdultCount <= r.RoomType.MaxAdults &&
                        dto.ChildCount <= r.RoomType.MaxChildren)
                    .Select(r => new { r.Id, r.RoomNumber, RoomTypeName = r.RoomType.Name })
                    .ToListAsync();

                if (candidateRooms.Count < item.TotalRooms)
                    throw new Exception("Room not available");

                var candidateRoomIds = candidateRooms.Select(r => r.Id).ToList();
                var unavailableRoomIds = await _db.RoomAvailabilities
                    .Where(a => candidateRoomIds.Contains(a.RoomId) && dates.Contains(a.Date) && !a.IsAvailable)
                    .Select(a => a.RoomId)
                    .Distinct()
                    .ToListAsync();

                var selectedRooms = candidateRooms
                    .Where(r => !unavailableRoomIds.Contains(r.Id))
                    .Take(item.TotalRooms)
                    .ToList();

                if (selectedRooms.Count < item.TotalRooms)
                    throw new Exception("Room not available");

                foreach (var selectedRoom in selectedRooms)
                {
                    var basePrice = item.PricePerNight * nights;
                    var tax = Math.Round(basePrice * 0.11m, 2);
                    var totalPrice = basePrice + tax;

                    var booking = new Booking
                    {
                        Id = Guid.NewGuid(),
                        RoomId = selectedRoom.Id,
                        CustomerId = customer.Id,
                        CheckIn = checkInDate,
                        CheckOut = checkOutDate,
                        AdultCount = dto.AdultCount,
                        ChildCount = dto.ChildCount,
                        BasePrice = basePrice,
                        Tax = tax,
                        TotalPrice = totalPrice,
                        BookingCode = await GenerateBookingCodeAsync(),
                        Status = "pending",
                        PaymentMethod = dto.PaymentMethod ?? item.RatePlan?.PaymentType,
                        PaymentStatus = "pending",
                        Notes = dto.Notes,
                        CreatedAt = DateTime.UtcNow
                    };

                    _db.Bookings.Add(booking);

                    var existingAvailabilities = await _db.RoomAvailabilities
                        .Where(a => a.RoomId == selectedRoom.Id && dates.Contains(a.Date))
                        .ToDictionaryAsync(a => a.Date);

                    foreach (var date in dates)
                    {
                        if (existingAvailabilities.TryGetValue(date, out var availability))
                        {
                            availability.IsAvailable = false;
                        }
                        else
                        {
                            _db.RoomAvailabilities.Add(new RoomAvailability
                            {
                                Id = Guid.NewGuid(),
                                RoomId = selectedRoom.Id,
                                Date = date,
                                IsAvailable = false,
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                    }

                    resultItems.Add(new CheckoutBookingItemDto
                    {
                        BookingId = booking.Id,
                        BookingCode = booking.BookingCode,
                        RoomId = selectedRoom.Id,
                        RoomNumber = selectedRoom.RoomNumber,
                        RoomTypeName = selectedRoom.RoomTypeName,
                        RatePlanName = item.RatePlan?.Name ?? string.Empty,
                        CheckIn = checkInDate,
                        CheckOut = checkOutDate,
                        TotalPrice = totalPrice
                    });
                }
            }

            draft.Status = "checked_out";
            draft.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            await tenantTransaction.CommitAsync();
            await masterTransaction.CommitAsync();
            await InvalidateBookingRelatedCachesAsync(branchCode);

            var bookingIds = resultItems.Select(i => i.BookingId).ToList();
            var loadedBookings = await _db.Bookings
                .AsNoTracking()
                .Include(b => b.Customer)
                .Include(b => b.Room)
                .ThenInclude(r => r.RoomType)
                .Where(b => bookingIds.Contains(b.Id))
                .ToListAsync();

            foreach (var loadedBooking in loadedBookings)
            {
                try
                {
                    await _bookingEmailService.SendBookingCreatedAsync(loadedBooking, branchName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Booking email failed after checkout from order. BookingId={BookingId}", loadedBooking.Id);
                }
            }

            return new CheckoutOrderResponseDto
            {
                Message = "Order checkout success",
                OrderDraftId = draft.Id,
                GrandTotal = resultItems.Sum(x => x.TotalPrice),
                Bookings = resultItems
            };
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

        using var masterTransaction = await _masterDb.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        using var tenantTransaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

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

            var customer = await _db.Customers
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

                _db.Customers.Add(customer);
            }

            var dates = Enumerable.Range(0, (checkOutDate - checkInDate).Days)
                .Select(offset => checkInDate.AddDays(offset))
                .ToList();

            var unavailable = await _db.RoomAvailabilities
                .Where(a => a.RoomId == roomId && dates.Contains(a.Date) && !a.IsAvailable)
                .AnyAsync();

            if (unavailable)
                throw new Exception("Room not available");

            var room = await _db.Rooms
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

            _db.Bookings.Add(booking);

            var existingAvailabilities = await _db.RoomAvailabilities
                .Where(a => a.RoomId == roomId && dates.Contains(a.Date))
                .ToDictionaryAsync(a => a.Date);

            foreach (var date in dates)
            {
                if (existingAvailabilities.TryGetValue(date, out var availability))
                {
                    availability.IsAvailable = false;
                }
                else
                {
                    _db.RoomAvailabilities.Add(new RoomAvailability
                    {
                        Id = Guid.NewGuid(),
                        RoomId = roomId,
                        Date = date,
                        IsAvailable = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await _db.SaveChangesAsync();

            await tenantTransaction.CommitAsync();
            await masterTransaction.CommitAsync();
            await InvalidateBookingRelatedCachesAsync(branchCode);

            var loadedBooking = await _db.Bookings
                .AsNoTracking()
                .Include(b => b.Customer)
                .Include(b => b.Room)
                .ThenInclude(r => r.RoomType)
                .FirstOrDefaultAsync(b => b.Id == booking.Id);

            if (loadedBooking != null)
            {
                try
                {
                    await _bookingEmailService.SendBookingCreatedAsync(loadedBooking, branchName);
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
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var code = $"BK-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(100000, 999999)}";
            var exists = await _db.Bookings.AnyAsync(b => b.BookingCode == code);

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
