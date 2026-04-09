using RewardService.DTOs;
using RewardService.Models;
using RewardService.Repositories;
using Shared.Events;
using Shared.RabbitMQ;

namespace RewardService.Services;

public interface IRewardService
{
    Task<RewardDto> EarnPointsAsync(int userId, int points, string transactionType, int? bookingId = null);
    Task<RewardBalanceDto> GetBalanceAsync(int userId);
    Task<IEnumerable<RewardDto>> GetHistoryAsync(int userId);
    Task<RewardDto> RedeemPointsAsync(int userId, int points);
    Task HandleRewardEarnedAsync(RewardEarnedEvent rewardEvent);
}

public class RewardServiceImpl : IRewardService
{
    private readonly IRewardRepository _repository;
    private readonly IEventPublisher _eventPublisher;
    private const int PointsPerDollar = 10;

    public RewardServiceImpl(IRewardRepository repository, IEventPublisher eventPublisher)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
    }

    public async Task<RewardDto> EarnPointsAsync(int userId, int points, string transactionType, int? bookingId = null)
    {
        var reward = new Reward
        {
            UserId = userId,
            Points = points,
            TransactionType = transactionType,
            BookingId = bookingId,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(reward);

        if (bookingId.HasValue)
        {
            await _eventPublisher.PublishAsync(new RewardEarnedEvent(
                userId,
                points,
                bookingId.Value,
                DateTime.UtcNow));
        }

        return MapToDto(reward);
    }

    public async Task<RewardBalanceDto> GetBalanceAsync(int userId)
    {
        var totalPoints = await _repository.GetTotalPointsAsync(userId);
        return new RewardBalanceDto
        {
            UserId = userId,
            TotalPoints = totalPoints
        };
    }

    public async Task<IEnumerable<RewardDto>> GetHistoryAsync(int userId)
    {
        var rewards = await _repository.GetByUserIdAsync(userId);
        return rewards.Select(MapToDto);
    }

    public async Task<RewardDto> RedeemPointsAsync(int userId, int points)
    {
        var balance = await GetBalanceAsync(userId);
        if (balance.TotalPoints < points)
            throw new InvalidOperationException("Insufficient points");

        var reward = new Reward
        {
            UserId = userId,
            Points = -points,
            TransactionType = "Redemption",
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(reward);
        return MapToDto(reward);
    }

    public async Task HandleRewardEarnedAsync(RewardEarnedEvent rewardEvent)
    {
        try
        {
            var reward = new Reward
            {
                UserId = rewardEvent.UserId,
                Points = rewardEvent.Points,
                TransactionType = "BookingConfirmed",
                BookingId = rewardEvent.BookingId,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(reward);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error handling reward earned event for user {rewardEvent.UserId}: {ex.Message}");
        }
    }

    private RewardDto MapToDto(Reward reward)
    {
        return new RewardDto
        {
            Id = reward.Id,
            UserId = reward.UserId,
            Points = reward.Points,
            TransactionType = reward.TransactionType,
            CreatedAt = reward.CreatedAt
        };
    }
}
