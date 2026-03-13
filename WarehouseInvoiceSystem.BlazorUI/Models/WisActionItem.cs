namespace WarehouseInvoiceSystem.BlazorUI.Models
{
    using MudBlazor;
    using Microsoft.AspNetCore.Components;

    /// <summary>
    /// Factory for building WisDetailActionItem instances by semantic color role.
    /// Construct once per page (passing the component as receiver), then call the
    /// color methods to build items — EventCallback wiring is handled internally.
    /// </summary>
    public class WisActionItem(object receiver)
    {
        // ── Color factories ───────────────────────────────────────────────────

        public WisDetailActionItem Primary(
            string icon, string label, Func<Task> onClick,
            bool dividerBefore = false,
            bool disabled = false, string? disabledReason = null)
        {
            return Build(Color.Default, icon, label, onClick, dividerBefore, disabled, disabledReason);
        }

        public WisDetailActionItem Primary(
            string icon, string label, Action onClick,
            bool dividerBefore = false,
            bool disabled = false, string? disabledReason = null)
        {
            return Primary(icon, label, Wrap(onClick), dividerBefore, disabled, disabledReason);
        }

        public WisDetailActionItem Success(
            string icon, string label, Func<Task> onClick,
            bool dividerBefore = false,
            bool disabled = false, string? disabledReason = null)
        {
            return Build(Color.Success, icon, label, onClick, dividerBefore, disabled, disabledReason);
        }

        public WisDetailActionItem Success(
            string icon, string label, Action onClick,
            bool dividerBefore = false,
            bool disabled = false, string? disabledReason = null)
        {
            return Success(icon, label, Wrap(onClick), dividerBefore, disabled, disabledReason);
        }

        public WisDetailActionItem Info(
            string icon, string label, Func<Task> onClick,
            bool dividerBefore = false,
            bool disabled = false, string? disabledReason = null)
        {
            return Build(Color.Info, icon, label, onClick, dividerBefore, disabled, disabledReason);
        }

        public WisDetailActionItem Info(
            string icon, string label, Action onClick,
            bool dividerBefore = false,
            bool disabled = false, string? disabledReason = null)
        {
            return Info(icon, label, Wrap(onClick), dividerBefore, disabled, disabledReason);
        }

        public WisDetailActionItem Secondary(
            string icon, string label, Func<Task> onClick,
            bool dividerBefore = false,
            bool disabled = false, string? disabledReason = null)
        {
            return Build(Color.Secondary, icon, label, onClick, dividerBefore, disabled, disabledReason);
        }

        public WisDetailActionItem Secondary(
            string icon, string label, Action onClick,
            bool dividerBefore = false,
            bool disabled = false, string? disabledReason = null)
        {
            return Secondary(icon, label, Wrap(onClick), dividerBefore, disabled, disabledReason);
        }

        public WisDetailActionItem Warning(
            string icon, string label, Func<Task> onClick,
            bool dividerBefore = false,
            bool disabled = false, string? disabledReason = null)
        {
            return Build(Color.Warning, icon, label, onClick, dividerBefore, disabled, disabledReason);
        }

        public WisDetailActionItem Warning(
            string icon, string label, Action onClick,
            bool dividerBefore = false,
            bool disabled = false, string? disabledReason = null)
        {
            return Warning(icon, label, Wrap(onClick), dividerBefore, disabled, disabledReason);
        }

        public WisDetailActionItem Error(
            string icon, string label, Func<Task> onClick,
            bool dividerBefore = false,
            bool disabled = false, string? disabledReason = null)
        {
            return Build(Color.Error, icon, label, onClick, dividerBefore, disabled, disabledReason);
        }

        public WisDetailActionItem Error(
            string icon, string label, Action onClick,
            bool dividerBefore = false,
            bool disabled = false, string? disabledReason = null)
        {
            return Error(icon, label, Wrap(onClick), dividerBefore, disabled, disabledReason);
        }

        private static Func<Task> Wrap(Action action)
        {
            return () => { action(); return Task.CompletedTask; };
        }

        // ── Core builder ─────────────────────────────────────────────────────

        private WisDetailActionItem Build(
            Color color, string icon, string label, Func<Task> onClick,
            bool dividerBefore, bool disabled, string? disabledReason)
        {
            return new()
            {
                Color = color,
                Icon = icon,
                Label = label,
                OnClick = EventCallback.Factory.Create(receiver, onClick),
                IsDividerBefore = dividerBefore,
                Disabled = disabled,
                DisabledReason = disabledReason
            };
        }
    }
}