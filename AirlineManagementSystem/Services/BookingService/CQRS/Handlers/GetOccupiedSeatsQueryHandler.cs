using BookingService.CQRS.Queries;
using BookingService.Repositories;

namespace BookingService.CQRS.Handlers;

public class GetOccupiedSeatsQueryHandler
{
    private readonly IBookingRepository _repository;
    private readonly ILogger<GetOccupiedSeatsQueryHandler> _logger;

    public GetOccupiedSeatsQueryHandler(
        IBookingRepository repository,
        ILogger<GetOccupiedSeatsQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IEnumerable<string>> HandleAsync(GetOccupiedSeatsQuery query)
    {
        var seats = await _repository.GetOccupiedSeatsAsync(query.FlightId, query.ScheduleId);

        _logger.LogInformation($"Retrieved occupied seats for flight {query.FlightId}");

        return seats;
    }
}
