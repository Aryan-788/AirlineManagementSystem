using RewardService.Models;
using Microsoft.EntityFrameworkCore;

namespace RewardService.Data;

public class RewardDbContext : DbContext
{
    public RewardDbContext(DbContextOptions<RewardDbContext> options) : base(options)
    {
    }

    public DbSet<Reward> Rewards { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Reward>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.Property(e => e.Points).IsRequired();
            entity.Property(e => e.TransactionType).IsRequired().HasMaxLength(50);
        });
    }
}
