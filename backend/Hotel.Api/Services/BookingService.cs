using Hotel.Api.Data;
using Hotel.Api.Entities.Master;
using Hotel.Api.Entities.Tenant;
using Microsoft.EntityFrameworkCore;
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
}



public class BookingService : IBookingService
{
    private readonly AppDbContext _db;
    private readonly MasterDbContext _masterDb;

    public BookingService(AppDbContext db, MasterDbContext masterDb)
    {
        _db = db;
        _masterDb = masterDb;
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
        var checkInDate = DateTime.SpecifyKind(checkIn.Date, DateTimeKind.Utc);
        var checkOutDate = DateTime.SpecifyKind(checkOut.Date, DateTimeKind.Utc);

        if (checkOutDate <= checkInDate)
            throw new Exception("Invalid date range");

        if (string.IsNullOrWhiteSpace(customerEmail))
            throw new Exception("Customer email is required");

        if (adult < 1)
            throw new Exception("At least one adult guest is required");

        var normalizedEmail = customerEmail.Trim().ToLowerInvariant();
        var normalizedName = customerName.Trim();
        var normalizedPhone = customerPhone.Trim();

        using var masterTransaction = await _masterDb.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        using var tenantTransaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

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
}
