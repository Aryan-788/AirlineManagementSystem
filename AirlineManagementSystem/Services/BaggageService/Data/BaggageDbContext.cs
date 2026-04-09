using BaggageService.Models;
using Microsoft.EntityFrameworkCore;

namespace BaggageService.Data;

public class BaggageDbContext : DbContext
{
    public BaggageDbContext(DbContextOptions<BaggageDbContext> options) : base(options)
    {
    }

    public DbSet<Baggage> Baggages { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Baggage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.BookingId);
            entity.HasIndex(e => e.TrackingNumber).IsUnique();
            entity.Property(e => e.Weight).HasPrecision(8, 2);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.TrackingNumber).IsRequired().HasMaxLength(50);
        });
    }
}
