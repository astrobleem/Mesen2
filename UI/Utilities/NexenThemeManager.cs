using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using Nexen.Config;

namespace Nexen.Utilities;

public static class NexenThemeManager {
	public static ThemeVariant ResolveThemeVariant(NexenTheme theme) {
		return theme == NexenTheme.Dark ? ThemeVariant.Dark : ThemeVariant.Light;
	}

	public static void ApplyTheme(Application app, NexenTheme theme) {
		if (app is null) {
			return;
		}

		ThemeVariant newTheme = ResolveThemeVariant(theme);
		if (app.RequestedThemeVariant != newTheme) {
			app.RequestedThemeVariant = newTheme;
		}

		ConfigManager.ActiveTheme = theme;
	}

	public static void ApplyTheme(Application app, PreferencesConfig preferences) {
		if (app is null) {
			return;
		}

		ThemeProfile profile = preferences.GetActiveThemeProfile() ?? ThemeProfile.CreateDefault("Default", preferences.Theme);
		ApplyTheme(app, profile.Theme);

		ApplyBrushOverride(app, "NexenStartupWindowBackgroundBrush", profile.StartupWindowBackgroundColor);
		ApplyBrushOverride(app, "NexenStartupSurfaceBackgroundBrush", profile.StartupSurfaceBackgroundColor);
		ApplyBrushOverride(app, "NexenStartupBorderBrush", profile.StartupBorderColor);
		ApplyBrushOverride(app, "NexenStartupCardBackgroundBrush", profile.StartupCardBackgroundColor);
		ApplyBrushOverride(app, "NexenStartupCardCheckedBackgroundBrush", profile.StartupCardCheckedBackgroundColor);
		ApplyBrushOverride(app, "NexenStartupTextBrush", profile.StartupTextColor);
		ApplyBrushOverride(app, "NexenStartupMutedTextBrush", profile.StartupMutedTextColor);
		ApplyBrushOverride(app, "NexenStartupActionBorderBrush", profile.StartupActionBorderColor);
		ApplyBrushOverride(app, "NexenStartupPrimaryActionBackgroundBrush", profile.StartupPrimaryActionColor);
		ApplyBrushOverride(app, "NexenStartupDividerBrush", profile.StartupDividerColor);
		ApplyBrushOverride(app, "NexenSetupSubtitleBrush", profile.SetupSubtitleColor);

		ApplyColorOverride(app, "NexenMenuBackground", profile.MenuBackgroundColor);
		ApplyColorOverride(app, "NexenMenuBackgroundHighlight", profile.MenuBackgroundHighlightColor);
		ApplyColorOverride(app, "NexenMenuBorderHighlight", profile.MenuBorderHighlightColor);
		ApplyColorOverride(app, "NexenMenuSeparatorBackground", profile.MenuSeparatorBackgroundColor);
		ApplyColorOverride(app, "SettingsTabStripBackgroundColor", profile.SettingsTabStripBackgroundColor);
		ApplyColorOverride(app, "ThemeAccentColor", profile.ThemeAccentColor);
		ApplyColorOverride(app, "HighlightColor", profile.HighlightColor);
		ApplyDerivedAccentVariants(app, profile.ThemeAccentColor);

		app.Resources["NexenSetupTitleFontSize"] = profile.SetupTitleFontSize;
		app.Resources["NexenSetupSubtitleFontSize"] = profile.SetupSubtitleFontSize;
		app.Resources["NexenSetupPrimaryActionFontSize"] = profile.SetupPrimaryActionFontSize;
	}

	private static void ApplyBrushOverride(Application app, string resourceKey, string colorValue) {
		try {
			Color color = Color.Parse(colorValue);
			app.Resources[resourceKey] = new SolidColorBrush(color);
		} catch {
			// Ignore invalid color values to keep startup resilient when importing malformed profiles.
		}
	}

	private static void ApplyColorOverride(Application app, string resourceKey, string colorValue) {
		try {
			app.Resources[resourceKey] = Color.Parse(colorValue);
		} catch {
			// Ignore invalid color values to keep runtime resilient when importing malformed profiles.
		}
	}

	private static void ApplyDerivedAccentVariants(Application app, string accentColorValue) {
		try {
			Color accent = Color.Parse(accentColorValue);
			app.Resources["ThemeAccentColor2"] = new Color(0x99, accent.R, accent.G, accent.B);
			app.Resources["ThemeAccentColor3"] = new Color(0x66, accent.R, accent.G, accent.B);
			app.Resources["ThemeAccentColor4"] = new Color(0x33, accent.R, accent.G, accent.B);
		} catch {
			// Ignore invalid accent values to keep runtime resilient when importing malformed profiles.
		}
	}
}
