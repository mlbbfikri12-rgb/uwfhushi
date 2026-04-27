using Bogus;
using Hotel.Api.Entities.Tenant;
using Microsoft.EntityFrameworkCore;

namespace Hotel.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.RoomTypes.AnyAsync()) return;

        // ======================
        // ROOM TYPE
        // ======================
        var roomTypeFaker = new Faker<RoomType>()
            .RuleFor(r => r.Id, f => Guid.NewGuid())
            .RuleFor(r => r.Name, f => f.PickRandom("Standard", "Deluxe", "Suite"))
            .RuleFor(r => r.Description, f => f.Lorem.Sentence())
            .RuleFor(r => r.BasePrice, f => f.Random.Decimal(200000, 800000))
            .RuleFor(r => r.MaxAdults, f => 2)
            .RuleFor(r => r.MaxChildren, f => 1)
            .RuleFor(r => r.CreatedAt, DateTime.UtcNow);

        var roomTypes = roomTypeFaker.Generate(3);
        roomTypes[0].Name = "Standard";
        roomTypes[0].BasePrice = 350000;
        roomTypes[0].MaxAdults = 2;
        roomTypes[0].MaxChildren = 1;

        roomTypes[1].Name = "Deluxe";
        roomTypes[1].BasePrice = 550000;
        roomTypes[1].MaxAdults = 2;
        roomTypes[1].MaxChildren = 2;

        roomTypes[2].Name = "Suite";
        roomTypes[2].BasePrice = 900000;
        roomTypes[2].MaxAdults = 4;
        roomTypes[2].MaxChildren = 2;

        await db.RoomTypes.AddRangeAsync(roomTypes);

        // ======================
        // ROOM
        // ======================
        var roomFaker = new Faker<Room>()
            .RuleFor(r => r.Id, f => Guid.NewGuid())
            .RuleFor(r => r.RoomNumber, f => (100 + f.UniqueIndex).ToString())
            .RuleFor(r => r.RoomTypeId, f => f.PickRandom(roomTypes).Id)
            .RuleFor(r => r.Status, "available")
            .RuleFor(r => r.CreatedAt, DateTime.UtcNow);

        var rooms = roomFaker.Generate(10);
        await db.Rooms.AddRangeAsync(rooms);

        // ======================
        // ROOM IMAGE
        // ======================
        var roomImages = new List<RoomImage>();

        foreach (var room in rooms)
        {
            roomImages.Add(new RoomImage
            {
                Id = Guid.NewGuid(),
                RoomId = room.Id,
                Url = $"https://picsum.photos/seed/{room.Id}/800/600",
                Format = "webp",
                CreatedAt = DateTime.UtcNow
            });
        }

        await db.RoomImages.AddRangeAsync(roomImages);

        // ======================
        // CUSTOMER (TENANT)
        // ======================
        var customerFaker = new Faker<Customer>()
            .RuleFor(c => c.Id, f => Guid.NewGuid())
            .RuleFor(c => c.GlobalCustomerId, f => Guid.NewGuid()) // sementara dummy
            .RuleFor(c => c.Name, f => f.Name.FullName())
            .RuleFor(c => c.Email, f => f.Internet.Email())
            .RuleFor(c => c.Phone, f => f.Phone.PhoneNumber("08##########"))
            .RuleFor(c => c.IsVerified, true)
            .RuleFor(c => c.CreatedAt, DateTime.UtcNow);

        var customers = customerFaker.Generate(5);
        await db.Customers.AddRangeAsync(customers);

        // ======================
        // ROOM AVAILABILITY (30 hari ke depan)
        // ======================
        var availabilities = new List<RoomAvailability>();

        foreach (var room in rooms)
        {
            for (int i = 0; i < 30; i++)
            {
                availabilities.Add(new RoomAvailability
                {
                    Id = Guid.NewGuid(),
                    RoomId = room.Id,
                    Date = DateTime.UtcNow.Date.AddDays(i),
                    IsAvailable = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await db.RoomAvailabilities.AddRangeAsync(availabilities);

        await db.SaveChangesAsync();
    }
}
