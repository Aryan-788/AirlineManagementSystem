using AdminService.DTOs;

namespace AdminService.Services;

public interface IAdminService
{
    Task<DashboardDto> GetDashboardAsync();
    Task<IEnumerable<BookingReportDto>> GetBookingReportAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<RevenueReportDto>> GetRevenueReportAsync(DateTime startDate, DateTime endDate);
}

public class AdminServiceImpl : IAdminService
{
    private readonly HttpClient _httpClient;

    public AdminServiceImpl(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<DashboardDto> GetDashboardAsync()
    {
        var totalBookings = 0;
        var totalRevenue = 0m;
        var activeFlights = 0;
        var totalUsers = 0;

        try
        {
            var bookingResponse = await _httpClient.GetAsync("http://booking-service:80/api/bookings");
            var flightResponse = await _httpClient.GetAsync("http://flight-service:80/api/flights");
            var paymentResponse = await _httpClient.GetAsync("http://payment-service:80/api/payments");
            var identityResponse = await _httpClient.GetAsync("http://identity-service:80/api/auth/users");
        }
        catch
        {
        }

        return new DashboardDto
        {
            TotalBookings = totalBookings,
            TotalRevenue = totalRevenue,
            ActiveFlights = activeFlights,
            TotalUsers = totalUsers
        };
    }

    public async Task<IEnumerable<BookingReportDto>> GetBookingReportAsync(DateTime startDate, DateTime endDate)
    {
        var reports = new List<BookingReportDto>();
        return reports;
    }

    public async Task<IEnumerable<RevenueReportDto>> GetRevenueReportAsync(DateTime startDate, DateTime endDate)
    {
        var reports = new List<RevenueReportDto>();
        return reports;
    }
}
