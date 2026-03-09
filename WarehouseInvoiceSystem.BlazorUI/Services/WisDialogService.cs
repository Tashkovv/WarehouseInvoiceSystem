namespace WarehouseInvoiceSystem.BlazorUI.Services
{
    using MudBlazor;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using Microsoft.AspNetCore.Components;

    /// <summary>
    /// Wraps MudBlazor dialog boilerplate so components only write business logic.
    /// Register as scoped in Program.cs.
    /// </summary>
    public class WisDialogService(IDialogService dialogService,
                                  ILocalizationService localizationService)
    {
        private static readonly DialogOptions DefaultFormOptions = new()
        {
            MaxWidth = MaxWidth.Medium,
            FullWidth = true,
            CloseOnEscapeKey = true
        };

        // ── Form dialogs ────────────────────────────────────────────────────────

        /// <summary>
        /// Opens a dialog with no parameters and returns the result DTO,
        /// or null if the user cancelled.
        /// </summary>
        public async Task<TResult?> ShowFormAsync<TDialog, TResult>(string title)
            where TDialog : ComponentBase
        {
            IDialogReference dialog = await dialogService.ShowAsync<TDialog>(title, DefaultFormOptions);
            DialogResult? result = await dialog.Result;

            if (result is { Canceled: false, Data: TResult dto })
                return dto;

            return default;
        }

        /// <summary>
        /// Opens a dialog, configures its parameters, and returns the result DTO,
        /// or null if the user cancelled.
        /// </summary>
        public async Task<TResult?> ShowFormAsync<TDialog, TResult>(
            string title,
            Action<DialogParameters<TDialog>> configure)
            where TDialog : ComponentBase
        {
            DialogParameters<TDialog> parameters = new();
            configure(parameters);

            IDialogReference dialog = await dialogService.ShowAsync<TDialog>(title, parameters, DefaultFormOptions);
            DialogResult? result = await dialog.Result;

            if (result is { Canceled: false, Data: TResult dto })
                return dto;

            return default;
        }

        // ── Confirmation dialogs ─────────────────────────────────────────────────

        /// <summary>
        /// Shows a confirmation message box. Returns true if the user confirmed.
        /// </summary>
        public async Task<bool> ConfirmAsync(
            string title,
            string message,
            string confirmText,
            string? cancelText = null)
        {
            bool? result = await dialogService.ShowMessageBoxAsync(new MessageBoxOptions
            {
                Title = title,
                Message = message,
                YesText = confirmText,
                CancelText = cancelText ?? localizationService.GetString("Cancel")
            });

            return result == true;
        }
    }
}