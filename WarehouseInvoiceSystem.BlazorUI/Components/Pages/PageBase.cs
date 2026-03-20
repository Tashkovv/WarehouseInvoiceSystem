namespace WarehouseInvoiceSystem.BlazorUI.Components.Pages
{
    using Microsoft.AspNetCore.Components;
    using MudBlazor;
    using WarehouseInvoiceSystem.Application.Interfaces;
    using WarehouseInvoiceSystem.BlazorUI.Models;
    using WarehouseInvoiceSystem.BlazorUI.Services;
    using WarehouseInvoiceSystem.Domain.Enums;

    public abstract class PageBase : ComponentBase, IDisposable
    {
        [Inject] protected ILocalizationService Localization { get; set; } = default!;
        [Inject] protected NavigationManager Navigation { get; set; } = default!;
        [Inject] protected ISnackbar Snackbar { get; set; } = default!;
        [Inject] protected WisDialogService WisDialog { get; set; } = default!;

        protected readonly CancellationTokenSource _cts = new();
        protected WisActionItem _act = null!;
        protected bool _loading = true;

        protected async Task SafeLoadAsync(Func<Task> work, string? errorKey = null)
        {
            _loading = true;
            try
            {
                await work();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                if (errorKey is not null)
                    Snackbar.Add(Localization.GetString(errorKey), Severity.Error);
            }
            finally
            {
                _loading = false;
            }
        }

        protected override void OnInitialized()
        {
            Localization.OnLanguageChanged += OnLanguageChanged;
            base.OnInitialized();
        }

        protected override Task OnInitializedAsync()
        {
            _act = new WisActionItem(this);
            return base.OnInitializedAsync();
        }

        protected virtual async void OnLanguageChanged()
        {
            await InvokeAsync(StateHasChanged);
        }

        protected void NavigateTo(string url) => Navigation.NavigateTo(url);

        protected WisDetailActionItem NavAction(string icon, string label, string href, Color color = Color.Default) => new()
        {
            Icon = icon,
            Label = label,
            Color = color,
            OnClick = EventCallback.Factory.Create(this, () => Navigation.NavigateTo(href)),
        };

        protected void NotifySuccess(string key) =>
            Snackbar.Add(Localization.GetString(key), Severity.Success);

        protected void NotifyError(string key) =>
            Snackbar.Add(Localization.GetString(key), Severity.Error);

        protected void NotifyWarning(string key) =>
            Snackbar.Add(Localization.GetString(key), Severity.Warning);

        // ── InvoiceStatus helpers ─────────────────────────────────────────────

        protected static Color GetInvoiceStatusColor(InvoiceStatus status) => status switch
        {
            InvoiceStatus.Draft        => Color.Default,
            InvoiceStatus.Sent         => Color.Info,
            InvoiceStatus.PartiallyPaid => Color.Warning,
            InvoiceStatus.Paid         => Color.Success,
            InvoiceStatus.Overdue      => Color.Error,
            InvoiceStatus.Cancelled    => Color.Dark,
            _                          => Color.Default
        };

        protected string GetInvoiceStatusText(InvoiceStatus status) => status switch
        {
            InvoiceStatus.Draft        => Localization.GetString("Draft"),
            InvoiceStatus.Sent         => Localization.GetString("Sent"),
            InvoiceStatus.PartiallyPaid => Localization.GetString("PartiallyPaid"),
            InvoiceStatus.Paid         => Localization.GetString("PaidStatus"),
            InvoiceStatus.Overdue      => Localization.GetString("OverdueStatus"),
            InvoiceStatus.Cancelled    => Localization.GetString("CancelledStatus"),
            _                          => status.ToString()
        };

        // ── PurchaseNoteStatus helpers ────────────────────────────────────────

        protected static Color GetPurchaseNoteStatusColor(PurchaseNoteStatus status) => status switch
        {
            PurchaseNoteStatus.Draft     => Color.Default,
            PurchaseNoteStatus.Pending   => Color.Warning,
            PurchaseNoteStatus.Paid      => Color.Success,
            PurchaseNoteStatus.Cancelled => Color.Error,
            _                            => Color.Default
        };

        protected string GetPurchaseNoteStatusText(PurchaseNoteStatus status) => status switch
        {
            PurchaseNoteStatus.Draft     => Localization.GetString("Draft"),
            PurchaseNoteStatus.Pending   => Localization.GetString("PendingStatus"),
            PurchaseNoteStatus.Paid      => Localization.GetString("PaidStatus"),
            PurchaseNoteStatus.Cancelled => Localization.GetString("CancelledStatus"),
            _                            => status.ToString()
        };

        // ── PaymentMethod helpers ─────────────────────────────────────────────

        protected static Color GetPaymentMethodColor(PaymentMethod method) => method switch
        {
            PaymentMethod.Cash        => Color.Success,
            PaymentMethod.BankTransfer => Color.Primary,
            PaymentMethod.CreditCard  => Color.Secondary,
            PaymentMethod.Other       => Color.Default,
            _                         => Color.Default
        };

        protected string GetPaymentMethodText(PaymentMethod method) => method switch
        {
            PaymentMethod.Cash        => Localization.GetString("Cash"),
            PaymentMethod.BankTransfer => Localization.GetString("BankTransfer"),
            PaymentMethod.CreditCard  => Localization.GetString("CreditCard"),
            PaymentMethod.Other       => Localization.GetString("Other"),
            _                         => method.ToString()
        };

        // ── CompanyType helpers ───────────────────────────────────────────────

        protected static Color GetCompanyTypeColor(CompanyType type) => type switch
        {
            CompanyType.Client => Color.Success,
            CompanyType.Vendor => Color.Warning,
            CompanyType.Both   => Color.Primary,
            _                  => Color.Default
        };

        protected string GetCompanyTypeText(CompanyType type) => type switch
        {
            CompanyType.Client => Localization.GetString("Client"),
            CompanyType.Vendor => Localization.GetString("Vendor"),
            CompanyType.Both   => Localization.GetString("ClientAndVendor"),
            _                  => type.ToString()
        };

        // ── Stock Movement helpers ────────────────────────────────────────────────

        protected static Color GetMovementTypeColor(InventoryTransactionType type) => type switch
        {
            InventoryTransactionType.Inbound => Color.Success,
            InventoryTransactionType.TransferIn => Color.Tertiary,
            InventoryTransactionType.Outbound => Color.Error,
            InventoryTransactionType.TransferOut => Color.Warning,
            InventoryTransactionType.Adjustment => Color.Info,
            InventoryTransactionType.Reversed => Color.Dark,
            _ => Color.Default
        };

        protected static string GetMovementTypeIcon(InventoryTransactionType type) => type switch
        {
            InventoryTransactionType.Inbound => Icons.Material.Filled.ArrowDownward,
            InventoryTransactionType.Outbound => Icons.Material.Filled.ArrowUpward,
            InventoryTransactionType.TransferIn => Icons.Material.Filled.CallReceived,
            InventoryTransactionType.TransferOut => Icons.Material.Filled.CallMade,
            InventoryTransactionType.Adjustment => Icons.Material.Filled.Tune,
            InventoryTransactionType.Reversed => Icons.Material.Filled.Undo,
            _ => Icons.Material.Filled.SwapVert
        };

        protected string GetMovementTypeLabel(InventoryTransactionType type) =>
            Localization.GetString(type.ToString());

        // ── InventoryTransactionType helpers ──────────────────────────────────

        protected static string GetQuantityText(InventoryTransactionType type, decimal quantity) => type switch
        {
            InventoryTransactionType.Inbound => $"+{quantity:N2}",
            InventoryTransactionType.Outbound => $"-{quantity:N2}",
            InventoryTransactionType.TransferIn => $"+{quantity:N2}",
            InventoryTransactionType.TransferOut => $"-{quantity:N2}",
            InventoryTransactionType.Adjustment => quantity > 0 ? $"+{quantity:N2}" : quantity.ToString("N2"),
            InventoryTransactionType.Reversed => quantity > 0 ? $"+{quantity:N2}" : quantity.ToString("N2"),
            _ => quantity.ToString("N2")
        };

        protected static Color GetQuantityColor(InventoryTransactionType type, decimal quantity) => type switch
        {
            InventoryTransactionType.Inbound => Color.Success,
            InventoryTransactionType.Outbound => Color.Error,
            InventoryTransactionType.TransferIn => Color.Tertiary,
            InventoryTransactionType.TransferOut => Color.Warning,
            InventoryTransactionType.Adjustment => quantity > 0 ? Color.Success : Color.Error,
            InventoryTransactionType.Reversed => quantity > 0 ? Color.Success : Color.Error,
            _ => Color.Default
        };

        // ── Confirm-and-execute helper ────────────────────────────────────────

        protected async Task ConfirmAndExecuteAsync(
            string title,
            string message,
            Func<Task> action,
            string successKey,
            string errorKey,
            string confirmLabel,
            string cancelLabel)
        {
            bool confirmed = await WisDialog.ConfirmAsync(title, message, confirmLabel, cancelLabel);
            if (!confirmed) return;

            try
            {
                await action();
                Snackbar.Add(Localization.GetString(successKey), Severity.Success);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Snackbar.Add(Localization.GetString(errorKey), Severity.Error);
            }
        }

        // ── Inline notes editing ──────────────────────────────────────────────

        protected bool _notesEditMode;
        protected bool _notesSaving;
        protected string? _notesEditValue;

        protected void StartNotesEdit(string? currentNotes)
        {
            _notesEditValue = currentNotes;
            _notesEditMode = true;
        }

        protected void CancelNotesEdit()
        {
            _notesEditMode = false;
            _notesEditValue = null;
        }

        protected async Task SaveNotesAsync(Func<string?, Task> saveFunc, string errorKey)
        {
            _notesSaving = true;
            try
            {
                await saveFunc(_notesEditValue);
                _notesEditMode = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating notes: {ex.Message}");
                Snackbar.Add(Localization.GetString(errorKey), Severity.Error);
            }
            finally
            {
                _notesSaving = false;
            }
        }

        // ── Dispose ───────────────────────────────────────────────────────────

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cts.Cancel();
                _cts.Dispose();
                Localization.OnLanguageChanged -= OnLanguageChanged;
            }
        }
    }
}
