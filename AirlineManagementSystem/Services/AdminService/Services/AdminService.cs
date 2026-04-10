using AdminService.DTOs;
using System.Text.Json;
using Microsoft.Extensions.Logging;

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
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AdminServiceImpl> _logger;
    private static readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    public AdminServiceImpl(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, ILogger<AdminServiceImpl> logger)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    /// <summary>
    /// Creates an HttpRequestMessage with the admin's JWT forwarded.
    /// </summary>
    private HttpRequestMessage CreateRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        var token = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(token))
        {
            _logger.LogInformation("Forwarding token for request to {Url}", url);
            request.Headers.TryAddWithoutValidation("Authorization", token);
        }
        else
        {
            _logger.LogWarning("No Authorization token found in HttpContext for request to {Url}", url);
        }
        return request;
    }

    public async Task<DashboardDto> GetDashboardAsync()
    {
        _logger.LogInformation("GetDashboardAsync started.");
        var totalBookings = 0;
        var totalRevenue = 0m;
        var activeFlights = 0;
        var totalUsers = 0;

        // --- Flights: count active/scheduled (public endpoint, no auth needed) ---
        try
        {
            var url = "http://flight-service:5002/api/flights";
            _logger.LogInformation("Calling flight-service: {Url}", url);
            var resp = await _httpClient.SendAsync(CreateRequest(HttpMethod.Get, url));
            _logger.LogInformation("Flight-service response: {StatusCode}", resp.StatusCode);
            if (resp.IsSuccessStatusCode)
            {
                var json = await resp.Content.ReadAsStringAsync();
                var flights = JsonSerializer.Deserialize<List<FlightDto>>(json, _jsonOpts);
                if (flights != null)
                {
                    activeFlights = flights.Count(f =>
                        f.Status != null &&
                        (f.Status.Equals("Scheduled", StringComparison.OrdinalIgnoreCase) ||
                         f.Status.Equals("Active", StringComparison.OrdinalIgnoreCase)));
                    _logger.LogInformation("Found {Count} active/scheduled flights.", activeFlights);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling flight-service");
        }

        // --- Payments: sum confirmed/paid payments for revenue ---
        try
        {
            var url = "http://payment-service:5004/api/payments/all";
            _logger.LogInformation("Calling payment-service: {Url}", url);
            var resp = await _httpClient.SendAsync(CreateRequest(HttpMethod.Get, url));
            _logger.LogInformation("Payment-service response: {StatusCode}", resp.StatusCode);
            if (resp.IsSuccessStatusCode)
            {
                var json = await resp.Content.ReadAsStringAsync();
                var payments = JsonSerializer.Deserialize<List<PaymentDto>>(json, _jsonOpts);
                if (payments != null)
                {
                    var confirmed = payments.Where(p =>
                        p.Status != null &&
                        (p.Status.Equals("Paid", StringComparison.OrdinalIgnoreCase) ||
                         p.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase) ||
                         p.Status.Equals("Success", StringComparison.OrdinalIgnoreCase)));
                    totalRevenue = confirmed.Sum(p => p.Amount);
                    _logger.LogInformation("Calculated total revenue: {Revenue}", totalRevenue);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling payment-service");
        }

        // --- Bookings: get all bookings and count non-cancelled ---
        try
        {
            var url = "http://booking-service:5003/api/bookings/all";
            _logger.LogInformation("Calling booking-service: {Url}", url);
            var resp = await _httpClient.SendAsync(CreateRequest(HttpMethod.Get, url));
            _logger.LogInformation("Booking-service response: {StatusCode}", resp.StatusCode);
            if (resp.IsSuccessStatusCode)
            {
                var json = await resp.Content.ReadAsStringAsync();
                // Use JsonElement to handle both String and Number types for Status enum
                var bookings = JsonSerializer.Deserialize<List<JsonElement>>(json, _jsonOpts);
                if (bookings != null)
                {
                    totalBookings = bookings.Count(b => {
                        // Look for status property (case insensitive via _jsonOpts)
                        if (b.TryGetProperty("status", out var statusProp) || b.TryGetProperty("Status", out statusProp)) {
                            if (statusProp.ValueKind == JsonValueKind.Number)
                                return statusProp.GetInt32() != 4; // 4 is Cancelled index
                            
                            if (statusProp.ValueKind == JsonValueKind.String)
                                return !statusProp.GetString()!.Equals("Cancelled", StringComparison.OrdinalIgnoreCase);
                        }
                        return true; // Count it if status is missing or null
                    });
                    _logger.LogInformation("Found {Count} non-cancelled bookings.", totalBookings);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling booking-service");
        }

        // --- Users ---
        try
        {
            var url = "http://identity-service:5001/api/auth/users";
            _logger.LogInformation("Calling identity-service: {Url}", url);
            var resp = await _httpClient.SendAsync(CreateRequest(HttpMethod.Get, url));
            _logger.LogInformation("Identity-service response: {StatusCode}", resp.StatusCode);
            if (resp.IsSuccessStatusCode)
            {
                var json = await resp.Content.ReadAsStringAsync();
                var users = JsonSerializer.Deserialize<List<JsonElement>>(json, _jsonOpts);
                if (users != null)
                {
                    totalUsers = users.Count;
                    _logger.LogInformation("Found {Count} total users.", totalUsers);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling identity-service");
        }

        _logger.LogInformation("Dashboard aggregation complete. Bookings={Bookings}, Revenue={Revenue}, Flights={Flights}, Users={Users}", 
            totalBookings, totalRevenue, activeFlights, totalUsers);

        return new DashboardDto
        {
            TotalBookings = totalBookings,
            TotalRevenue = totalRevenue,
            ActiveFlights = activeFlights,
            TotalUsers = totalUsers
        };
    }

    public Task<IEnumerable<BookingReportDto>> GetBookingReportAsync(DateTime startDate, DateTime endDate)
        => Task.FromResult<IEnumerable<BookingReportDto>>(new List<BookingReportDto>());

    public Task<IEnumerable<RevenueReportDto>> GetRevenueReportAsync(DateTime startDate, DateTime endDate)
        => Task.FromResult<IEnumerable<RevenueReportDto>>(new List<RevenueReportDto>());
}

// ─── Lightweight internal DTOs for JSON deserialization ─────────────────────

file class FlightDto
{
    public int Id { get; set; }
    public string? Status { get; set; }
}

file class PaymentDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string? Status { get; set; }
}
