using FlightService.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace FlightService.Repositories;

public interface IFlightRepository
{
    Task<Flight?> GetByIdAsync(int id);
    Task<Flight?> GetByFlightNumberAsync(string flightNumber);
    Task<Flight> AddAsync(Flight flight);
    Task UpdateAsync(Flight flight);
    Task DeleteAsync(int id);
    Task<IEnumerable<Flight>> GetAllAsync();
    Task<IEnumerable<Flight>> SearchAsync(string source, string destination, DateTime departureDate);

    // FlightSchedule methods
    Task<FlightSchedule?> GetScheduleByIdAsync(int id);
    Task<IEnumerable<FlightSchedule>> GetSchedulesByFlightIdAsync(int flightId);
    Task<FlightSchedule> AddScheduleAsync(FlightSchedule schedule);
    Task UpdateScheduleAsync(FlightSchedule schedule);
    Task DeleteScheduleAsync(int id);
    Task<IEnumerable<FlightSchedule>> SearchSchedulesAsync(string source, string destination, DateTime departureDate);
    Task<IEnumerable<FlightSchedule>> GetAllSchedulesAsync();
    Task<IEnumerable<FlightSchedule>> GetExpiredSchedulesAsync();
}

public class FlightRepository : IFlightRepository
{
    private readonly FlightService.Data.FlightDbContext _context;

    public FlightRepository(FlightService.Data.FlightDbContext context)
    {
        _context = context;
    }

    public async Task<Flight?> GetByIdAsync(int id)
    {
        return await _context.Flights.FindAsync(id);
    }

    public async Task<Flight?> GetByFlightNumberAsync(string flightNumber)
    {
        return await _context.Flights.FirstOrDefaultAsync(f => f.FlightNumber == flightNumber);
    }

    public async Task<Flight> AddAsync(Flight flight)
    {
        _context.Flights.Add(flight);
        await _context.SaveChangesAsync();
        return flight;
    }

    public async Task UpdateAsync(Flight flight)
    {
        _context.Flights.Update(flight);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var flight = await GetByIdAsync(id);
        if (flight != null)
        {
            _context.Flights.Remove(flight);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Flight>> GetAllAsync()
    {
        return await _context.Flights.ToListAsync();
    }

    public async Task<IEnumerable<Flight>> SearchAsync(string source, string destination, DateTime departureDate)
    {
        return await _context.Flights
            .Where(f => f.Source == source && 
                        f.Destination == destination && 
                        f.DepartureTime.Date == departureDate.Date)
            .ToListAsync();
    }

    public async Task<FlightSchedule?> GetScheduleByIdAsync(int id)
    {
        return await _context.FlightSchedules
            .Include(s => s.Flight)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<IEnumerable<FlightSchedule>> GetSchedulesByFlightIdAsync(int flightId)
    {
        return await _context.FlightSchedules
            .Include(s => s.Flight)
            .Where(s => s.FlightId == flightId)
            .ToListAsync();
    }

    public async Task<FlightSchedule> AddScheduleAsync(FlightSchedule schedule)
    {
        _context.FlightSchedules.Add(schedule);
        await _context.SaveChangesAsync();
        return schedule;
    }

    public async Task UpdateScheduleAsync(FlightSchedule schedule)
    {
        _context.FlightSchedules.Update(schedule);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteScheduleAsync(int id)
    {
        var schedule = await GetScheduleByIdAsync(id);
        if (schedule != null)
        {
            _context.FlightSchedules.Remove(schedule);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<FlightSchedule>> SearchSchedulesAsync(string source, string destination, DateTime departureDate)
    {
        return await _context.FlightSchedules
            .Include(s => s.Flight)
            .Where(s => s.Flight!.Source == source &&
                        s.Flight.Destination == destination &&
                        s.DepartureTime.Date == departureDate.Date &&
                        s.Status != FlightStatus.Cancelled &&
                        s.DepartureTime > DateTime.UtcNow)
            .ToListAsync();
    }

    public async Task<IEnumerable<FlightSchedule>> GetAllSchedulesAsync()
    {
        return await _context.FlightSchedules
            .Include(s => s.Flight)
            .ToListAsync();
    }

    public async Task<IEnumerable<FlightSchedule>> GetExpiredSchedulesAsync()
    {
        return await _context.FlightSchedules
            .Where(s => s.DepartureTime < DateTime.UtcNow &&
                        s.Status != FlightStatus.Completed &&
                        s.Status != FlightStatus.Cancelled)
            .ToListAsync();
    }
}
