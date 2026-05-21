using System;
using Avalonia.Media;

namespace Nexen.Config;

public sealed class ThemeProfile {
	public string Name { get; set; } = "Custom Theme";
	public NexenTheme Theme { get; set; } = NexenTheme.Light;
	public string UiFontFamily { get; set; } = "Microsoft Sans Serif";
	public double UiFontSize { get; set; } = 11;
	public string MenuFontFamily { get; set; } = "Segoe UI";
	public double MenuFontSize { get; set; } = 12;
	public string StartupWindowBackgroundColor { get; set; } = "#ff1d130d";
	public string StartupSurfaceBackgroundColor { get; set; } = "#ff2a1a10";
	public string StartupBorderColor { get; set; } = "#ff6b3f24";
	public string StartupCardBackgroundColor { get; set; } = "#ff2a1a10";
	public string StartupCardCheckedBackgroundColor { get; set; } = "#ffb65e18";
	public string StartupTextColor { get; set; } = "#ffffffff";
	public string StartupMutedTextColor { get; set; } = "#ffefcfb6";
	public string StartupActionBorderColor { get; set; } = "#ffffce87";
	public string StartupPrimaryActionColor { get; set; } = "#ffd47a22";
	public string StartupDividerColor { get; set; } = "#553f2b1f";
	public string SetupSubtitleColor { get; set; } = "#ffefcfb6";
	public double SetupTitleFontSize { get; set; } = 34;
	public double SetupSubtitleFontSize { get; set; } = 16;
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
				StartupSurfaceBackgroundColor = "#ff2a1a10",
				StartupBorderColor = "#ff6b3f24",
				StartupCardBackgroundColor = "#ff2a1a10",
				StartupCardCheckedBackgroundColor = "#ffb65e18",
				StartupTextColor = "#ffffffff",
				StartupMutedTextColor = "#ffefcfb6",
				StartupActionBorderColor = "#ffffce87",
				StartupPrimaryActionColor = "#ffd47a22",
				StartupDividerColor = "#553f2b1f",
				SetupSubtitleColor = "#ffefcfb6",
				SetupTitleFontSize = 34,
				SetupSubtitleFontSize = 16,
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
			StartupSurfaceBackgroundColor = "#fffbeee2",
			StartupBorderColor = "#ffd2a880",
			StartupCardBackgroundColor = "#fffbeee2",
			StartupCardCheckedBackgroundColor = "#ffe28f43",
			StartupTextColor = "#ff2a1a10",
			StartupMutedTextColor = "#ff6a3f20",
			StartupActionBorderColor = "#ff8a5116",
			StartupPrimaryActionColor = "#ffeea55d",
			StartupDividerColor = "#66bf8b66",
			SetupSubtitleColor = "#ff6a3f20",
			SetupTitleFontSize = 34,
			SetupSubtitleFontSize = 16,
			SetupPrimaryActionFontSize = 34
		};
	}

	public static bool IsValidColor(string? value) {
		if (string.IsNullOrWhiteSpace(value)) {
			return false;
		}

		try {
			Color.Parse(value);
			return true;
		} catch {
			return false;
		}
	}
}

public sealed class ThemeProfileFile {
	public string Format { get; set; } = "nexen-theme-profile";
	public UInt32 Version { get; set; } = 1;
	public ThemeProfile Profile { get; set; } = new ThemeProfile();

	public bool IsValid() {
		if (Format != "nexen-theme-profile" || Version != 1) {
			return false;
		}

		if (string.IsNullOrWhiteSpace(Profile.Name)) {
			return false;
		}

		return ThemeProfile.IsValidColor(Profile.StartupWindowBackgroundColor)
			&& ThemeProfile.IsValidColor(Profile.StartupSurfaceBackgroundColor)
			&& ThemeProfile.IsValidColor(Profile.StartupBorderColor)
			&& ThemeProfile.IsValidColor(Profile.StartupCardBackgroundColor)
			&& ThemeProfile.IsValidColor(Profile.StartupCardCheckedBackgroundColor)
			&& ThemeProfile.IsValidColor(Profile.StartupTextColor)
			&& ThemeProfile.IsValidColor(Profile.StartupMutedTextColor)
			&& ThemeProfile.IsValidColor(Profile.StartupActionBorderColor)
			&& ThemeProfile.IsValidColor(Profile.StartupPrimaryActionColor)
			&& ThemeProfile.IsValidColor(Profile.StartupDividerColor)
			&& ThemeProfile.IsValidColor(Profile.SetupSubtitleColor);
	}
}
