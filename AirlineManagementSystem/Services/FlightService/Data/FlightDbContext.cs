using FlightService.Models;
using Microsoft.EntityFrameworkCore;

namespace FlightService.Data;

public class FlightDbContext : DbContext
{
    public FlightDbContext(DbContextOptions<FlightDbContext> options) : base(options)
    {
    }

    public DbSet<Flight> Flights { get; set; } = null!;
    public DbSet<FlightSchedule> FlightSchedules { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Flight>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.FlightNumber).IsUnique();
            entity.Property(e => e.FlightNumber).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Source).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Destination).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Aircraft).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Gate).HasMaxLength(10);
            entity.Property(e => e.Status).HasConversion<string>();
        });

        modelBuilder.Entity<FlightSchedule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Gate).HasMaxLength(10);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.HasOne(e => e.Flight)
                  .WithMany()
                  .HasForeignKey(e => e.FlightId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
