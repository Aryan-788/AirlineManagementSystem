using FlightService.Caching;
using FlightService.DTOs;
using FlightService.Models;
using FlightService.Repositories;
using Shared.Models;

namespace FlightService.Services;

public interface IFlightScheduleService
{
    Task<FlightScheduleDto> CreateScheduleAsync(CreateScheduleDto dto);
    Task<FlightScheduleDto> GetScheduleAsync(int id);
    Task<FlightScheduleDto> UpdateScheduleAsync(int id, UpdateScheduleDto dto);
    Task DeleteScheduleAsync(int id);
    Task CancelScheduleAsync(int id);
    Task<IEnumerable<FlightScheduleDto>> GetSchedulesByFlightIdAsync(int flightId);
    Task<IEnumerable<FlightScheduleDto>> SearchSchedulesAsync(string source, string destination, DateTime departureDate);
    Task<IEnumerable<FlightScheduleDto>> GetAllSchedulesAsync();
    Task BookScheduleSeatAsync(int scheduleId, string seatClass, int count);
    Task MarkExpiredSchedulesCompletedAsync();
}

public class FlightScheduleService : IFlightScheduleService
{
    private readonly IFlightRepository _repository;
    private readonly ILogger<FlightScheduleService> _logger;
    private readonly ICacheService _cacheService;

    public FlightScheduleService(
        IFlightRepository repository,
        ILogger<FlightScheduleService> logger,
        ICacheService cacheService)
    {
        _repository = repository;
        _logger = logger;
        _cacheService = cacheService;

    }

    public async Task<FlightScheduleDto> CreateScheduleAsync(CreateScheduleDto dto)
    {
        var flight = await _repository.GetByIdAsync(dto.FlightId);
        if (flight == null)
            throw new KeyNotFoundException($"Flight {dto.FlightId} not found");

        var schedule = new FlightSchedule
        {
            FlightId = dto.FlightId,
            DepartureTime = dto.DepartureTime,
            ArrivalTime = dto.ArrivalTime,
            Gate = string.IsNullOrEmpty(dto.Gate) ? flight.Gate : dto.Gate,
            Status = FlightStatus.Scheduled,
            // Copy from DTO or fallback to Flight template defaults
            EconomySeats = dto.EconomySeats > 0 ? dto.EconomySeats : flight.EconomySeats,
            BusinessSeats = dto.BusinessSeats > 0 ? dto.BusinessSeats : flight.BusinessSeats,
            FirstSeats = dto.FirstSeats > 0 ? dto.FirstSeats : flight.FirstSeats,
            TotalSeats = (dto.EconomySeats > 0 ? dto.EconomySeats : flight.EconomySeats)
                       + (dto.BusinessSeats > 0 ? dto.BusinessSeats : flight.BusinessSeats)
                       + (dto.FirstSeats > 0 ? dto.FirstSeats : flight.FirstSeats),
            EconomyPrice = dto.EconomyPrice > 0 ? dto.EconomyPrice : flight.EconomyPrice,
            BusinessPrice = dto.BusinessPrice > 0 ? dto.BusinessPrice : flight.BusinessPrice,
            FirstClassPrice = dto.FirstClassPrice > 0 ? dto.FirstClassPrice : flight.FirstClassPrice,
            CreatedAt = DateTime.UtcNow
        };

        schedule.AvailableSeats = schedule.TotalSeats;

        await _repository.AddScheduleAsync(schedule);

        // Reload with flight navigation
        var saved = await _repository.GetScheduleByIdAsync(schedule.Id);
        return MapToDto(saved!);
    }

    public async Task<FlightScheduleDto> GetScheduleAsync(int id)
    {
        var schedule = await _repository.GetScheduleByIdAsync(id);
        if (schedule == null)
            throw new KeyNotFoundException($"Schedule {id} not found");

        return MapToDto(schedule);
    }

    public async Task<FlightScheduleDto> UpdateScheduleAsync(int id, UpdateScheduleDto dto)
    {
        var schedule = await _repository.GetScheduleByIdAsync(id);
        if (schedule == null)
            throw new KeyNotFoundException($"Schedule {id} not found");

        if (dto.DepartureTime.HasValue)
            schedule.DepartureTime = dto.DepartureTime.Value;

        if (dto.ArrivalTime.HasValue)
            schedule.ArrivalTime = dto.ArrivalTime.Value;

        if (!string.IsNullOrEmpty(dto.Gate))
            schedule.Gate = dto.Gate;

        if (!string.IsNullOrEmpty(dto.Status) && Enum.TryParse<FlightStatus>(dto.Status, out var status))
            schedule.Status = status;

        if (dto.EconomyPrice.HasValue)
            schedule.EconomyPrice = dto.EconomyPrice.Value;

        if (dto.BusinessPrice.HasValue)
            schedule.BusinessPrice = dto.BusinessPrice.Value;

        if (dto.FirstClassPrice.HasValue)
            schedule.FirstClassPrice = dto.FirstClassPrice.Value;

        if (dto.EconomySeats.HasValue)
            schedule.EconomySeats = dto.EconomySeats.Value;

        if (dto.BusinessSeats.HasValue)
            schedule.BusinessSeats = dto.BusinessSeats.Value;

        if (dto.FirstSeats.HasValue)
            schedule.FirstSeats = dto.FirstSeats.Value;

        await _repository.UpdateScheduleAsync(schedule);
        return MapToDto(schedule);
    }

