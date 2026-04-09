namespace BookingService.CQRS.Queries;

public class GetPassengersForBookingQuery
{
    public int BookingId { get; set; }

    public GetPassengersForBookingQuery(int bookingId)
    {
        BookingId = bookingId;
    }
}
