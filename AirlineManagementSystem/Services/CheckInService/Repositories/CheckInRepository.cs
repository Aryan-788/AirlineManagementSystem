using CheckInService.Models;
using Microsoft.EntityFrameworkCore;

namespace CheckInService.Repositories;

public interface ICheckInRepository
{
    Task<CheckIn?> GetByIdAsync(int id);
    Task<CheckIn?> GetByBookingIdAsync(int bookingId);
    Task<CheckIn> AddAsync(CheckIn checkIn);
    Task UpdateAsync(CheckIn checkIn);
    Task DeleteAsync(int id);
    Task<IEnumerable<CheckIn>> GetAllAsync();
}

public class CheckInRepository : ICheckInRepository
{
    private readonly CheckInService.Data.CheckInDbContext _context;

    public CheckInRepository(CheckInService.Data.CheckInDbContext context)
    {
        _context = context;
    }

    public async Task<CheckIn?> GetByIdAsync(int id)
    {
        return await _context.CheckIns.FindAsync(id);
    }

    public async Task<CheckIn?> GetByBookingIdAsync(int bookingId)
    {
        return await _context.CheckIns.FirstOrDefaultAsync(c => c.BookingId == bookingId);
    }

    public async Task<CheckIn> AddAsync(CheckIn checkIn)
    {
        _context.CheckIns.Add(checkIn);
        await _context.SaveChangesAsync();
        return checkIn;
    }

    public async Task UpdateAsync(CheckIn checkIn)
    {
        _context.CheckIns.Update(checkIn);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var checkIn = await GetByIdAsync(id);
        if (checkIn != null)
        {
            _context.CheckIns.Remove(checkIn);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<CheckIn>> GetAllAsync()
    {
        return await _context.CheckIns.ToListAsync();
    }
}