    public async Task DeleteScheduleAsync(int id)
    {
        await _repository.DeleteScheduleAsync(id);
    }

    public async Task CancelScheduleAsync(int id)
    {
        var schedule = await _repository.GetScheduleByIdAsync(id);
        if (schedule == null)
            throw new KeyNotFoundException($"Schedule {id} not found");

        schedule.Status = FlightStatus.Cancelled;
        await _repository.UpdateScheduleAsync(schedule);
    }

    public async Task<IEnumerable<FlightScheduleDto>> GetSchedulesByFlightIdAsync(int flightId)
    {
        var schedules = await _repository.GetSchedulesByFlightIdAsync(flightId);
        return schedules.Select(MapToDto);
    }

    public async Task<IEnumerable<FlightScheduleDto>> SearchSchedulesAsync(string source, string destination, DateTime departureDate)
    {
        var schedules = await _repository.SearchSchedulesAsync(source, destination, departureDate);
        return schedules.Select(MapToDto);
    }

    public async Task<IEnumerable<FlightScheduleDto>> GetAllSchedulesAsync()
    {
        var schedules = await _repository.GetAllSchedulesAsync();
        return schedules.Select(MapToDto);
    }

    public async Task BookScheduleSeatAsync(int scheduleId, string seatClass, int count)
    {
        // Generate lock key: lock:flight_schedule_{scheduleId}_seat_{seatClass}
        var lockKey = $"lock:flight_schedule_{scheduleId}_seat_{seatClass}";
        var lockTtl = TimeSpan.FromMinutes(2);

        // Try to acquire lock
        var lockAcquired = await _cacheService.AcquireLockAsync(lockKey, lockTtl);
        if (!lockAcquired)
        {
            _logger.LogWarning($"Seat lock not acquired for {lockKey}. Seat may be under booking.");
            throw new InvalidOperationException($"Seat {seatClass} is currently being booked. Please try again.");
        }

        try
        {
            var schedule = await _repository.GetScheduleByIdAsync(scheduleId);
            if (schedule == null)
                throw new KeyNotFoundException($"Schedule {scheduleId} not found");

            if (seatClass == "Economy")
            {
                if (schedule.EconomySeats < count)
                    throw new InvalidOperationException($"Not enough Economy seats. Available: {schedule.EconomySeats}, Requested: {count}");
                schedule.EconomySeats -= count;
            }
            else if (seatClass == "Business")
            {
                if (schedule.BusinessSeats < count)
                    throw new InvalidOperationException($"Not enough Business seats. Available: {schedule.BusinessSeats}, Requested: {count}");
                schedule.BusinessSeats -= count;
            }
            else if (seatClass == "First")
            {
                if (schedule.FirstSeats < count)
                    throw new InvalidOperationException($"Not enough First Class seats. Available: {schedule.FirstSeats}, Requested: {count}");
                schedule.FirstSeats -= count;
            }
            else
            {
                throw new InvalidOperationException($"Invalid seat class: {seatClass}");
            }

            schedule.AvailableSeats -= count;
            await _repository.UpdateScheduleAsync(schedule);
            _logger.LogInformation($"Seat booked: Schedule {scheduleId}, Class: {seatClass}, Count: {count}");
        }
        finally
        {
            // Always release the lock
            await _cacheService.ReleaseLockAsync(lockKey);
            _logger.LogDebug($"Lock released for {lockKey}");
        }
    }

    public async Task MarkExpiredSchedulesCompletedAsync()
    {
        var expired = await _repository.GetExpiredSchedulesAsync();
        foreach (var schedule in expired)
        {
            schedule.Status = FlightStatus.Completed;
            await _repository.UpdateScheduleAsync(schedule);
            _logger.LogInformation($"Schedule {schedule.Id} (Flight {schedule.FlightId}) marked as Completed");
        }
    }

    private FlightScheduleDto MapToDto(FlightSchedule schedule)
    {
        return new FlightScheduleDto
        {
            Id = schedule.Id,
            FlightId = schedule.FlightId,
            FlightNumber = schedule.Flight?.FlightNumber ?? "",
            Source = schedule.Flight?.Source ?? "",
            Destination = schedule.Flight?.Destination ?? "",
            Aircraft = schedule.Flight?.Aircraft ?? "",
            DepartureTime = schedule.DepartureTime,
            ArrivalTime = schedule.ArrivalTime,
            Gate = schedule.Gate,
            Status = schedule.Status.ToString(),
            TotalSeats = schedule.TotalSeats,
            AvailableSeats = schedule.AvailableSeats,
            EconomySeats = schedule.EconomySeats,
            BusinessSeats = schedule.BusinessSeats,
            FirstSeats = schedule.FirstSeats,
            EconomyPrice = schedule.EconomyPrice,
            BusinessPrice = schedule.BusinessPrice,
            FirstClassPrice = schedule.FirstClassPrice,
            CreatedAt = schedule.CreatedAt
        };
    }
}
