using FlightService.Services;

namespace FlightService.Workers;

public class ScheduleCompletionWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScheduleCompletionWorker> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(60);

    public ScheduleCompletionWorker(IServiceScopeFactory scopeFactory, ILogger<ScheduleCompletionWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ScheduleCompletionWorker started.");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var scheduleService = scope.ServiceProvider.GetRequiredService<IFlightScheduleService>();
                await scheduleService.MarkExpiredSchedulesCompletedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ScheduleCompletionWorker cycle");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
