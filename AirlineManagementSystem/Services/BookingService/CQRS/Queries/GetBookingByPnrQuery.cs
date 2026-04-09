namespace BookingService.CQRS.Queries;

public class GetBookingByPnrQuery
{
    public string PNR { get; set; }

    public GetBookingByPnrQuery(string pnr)
    {
        PNR = pnr;
    }
}
