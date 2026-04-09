namespace BookingService.CQRS.Queries;

public class GetOccupiedSeatsQuery
{
    public int FlightId { get; set; }
    public int? ScheduleId { get; set; }

    public GetOccupiedSeatsQuery(int flightId, int? scheduleId)
    {
        FlightId = flightId;
        ScheduleId = scheduleId;
    }
}
