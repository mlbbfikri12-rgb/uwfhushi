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
            .HasIndex(c => c.GlobalCustomerId);

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
            .HasIndex(b => new { b.CheckIn, b.CheckOut });

        modelBuilder.Entity<Booking>()
            .HasIndex(b => b.BookingCode)
            .IsUnique();

        // ======================
        // ROOM
        // ======================
        modelBuilder.Entity<Room>()
            .HasIndex(r => r.RoomNumber)
            .IsUnique();

        modelBuilder.Entity<Room>()
            .HasIndex(r => r.RoomTypeId);

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
            .HasForeignKey(b => b.RoomId);

        // Booking → Customer
        modelBuilder.Entity<Booking>()
            .HasOne(b => b.Customer)
            .WithMany(c => c.Bookings)
            .HasForeignKey(b => b.CustomerId);

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
    }



}
