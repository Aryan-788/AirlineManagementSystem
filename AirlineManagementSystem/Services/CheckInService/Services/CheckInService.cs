using CheckInService.DTOs;
using CheckInService.Models;
using CheckInService.Repositories;
using Shared.Events;
using Shared.RabbitMQ;

namespace CheckInService.Services;

public interface ICheckInService
{
    Task<CheckInDto> OnlineCheckInAsync(OnlineCheckInDto dto, string passengerName, string flightNumber, int flightId, DateTime departureTime, string token);
    Task<CheckInDto> GetCheckInAsync(int id);
    Task<BoardingPassDto> GenerateBoardingPassAsync(int checkInId);
    Task<CheckInSummaryDto> GetSummaryAsync();
}

public class CheckInServiceImpl : ICheckInService
{
    private readonly ICheckInRepository _repository;
    private readonly IEventPublisher _eventPublisher;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public CheckInServiceImpl(ICheckInRepository repository, IEventPublisher eventPublisher, HttpClient httpClient, IConfiguration configuration)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<CheckInDto> OnlineCheckInAsync(OnlineCheckInDto dto, string passengerName, string flightNumber, int flightId, DateTime departureTime, string token)
    {
        // 1. Check if already checked in (Idempotency)
        var existingCheckIn = await _repository.GetByBookingIdAsync(dto.BookingId);
        if (existingCheckIn != null)
        {
            return MapToDto(existingCheckIn);
        }

        // 2. Validate booking exists
        try
        {
            var bookingServiceUrl = _configuration["ServiceUrls:BookingService"];
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{bookingServiceUrl}/api/bookings/{dto.BookingId}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            
            var bookingResponse = await _httpClient.SendAsync(request);
            if (!bookingResponse.IsSuccessStatusCode)
            {
                var statusCode = bookingResponse.StatusCode;
                throw new InvalidOperationException($"Booking service validation failed with status: {statusCode}");
            }
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Unable to contact booking service: {ex.Message}");
        }

        var seatNumber = !string.IsNullOrWhiteSpace(dto.SeatNumber) ? dto.SeatNumber : GenerateSeatNumber();
        var qrCode = GenerateQRCode($"{flightNumber}-{seatNumber}");

        var checkIn = new CheckIn
        {
            BookingId = dto.BookingId,
            UserId = dto.UserId,
            FlightId = flightId,
            SeatNumber = seatNumber,
            Gate = "",
            BoardingPass = $"{passengerName}|{flightNumber}|{seatNumber}",
            QRCode = qrCode,
            CheckInTime = DateTime.UtcNow,
            IsCheckedIn = true,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(checkIn);

        await _eventPublisher.PublishAsync(new CheckInCompletedEvent(
            dto.BookingId,
            dto.UserId,
            flightId,
            checkIn.BoardingPass,
            DateTime.UtcNow));

        return MapToDto(checkIn);
    }

    public async Task<CheckInDto> GetCheckInAsync(int id)
    {
        var checkIn = await _repository.GetByIdAsync(id);
        if (checkIn == null)
            throw new KeyNotFoundException($"Check-in {id} not found");

        return MapToDto(checkIn);
    }

    public async Task<BoardingPassDto> GenerateBoardingPassAsync(int checkInId)
    {
        var checkIn = await _repository.GetByIdAsync(checkInId);
        if (checkIn == null)
            throw new KeyNotFoundException($"Check-in {checkInId} not found");

        var parts = checkIn.BoardingPass.Split('|');
        return new BoardingPassDto
        {
            PassengerName = parts[0],
            FlightNumber = parts[1],
            SeatNumber = checkIn.SeatNumber,
            Gate = checkIn.Gate,
            QRCode = checkIn.QRCode
        };
    }

    private string GenerateSeatNumber()
    {
        var random = new Random();
        var row = random.Next(1, 51);
        var seat = (char)('A' + random.Next(0, 6));
        return $"{row}{seat}";
    }

    private string GenerateQRCode(string data)
    {
        using var qrGenerator = new QRCoder.QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(data, QRCoder.QRCodeGenerator.ECCLevel.Q);
        var pngBytes = new QRCoder.PngByteQRCode(qrCodeData).GetGraphic(10);
        return Convert.ToBase64String(pngBytes);
    }

    public async Task<CheckInSummaryDto> GetSummaryAsync()
    {
        var allCheckIns = await _repository.GetAllAsync();
        var checkInList = allCheckIns.ToList();
        var today = DateTime.UtcNow.Date;
        return new CheckInSummaryDto
        {
            TotalCheckIns = checkInList.Count,
            TodayCheckIns = checkInList.Count(c => c.CheckInTime.Date == today)
        };
    }

    private CheckInDto MapToDto(CheckIn checkIn)
    {
        return new CheckInDto
        {
            Id = checkIn.Id,
            BookingId = checkIn.BookingId,
            SeatNumber = checkIn.SeatNumber,
            Gate = checkIn.Gate,
            BoardingPass = checkIn.BoardingPass,
            CheckInTime = checkIn.CheckInTime
        };
    }
}
