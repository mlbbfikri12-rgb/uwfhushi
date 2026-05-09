using Hotel.Api.Data;
using Hotel.Api.Entities.Tenant;
using Microsoft.EntityFrameworkCore;

namespace Hotel.Api.Services;

public interface IRoomAssignmentService
{
    Task AssignRoomAfterPaymentAsync(AppDbContext db, Booking booking, CancellationToken ct = default);
    Task AssignRoomToBookingAsync(AppDbContext db, Guid bookingId, Guid roomId, CancellationToken ct = default);
    Task AutoAssignRoomsAsync(AppDbContext db, DateTime dateUtc, CancellationToken ct = default);
}

public class RoomAssignmentService : IRoomAssignmentService
{
    public async Task AssignRoomAfterPaymentAsync(AppDbContext db, Booking booking, CancellationToken ct = default)
    {
        if (booking.RoomId.HasValue)
            return;

        var checkInDate = booking.CheckIn.Date;
        var checkOutDate = booking.CheckOut.Date;

        var candidateRooms = await db.Rooms
            .Where(r =>
                r.RoomTypeId == booking.RoomTypeId &&
                r.Status == "available" &&
                r.OperationalStatus == RoomOperationalStatuses.Clean)
            .OrderBy(r => r.RoomNumber)
            .ToListAsync(ct);

        foreach (var room in candidateRooms)
        {
            var hasOverlappingBooking = await db.Bookings
                .AnyAsync(b =>
                    b.RoomId == room.Id &&
                    (
                        b.Status == "confirmed" ||
                        b.Status == "paid" ||
                        (b.Status == "pending" && b.HoldUntilUtc > DateTime.UtcNow)
                    ) &&
                    b.CheckIn < checkOutDate &&
                    b.CheckOut > checkInDate,
                    ct);

            if (hasOverlappingBooking)
                continue;

            var hasBlockedAvailability = await db.RoomAvailabilities
                .AnyAsync(a =>
                    a.RoomId == room.Id &&
                    a.Date >= checkInDate &&
                    a.Date < checkOutDate &&
                    !a.IsAvailable,
                    ct);

            if (hasBlockedAvailability)
                continue;

            booking.RoomId = room.Id;
            room.Status = "occupied";
            room.OperationalStatus = RoomOperationalStatuses.Occupied;

            var existingAvailabilities = await db.RoomAvailabilities
                .Where(a =>
                    a.RoomId == room.Id &&
                    a.Date >= checkInDate &&
                    a.Date < checkOutDate)
                .ToDictionaryAsync(a => a.Date, ct);

            var dates = Enumerable.Range(0, (checkOutDate - checkInDate).Days)
                .Select(offset => checkInDate.AddDays(offset));

            foreach (var date in dates)
            {
                if (existingAvailabilities.TryGetValue(date, out var existing))
                {
                    existing.IsAvailable = false;
                }
                else
                {
                    db.RoomAvailabilities.Add(new RoomAvailability
                    {
                        Id = Guid.NewGuid(),
                        RoomId = room.Id,
                        Date = date,
                        IsAvailable = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            return;
        }

        throw new InvalidOperationException("No clean available room can be assigned for this booking");
    }

    public async Task AssignRoomToBookingAsync(AppDbContext db, Guid bookingId, Guid roomId, CancellationToken ct = default)
    {
        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, ct)
            ?? throw new InvalidOperationException("Booking not found");

        var room = await db.Rooms.FirstOrDefaultAsync(r => r.Id == roomId, ct)
            ?? throw new InvalidOperationException("Room not found");

        if (room.RoomTypeId != booking.RoomTypeId)
            throw new InvalidOperationException("Room type mismatch");

        if (!string.Equals(room.Status, "available", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(room.OperationalStatus, RoomOperationalStatuses.Clean, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Room is not assignable");

        booking.RoomId = room.Id;
        room.Status = "occupied";
        room.OperationalStatus = RoomOperationalStatuses.Occupied;
    }

    public async Task AutoAssignRoomsAsync(AppDbContext db, DateTime dateUtc, CancellationToken ct = default)
    {
        var targetDate = dateUtc.Date;
        var bookings = await db.Bookings
            .Where(b =>
                b.RoomId == null &&
                b.Status == "confirmed" &&
                b.CheckIn.Date <= targetDate.AddDays(1))
            .OrderBy(b => b.CheckIn)
            .ToListAsync(ct);

        foreach (var booking in bookings)
        {
            await AssignRoomAfterPaymentAsync(db, booking, ct);
        }
    }
}
