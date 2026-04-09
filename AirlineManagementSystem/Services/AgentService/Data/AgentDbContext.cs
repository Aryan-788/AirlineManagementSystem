using AgentService.Models;
using Microsoft.EntityFrameworkCore;

namespace AgentService.Data;

public class AgentDbContext : DbContext
{
    public AgentDbContext(DbContextOptions<AgentDbContext> options) : base(options)
    {
    }

    public DbSet<Dealer> Dealers { get; set; } = null!;
    public DbSet<DealerBooking> DealerBookings { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Dealer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DealerEmail).IsUnique();
            entity.Property(e => e.DealerName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DealerEmail).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CommissionRate).HasPrecision(5, 2);
        });

        modelBuilder.Entity<DealerBooking>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DealerId);
            entity.HasIndex(e => e.BookingId);
            entity.Property(e => e.Commission).HasPrecision(10, 2);
        });
    }
}
