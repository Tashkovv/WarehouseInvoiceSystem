namespace WarehouseInvoiceSystem.BlazorUI.Services
{
    using MudBlazor;
    using WarehouseInvoiceSystem.Application.DTOs.Payment;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.BlazorUI.Components.Dialogs;
    using Microsoft.AspNetCore.Components;

    /// <summary>
    /// Wraps MudBlazor dialog boilerplate so components only write business logic.
    /// Register as scoped in Program.cs.
    /// </summary>
    public class WisDialogService(IDialogService dialogService,
                                  ILocalizationService localizationService,
                                  IPaymentService paymentService)
    {
        private static DialogOptions FormOptions(MaxWidth maxWidth) => new()
        {
            MaxWidth = maxWidth,
            FullWidth = true,
            CloseOnEscapeKey = true
        };

        // ── Form dialogs ────────────────────────────────────────────────────────

        public async Task<TResult?> ShowFormAsync<TDialog, TResult>(
            string title,
            MaxWidth maxWidth = MaxWidth.Medium)
            where TDialog : ComponentBase
        {
            IDialogReference dialog = await dialogService.ShowAsync<TDialog>(title, FormOptions(maxWidth));
            DialogResult? result = await dialog.Result;

            if (result is { Canceled: false, Data: TResult dto })
                return dto;

            return default;
        }

        public async Task<TResult?> ShowFormAsync<TDialog, TResult>(
            string title,
            Action<DialogParameters<TDialog>> configure,
            MaxWidth maxWidth = MaxWidth.Medium)
            where TDialog : ComponentBase
        {
            DialogParameters<TDialog> parameters = new();
            configure(parameters);

            IDialogReference dialog = await dialogService.ShowAsync<TDialog>(title, parameters, FormOptions(maxWidth));
            DialogResult? result = await dialog.Result;

            if (result is { Canceled: false, Data: TResult dto })
                return dto;

            return default;
        }

        // ── Confirmation dialogs ─────────────────────────────────────────────────

        public async Task<bool> ConfirmAsync(
            string title,
            string message,
            string confirmText,
            string? cancelText = null)
        {
            DialogParameters<WisConfirmDialog> parameters = new()
            {
                { x => x.Message, message },
                { x => x.ConfirmText, confirmText },
                { x => x.CancelText, cancelText ?? localizationService.GetString("Cancel") }
            };

            DialogOptions options = new()
            {
                MaxWidth = MaxWidth.ExtraSmall,
                FullWidth = true,
                CloseOnEscapeKey = true
            };

            IDialogReference dialog = await dialogService.ShowAsync<WisConfirmDialog>(title, parameters, options);
            DialogResult? result = await dialog.Result;

            return result is { Canceled: false };
        }

        // ── Payment dialog ───────────────────────────────────────────────────────

        /// <summary>
        /// Opens the PaymentDialog pre-locked to the given invoice, saves the payment,
        /// and returns true if a payment was successfully recorded.
        /// The caller is responsible for showing feedback (snackbar) and reloading its data.
        /// </summary>
        public async Task<bool> ShowPaymentDialogAsync(Guid invoiceId)
        {
            DialogParameters<PaymentDialog> parameters = new();
            parameters.Add(x => x.PreSelectedInvoiceId, invoiceId);

            IDialogReference dialog = await dialogService.ShowAsync<PaymentDialog>(
                localizationService.GetString("RecordPayment"),
                parameters,
                FormOptions(MaxWidth.Medium));

            DialogResult? result = await dialog.Result;

            if (result is not { Canceled: false, Data: CreatePaymentDto dto })
                return false;

            await paymentService.CreatePaymentAsync(dto);
            return true;
        }

        /// <summary>
        /// Opens the PaymentDialog with free invoice selection (no pre-selection),
        /// saves the payment, and returns true if a payment was successfully recorded.
        /// </summary>
        public async Task<bool> ShowPaymentDialogAsync()
        {
            IDialogReference dialog = await dialogService.ShowAsync<PaymentDialog>(
                localizationService.GetString("RecordPayment"),
                FormOptions(MaxWidth.Medium));

            DialogResult? result = await dialog.Result;

            if (result is not { Canceled: false, Data: CreatePaymentDto dto })
                return false;

            await paymentService.CreatePaymentAsync(dto);
            return true;
        }
    }
}