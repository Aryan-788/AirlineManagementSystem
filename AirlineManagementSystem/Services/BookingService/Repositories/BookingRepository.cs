using BookingService.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace BookingService.Repositories;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(int id);
    Task<Booking?> GetByPNRAsync(string pnr);
    Task<Booking> AddAsync(Booking booking);
    Task UpdateAsync(Booking booking);
    Task DeleteAsync(int id);
    Task<IEnumerable<Booking>> GetByUserIdAsync(int userId);
    Task<IEnumerable<Booking>> GetByScheduleIdAsync(int scheduleId);
    Task<IEnumerable<string>> GetOccupiedSeatsAsync(int flightId, int? scheduleId);
    Task<IEnumerable<Booking>> GetAllAsync();
}

public class BookingRepository : IBookingRepository
{
    private readonly BookingService.Data.BookingDbContext _context;

    public BookingRepository(BookingService.Data.BookingDbContext context)
    {
        _context = context;
    }

    public async Task<Booking?> GetByIdAsync(int id)
    {
        return await _context.Bookings.FindAsync(id);
    }

    public async Task<Booking?> GetByPNRAsync(string pnr)
    {
        return await _context.Bookings.FirstOrDefaultAsync(b => b.PNR == pnr);
    }

    public async Task<Booking> AddAsync(Booking booking)
    {
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();
        return booking;
    }

    public async Task UpdateAsync(Booking booking)
    {
        _context.Bookings.Update(booking);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var booking = await GetByIdAsync(id);
        if (booking != null)
        {
            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Booking>> GetByUserIdAsync(int userId)
    {
        return await _context.Bookings.Where(b => b.UserId == userId).ToListAsync();
    }

    public async Task<IEnumerable<Booking>> GetByScheduleIdAsync(int scheduleId)
    {
        return await _context.Bookings.Include(b => b.Passengers).Where(b => b.ScheduleId == scheduleId).ToListAsync();
    }

    public async Task<IEnumerable<string>> GetOccupiedSeatsAsync(int flightId, int? scheduleId)
    {
        var query = _context.Bookings
            .Include(b => b.Passengers)
            .Where(b => b.FlightId == flightId && b.Status != BookingStatus.Cancelled);

        if (scheduleId.HasValue)
        {
            query = query.Where(b => b.ScheduleId == scheduleId.Value);
        }

        var bookings = await query.ToListAsync();

        var occupiedSeats = bookings
            .SelectMany(b => b.Passengers)
            .Where(p => p.Status != PassengerStatus.Cancelled && !string.IsNullOrEmpty(p.SeatNumber))
            .Select(p => p.SeatNumber!)
            .Distinct()
            .ToList();

        return occupiedSeats;
    }

    public async Task<IEnumerable<Booking>> GetAllAsync()
    {
        return await _context.Bookings.ToListAsync();
    }
}
