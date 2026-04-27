using Microsoft.EntityFrameworkCore;
using Hotel.Api.Entities.Master;

namespace Hotel.Api.Data;

public class MasterDbContext : DbContext
{
    public MasterDbContext(DbContextOptions<MasterDbContext> options) : base(options) { }

    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<CustomerGlobal> CustomersGlobal => Set<CustomerGlobal>();
    public DbSet<Staff> Staffs => Set<Staff>();
    public DbSet<StaffBranch> StaffBranches => Set<StaffBranch>();

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

        modelBuilder.Entity<StaffBranch>()
            .HasOne(sb => sb.Staff)
            .WithMany(s => s.StaffBranches)
            .HasForeignKey(sb => sb.StaffId);

        modelBuilder.Entity<StaffBranch>()
            .HasOne(sb => sb.Branch)
            .WithMany(b => b.StaffBranches)
            .HasForeignKey(sb => sb.BranchId);
    }
}
