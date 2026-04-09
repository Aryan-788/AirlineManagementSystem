using BookingService.CQRS.Queries;
using BookingService.DTOs;
using BookingService.Repositories;

namespace BookingService.CQRS.Handlers;

public class GetBookingHistoryQueryHandler
{
    private readonly IBookingRepository _repository;
    private readonly ILogger<GetBookingHistoryQueryHandler> _logger;

    public GetBookingHistoryQueryHandler(
        IBookingRepository repository,
        ILogger<GetBookingHistoryQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IEnumerable<BookingHistoryDto>> HandleAsync(GetBookingHistoryQuery query)
    {
        var bookings = await _repository.GetByUserIdAsync(query.UserId);

        _logger.LogInformation($"Retrieved booking history for user {query.UserId}");

        return bookings.Select(b => new BookingHistoryDto
        {
            Id = b.Id,
            FlightId = b.FlightId,
            ScheduleId = b.ScheduleId,
            PNR = b.PNR,
            Status = b.Status.ToString(),
            CreatedAt = b.CreatedAt,
            TotalAmount = b.TotalAmount
        });
    }
}
