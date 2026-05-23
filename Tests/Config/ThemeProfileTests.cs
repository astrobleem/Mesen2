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
		profile.DataGridHeaderForegroundColor = "bad-color";

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
		Assert.Equal("#80573721", dark.ControlPointerOverBackgroundColor);
		Assert.Equal("#c27b38", dark.ControlPressedBorderColor);
		Assert.Equal("#80808080", dark.SettingsTabStripBorderColor);
		Assert.Equal("#ff282828", dark.DockTabStripBackgroundColor);
		Assert.Equal("#40808080", dark.DockTabPointerOverColor);
		Assert.Equal("#ff282828", dark.DockTabActiveBackgroundColor);
		Assert.Equal("#ff505050", dark.DockTabActiveBorderColor);
		Assert.Equal("#e09a52", dark.CheckBoxPointerOverBorderColor);
		Assert.Equal("#9960382a", dark.CheckBoxPressedBackgroundColor);
		Assert.Equal("#c27b38", dark.CheckBoxPressedBorderColor);
		Assert.Equal("#e09a52", dark.RadioButtonPointerOverBorderColor);
		Assert.Equal("#9960382a", dark.RadioButtonPressedBackgroundColor);
		Assert.Equal("#c27b38", dark.RadioButtonPressedBorderColor);
		Assert.Equal("#e09a52", dark.SliderTrackPointerOverColor);
		Assert.Equal("#c27b38", dark.SliderTrackPressedColor);
		Assert.Equal("#2a509f", dark.TextBoxSelectionColor);
		Assert.Equal("#ff333333", dark.TextControlDisabledBackgroundColor);
		Assert.Equal("#181818", dark.ToolTipBackgroundColor);
		Assert.Equal("#ff2a1a10", dark.MenuFlyoutBackgroundColor);
		Assert.Equal("#a87343", dark.MenuFlyoutBorderColor);
		Assert.Equal("#ff2a2a2a", dark.ComboBoxDropDownBackgroundColor);
		Assert.Equal("#ff909090", dark.ComboBoxDropDownBorderColor);
		Assert.Equal("#ff303030", dark.DataGridHeaderBackgroundColor);
		Assert.Equal("#ffdedede", dark.DataGridHeaderForegroundColor);
		Assert.Equal("#ffffffff", dark.DataGridSelectedRowForegroundColor);
		Assert.Equal("#66403020", dark.ListBoxItemHoverBackgroundColor);
		Assert.Equal("#ff6a3f20", dark.ListBoxItemSelectedBackgroundColor);
		Assert.Equal("#ffffffff", dark.ListBoxItemSelectedForegroundColor);
		Assert.Equal("#663d2a1b", dark.TreeViewItemHoverBackgroundColor);
		Assert.Equal("#ff6a3f20", dark.TreeViewItemSelectedBackgroundColor);
		Assert.Equal("#ffffffff", dark.TreeViewItemSelectedForegroundColor);

		Assert.Equal("#f3e5d7", light.MenuBackgroundColor);
		Assert.Equal("#efb57b", light.MenuBackgroundHighlightColor);
		Assert.Equal("#ffc56e21", light.ThemeAccentColor);
		Assert.Equal("#f8d9b8", light.ControlPointerOverBackgroundColor);
		Assert.Equal("#7b4517", light.ControlPressedBorderColor);
		Assert.Equal("#80808080", light.SettingsTabStripBorderColor);
		Assert.Equal("#ffffffff", light.DockTabStripBackgroundColor);
		Assert.Equal("#40808080", light.DockTabPointerOverColor);
		Assert.Equal("#ffffffff", light.DockTabActiveBackgroundColor);
		Assert.Equal("#ff808080", light.DockTabActiveBorderColor);
		Assert.Equal("#9f5c22", light.CheckBoxPointerOverBorderColor);
		Assert.Equal("#e9c098", light.CheckBoxPressedBackgroundColor);
		Assert.Equal("#7b4517", light.CheckBoxPressedBorderColor);
		Assert.Equal("#9f5c22", light.RadioButtonPointerOverBorderColor);
		Assert.Equal("#e9c098", light.RadioButtonPressedBackgroundColor);
		Assert.Equal("#7b4517", light.RadioButtonPressedBorderColor);
		Assert.Equal("#9f5c22", light.SliderTrackPointerOverColor);
		Assert.Equal("#7b4517", light.SliderTrackPressedColor);
		Assert.Equal("#70c0ff", light.TextBoxSelectionColor);
		Assert.Equal("#e0e0e0", light.TextControlDisabledBackgroundColor);
		Assert.Equal("#ffffed", light.ToolTipBackgroundColor);
		Assert.Equal("#fff6ee", light.MenuFlyoutBackgroundColor);
		Assert.Equal("#a87343", light.MenuFlyoutBorderColor);
		Assert.Equal("#ffffffff", light.ComboBoxDropDownBackgroundColor);
		Assert.Equal("#ff909090", light.ComboBoxDropDownBorderColor);
		Assert.Equal("#ffededed", light.DataGridHeaderBackgroundColor);
		Assert.Equal("#ff000000", light.DataGridHeaderForegroundColor);
		Assert.Equal("#ffffffff", light.DataGridSelectedRowForegroundColor);
		Assert.Equal("#fff4ddc7", light.ListBoxItemHoverBackgroundColor);
		Assert.Equal("#ffb56f33", light.ListBoxItemSelectedBackgroundColor);
		Assert.Equal("#ffffffff", light.ListBoxItemSelectedForegroundColor);
		Assert.Equal("#fff2dcc8", light.TreeViewItemHoverBackgroundColor);
		Assert.Equal("#ffb56f33", light.TreeViewItemSelectedBackgroundColor);
		Assert.Equal("#ffffffff", light.TreeViewItemSelectedForegroundColor);
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
