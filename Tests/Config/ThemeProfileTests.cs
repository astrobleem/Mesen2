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
		profile.MenuBackgroundColor = "bad-color";

		ThemeProfileFile file = new ThemeProfileFile {
			Format = "nexen-theme-profile",
			Version = 1,
			Profile = profile
		};

		Assert.False(file.IsValid());
	}

	[Fact]
	public void ThemeProfile_CreateDefault_IncludesCentralizedMenuAndAccentTokens() {
		ThemeProfile dark = ThemeProfile.CreateDefault("Default Dark", NexenTheme.Dark);
		ThemeProfile light = ThemeProfile.CreateDefault("Default Light", NexenTheme.Light);

		Assert.Equal("#55281a10", dark.MenuBackgroundColor);
		Assert.Equal("#80573721", dark.MenuBackgroundHighlightColor);
		Assert.Equal("#ccc87423", dark.ThemeAccentColor);

		Assert.Equal("#f3e5d7", light.MenuBackgroundColor);
		Assert.Equal("#efb57b", light.MenuBackgroundHighlightColor);
		Assert.Equal("#ffc56e21", light.ThemeAccentColor);
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

	[Fact]
	public void PreferencesConfig_GenerateUniqueThemeProfileName_ReturnsCopyNameOnConflict() {
		PreferencesConfig config = new PreferencesConfig();

		string unique = config.GenerateUniqueThemeProfileName("Default Dark");

		Assert.Equal("Default Dark (Copy)", unique);
	}

	[Fact]
	public void PreferencesConfig_GenerateUniqueThemeProfileName_IncrementsCopySuffixWhenNeeded() {
		PreferencesConfig config = new PreferencesConfig();
		config.UpsertThemeProfile(ThemeProfile.CreateDefault("Default Dark (Copy)", NexenTheme.Dark), false);
		config.UpsertThemeProfile(ThemeProfile.CreateDefault("Default Dark (Copy 2)", NexenTheme.Dark), false);

		string unique = config.GenerateUniqueThemeProfileName("Default Dark");

		Assert.Equal("Default Dark (Copy 3)", unique);
	}

	[Fact]
	public void PreferencesConfig_ResetThemeProfileToDefaults_RestoresCanonicalValues() {
		PreferencesConfig config = new PreferencesConfig();
		ThemeProfile dark = ThemeProfile.CreateDefault("Default Dark", NexenTheme.Dark);
		dark.StartupWindowBackgroundColor = "#ff000000";
		dark.StartupPrimaryActionColor = "#ff101010";
		dark.UiFontSize = 99;
		config.UpsertThemeProfile(dark, false);

		bool reset = config.ResetThemeProfileToDefaults("Default Dark");
		ThemeProfile? resetProfile = config.GetThemeProfileByName("Default Dark");

		Assert.True(reset);
		Assert.NotNull(resetProfile);
		Assert.Equal("#ff1d130d", resetProfile!.StartupWindowBackgroundColor);
		Assert.Equal("#ffd47a22", resetProfile.StartupPrimaryActionColor);
		Assert.Equal(11, resetProfile.UiFontSize);
	}

	[Fact]
	public void PreferencesConfig_ApplyThemePresetToProfile_AppliesLightDefaults() {
		PreferencesConfig config = new PreferencesConfig();

		bool applied = config.ApplyThemePresetToProfile("Default Dark", NexenTheme.Light);
		ThemeProfile? profile = config.GetThemeProfileByName("Default Dark");

		Assert.True(applied);
		Assert.NotNull(profile);
		Assert.Equal(NexenTheme.Light, profile!.Theme);
		Assert.Equal("#fff6e7da", profile.StartupWindowBackgroundColor);
		Assert.Equal("#ffeea55d", profile.StartupPrimaryActionColor);
	}

	[Fact]
	public void PreferencesConfig_ApplyThemePresetToProfile_AppliesDarkDefaults() {
		PreferencesConfig config = new PreferencesConfig();
		config.ApplyThemePresetToProfile("Default Light", NexenTheme.Light);

		bool applied = config.ApplyThemePresetToProfile("Default Light", NexenTheme.Dark);
		ThemeProfile? profile = config.GetThemeProfileByName("Default Light");

		Assert.True(applied);
		Assert.NotNull(profile);
		Assert.Equal(NexenTheme.Dark, profile!.Theme);
		Assert.Equal("#ff1d130d", profile.StartupWindowBackgroundColor);
		Assert.Equal("#ffd47a22", profile.StartupPrimaryActionColor);
	}

	[Fact]
	public void PreferencesConfig_GetThemeProfileDivergenceCount_DefaultProfileHasNoDivergence() {
		PreferencesConfig config = new PreferencesConfig();

		int count = config.GetThemeProfileDivergenceCount("Default Dark");

		Assert.Equal(0, count);
	}

	[Fact]
	public void PreferencesConfig_GetThemeProfileDivergenceCount_CustomizedProfileHasDivergence() {
		PreferencesConfig config = new PreferencesConfig();
		ThemeProfile? profile = config.GetThemeProfileByName("Default Dark");
		Assert.NotNull(profile);
		profile!.StartupTextColor = "#ff000000";
		profile.UiFontSize = 13;

		int count = config.GetThemeProfileDivergenceCount("Default Dark");

		Assert.Equal(2, count);
	}

	[Fact]
	public void PreferencesConfig_GetThemeProfileCustomizedTokenNames_ReturnsExpectedTokenNames() {
		PreferencesConfig config = new PreferencesConfig();
		ThemeProfile? profile = config.GetThemeProfileByName("Default Dark");
		Assert.NotNull(profile);
		profile!.StartupTextColor = "#ff000000";
		profile.UiFontSize = 13;

		List<string> tokens = config.GetThemeProfileCustomizedTokenNames("Default Dark");

		Assert.Contains("StartupTextColor", tokens);
		Assert.Contains("UiFontSize", tokens);
		Assert.Equal(2, tokens.Count);
		Assert.Equal("UiFontSize", tokens[0]);
		Assert.Equal("StartupTextColor", tokens[1]);
	}
}
