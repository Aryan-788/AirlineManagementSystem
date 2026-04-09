using PaymentService.Models;
using Microsoft.EntityFrameworkCore;

namespace PaymentService.Repositories;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(int id);
    Task<Payment?> GetByBookingIdAsync(int bookingId);
    Task<Payment> AddAsync(Payment payment);
    Task UpdateAsync(Payment payment);
    Task<IEnumerable<Payment>> GetAllAsync();
}

public class PaymentRepository : IPaymentRepository
{
    private readonly PaymentService.Data.PaymentDbContext _context;

    public PaymentRepository(PaymentService.Data.PaymentDbContext context)
    {
        _context = context;
    }

    public async Task<Payment?> GetByIdAsync(int id)
    {
        return await _context.Payments.FindAsync(id);
    }

    public async Task<Payment?> GetByBookingIdAsync(int bookingId)
    {
        return await _context.Payments.FirstOrDefaultAsync(p => p.BookingId == bookingId);
    }

    public async Task<Payment> AddAsync(Payment payment)
    {
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();
        return payment;
    }

    public async Task UpdateAsync(Payment payment)
    {
        _context.Payments.Update(payment);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Payment>> GetAllAsync()
    {
        return await _context.Payments.ToListAsync();
    }
}
