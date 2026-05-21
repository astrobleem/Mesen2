using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Nexen.Config;
using Nexen.Config.Shortcuts;
using Nexen.Localization;
using Nexen.Utilities;
using ReactiveUI.SourceGenerators;

namespace Nexen.ViewModels;
/// <summary>
/// ViewModel for the preferences configuration tab.
/// </summary>
public sealed partial class PreferencesConfigViewModel : DisposableViewModel {
	/// <summary>Gets or sets the current preferences configuration.</summary>
	[Reactive] public partial PreferencesConfig Config { get; set; }

	/// <summary>Gets or sets the original preferences configuration for revert.</summary>
	[Reactive] public partial PreferencesConfig OriginalConfig { get; set; }

	/// <summary>Gets the data storage location path.</summary>
	public string DataStorageLocation { get; }

	/// <summary>Gets a comma-separated list of bundled UI languages.</summary>
	public string AvailableLanguagesSummary => string.Join(", ", ResourceHelper.GetAvailableLanguageDisplayNames());

	/// <summary>Gets whether the current platform is macOS.</summary>
	public bool IsOsx { get; }

	/// <summary>Gets or sets the list of shortcut key bindings.</summary>
	public List<ShortcutKeyInfo> ShortcutKeys { get; set; }

	/// <summary>Gets or sets available theme profile names.</summary>
	[Reactive] public partial List<string> ThemeProfileNames { get; set; }

	/// <summary>Gets or sets the selected theme profile name.</summary>
	[Reactive] public partial string SelectedThemeProfileName { get; set; }

	/// <summary>Gets or sets selected profile startup background color as ARGB hex.</summary>
	[Reactive] public partial string SelectedProfileStartupBackgroundColor { get; set; }

	/// <summary>Gets or sets selected profile startup text color as ARGB hex.</summary>
	[Reactive] public partial string SelectedProfileStartupTextColor { get; set; }

	/// <summary>Gets or sets selected profile primary action color as ARGB hex.</summary>
	[Reactive] public partial string SelectedProfileStartupPrimaryActionColor { get; set; }

	/// <summary>Gets or sets selected profile startup background preview brush.</summary>
	[Reactive] public partial IBrush SelectedProfileStartupBackgroundBrush { get; set; }

	/// <summary>Gets or sets selected profile startup text preview brush.</summary>
	[Reactive] public partial IBrush SelectedProfileStartupTextBrush { get; set; }

	/// <summary>Gets or sets selected profile startup primary action preview brush.</summary>
	[Reactive] public partial IBrush SelectedProfileStartupPrimaryActionBrush { get; set; }

	/// <summary>Gets or sets target name for profile rename.</summary>
	[Reactive] public partial string RenameThemeProfileName { get; set; }

