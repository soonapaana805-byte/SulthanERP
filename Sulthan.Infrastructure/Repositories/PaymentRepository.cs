using Microsoft.EntityFrameworkCore;
using Sulthan.Core.Entities;
using Sulthan.Core.Interfaces;
using Sulthan.Infrastructure.Data;

namespace Sulthan.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly RestaurantDbContext _context;

    public PaymentRepository(RestaurantDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Payment>> GetAllAsync()
    {
        return await _context.Payments
            .Include(x => x.Order)
            .Include(x => x.User)
            .OrderByDescending(x => x.PaymentDate)
            .ToListAsync();
    }

    public async Task<Payment?> GetByIdAsync(int id)
    {
        return await _context.Payments
            .Include(x => x.Order)
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<Payment?> GetByOrderIdAsync(int orderId)
    {
        return await _context.Payments
            .Include(x => x.Order)
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.OrderId == orderId);
    }

    public async Task<Payment> AddAsync(Payment payment)
    {
        await _context.Payments.AddAsync(payment);
        await _context.SaveChangesAsync();
        return payment;
    }

    public async Task<Payment> UpdateAsync(Payment payment)
    {
        _context.Payments.Update(payment);
        await _context.SaveChangesAsync();
        return payment;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var payment = await _context.Payments.FindAsync(id);

        if (payment == null)
            return false;

        _context.Payments.Remove(payment);
        await _context.SaveChangesAsync();

        return true;
    }
}