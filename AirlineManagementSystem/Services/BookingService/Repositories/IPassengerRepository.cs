using BookingService.Data;
using BookingService.Models;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Repositories;

public interface IPassengerRepository
{
    Task<Passenger?> GetPassengerByIdAsync(int passengerId);
    Task<List<Passenger>> GetPassengersByBookingIdAsync(int bookingId);
    Task<Passenger?> GetPassengerByAadharAsync(string aadharCardNo);
    Task AddPassengerAsync(Passenger passenger);
    Task UpdatePassengerAsync(Passenger passenger);
    Task DeletePassengerAsync(int passengerId);
    Task<bool> IsAadharUniqueAsync(string aadharCardNo, int? excludePassengerId = null);
    Task<bool> IsAadharDuplicateInScheduleAsync(string aadharCardNo, int scheduleId, int? excludePassengerId = null);
}

public class PassengerRepository : IPassengerRepository
{
    private readonly BookingDbContext _context;

    public PassengerRepository(BookingDbContext context)
    {
        _context = context;
    }

    public async Task<Passenger?> GetPassengerByIdAsync(int passengerId)
    {
        return await _context.Passengers.FindAsync(passengerId);
    }

    public async Task<List<Passenger>> GetPassengersByBookingIdAsync(int bookingId)
    {
        return await _context.Passengers
            .Where(p => p.BookingId == bookingId)
            .ToListAsync();
    }

    public async Task<Passenger?> GetPassengerByAadharAsync(string aadharCardNo)
    {
        return await _context.Passengers
            .FirstOrDefaultAsync(p => p.AadharCardNo == aadharCardNo);
    }

    public async Task AddPassengerAsync(Passenger passenger)
    {
        await _context.Passengers.AddAsync(passenger);
        await _context.SaveChangesAsync();
    }

    public async Task UpdatePassengerAsync(Passenger passenger)
    {
        _context.Passengers.Update(passenger);
        await _context.SaveChangesAsync();
    }

    public async Task DeletePassengerAsync(int passengerId)
    {
        var passenger = await GetPassengerByIdAsync(passengerId);
        if (passenger != null)
        {
            _context.Passengers.Remove(passenger);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> IsAadharUniqueAsync(string aadharCardNo, int? excludePassengerId = null)
    {
        var query = _context.Passengers.Where(p => p.AadharCardNo == aadharCardNo);
        
        if (excludePassengerId.HasValue)
        {
            query = query.Where(p => p.Id != excludePassengerId.Value);
        }

        return !await query.AnyAsync();
    }

    public async Task<bool> IsAadharDuplicateInScheduleAsync(string aadharCardNo, int scheduleId, int? excludePassengerId = null)
    {
        var query = from p in _context.Passengers
                    join b in _context.Bookings on p.BookingId equals b.Id
                    where p.AadharCardNo == aadharCardNo && b.ScheduleId == scheduleId
                    select p;

        if (excludePassengerId.HasValue)
        {
            query = query.Where(p => p.Id != excludePassengerId.Value);
        }

        return await query.AnyAsync();
    }
}
