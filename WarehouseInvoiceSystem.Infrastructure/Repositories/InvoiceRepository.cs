namespace WarehouseInvoiceSystem.Infrastructure.Repositories
{
    using Microsoft.EntityFrameworkCore;
    using System.Collections.Generic;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Domain.Invoice.Domain;
    using WarehouseInvoiceSystem.Domain.Invoice.Enums;
    using WarehouseInvoiceSystem.Infrastructure.Data;

    public class InvoiceRepository(ApplicationDbContext context) : IInvoiceRepository
    {
        public async Task<IEnumerable<Invoice>> GetAllAsync()
        {
            return await context.Invoices
                .Where(i => i.DeletedOn == null)
                .Include(i => i.Company)
                .Include(i => i.LineItems)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Invoice>> GetByCompanyIdAsync(int companyId)
        {
            return await context.Invoices
                .Include(i => i.Company)
                .Include(i => i.LineItems)
                .Where(i => i.CompanyId == companyId && i.DeletedOn == null)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Invoice>> GetByTypeAsync(InvoiceType type)
        {
            return await context.Invoices
                .Include(i => i.Company)
                .Include(i => i.LineItems)
                .Where(i => i.Type == type)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Invoice>> GetByStatusAsync(InvoiceStatus status)
        {
            return await context.Invoices
                .Include(i => i.Company)
                .Include(i => i.LineItems)
                .Where(i => i.DeletedOn == null && i.Status == status)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Invoice>> GetOverdueInvoicesAsync()
        {
            DateTime today = DateTime.Now.Date;
            return await context.Invoices
                .Include(i => i.Company)
                .Include(i => i.LineItems)
                .Where(i => i.DeletedOn == null &&
                            i.DueDate < today &&
                            i.Status != InvoiceStatus.Paid &&
                            i.Status != InvoiceStatus.Cancelled)
                .OrderBy(i => i.DueDate)
                .ToListAsync();
        }

        public async Task<Invoice?> GetByIdAsync(int id)
        {
            return await context.Invoices
                .Where(i => i.DeletedOn == null)
                .Include(i => i.Company)
                .Include(i => i.LineItems)
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber)
        {
            return await context.Invoices
                .Include(i => i.Company)
                .Include(i => i.LineItems)
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.DeletedOn == null && i.InvoiceNumber == invoiceNumber);
        }

        public async Task<Invoice> CreateAsync(Invoice invoice)
        {
            invoice.CreatedAt = DateTime.Now;
            context.Invoices.Add(invoice);
            await context.SaveChangesAsync();

            // Reload with includes
            return (await GetByIdAsync(invoice.Id))!;
        }

        public async Task<Invoice> UpdateAsync(Invoice invoice)
        {
            context.Invoices.Update(invoice);
            await context.SaveChangesAsync();

            // Reload with includes
            return (await GetByIdAsync(invoice.Id))!;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            Invoice? invoice = await context.Invoices.IgnoreQueryFilters()
                                                     .FirstOrDefaultAsync(i => i.Id == id);
            if (invoice == null)
            {
                return false;
            }

            invoice.DeletedOn = DateTime.Now;
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await context.Invoices.AnyAsync(i => i.Id == id && i.DeletedOn == null);
        }

        public async Task<string> GenerateInvoiceNumberAsync(InvoiceType type)
        {
            string prefix = type == InvoiceType.Receivable ? "INV" : "BILL";
            int year = DateTime.Now.Year;
            int month = DateTime.Now.Month;

            Invoice? lastInvoice = await context.Invoices
                .Where(i => i.Type == type &&
                            i.InvoiceNumber.StartsWith($"{prefix}-{year:D4}{month:D2}"))
                .OrderByDescending(i => i.InvoiceNumber)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastInvoice != null)
            {
                string[] parts = lastInvoice.InvoiceNumber.Split('-');
                if (parts.Length == 2 && int.TryParse(parts[1].AsSpan(6), out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"{prefix}-{year:D4}{month:D2}{nextNumber:D4}";
        }

        public async Task<(int total, int paid, int unpaid, int overdue)> GetInvoiceCountsAsync()
        {
            DateTime today = DateTime.Now.Date;

            int total = await context.Invoices.CountAsync(i => i.DeletedOn == null);
            int paid = await context.Invoices.CountAsync(i => i.Status == InvoiceStatus.Paid && i.DeletedOn == null);
            int unpaid = await context.Invoices.CountAsync(i =>
                i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled && i.DeletedOn == null);
            int overdue = await context.Invoices.CountAsync(i =>
                i.DueDate < today &&
                i.Status != InvoiceStatus.Paid &&
                i.Status != InvoiceStatus.Cancelled &&
                i.DeletedOn == null);

            return (total, paid, unpaid, overdue);
        }

        public async Task<(decimal totalAmount, decimal totalPaid, decimal totalDue)> GetInvoiceTotalsAsync()
        {
            List<Invoice> invoices = await context.Invoices
                .Where(i => i.Status != InvoiceStatus.Cancelled && i.DeletedOn == null)
                .ToListAsync();


            decimal totalAmount = invoices.Sum(i => i.TotalAmount);
            decimal totalPaid = invoices.Sum(i => i.AmountPaid);
            decimal totalDue = invoices.Sum(i => i.TotalAmount - i.AmountPaid);

            return (totalAmount, totalPaid, totalDue);
        }
    }
}
