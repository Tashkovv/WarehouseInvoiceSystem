namespace WarehouseInvoiceSystem.Infrastructure.Repositories
{
    using Microsoft.EntityFrameworkCore;
    using System.Collections.Generic;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Enums;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Infrastructure.Data;

    public class InvoiceRepository(ApplicationDbContext context) : IInvoiceRepository
    {
        public async Task<IEnumerable<Invoice>> GetAllAsync()
        {
            return await context.Invoices
                .Where(i => i.DeletedOn == null)
                .Include(i => i.Company)
                .Include(i => i.LineItems)
                  .ThenInclude(li => li.Product)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Invoice>> GetByCompanyIdAsync(Guid companyId)
        {
            return await context.Invoices
                .Where(i => i.CompanyId == companyId && i.DeletedOn == null)
                .Include(i => i.Company)
                .Include(i => i.LineItems)
                  .ThenInclude(li => li.Product)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Invoice>> GetByTypeAsync(InvoiceType type)
        {
            return await context.Invoices
                .Where(i => i.Type == type && i.DeletedOn == null)
                .Include(i => i.Company)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Invoice>> GetByStatusAsync(InvoiceStatus status)
        {
            return await context.Invoices
                .Where(i => i.DeletedOn == null && i.Status == status)
                .Include(i => i.Company)
                .Include(i => i.LineItems)
                  .ThenInclude(li => li.Product)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Invoice>> GetOverdueInvoicesAsync()
        {
            DateTime today = DateTime.UtcNow.Date;
            return await context.Invoices
                .Where(i => i.DeletedOn == null &&
                            i.DueDate < today &&
                            i.Status != InvoiceStatus.Paid &&
                            i.Status != InvoiceStatus.Cancelled)
                .Include(i => i.Company)
                .OrderBy(i => i.DueDate)
                .ToListAsync();
        }

        public async Task<Invoice?> GetByIdAsync(Guid id)
        {
            return await context.Invoices
                .Where(i => i.DeletedOn == null)
                .Include(i => i.Company)
                .Include(i => i.LineItems)
                  .ThenInclude(li => li.Product)
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber)
        {
            return await context.Invoices
                .Include(i => i.Company)
                .Include(i => i.LineItems)
                  .ThenInclude(li => li.Product)
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.DeletedOn == null && i.InvoiceNumber == invoiceNumber);
        }

        public async Task<IEnumerable<InvoiceLine>> GetLineItemsByProductIdAsync(Guid productId)
        {
            return await context.InvoiceLines
                .Where(li => li.ProductId == productId &&
                             li.DeletedOn == null &&
                             li.Invoice.DeletedOn == null &&
                             li.Invoice.Status != InvoiceStatus.Cancelled)
                .Include(li => li.Invoice)
                    .ThenInclude(i => i.Company)
                .Include(li => li.Invoice)
                    .ThenInclude(i => i.Warehouse)
                .OrderByDescending(li => li.Invoice.IssueDate)
                .ToListAsync();
        }

        public async Task<Invoice> CreateAsync(Invoice invoice)
        {
            invoice.CreatedAt = DateTime.UtcNow;
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

        public async Task<bool> DeleteAsync(Guid id)
        {
            Invoice? invoice = await context.Invoices.IgnoreQueryFilters()
                                                     .FirstOrDefaultAsync(i => i.Id == id);
            if (invoice == null)
            {
                return false;
            }

            invoice.DeletedOn = DateTime.UtcNow;
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await context.Invoices.AnyAsync(i => i.Id == id && i.DeletedOn == null);
        }

        public async Task<string> GenerateInvoiceNumberAsync(InvoiceType type)
        {
            string prefix = type == InvoiceType.Receivable ? "INV" : "BILL";
            int year = DateTime.UtcNow.Year;
            int month = DateTime.UtcNow.Month;

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

        public async Task<(int total, int paid, int unpaid, int overdue)> GetPayableInvoiceCountsAsync()
        {
            DateTime today = DateTime.UtcNow.Date;
            IQueryable<Invoice> invoices = context.Invoices.Where(i => i.DeletedOn == null);

            int total = await invoices.CountAsync();
            int paid = await invoices.CountAsync(i => i.Status == InvoiceStatus.Paid);
            int unpaid = await invoices.CountAsync(i => i.Type == InvoiceType.Payable &&
                                                          i.Status != InvoiceStatus.Paid &&
                                                          i.Status != InvoiceStatus.Cancelled);
            int overdue = await invoices.CountAsync(i => i.DueDate < today &&
                                                          i.Status != InvoiceStatus.Paid &&
                                                          i.Status != InvoiceStatus.Cancelled);

            return (total, paid, unpaid, overdue);
        }

        public async Task<(decimal totalAmount, decimal totalPaid, decimal totalDue)> GetPayableInvoiceTotalsAsync()
        {
            IQueryable<Invoice> invoices = context.Invoices
                .Where(i => i.Status != InvoiceStatus.Cancelled &&
                            i.Type == InvoiceType.Payable &&
                            i.DeletedOn == null);

            decimal totalAmount = await invoices.SumAsync(i => i.TotalAmount);
            decimal totalPaid = await invoices.SumAsync(i => i.AmountPaid);
            decimal totalDue = await invoices.SumAsync(i => i.TotalAmount - i.AmountPaid);

            return (totalAmount, totalPaid, totalDue);
        }
    }
}
