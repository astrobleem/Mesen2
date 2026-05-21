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
		ApplyBrushOverride(app, "NexenStartupTextBrush", profile.StartupTextColor);
		ApplyBrushOverride(app, "NexenStartupPrimaryActionBackgroundBrush", profile.StartupPrimaryActionColor);

		app.Resources["NexenSetupTitleFontSize"] = profile.SetupTitleFontSize;
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
}
