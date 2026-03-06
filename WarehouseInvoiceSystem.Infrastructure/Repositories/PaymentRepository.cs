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
                .Where(p => p.DeletedOn == null)
                .Include(p => p.Invoice)
                    .ThenInclude(i => i.Company)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return payments;
        }

        public async Task<IEnumerable<Payment>> GetByInvoiceIdAsync(Guid invoiceId)
        {
            IEnumerable<Payment> payments = await context.Payments
                .Include(p => p.Invoice)
                .Where(p => p.InvoiceId == invoiceId && p.DeletedOn == null)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return payments;
        }

        public async Task<Payment?> GetByIdAsync(Guid id)
        {
            return await context.Payments
                .Include(p => p.Invoice)
                    .ThenInclude(i => i.Company)
                .FirstOrDefaultAsync(p => p.Id == id && p.DeletedOn == null);
        }

        public async Task<Payment> CreateAsync(Payment payment)
        {
            payment.CreatedAt = DateTime.UtcNow;
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

        public async Task<bool> DeleteAsync(Guid id)
        {
            Payment? payment = await context.Payments.IgnoreQueryFilters()
                                                     .FirstOrDefaultAsync(p => p.Id == id);
            if (payment == null)
                return false;

            payment.DeletedOn = DateTime.UtcNow;
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<decimal> GetTotalPaymentsByInvoiceAsync(Guid invoiceId)
        {
            decimal total = await context.Payments
                .Where(p => p.InvoiceId == invoiceId && p.DeletedOn == null)
                .SumAsync(p => p.Amount);

            return total;
        }
    }
}
