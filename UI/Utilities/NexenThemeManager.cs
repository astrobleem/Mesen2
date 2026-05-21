using Avalonia;
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
}
