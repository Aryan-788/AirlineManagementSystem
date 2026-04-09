using CheckInService.Models;
using Microsoft.EntityFrameworkCore;

namespace CheckInService.Data;

public class CheckInDbContext : DbContext
{
    public CheckInDbContext(DbContextOptions<CheckInDbContext> options) : base(options)
    {
    }

    public DbSet<CheckIn> CheckIns { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CheckIn>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.BookingId).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.Property(e => e.SeatNumber).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Gate).HasMaxLength(10);
            entity.Property(e => e.BoardingPass).IsRequired();
            entity.Property(e => e.QRCode).IsRequired();
        });
    }
}
