namespace BookingService.CQRS.Queries;

public class GetBookingByIdQuery
{
    public int BookingId { get; set; }

    public GetBookingByIdQuery(int bookingId)
    {
        BookingId = bookingId;
    }
}
