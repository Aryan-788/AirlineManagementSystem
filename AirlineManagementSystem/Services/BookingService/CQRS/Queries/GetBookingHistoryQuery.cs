namespace BookingService.CQRS.Queries;

public class GetBookingHistoryQuery
{
    public int UserId { get; set; }

    public GetBookingHistoryQuery(int userId)
    {
        UserId = userId;
    }
}
