using FlightService.Caching;
using FlightService.DTOs;
using FlightService.Models;
using FlightService.Repositories;
using Shared.Events;
using Shared.Models;
using Shared.RabbitMQ;

namespace FlightService.Services;

public interface IFlightService
{
    Task<FlightDto> CreateFlightAsync(CreateFlightDto dto);
    Task<FlightDto> GetFlightAsync(int id);
    Task<FlightDto> UpdateFlightAsync(int id, UpdateFlightDto dto);
    Task DeleteFlightAsync(int id);
    Task<IEnumerable<FlightDto>> SearchFlightsAsync(string source, string destination, DateTime departureDate);
    Task<IEnumerable<FlightDto>> GetAllFlightsAsync();
    Task DelayFlightAsync(int flightId, DateTime newDepartureTime);
    Task CancelFlightAsync(int flightId);
    Task AssignGateAsync(int flightId, string gate);
    Task AssignAircraftAsync(int flightId, string aircraft);
    Task AssignCrewAsync(int flightId, string crew);
    Task BookSeatAsync(int flightId, string seatClass, int count);
}

public class FlightService : IFlightService
{
    private readonly IFlightRepository _repository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ICacheService _cacheService;
    private readonly ILogger<FlightService> _logger;

    public FlightService(
        IFlightRepository repository,
        IEventPublisher eventPublisher,
        ICacheService cacheService,
        ILogger<FlightService> logger)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<FlightDto> CreateFlightAsync(CreateFlightDto dto)
    {
        var flight = new Flight
        {
            FlightNumber = dto.FlightNumber,
            Source = dto.Source,
            Destination = dto.Destination,
            DepartureTime = dto.DepartureTime,
            ArrivalTime = dto.ArrivalTime,
            Aircraft = dto.Aircraft,
            TotalSeats = dto.TotalSeats,
            AvailableSeats = dto.TotalSeats,
            EconomySeats = dto.EconomySeats,
            BusinessSeats = dto.BusinessSeats,
            FirstSeats = dto.FirstSeats,
            EconomyPrice = dto.EconomyPrice,
            BusinessPrice = dto.BusinessPrice,
            FirstClassPrice = dto.FirstClassPrice,
            Status = FlightStatus.Scheduled,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(flight);
        return MapToDto(flight);
    }

    public async Task<FlightDto> GetFlightAsync(int id)
    {
        var flight = await _repository.GetByIdAsync(id);
        if (flight == null)
            throw new KeyNotFoundException($"Flight {id} not found");

        return MapToDto(flight);
    }

    public async Task<FlightDto> UpdateFlightAsync(int id, UpdateFlightDto dto)
    {
        var flight = await _repository.GetByIdAsync(id);
        if (flight == null)
            throw new KeyNotFoundException($"Flight {id} not found");

        if (dto.DepartureTime.HasValue)
            flight.DepartureTime = dto.DepartureTime.Value;

        if (dto.ArrivalTime.HasValue)
            flight.ArrivalTime = dto.ArrivalTime.Value;

        if (!string.IsNullOrEmpty(dto.Gate))
            flight.Gate = dto.Gate;

        if (!string.IsNullOrEmpty(dto.Aircraft))
            flight.Aircraft = dto.Aircraft;

        if (!string.IsNullOrEmpty(dto.CrewAssignment))
            flight.CrewAssignment = dto.CrewAssignment;

        await _repository.UpdateAsync(flight);
        return MapToDto(flight);
    }

    public async Task DeleteFlightAsync(int id)
    {
        await _repository.DeleteAsync(id);
    }

    public async Task<IEnumerable<FlightDto>> SearchFlightsAsync(string source, string destination, DateTime departureDate)
    {
        // Generate cache key: flight_search_{source}_{destination}_{date}
        var cacheKey = $"flight_search_{source}_{destination}_{departureDate:yyyy-MM-dd}";

        try
        {
            // Try to get from cache
            var cachedResults = await _cacheService.GetAsync<List<FlightDto>>(cacheKey);
            if (cachedResults != null && cachedResults.Count > 0)
            {
                _logger.LogInformation($"Cache hit for search: {cacheKey}");
                return cachedResults;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Cache retrieval failed for key '{cacheKey}': {ex.Message}");
            // Continue to fetch from database
        }

        // Cache miss or error - fetch from database
        var flights = await _repository.SearchAsync(source, destination, departureDate);
        var flightDtos = flights.Select(MapToDto).ToList();

        // Store in cache with 5-minute TTL
        try
        {
            await _cacheService.SetAsync(cacheKey, flightDtos, TimeSpan.FromMinutes(5));
            _logger.LogInformation($"Cached search results for key: {cacheKey}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Cache storage failed for key '{cacheKey}': {ex.Message}");
            // Continue even if caching failed
        }

        return flightDtos;
    }

    public async Task<IEnumerable<FlightDto>> GetAllFlightsAsync()
    {
        var flights = await _repository.GetAllAsync();
        return flights.Select(MapToDto);
    }

    public async Task DelayFlightAsync(int flightId, DateTime newDepartureTime)
    {
        var flight = await _repository.GetByIdAsync(flightId);
        if (flight == null)
            throw new KeyNotFoundException($"Flight {flightId} not found");

        flight.DepartureTime = newDepartureTime;
        flight.Status = FlightStatus.Delayed;

        await _repository.UpdateAsync(flight);

        await _eventPublisher.PublishAsync(new FlightDelayedEvent(
            flightId,
            flight.FlightNumber,
            newDepartureTime,
            DateTime.UtcNow));
    }

    public async Task CancelFlightAsync(int flightId)
    {
        var flight = await _repository.GetByIdAsync(flightId);
        if (flight == null)
            throw new KeyNotFoundException($"Flight {flightId} not found");

        flight.Status = FlightStatus.Cancelled;
        await _repository.UpdateAsync(flight);
    }

    public async Task AssignGateAsync(int flightId, string gate)
    {
        var flight = await _repository.GetByIdAsync(flightId);
        if (flight == null)
            throw new KeyNotFoundException($"Flight {flightId} not found");

        flight.Gate = gate;
        await _repository.UpdateAsync(flight);
    }

    public async Task AssignAircraftAsync(int flightId, string aircraft)
    {
        var flight = await _repository.GetByIdAsync(flightId);
        if (flight == null)
            throw new KeyNotFoundException($"Flight {flightId} not found");

        flight.Aircraft = aircraft;
        await _repository.UpdateAsync(flight);
    }

    public async Task AssignCrewAsync(int flightId, string crew)
    {
        var flight = await _repository.GetByIdAsync(flightId);
        if (flight == null)
            throw new KeyNotFoundException($"Flight {flightId} not found");

        flight.CrewAssignment = crew;
        await _repository.UpdateAsync(flight);
    }

    public async Task BookSeatAsync(int flightId, string seatClass, int count)
    {
        // Generate lock key: lock:flight_{flightId}_seat_{seatClass}
        var lockKey = $"lock:flight_{flightId}_seat_{seatClass}";
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
            var flight = await _repository.GetByIdAsync(flightId);
            if (flight == null)
                throw new KeyNotFoundException($"Flight {flightId} not found");

            if (seatClass == "Economy")
            {
                if (flight.EconomySeats < count)
                    throw new InvalidOperationException($"Not enough Economy seats available. Available: {flight.EconomySeats}, Requested: {count}");
                flight.EconomySeats -= count;
            }
            else if (seatClass == "Business")
            {
                if (flight.BusinessSeats < count)
                    throw new InvalidOperationException($"Not enough Business seats available. Available: {flight.BusinessSeats}, Requested: {count}");
                flight.BusinessSeats -= count;
            }
            else if (seatClass == "First")
            {
                if (flight.FirstSeats < count)
                    throw new InvalidOperationException($"Not enough First Class seats available. Available: {flight.FirstSeats}, Requested: {count}");
                flight.FirstSeats -= count;
            }
            else
            {
                throw new InvalidOperationException($"Invalid seat class: {seatClass}");
            }

            flight.AvailableSeats -= count;
            await _repository.UpdateAsync(flight);
            _logger.LogInformation($"Seat booked: Flight {flightId}, Class: {seatClass}, Count: {count}");
        }
        finally
        {
            // Always release the lock
            await _cacheService.ReleaseLockAsync(lockKey);
            _logger.LogDebug($"Lock released for {lockKey}");
        }
    }

    private FlightDto MapToDto(Flight flight)
    {
        return new FlightDto
        {
            Id = flight.Id,
            FlightNumber = flight.FlightNumber,
            Source = flight.Source,
            Destination = flight.Destination,
            DepartureTime = flight.DepartureTime,
            ArrivalTime = flight.ArrivalTime,
            Gate = flight.Gate,
            Aircraft = flight.Aircraft,
            Status = flight.Status.ToString(),
            TotalSeats = flight.TotalSeats,
            AvailableSeats = flight.AvailableSeats,
            EconomySeats = flight.EconomySeats,
            BusinessSeats = flight.BusinessSeats,
            FirstSeats = flight.FirstSeats,
            EconomyPrice = flight.EconomyPrice,
            BusinessPrice = flight.BusinessPrice,
            FirstClassPrice = flight.FirstClassPrice
        };
    }
}
