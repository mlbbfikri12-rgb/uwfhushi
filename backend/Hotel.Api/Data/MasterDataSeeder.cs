using Hotel.Api.Entities.Master;
using Microsoft.EntityFrameworkCore;
using MasterHotel = Hotel.Api.Entities.Master.Hotel;

namespace Hotel.Api.Data;

public static class MasterDataSeeder
{
    public static async Task SeedAsync(MasterDbContext db)
    {
        var citySurabaya = await EnsureCityAsync(db, "Surabaya");
        var cityJakarta = await EnsureCityAsync(db, "Jakarta");
        var cityBali = await EnsureCityAsync(db, "Bali");

        var brandSantika = await EnsureBrandAsync(db, "Santika", null);
        var brandAmaris = await EnsureBrandAsync(db, "Amaris", null);

        await db.SaveChangesAsync();

        await EnsureHotelAsync(db, "Hotel Surabaya", "hotel-surabaya", "SBY", citySurabaya.Id, brandSantika.Id, "Surabaya Center");
        await EnsureHotelAsync(db, "Hotel Jakarta", "hotel-jakarta", "JKT", cityJakarta.Id, brandAmaris.Id, "Jakarta CBD");
        await EnsureHotelAsync(db, "Hotel Bali", "hotel-bali", "BLI", cityBali.Id, brandSantika.Id, "Bali Beach Area");

        await EnsureBlogAsync(db, "Best Weekend Staycation Tips", "Book smart for your weekend getaway.");
        await EnsureBlogAsync(db, "Top Hotel Facilities for Family", "Choose hotel with kid-friendly amenities.");

        await db.SaveChangesAsync();
    }

    private static async Task<City> EnsureCityAsync(MasterDbContext db, string name)
    {
        var city = await db.Cities.FirstOrDefaultAsync(x => x.Name == name);
        if (city != null)
        {
            city.IsActive = true;
            return city;
        }

        city = new City
        {
            Id = Guid.NewGuid(),
            Name = name,
            Province = string.Empty,
            IsActive = true
        };
        db.Cities.Add(city);
        return city;
    }

    private static async Task<Brand> EnsureBrandAsync(MasterDbContext db, string name, string? logoUrl)
    {
        var brand = await db.Brands.FirstOrDefaultAsync(x => x.Name == name);
        if (brand != null)
        {
            brand.IsActive = true;
            brand.LogoUrl = logoUrl;
            return brand;
        }

        brand = new Brand
        {
            Id = Guid.NewGuid(),
            Name = name,
            LogoUrl = logoUrl,
            IsActive = true
        };
        db.Brands.Add(brand);
        return brand;
    }

    private static async Task EnsureHotelAsync(
        MasterDbContext db,
        string name,
        string slug,
        string branchCode,
        Guid cityId,
        Guid? brandId,
        string address)
    {
        var branch = await db.Branches.AsNoTracking().FirstOrDefaultAsync(x => x.Code == branchCode && x.IsActive);
        if (branch == null)
            return;

        var hotel = await db.Hotels.FirstOrDefaultAsync(x => x.BranchCode == branchCode);
        if (hotel == null)
        {
            db.Hotels.Add(new MasterHotel
            {
                Id = Guid.NewGuid(),
                Name = name,
                Slug = slug,
                BranchCode = branchCode,
                CityId = cityId,
                BrandId = brandId,
                Address = address,
                Description = $"{name} description",
                StarRating = 4,
                Rating = 4,
                ReviewCount = 0,
                Latitude = 0,
                Longitude = 0,
                IsActive = true
            });
            return;
        }

        hotel.Name = name;
        hotel.Slug = slug;
        hotel.CityId = cityId;
        hotel.BrandId = brandId;
        hotel.Address = address;
        hotel.IsActive = true;
    }

    private static async Task EnsureBlogAsync(MasterDbContext db, string title, string content)
    {
        var blog = await db.BlogPosts.FirstOrDefaultAsync(x => x.Title == title);
        if (blog != null)
        {
            blog.IsActive = true;
            return;
        }

        db.BlogPosts.Add(new BlogPost
        {
            Id = Guid.NewGuid(),
            Title = title,
            Content = content,
            ImageUrl = "https://images.unsplash.com/photo-1520250497591-112f2f40a3f4?w=1200&q=80",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        });
    }
}
