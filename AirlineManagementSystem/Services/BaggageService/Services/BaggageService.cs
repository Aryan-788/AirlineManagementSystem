using BaggageService.DTOs;
using BaggageService.Models;
using BaggageService.Repositories;
using Shared.Events;
using Shared.Models;
using Shared.RabbitMQ;

namespace BaggageService.Services;

public interface IBaggageService
{
    Task<BaggageDto> AddBaggageAsync(AddBaggageDto dto);
    Task<BaggageDto> GetBaggageAsync(int id);
    Task<BaggageDto> UpdateBaggageStatusAsync(int id, UpdateBaggageStatusDto dto);
    Task<BaggageDto> MarkDeliveredAsync(int id);
    Task<IEnumerable<BaggageDto>> GetByBookingIdAsync(int bookingId);
    Task<BaggageDto> TrackBaggageAsync(string trackingNumber);
    Task<IEnumerable<BaggageDto>> GetAllBaggageAsync();
    Task<BaggageSummaryDto> GetSummaryAsync();
}

public class BaggageServiceImpl : IBaggageService
{
    private readonly IBaggageRepository _repository;
    private readonly IEventPublisher _eventPublisher;

    public BaggageServiceImpl(IBaggageRepository repository, IEventPublisher eventPublisher)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
    }

    public async Task<BaggageDto> AddBaggageAsync(AddBaggageDto dto)
    {
        if (dto.Weight > 23)
            throw new InvalidOperationException("Baggage weight exceeds the maximum allowed limit of 23kg.");

        var trackingNumber = GenerateTrackingNumber();

        var baggage = new Baggage
        {
            BookingId = dto.BookingId,
            Weight = dto.Weight,
            PassengerName = dto.PassengerName,
            FlightNumber = dto.FlightNumber,
            Status = BaggageStatus.Checked,
            TrackingNumber = trackingNumber,
            IsDelivered = false,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(baggage);

        await _eventPublisher.PublishAsync(new BaggageCheckedEvent(
            baggage.Id,
            dto.BookingId,
            dto.Weight,
            DateTime.UtcNow));

        return MapToDto(baggage);
    }

    public async Task<BaggageDto> GetBaggageAsync(int id)
    {
        var baggage = await _repository.GetByIdAsync(id);
        if (baggage == null)
            throw new KeyNotFoundException($"Baggage {id} not found");

        return MapToDto(baggage);
    }

    public async Task<BaggageDto> UpdateBaggageStatusAsync(int id, UpdateBaggageStatusDto dto)
    {
        var baggage = await _repository.GetByIdAsync(id);
        if (baggage == null)
            throw new KeyNotFoundException($"Baggage {id} not found");

        baggage.Status = Enum.Parse<BaggageStatus>(dto.Status);
        await _repository.UpdateAsync(baggage);

        return MapToDto(baggage);
    }

    public async Task<BaggageDto> MarkDeliveredAsync(int id)
    {
        var baggage = await _repository.GetByIdAsync(id);
        if (baggage == null)
            throw new KeyNotFoundException($"Baggage {id} not found");

        baggage.Status = BaggageStatus.Delivered;
        baggage.IsDelivered = true;
        await _repository.UpdateAsync(baggage);

        return MapToDto(baggage);
    }

    public async Task<IEnumerable<BaggageDto>> GetByBookingIdAsync(int bookingId)
    {
        var baggages = await _repository.GetByBookingIdAsync(bookingId);
        return baggages.Select(MapToDto);
    }

    public async Task<BaggageDto> TrackBaggageAsync(string trackingNumber)
    {
        var baggage = await _repository.GetByTrackingNumberAsync(trackingNumber);
        if (baggage == null)
            throw new KeyNotFoundException($"Baggage with tracking number {trackingNumber} not found");

        return MapToDto(baggage);
    }

    public async Task<IEnumerable<BaggageDto>> GetAllBaggageAsync()
    {
        var baggages = await _repository.GetAllAsync();
        return baggages.OrderByDescending(b => b.CreatedAt).Select(MapToDto);
    }

    public async Task<BaggageSummaryDto> GetSummaryAsync()
    {
        var allBags = await _repository.GetAllAsync();
        var bagList = allBags.ToList();
        return new BaggageSummaryDto
        {
            TotalBags = bagList.Count,
            DeliveredCount = bagList.Count(b => b.IsDelivered),
            InTransitCount = bagList.Count(b => !b.IsDelivered && b.Status != BaggageStatus.Checked),
            CheckedCount = bagList.Count(b => b.Status == BaggageStatus.Checked),
            TotalWeight = bagList.Sum(b => b.Weight)
        };
    }

    private string GenerateTrackingNumber()
    {
        return $"BAG-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
    }

    private BaggageDto MapToDto(Baggage baggage)
    {
        return new BaggageDto
        {
            Id = baggage.Id,
            BookingId = baggage.BookingId,
            Weight = baggage.Weight,
            PassengerName = baggage.PassengerName,
            FlightNumber = baggage.FlightNumber,
            Status = baggage.Status.ToString(),
            IsDelivered = baggage.IsDelivered,
            TrackingNumber = baggage.TrackingNumber
        };
    }
}
