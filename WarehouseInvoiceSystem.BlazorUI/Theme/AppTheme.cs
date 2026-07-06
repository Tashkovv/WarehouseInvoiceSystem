namespace WarehouseInvoiceSystem.BlazorUI.Theme
{
    using MudBlazor;

    public static class AppTheme
    {
        public static PaletteLight LightPalette { get; } = new()
        {
            Primary = "#1E40AF",
            Secondary = "#64748B",
            Success = "#10B981",
            Warning = "#F59E0B",
            Error = "#EF4444",
            Info = "#3B82F6",
            Background = "#F8FAFC",
            Surface = "#FFFFFF",
            AppbarBackground = "#1E40AF",
            DrawerBackground = "#FFFFFF",
            TextPrimary = "#0F172A",
            TextSecondary = "#475569",
        };

        public static PaletteDark DarkPalette { get; } = new()
        {
            Primary = "#3B82F6",
            Secondary = "#94A3B8",
            Success = "#34D399",
            Warning = "#FBBF24",
            Error = "#F87171",
            Info = "#60A5FA",
            Background = "#0F172A",
            Surface = "#1E293B",
            AppbarBackground = "#1E293B",
            DrawerBackground = "#1E293B",
            TextPrimary = "#F1F5F9",
            TextSecondary = "#94A3B8",
        };

        public static Typography AppTypography { get; } = new()
        {
            Default = new DefaultTypography()
            {
                FontFamily = new[] { "Inter", "Roboto", "Helvetica", "Arial", "sans-serif" },
                FontSize = "1rem",
                LineHeight = "1.5"
            },
            H1 = new H1Typography()
            {
                FontSize = "2rem",
                FontWeight = "700",
                LineHeight = "1.2"
            },
            H2 = new H2Typography()
            {
                FontSize = "1.75rem",
                FontWeight = "600",
                LineHeight = "1.3"
            },
            H3 = new H3Typography()
            {
                FontSize = "1.5rem",
                FontWeight = "600",
                LineHeight = "1.3"
            },
            H4 = new H4Typography()
            {
                FontSize = "1.25rem",
                FontWeight = "600",
                LineHeight = "1.4"
            },
            H5 = new H5Typography()
            {
                FontSize = "1.125rem",
                FontWeight = "600",
                LineHeight = "1.4"
            },
            H6 = new H6Typography()
            {
                FontSize = "1rem",
                FontWeight = "600",
                LineHeight = "1.4"
            },
            Body1 = new Body1Typography()
            {
                FontSize = "1rem",
                LineHeight = "1.5"
            },
            Body2 = new Body2Typography()
            {
                FontSize = "0.875rem",
                LineHeight = "1.5"
            }
        };

        public static MudTheme Theme { get; } = new()
        {
            PaletteLight = LightPalette,
            PaletteDark = DarkPalette,
            Typography = AppTypography,
            LayoutProperties = new LayoutProperties()
            {
                DefaultBorderRadius = "8px"
            }
        };
    }
}
