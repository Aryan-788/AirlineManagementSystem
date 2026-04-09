using RewardService.Models;
using Microsoft.EntityFrameworkCore;
namespace RewardService.Repositories;

public interface IRewardRepository
{
    Task<Reward> AddAsync(Reward reward);
    Task<IEnumerable<Reward>> GetByUserIdAsync(int userId);
    Task<int> GetTotalPointsAsync(int userId);
    Task<IEnumerable<Reward>> GetAllAsync();
}

public class RewardRepository : IRewardRepository
{
    private readonly RewardService.Data.RewardDbContext _context;

    public RewardRepository(RewardService.Data.RewardDbContext context)
    {
        _context = context;
    }

    public async Task<Reward> AddAsync(Reward reward)
    {
        _context.Rewards.Add(reward);
        await _context.SaveChangesAsync();
        return reward;
    }

    public async Task<IEnumerable<Reward>> GetByUserIdAsync(int userId)
    {
        return await _context.Rewards.Where(r => r.UserId == userId).ToListAsync();
    }

    public async Task<int> GetTotalPointsAsync(int userId)
    {
        return await _context.Rewards
            .Where(r => r.UserId == userId)
            .SumAsync(r => r.Points);
    }

    public async Task<IEnumerable<Reward>> GetAllAsync()
    {
        return await _context.Rewards.ToListAsync();
    }
}
