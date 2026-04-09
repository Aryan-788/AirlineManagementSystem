using AgentService.Models;
using Microsoft.EntityFrameworkCore;

namespace AgentService.Repositories;

public interface IDealerRepository
{
    Task<Dealer?> GetByIdAsync(int id);
    Task<Dealer?> GetByEmailAsync(string email);
    Task<Dealer> AddAsync(Dealer dealer);
    Task UpdateAsync(Dealer dealer);
    Task<IEnumerable<Dealer>> GetAllAsync();
}

public interface IDealerBookingRepository
{
    Task<DealerBooking> AddAsync(DealerBooking dealerBooking);
    Task<IEnumerable<DealerBooking>> GetByDealerIdAsync(int dealerId);
    Task<decimal> GetTotalCommissionAsync(int dealerId);
}

public class DealerRepository : IDealerRepository
{
    private readonly AgentService.Data.AgentDbContext _context;

    public DealerRepository(AgentService.Data.AgentDbContext context)
    {
        _context = context;
    }

    public async Task<Dealer?> GetByIdAsync(int id)
    {
        return await _context.Dealers.FindAsync(id);
    }

    public async Task<Dealer?> GetByEmailAsync(string email)
    {
        return await _context.Dealers.FirstOrDefaultAsync(d => d.DealerEmail == email);
    }

    public async Task<Dealer> AddAsync(Dealer dealer)
    {
        _context.Dealers.Add(dealer);
        await _context.SaveChangesAsync();
        return dealer;
    }

    public async Task UpdateAsync(Dealer dealer)
    {
        _context.Dealers.Update(dealer);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Dealer>> GetAllAsync()
    {
        return await _context.Dealers.ToListAsync();
    }
}

public class DealerBookingRepository : IDealerBookingRepository
{
    private readonly AgentService.Data.AgentDbContext _context;

    public DealerBookingRepository(AgentService.Data.AgentDbContext context)
    {
        _context = context;
    }

    public async Task<DealerBooking> AddAsync(DealerBooking dealerBooking)
    {
        _context.DealerBookings.Add(dealerBooking);
        await _context.SaveChangesAsync();
        return dealerBooking;
    }

    public async Task<IEnumerable<DealerBooking>> GetByDealerIdAsync(int dealerId)
    {
        return await _context.DealerBookings.Where(db => db.DealerId == dealerId).ToListAsync();
    }

    public async Task<decimal> GetTotalCommissionAsync(int dealerId)
    {
        return await _context.DealerBookings
            .Where(db => db.DealerId == dealerId)
            .SumAsync(db => db.Commission);
    }
}
