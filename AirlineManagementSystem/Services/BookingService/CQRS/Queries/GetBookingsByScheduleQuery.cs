namespace BookingService.CQRS.Queries;

public class GetBookingsByScheduleQuery
{
    public int ScheduleId { get; set; }

    public GetBookingsByScheduleQuery(int scheduleId)
    {
        ScheduleId = scheduleId;
    }
}
