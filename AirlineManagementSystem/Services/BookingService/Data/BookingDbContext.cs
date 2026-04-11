using BookingService.Models;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Data;

public class BookingDbContext : DbContext
{
    public BookingDbContext(DbContextOptions<BookingDbContext> options) : base(options)
    {
    }

    public DbSet<Booking> Bookings { get; set; } = null!;
    public DbSet<Passenger> Passengers { get; set; } = null!;
    public DbSet<Refund> Refunds { get; set; } = null!;
    public DbSet<RefundPolicy> RefundPolicies { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PNR).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ScheduleId);
            entity.Property(e => e.PNR).IsRequired().HasMaxLength(10);
            entity.Property(e => e.SeatClass).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.PaymentStatus).HasConversion<string>();

            // One-to-Many relationship
            entity.HasMany(e => e.Passengers)
                .WithOne(p => p.Booking)
                .HasForeignKey(p => p.BookingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Passenger>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.BookingId);
            entity.HasIndex(e => e.AadharCardNo).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Gender).IsRequired().HasMaxLength(20);
            entity.Property(e => e.AadharCardNo).IsRequired().HasMaxLength(12);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.CancellationReason).HasMaxLength(500);
        });

        modelBuilder.Entity<RefundPolicy>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasData(
                new RefundPolicy { Id = 1, MinHoursBeforeDeparture = 24, MaxHoursBeforeDeparture = 99999, RefundPercentage = 100, CreatedAt = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc) },
                new RefundPolicy { Id = 2, MinHoursBeforeDeparture = 2, MaxHoursBeforeDeparture = 24, RefundPercentage = 70, CreatedAt = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc) },
                new RefundPolicy { Id = 3, MinHoursBeforeDeparture = 0, MaxHoursBeforeDeparture = 2, RefundPercentage = 30, CreatedAt = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc) },
                new RefundPolicy { Id = 4, MinHoursBeforeDeparture = -99999, MaxHoursBeforeDeparture = 0, RefundPercentage = 0, CreatedAt = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc) }
            );
        });

        modelBuilder.Entity<Refund>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.BookingId);
            entity.Property(e => e.RefundAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.RefundPercentage).HasColumnType("decimal(18,2)");
            entity.HasOne(r => r.Booking)
                .WithMany()
                .HasForeignKey(r => r.BookingId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
