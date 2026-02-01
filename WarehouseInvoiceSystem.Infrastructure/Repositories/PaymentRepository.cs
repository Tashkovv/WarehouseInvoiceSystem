namespace WarehouseInvoiceSystem.Infrastructure.Repositories
{
    using Microsoft.EntityFrameworkCore;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Infrastructure.Data;

    public class PaymentRepository(ApplicationDbContext context) : IPaymentRepository
    {
        public async Task<IEnumerable<Payment>> GetAllAsync()
        {
            IEnumerable<Payment> payments = await context.Payments
                .Include(p => p.Invoice)
                    .ThenInclude(i => i.Company)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return payments;
        }

        public async Task<IEnumerable<Payment>> GetByInvoiceIdAsync(int invoiceId)
        {
            IEnumerable<Payment> payments = await context.Payments
                .Include(p => p.Invoice)
                .Where(p => p.InvoiceId == invoiceId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return payments;
        }

        public async Task<Payment?> GetByIdAsync(int id)
        {
            Payment? payment = await context.Payments
                .Include(p => p.Invoice)
                    .ThenInclude(i => i.Company)
                .FirstOrDefaultAsync(p => p.Id == id);

            return payment;
        }

        public async Task<Payment> CreateAsync(Payment payment)
        {
            payment.CreatedAt = DateTime.Now;
            context.Payments.Add(payment);
            await context.SaveChangesAsync();

            // Reload with includes
            Payment? created = await GetByIdAsync(payment.Id);
            return created!;
        }

        public async Task<Payment> UpdateAsync(Payment payment)
        {
            context.Payments.Update(payment);
            await context.SaveChangesAsync();

            // Reload with includes
            Payment? updated = await GetByIdAsync(payment.Id);
            return updated!;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            Payment? payment = await context.Payments.FindAsync(id);
            if (payment == null)
                return false;

            context.Payments.Remove(payment);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<decimal> GetTotalPaymentsByInvoiceAsync(int invoiceId)
        {
            decimal total = await context.Payments
                .Where(p => p.InvoiceId == invoiceId)
                .SumAsync(p => p.Amount);

            return total;
        }
    }
}
