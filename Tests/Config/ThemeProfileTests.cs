using Nexen.Config;
using Xunit;

namespace Nexen.Tests.Config;

public sealed class ThemeProfileTests {
	[Fact]
	public void ThemeProfileFile_IsValid_RejectsUnsupportedFormat() {
		ThemeProfileFile file = new ThemeProfileFile {
			Format = "other-format",
			Version = 1,
			Profile = ThemeProfile.CreateDefault("Test", NexenTheme.Dark)
		};

		Assert.False(file.IsValid());
	}

	[Fact]
	public void ThemeProfileFile_IsValid_RejectsInvalidColor() {
		ThemeProfile profile = ThemeProfile.CreateDefault("Test", NexenTheme.Dark);
		profile.StartupTextColor = "bad-color";

		ThemeProfileFile file = new ThemeProfileFile {
			Format = "nexen-theme-profile",
			Version = 1,
			Profile = profile
		};

		Assert.False(file.IsValid());
	}

	[Fact]
	public void PreferencesConfig_RenameThemeProfile_UpdatesActiveName() {
		PreferencesConfig config = new PreferencesConfig();
		config.SetActiveThemeProfile("Default Dark");

		bool renamed = config.RenameThemeProfile("Default Dark", "Dark Variant A");

		Assert.True(renamed);
		Assert.Equal("Dark Variant A", config.ActiveThemeProfileName);
		Assert.NotNull(config.GetThemeProfileByName("Dark Variant A"));
	}

	[Fact]
	public void PreferencesConfig_DuplicateThemeProfile_AddsNewProfile() {
		PreferencesConfig config = new PreferencesConfig();

		bool duplicated = config.DuplicateThemeProfile("Default Light", "Default Light Copy");

		Assert.True(duplicated);
		Assert.NotNull(config.GetThemeProfileByName("Default Light Copy"));
	}

	[Fact]
	public void PreferencesConfig_SetActiveThemeProfile_AppliesFontsAndTheme() {
		PreferencesConfig config = new PreferencesConfig();
		ThemeProfile profile = new ThemeProfile {
			Name = "MyProfile",
			Theme = NexenTheme.Dark,
			UiFontFamily = "Segoe UI",
			UiFontSize = 13,
			MenuFontFamily = "Consolas",
			MenuFontSize = 14,
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
			SetupTitleFontSize = 36,
			SetupSubtitleFontSize = 17,
			SetupPrimaryActionFontSize = 35
		};
		config.UpsertThemeProfile(profile, false);

		config.SetActiveThemeProfile("MyProfile");

		Assert.Equal(NexenTheme.Dark, config.Theme);
		Assert.Equal("Segoe UI", config.NexenFont.FontFamily);
		Assert.Equal(13, config.NexenFont.FontSize);
		Assert.Equal("Consolas", config.NexenMenuFont.FontFamily);
		Assert.Equal(14, config.NexenMenuFont.FontSize);
	}
}
