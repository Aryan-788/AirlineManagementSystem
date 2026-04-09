using BookingService.CQRS.Queries;
using BookingService.Repositories;

namespace BookingService.CQRS.Handlers;

public class GetBookingsByScheduleQueryHandler
{
    private readonly IBookingRepository _repository;
    private readonly ILogger<GetBookingsByScheduleQueryHandler> _logger;

    public GetBookingsByScheduleQueryHandler(
        IBookingRepository repository,
        ILogger<GetBookingsByScheduleQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IEnumerable<object>> HandleAsync(GetBookingsByScheduleQuery query)
    {
        var bookings = await _repository.GetByScheduleIdAsync(query.ScheduleId);

        _logger.LogInformation($"Retrieved {bookings.Count()} bookings for schedule {query.ScheduleId}");

        return bookings.Select(b => (object)new
        {
            Id = b.Id,
            PNR = b.PNR,
            UserId = b.UserId,
            SeatClass = b.SeatClass.ToString(),
            Status = b.Status.ToString(),
            PaymentStatus = b.PaymentStatus.ToString(),
            Passengers = b.Passengers?.Select(p => (object)new
            {
                p.Id,
                p.Name,
                p.Age,
                p.Gender,
                Status = p.Status.ToString(),
                Seat = p.SeatNumber ?? "TBD"
            }).ToList() ?? new List<object>()
        }).ToList();
    }
}
