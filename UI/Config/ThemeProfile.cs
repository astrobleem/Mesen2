using System;

namespace Nexen.Config;

public sealed class ThemeProfile {
	public string Name { get; set; } = "Custom Theme";
	public NexenTheme Theme { get; set; } = NexenTheme.Light;
	public string UiFontFamily { get; set; } = "Microsoft Sans Serif";
	public double UiFontSize { get; set; } = 11;
	public string MenuFontFamily { get; set; } = "Segoe UI";
	public double MenuFontSize { get; set; } = 12;
	public string StartupWindowBackgroundColor { get; set; } = "#ff1d130d";
	public string StartupTextColor { get; set; } = "#ffffffff";
	public string StartupPrimaryActionColor { get; set; } = "#ffd47a22";
	public double SetupTitleFontSize { get; set; } = 34;
	public double SetupPrimaryActionFontSize { get; set; } = 34;

	public static ThemeProfile CreateDefault(string name, NexenTheme theme) {
		if (theme == NexenTheme.Dark) {
			return new ThemeProfile {
				Name = name,
				Theme = NexenTheme.Dark,
				UiFontFamily = "Microsoft Sans Serif",
				UiFontSize = 11,
				MenuFontFamily = "Segoe UI",
				MenuFontSize = 12,
				StartupWindowBackgroundColor = "#ff1d130d",
				StartupTextColor = "#ffffffff",
				StartupPrimaryActionColor = "#ffd47a22",
				SetupTitleFontSize = 34,
				SetupPrimaryActionFontSize = 34
			};
		}

		return new ThemeProfile {
			Name = name,
			Theme = NexenTheme.Light,
			UiFontFamily = "Microsoft Sans Serif",
			UiFontSize = 11,
			MenuFontFamily = "Segoe UI",
			MenuFontSize = 12,
			StartupWindowBackgroundColor = "#fff6e7da",
			StartupTextColor = "#ff2a1a10",
			StartupPrimaryActionColor = "#ffeea55d",
			SetupTitleFontSize = 34,
			SetupPrimaryActionFontSize = 34
		};
	}
}

public sealed class ThemeProfileFile {
	public string Format { get; set; } = "nexen-theme-profile";
	public UInt32 Version { get; set; } = 1;
	public ThemeProfile Profile { get; set; } = new ThemeProfile();
}
