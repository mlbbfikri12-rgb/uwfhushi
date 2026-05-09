using Microsoft.EntityFrameworkCore;
using Hotel.Api.Entities.Tenant;

namespace Hotel.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<RoomType> RoomTypes => Set<RoomType>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<RoomImage> RoomImages => Set<RoomImage>();
    public DbSet<RoomAvailability> RoomAvailabilities => Set<RoomAvailability>();
    public DbSet<RoomTypeFacility> RoomTypeFacilities => Set<RoomTypeFacility>();
    public DbSet<RatePlan> RatePlans => Set<RatePlan>();
    public DbSet<OrderDraft> OrderDrafts => Set<OrderDraft>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<BookingGroup> BookingGroups => Set<BookingGroup>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ======================
        // CUSTOMER
        // ======================
        modelBuilder.Entity<Customer>()
.HasIndex(c => c.GlobalCustomerId)
.IsUnique();

        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.Email);

        // ======================
        // BOOKING
        // ======================
        modelBuilder.Entity<Booking>()
            .HasIndex(b => b.CustomerId);


        modelBuilder.Entity<Booking>()
            .HasIndex(b => b.RoomId);
        modelBuilder.Entity<Booking>()
            .HasIndex(b => b.RoomTypeId);
        modelBuilder.Entity<Booking>()
            .HasIndex(b => b.BookingGroupId);

        modelBuilder.Entity<Booking>()
            .HasIndex(b => new { b.CheckIn, b.CheckOut });

        modelBuilder.Entity<Booking>()
            .HasIndex(b => b.BookingCode)
            .IsUnique();
        modelBuilder.Entity<Booking>()
.HasIndex(b => new { b.Status, b.CheckIn });
        modelBuilder.Entity<Booking>()
            .HasIndex(b => new { b.Status, b.HoldUntilUtc, b.RoomTypeId });

        modelBuilder.Entity<BookingGroup>()
            .HasIndex(g => g.GroupCode)
            .IsUnique();
        modelBuilder.Entity<BookingGroup>()
            .HasIndex(g => new { g.Status, g.HoldUntilUtc });

        // ======================
        // ROOM
        // ======================
        modelBuilder.Entity<Room>()
            .HasIndex(r => r.RoomNumber)
            .IsUnique();

        modelBuilder.Entity<Room>()
            .HasIndex(r => r.RoomTypeId);
        modelBuilder.Entity<Room>()
            .HasIndex(r => new { r.RoomTypeId, r.Status, r.OperationalStatus });

        modelBuilder.Entity<RatePlan>()
            .HasIndex(rp => new { rp.RoomTypeId, rp.IsActive });

        // ======================
        // ROOM AVAILABILITY
        // ======================
        modelBuilder.Entity<RoomAvailability>()
            .HasIndex(a => new { a.RoomId, a.Date })
            .IsUnique();

        // ======================
        // PAYMENT
        // ======================
        modelBuilder.Entity<Payment>()
            .HasIndex(p => p.BookingId);

        modelBuilder.Entity<OrderDraft>()
            .HasIndex(o => new { o.CustomerId, o.Status });

        modelBuilder.Entity<OrderItem>()
            .HasIndex(i => new { i.OrderDraftId, i.RoomTypeId, i.RatePlanId });

        // ======================
        // RELATIONSHIPS (WAJIB)
        // ======================

        // Room → RoomImage
        modelBuilder.Entity<RoomImage>()
            .HasOne(i => i.Room)
            .WithMany(r => r.Images)
            .HasForeignKey(i => i.RoomId);

        // Room → Booking
        modelBuilder.Entity<Booking>()
            .HasOne(b => b.Room)
            .WithMany(r => r.Bookings)
            .HasForeignKey(b => b.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Booking>()
            .HasOne(b => b.RoomType)
            .WithMany()
            .HasForeignKey(b => b.RoomTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Booking → Customer
        modelBuilder.Entity<Booking>()
            .HasOne(b => b.Customer)
            .WithMany(c => c.Bookings)
            .HasForeignKey(b => b.CustomerId);

        modelBuilder.Entity<BookingGroup>()
            .HasOne(g => g.Customer)
            .WithMany()
            .HasForeignKey(g => g.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Booking>()
            .HasOne(b => b.BookingGroup)
            .WithMany(g => g.Bookings)
            .HasForeignKey(b => b.BookingGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RoomTypeFacility>()
            .HasOne(rtf => rtf.RoomType)
            .WithMany(rt => rt.Facilities)
            .HasForeignKey(rtf => rtf.RoomTypeId);

        modelBuilder.Entity<RatePlan>()
            .HasOne(rp => rp.RoomType)
            .WithMany(rt => rt.RatePlans)
            .HasForeignKey(rp => rp.RoomTypeId);

        modelBuilder.Entity<OrderDraft>()
            .HasOne(o => o.Customer)
            .WithMany()
            .HasForeignKey(o => o.CustomerId);

        modelBuilder.Entity<OrderItem>()
            .HasOne(i => i.OrderDraft)
            .WithMany(o => o.Items)
            .HasForeignKey(i => i.OrderDraftId);

        modelBuilder.Entity<OrderItem>()
            .HasOne(i => i.RoomType)
            .WithMany()
            .HasForeignKey(i => i.RoomTypeId);

        modelBuilder.Entity<OrderItem>()
            .HasOne(i => i.RatePlan)
            .WithMany()
            .HasForeignKey(i => i.RatePlanId);

        modelBuilder.Entity<RoomAvailability>()
            .HasIndex(a => new { a.RoomId, a.Date, a.IsAvailable })
            .HasDatabaseName("idx_room_availability_lookup");

        modelBuilder.Entity<RoomAvailability>()
            .HasOne(a => a.Room)
            .WithMany(r => r.Availabilities)
            .HasForeignKey(a => a.RoomId);

        modelBuilder.Entity<RoomType>()
.HasIndex(rt => rt.Name);
        modelBuilder.Entity<RoomTypeFacility>()
            .HasIndex(rtf => new { rtf.RoomTypeId, rtf.Name })
             .HasDatabaseName("idx_roomtype_facility_lookup")
             .IsUnique();

        modelBuilder.Entity<RatePlan>()
            .HasIndex(rp => new { rp.RoomTypeId, rp.IsActive });
    }



}
