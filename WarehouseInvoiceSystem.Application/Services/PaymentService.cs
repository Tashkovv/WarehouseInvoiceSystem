namespace WarehouseInvoiceSystem.Application.Services
{
    using WarehouseInvoiceSystem.Application.DTOs.Payment;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.Domain.Interfaces;
    using WarehouseInvoiceSystem.Domain.Invoice.Domain;
    using WarehouseInvoiceSystem.Domain.Invoice.Enums;
    using WarehouseInvoiceSystem.Domain.Payment.Domain;

    public class PaymentService(IPaymentRepository paymentRepository,
                                IInvoiceRepository invoiceRepository) : IPaymentService
    {
        public async Task<IEnumerable<PaymentDto>> GetAllPaymentsAsync()
        {
            IEnumerable<Payment> payments = await paymentRepository.GetAllAsync();
            IEnumerable<PaymentDto> paymentDtos = payments.Select(MapToDto);
            return paymentDtos;
        }

        public async Task<IEnumerable<PaymentDto>> GetPaymentsByInvoiceAsync(int invoiceId)
        {
            IEnumerable<Payment> payments = await paymentRepository.GetByInvoiceIdAsync(invoiceId);
            IEnumerable<PaymentDto> paymentDtos = payments.Select(MapToDto);
            return paymentDtos;
        }

        public async Task<PaymentDto?> GetPaymentByIdAsync(int id)
        {
            Payment? payment = await paymentRepository.GetByIdAsync(id);
            PaymentDto? paymentDto = payment == null ? null : MapToDto(payment);
            return paymentDto;
        }

        public async Task<PaymentDto> CreatePaymentAsync(CreatePaymentDto createDto)
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

            Payment created = await paymentRepository.CreateAsync(payment);

            // Update invoice paid amount and status
            invoice.AmountPaid += created.Amount;

            // Update status based on payment
            if (invoice.AmountPaid >= invoice.TotalAmount)
            {
                invoice.Status = InvoiceStatus.Paid;
            }
            else if (invoice.AmountPaid > 0)
            {
                invoice.Status = InvoiceStatus.PartiallyPaid;
            }

            await invoiceRepository.UpdateAsync(invoice);

            PaymentDto paymentDto = MapToDto(created);
            return paymentDto;
        }

        public async Task<PaymentDto> UpdatePaymentAsync(int id, UpdatePaymentDto updateDto)
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

            Payment updated = await paymentRepository.UpdateAsync(payment);

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
                invoice.Status = InvoiceStatus.Sent; // or whatever the previous status was
            }

            await invoiceRepository.UpdateAsync(invoice);

            PaymentDto paymentDto = MapToDto(updated);
            return paymentDto;
        }

        public async Task<bool> DeletePaymentAsync(int id)
        {
            Payment? payment = await paymentRepository.GetByIdAsync(id);
            if (payment == null)
                return false;

            Invoice? invoice = await invoiceRepository.GetByIdAsync(payment.InvoiceId);
            if (invoice != null)
            {
                // Subtract payment from invoice
                invoice.AmountPaid -= payment.Amount;

                // Update status
                if (invoice.AmountPaid <= 0)
                {
                    invoice.Status = InvoiceStatus.Sent;
                }
                else if (invoice.AmountPaid < invoice.TotalAmount)
                {
                    invoice.Status = InvoiceStatus.PartiallyPaid;
                }

                await invoiceRepository.UpdateAsync(invoice);
            }

            bool deleted = await paymentRepository.DeleteAsync(id);
            return deleted;
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
