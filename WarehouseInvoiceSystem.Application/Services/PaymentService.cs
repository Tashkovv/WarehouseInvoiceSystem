namespace WarehouseInvoiceSystem.Application.Services
{
    using WarehouseInvoiceSystem.Application.DTOs.Payment;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Enums;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Domain.Queries;
    using WarehouseInvoiceSystem.Domain.Queries.Common;
    using WarehouseInvoiceSystem.Domain.Queries.Results;

    public class PaymentService(IPaymentRepository paymentRepository,
                                IInvoiceRepository invoiceRepository,
                                IInvoiceService invoiceService,
                                IInventoryTransactionRepository transactionRepository,
                                ILocalizationService localizationService) : IPaymentService
    {
        public async Task<IEnumerable<PaymentDto>> GetAllPaymentsAsync(CancellationToken ct = default)
        {
            IEnumerable<Payment> payments = await paymentRepository.GetAllAsync(ct);
            IEnumerable<PaymentDto> paymentDtos = payments.Select(MapToDto);
            return paymentDtos;
        }

        public async Task<PagedResult<PaymentDto>> GetPagedAsync(GetPaymentsQuery query, CancellationToken ct = default)
        {
            PagedResult<Payment> result = await paymentRepository.GetPagedAsync(query, ct);
            return new PagedResult<PaymentDto>
            {
                Items = [.. result.Items.Select(MapToDto)],
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }

        public async Task<IEnumerable<PaymentDto>> GetPaymentsByInvoiceAsync(Guid invoiceId, CancellationToken ct = default)
        {
            IEnumerable<Payment> payments = await paymentRepository.GetByInvoiceIdAsync(invoiceId, ct);
            IEnumerable<PaymentDto> paymentDtos = payments.Select(MapToDto);
            return paymentDtos;
        }

        public async Task<PaymentDto?> GetPaymentByIdAsync(Guid id, CancellationToken ct = default)
        {
            Payment? payment = await paymentRepository.GetByIdAsync(id, ct);
            PaymentDto? paymentDto = payment == null ? null : MapToDto(payment);
            return paymentDto;
        }

        public async Task CreatePaymentAsync(CreatePaymentDto createDto)
        {
            // Validate invoice exists
            Invoice? invoice = await invoiceRepository.GetByIdAsync(createDto.InvoiceId)
                ?? throw new KeyNotFoundException($"Invoice with ID {createDto.InvoiceId} not found");

            // Validate payment amount doesn't exceed remaining balance
            decimal remainingBalance = invoice.TotalAmount - invoice.AmountPaid;
            if (createDto.Amount > remainingBalance)
                throw new InvalidOperationException(
                    $"Payment amount ({createDto.Amount:C}) exceeds remaining balance ({remainingBalance:C})");

            // Create payment
            Payment payment = new Payment
            {
                InvoiceId = createDto.InvoiceId,
                PaymentDate = createDto.PaymentDate,
                Amount = createDto.Amount,
                PaymentMethod = createDto.PaymentMethod,
                ReferenceNumber = createDto.ReferenceNumber,
                Notes = createDto.Notes,
                RecordedBy = createDto.RecordedBy
            };

            await paymentRepository.CreateAsync(payment);

            // Update invoice paid amount and status
            bool wasDraft = invoice.Status == InvoiceStatus.Draft;

            invoice.AmountPaid += createDto.Amount;

            if (invoice.AmountPaid >= invoice.TotalAmount)
                invoice.Status = InvoiceStatus.Paid;
            else if (invoice.AmountPaid > 0)
                invoice.Status = InvoiceStatus.PartiallyPaid;

            try
            {
                await invoiceRepository.UpdateAsync(invoice);

                if (wasDraft)
                    await invoiceService.CreateInventoryTransactionsIfNeededAsync(invoice);
            }
            catch (Exception)
            {
                // Invoice update or inventory creation failed — delete the payment record
                // so the DB doesn't have an orphaned payment with no corresponding invoice change.
                await paymentRepository.DeleteAsync(payment.Id);
                throw;
            }
        }

        public async Task UpdatePaymentAsync(Guid id, UpdatePaymentDto updateDto)
        {
            Payment? payment = await paymentRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Payment with ID {id} not found");
            Invoice? invoice = await invoiceRepository.GetByIdAsync(payment.InvoiceId)
                ?? throw new KeyNotFoundException($"Invoice with ID {payment.InvoiceId} not found");

            // Calculate the difference in payment amount
            decimal oldAmount = payment.Amount;
            decimal newAmount = updateDto.Amount;
            decimal difference = newAmount - oldAmount;

            // Validate new total doesn't exceed invoice total
            decimal newTotalPaid = invoice.AmountPaid + difference;
            if (newTotalPaid > invoice.TotalAmount)
                throw new InvalidOperationException(
                    $"Updated payment would exceed invoice total. Maximum allowed: {invoice.TotalAmount - (invoice.AmountPaid - oldAmount):C}");

            // Update payment
            payment.PaymentDate = updateDto.PaymentDate;
            payment.Amount = updateDto.Amount;
            payment.PaymentMethod = updateDto.PaymentMethod;
            payment.ReferenceNumber = updateDto.ReferenceNumber;
            payment.Notes = updateDto.Notes;

            await paymentRepository.UpdateAsync(payment);

            // Update invoice paid amount and status
            invoice.AmountPaid = newTotalPaid;

            if (invoice.AmountPaid >= invoice.TotalAmount)
            {
                invoice.Status = InvoiceStatus.Paid;
            }
            else if (invoice.AmountPaid > 0)
            {
                invoice.Status = InvoiceStatus.PartiallyPaid;
            }
            else
            {
                invoice.Status = InvoiceStatus.Confirmed;
            }

            await invoiceRepository.UpdateAsync(invoice);
        }

        public async Task UpdateNotesAsync(Guid id, string? notes, CancellationToken ct = default)
        {
            Payment? payment = await paymentRepository.GetByIdAsync(id, ct)
                ?? throw new KeyNotFoundException($"Payment with ID {id} not found");
            payment.Notes = notes;
            await paymentRepository.UpdateAsync(payment);
        }

        public async Task<bool> DeletePaymentAsync(Guid id)
        {
            // Load payment and invoice
            var (payment, invoice) = await LoadPaymentAndInvoiceAsync(id);
            if (payment == null)
                return false;

            if (invoice != null)
            {
                await UpdateInvoiceAfterPaymentRemovalAsync(invoice, payment);
            }

            return await paymentRepository.DeleteAsync(id);
        }

        private async Task<(Payment? Payment, Invoice? Invoice)> LoadPaymentAndInvoiceAsync(Guid id)
        {
            Payment? payment = await paymentRepository.GetByIdAsync(id);
            if (payment == null)
                return (null, null);

            Invoice? invoice = payment.Invoice;
            invoice ??= await invoiceRepository.GetByIdAsync(payment.InvoiceId);

            return (payment, invoice);
        }

        private async Task UpdateInvoiceAfterPaymentRemovalAsync(Invoice invoice, Payment payment)
        {
            // Adjust the paid amount
            invoice.AmountPaid -= payment.Amount;

            if (invoice.AmountPaid <= 0)
            {
                invoice.AmountPaid = 0;

                // Check whether transactions exist for this invoice.
                bool hasTransactions = await transactionRepository
                    .HasTransactionsForDocumentAsync(invoice.Id, "Invoice");

                if (hasTransactions)
                {
                    bool invoiceWasNeverConfirmed = (invoice.Status is InvoiceStatus.Paid or InvoiceStatus.PartiallyPaid)
                                                && !await InvoiceHadConfirmedStatusAsync(invoice);

                    if (invoiceWasNeverConfirmed)
                    {
                        // Reverse the stock movements — invoice never formally left Draft
                        string reason = $"{localizationService.GetString("PaymentsRemovedFromInvoice")} {invoice.InvoiceNumber}";
                        await invoiceService.CreateReverseTransactionsIfNeeded(invoice, reason);

                        invoice.Status = InvoiceStatus.Draft;
                    }
                    else
                    {
                        // Invoice was formally Confirmed before payments; stock correctly moved.
                        // Revert to Confirmed (awaiting payment again).
                        invoice.Status = InvoiceStatus.Confirmed;
                    }
                }
                else
                {
                    // No transactions at all — invoice was Draft and had only a payment.
                    invoice.Status = InvoiceStatus.Draft;
                }
            }
            else
            {
                // Still has remaining paid amount — keep as PartiallyPaid.
                invoice.Status = InvoiceStatus.PartiallyPaid;
            }

            await invoiceRepository.UpdateAsync(invoice);
        }

        public async Task<IEnumerable<PaymentDto>> GetRecentAsync(int count, CancellationToken ct = default)
        {
            IEnumerable<Payment> payments = await paymentRepository.GetRecentAsync(count, ct);
            return payments.Select(MapToDto);
        }

        private async Task<bool> InvoiceHadConfirmedStatusAsync(Invoice invoice)
        {
            IEnumerable<Payment> remainingPayments =
                await paymentRepository.GetByInvoiceIdAsync(invoice.Id);

            // remainingPayments at this point already excludes the soft-deleted one
            // because GetByInvoiceIdAsync filters DeletedOn == null.
            if (remainingPayments.Any())
                return true; // still has payments — treat as "was properly sent"

            return false;
        }

        private static PaymentDto MapToDto(Payment payment)
        {
            PaymentDto dto = new()
            {
                Id = payment.Id,
                InvoiceId = payment.InvoiceId,
                InvoiceNumber = payment.Invoice?.InvoiceNumber ?? string.Empty,
                PaymentDate = payment.PaymentDate,
                Amount = payment.Amount,
                PaymentMethod = payment.PaymentMethod,
                ReferenceNumber = payment.ReferenceNumber,
                Notes = payment.Notes,
                RecordedBy = payment.RecordedBy,
                CreatedAt = payment.CreatedAt
            };

            return dto;
        }
    }
}