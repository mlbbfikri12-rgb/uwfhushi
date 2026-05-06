using Microsoft.EntityFrameworkCore;
using Hotel.Api.Entities.Master;
using MasterHotel = Hotel.Api.Entities.Master.Hotel;

namespace Hotel.Api.Data;

public class MasterDbContext : DbContext
{
    public MasterDbContext(DbContextOptions<MasterDbContext> options) : base(options) { }

    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<CustomerGlobal> CustomersGlobal => Set<CustomerGlobal>();
    public DbSet<Staff> Staffs => Set<Staff>();
    public DbSet<StaffBranch> StaffBranches => Set<StaffBranch>();
    public DbSet<HeroBanner> HeroBanners => Set<HeroBanner>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<MasterHotel> Hotels => Set<MasterHotel>();
    public DbSet<HotelImage> HotelImages => Set<HotelImage>();
    public DbSet<Facility> Facilities => Set<Facility>();
    public DbSet<HotelFacility> HotelFacilities => Set<HotelFacility>();
    public DbSet<NearbyPlace> NearbyPlaces => Set<NearbyPlace>();
    public DbSet<BlogPost> BlogPosts => Set<BlogPost>();
    public DbSet<HotelPriceSummary> HotelPriceSummaries => Set<HotelPriceSummary>();
    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Branch>()
            .HasIndex(b => b.Code)
            .IsUnique();

        modelBuilder.Entity<CustomerGlobal>()
            .HasIndex(c => c.Email)
            .IsUnique();

        modelBuilder.Entity<Staff>()
            .HasIndex(s => s.Email)
            .IsUnique();

        modelBuilder.Entity<StaffBranch>()
            .HasIndex(sb => new { sb.StaffId, sb.BranchId })
            .IsUnique();

        modelBuilder.Entity<HeroBanner>()
            .HasIndex(b => new { b.IsActive, b.SortOrder });

        modelBuilder.Entity<City>()
            .HasIndex(c => c.Name);

        modelBuilder.Entity<Brand>()
            .HasIndex(b => b.Name)
            .IsUnique();

        modelBuilder.Entity<MasterHotel>()
            .HasIndex(h => h.BranchCode)
            .IsUnique();
        modelBuilder.Entity<MasterHotel>()
            .HasIndex(h => h.Slug)
            .IsUnique();

        modelBuilder.Entity<MasterHotel>()
            .HasIndex(h => new { h.CityId, h.IsActive });

        modelBuilder.Entity<Facility>()
            .HasIndex(f => f.Name)
            .IsUnique();
        modelBuilder.Entity<BlogPost>()
            .HasIndex(b => new { b.IsActive, b.CreatedAt });

        modelBuilder.Entity<HotelFacility>()
            .HasKey(hf => new { hf.HotelId, hf.FacilityId });

        modelBuilder.Entity<MasterHotel>()
            .HasOne(h => h.City)
            .WithMany(c => c.Hotels)
            .HasForeignKey(h => h.CityId);

        modelBuilder.Entity<MasterHotel>()
            .HasOne(h => h.Brand)
            .WithMany(b => b.Hotels)
            .HasForeignKey(h => h.BrandId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<HotelImage>()
            .HasOne(i => i.Hotel)
            .WithMany(h => h.Images)
            .HasForeignKey(i => i.HotelId);

        modelBuilder.Entity<HotelFacility>()
            .HasOne(hf => hf.Hotel)
            .WithMany(h => h.HotelFacilities)
            .HasForeignKey(hf => hf.HotelId);

        modelBuilder.Entity<HotelFacility>()
            .HasOne(hf => hf.Facility)
            .WithMany(f => f.HotelFacilities)
            .HasForeignKey(hf => hf.FacilityId);

        modelBuilder.Entity<NearbyPlace>()
            .HasOne(np => np.Hotel)
            .WithMany(h => h.NearbyPlaces)
            .HasForeignKey(np => np.HotelId);

        modelBuilder.Entity<StaffBranch>()
            .HasOne(sb => sb.Staff)
            .WithMany(s => s.StaffBranches)
            .HasForeignKey(sb => sb.StaffId);

        modelBuilder.Entity<StaffBranch>()
            .HasOne(sb => sb.Branch)
            .WithMany(b => b.StaffBranches)
            .HasForeignKey(sb => sb.BranchId);

        modelBuilder.Entity<HotelPriceSummary>()
            .HasIndex(x => x.Slug)
            .IsUnique();

        modelBuilder.Entity<CustomerGlobal>()
.HasIndex(c => new { c.Email, c.IsVerified });
        modelBuilder.Entity<EmailVerificationToken>()
            .HasIndex(x => x.Token)
            .IsUnique();

        modelBuilder.Entity<EmailVerificationToken>()
            .HasIndex(x => x.CustomerId);
        modelBuilder.Entity<EmailVerificationToken>()
.HasIndex(x => x.ExpiredAt);
    }
}