	/// <summary>Gets or sets target name for profile duplication.</summary>
	[Reactive] public partial string DuplicateThemeProfileName { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="PreferencesConfigViewModel"/> class.
	/// </summary>
	public PreferencesConfigViewModel() {
		Config = ConfigManager.Config.Preferences;
		OriginalConfig = Config.Clone();
		ThemeProfileNames = [];
		SelectedThemeProfileName = "";
		SelectedProfileStartupBackgroundColor = "";
		SelectedProfileStartupTextColor = "";
		SelectedProfileStartupPrimaryActionColor = "";
		SelectedProfileStartupBackgroundBrush = Brushes.Transparent;
		SelectedProfileStartupTextBrush = Brushes.Transparent;
		SelectedProfileStartupPrimaryActionBrush = Brushes.Transparent;
		RenameThemeProfileName = "";
		DuplicateThemeProfileName = "";
		RefreshThemeProfiles();

		IsOsx = OperatingSystem.IsMacOS();
		DataStorageLocation = ConfigManager.HomeFolder;

		EmulatorShortcut[] displayOrder = new EmulatorShortcut[] {
			EmulatorShortcut.FastForward,
			EmulatorShortcut.ToggleFastForward,
			EmulatorShortcut.Rewind,
			EmulatorShortcut.ToggleRewind,
			EmulatorShortcut.RewindTenSecs,
			EmulatorShortcut.RewindOneMin,

			EmulatorShortcut.Pause,
			EmulatorShortcut.Reset,
			EmulatorShortcut.PowerCycle,
			EmulatorShortcut.ReloadRom,
			EmulatorShortcut.PowerOff,
			EmulatorShortcut.Exit,

			EmulatorShortcut.ToggleRecordVideo,
			EmulatorShortcut.ToggleRecordAudio,
			EmulatorShortcut.ToggleRecordMovie,

			EmulatorShortcut.TakeScreenshot,
			EmulatorShortcut.RunSingleFrame,

			EmulatorShortcut.SetScale1x,
			EmulatorShortcut.SetScale2x,
			EmulatorShortcut.SetScale3x,
			EmulatorShortcut.SetScale4x,
			EmulatorShortcut.SetScale5x,
			EmulatorShortcut.SetScale6x,
			EmulatorShortcut.SetScale7x,
			EmulatorShortcut.SetScale8x,
			EmulatorShortcut.SetScale9x,
			EmulatorShortcut.SetScale10x,
			EmulatorShortcut.ToggleFullscreen,

			EmulatorShortcut.ToggleDebugInfo,
			EmulatorShortcut.ToggleFps,
			EmulatorShortcut.ToggleGameTimer,
			EmulatorShortcut.ToggleFrameCounter,
			EmulatorShortcut.ToggleAlwaysOnTop,
			EmulatorShortcut.ToggleCheats,
			EmulatorShortcut.ToggleOsd,

			EmulatorShortcut.ToggleBgLayer1,
			EmulatorShortcut.ToggleBgLayer2,
			EmulatorShortcut.ToggleBgLayer3,
			EmulatorShortcut.ToggleBgLayer4,
			EmulatorShortcut.ToggleSprites1,
			EmulatorShortcut.ToggleSprites2,
			EmulatorShortcut.EnableAllLayers,

			EmulatorShortcut.ToggleLagCounter,
			EmulatorShortcut.ResetLagCounter,

			EmulatorShortcut.ToggleAudio,
			EmulatorShortcut.IncreaseVolume,
			EmulatorShortcut.DecreaseVolume,

			EmulatorShortcut.PreviousTrack,
			EmulatorShortcut.NextTrack,

			EmulatorShortcut.MaxSpeed,
			EmulatorShortcut.IncreaseSpeed,
			EmulatorShortcut.DecreaseSpeed,

			EmulatorShortcut.OpenFile,

			EmulatorShortcut.InputBarcode,
			EmulatorShortcut.LoadTape,
			EmulatorShortcut.RecordTape,
			EmulatorShortcut.StopRecordTape,

			// Infinite save state system (replaces slot-based system)
			EmulatorShortcut.QuickSaveTimestamped,
			EmulatorShortcut.OpenSaveStatePicker,
			EmulatorShortcut.SaveDesignatedSlot,
			EmulatorShortcut.LoadDesignatedSlot,
			EmulatorShortcut.SaveDesignatedSlot2,
			EmulatorShortcut.LoadDesignatedSlot2,
			EmulatorShortcut.SaveDesignatedSlot3,
			EmulatorShortcut.LoadDesignatedSlot3,
			EmulatorShortcut.SaveStateToFile,
			EmulatorShortcut.SaveStateDialog,
			EmulatorShortcut.LoadStateFromFile,
			EmulatorShortcut.LoadStateDialog,
			EmulatorShortcut.LoadLastSession
		};

		Dictionary<EmulatorShortcut, ShortcutKeyInfo> shortcuts = new Dictionary<EmulatorShortcut, ShortcutKeyInfo>();
		foreach (ShortcutKeyInfo shortcut in Config.ShortcutKeys) {
			shortcuts[shortcut.Shortcut] = shortcut;
		}

		ShortcutKeys = [];
		for (int i = 0; i < displayOrder.Length; i++) {
			if (shortcuts.TryGetValue(displayOrder[i], out var shortcut)) {
				ShortcutKeys.Add(shortcut);
			}
		}

		if (Design.IsDesignMode) {
			return;
		}

		AddDisposable(ReactiveHelper.RegisterRecursiveObserver(Config, (s, e) => {
			Config.ApplyConfig();
			PreferencesConfig.UpdateTheme();
			RefreshSelectedThemeProfileDetails();
		}));
	}

	public void RefreshThemeProfiles() {
		Config.EnsureThemeProfiles();
		ThemeProfileNames = Config.ThemeProfiles.Select(profile => profile.Name).ToList();
		if (string.IsNullOrWhiteSpace(SelectedThemeProfileName) || !ThemeProfileNames.Contains(SelectedThemeProfileName)) {
			SelectedThemeProfileName = Config.ActiveThemeProfileName;
		}

		RefreshSelectedThemeProfileDetails();
	}

	public ThemeProfile? GetSelectedThemeProfile() {
		return Config.GetThemeProfileByName(SelectedThemeProfileName);
	}

	public void ActivateSelectedThemeProfile() {
		Config.SetActiveThemeProfile(SelectedThemeProfileName);
		RefreshThemeProfiles();
	}

	public void SaveCurrentToSelectedThemeProfile() {
		Config.SaveCurrentToProfile(SelectedThemeProfileName);
		RefreshThemeProfiles();
	}

	public bool DeleteSelectedThemeProfile() {
		bool removed = Config.DeleteThemeProfile(SelectedThemeProfileName);
		RefreshThemeProfiles();
		return removed;
	}

	public bool RenameSelectedThemeProfile() {
		bool renamed = Config.RenameThemeProfile(SelectedThemeProfileName, RenameThemeProfileName);
		if (renamed) {
			SelectedThemeProfileName = RenameThemeProfileName.Trim();
		}

		RefreshThemeProfiles();
		return renamed;
	}

	public bool DuplicateSelectedThemeProfile() {
		bool duplicated = Config.DuplicateThemeProfile(SelectedThemeProfileName, DuplicateThemeProfileName);
		RefreshThemeProfiles();
		return duplicated;
	}

	public void UpsertImportedThemeProfile(ThemeProfile profile, bool activateProfile) {
		Config.UpsertThemeProfile(profile, activateProfile);
		RefreshThemeProfiles();
	}

	public void SetSelectedProfileColors(string background, string text, string primaryAction) {
		ThemeProfile? selectedProfile = GetSelectedThemeProfile();
		if (selectedProfile is null) {
			return;
		}

		selectedProfile.StartupWindowBackgroundColor = background;
		selectedProfile.StartupTextColor = text;
		selectedProfile.StartupPrimaryActionColor = primaryAction;
		Config.SetActiveThemeProfile(selectedProfile.Name);
		RefreshThemeProfiles();
	}

	private void RefreshSelectedThemeProfileDetails() {
		ThemeProfile? selectedProfile = GetSelectedThemeProfile();
		if (selectedProfile is null) {
			SelectedProfileStartupBackgroundColor = "";
			SelectedProfileStartupTextColor = "";
			SelectedProfileStartupPrimaryActionColor = "";
			SelectedProfileStartupBackgroundBrush = Brushes.Transparent;
			SelectedProfileStartupTextBrush = Brushes.Transparent;
			SelectedProfileStartupPrimaryActionBrush = Brushes.Transparent;
			return;
		}

		SelectedProfileStartupBackgroundColor = selectedProfile.StartupWindowBackgroundColor;
		SelectedProfileStartupTextColor = selectedProfile.StartupTextColor;
		SelectedProfileStartupPrimaryActionColor = selectedProfile.StartupPrimaryActionColor;
		SelectedProfileStartupBackgroundBrush = CreatePreviewBrush(selectedProfile.StartupWindowBackgroundColor);
		SelectedProfileStartupTextBrush = CreatePreviewBrush(selectedProfile.StartupTextColor);
		SelectedProfileStartupPrimaryActionBrush = CreatePreviewBrush(selectedProfile.StartupPrimaryActionColor);
		RenameThemeProfileName = selectedProfile.Name;
	}

	private static IBrush CreatePreviewBrush(string colorValue) {
		try {
			Color color = Color.Parse(colorValue);
			return new SolidColorBrush(color);
		} catch {
			return Brushes.Transparent;
		}
	}
}
