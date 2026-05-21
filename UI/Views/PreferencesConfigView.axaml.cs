using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.VisualTree;
using Nexen.Config;
using Nexen.Controls;
using Nexen.Interop;
using Nexen.Localization;
using Nexen.Utilities;
using Nexen.ViewModels;
using Nexen.Windows;

namespace Nexen.Views;
public partial class PreferencesConfigView : UserControl {
	public PreferencesConfigView() {
		InitializeComponent();
		UpdateMigrationStatus();
	}

	private void btnResetLagCounter_OnClick(object sender, RoutedEventArgs e) {
		InputApi.ResetLagCounter();
	}

	private void btnChangeStorageFolder_OnClick(object sender, RoutedEventArgs e) {
		ShowSelectFolderWindow();
	}

	private async void ShowSelectFolderWindow() {
		SelectStorageFolderWindow wnd = new();
		if (await wnd.ShowCenteredDialog<bool>(TopLevel.GetTopLevel(this) as Visual)) {
			(TopLevel.GetTopLevel(this) as Window)?.Close();
			ApplicationHelper.GetMainWindow()?.Close();
			ConfigManager.RestartNexen();
		}
	}

	private void UpdateMigrationStatus() {
		var (legacyFiles, gameCount) = GameDataManager.GetCleanupStatus();
		if (lblMigrationStatus is not null) {
			if (legacyFiles > 0) {
				lblMigrationStatus.Text = $"{legacyFiles} legacy file(s) from {gameCount} game(s) can be cleaned up";
			} else {
				lblMigrationStatus.Text = "No legacy files to clean up";
			}
		}
	}

	private async void btnCleanupLegacyData_OnClick(object sender, RoutedEventArgs e) {
		var (legacyFiles, gameCount) = GameDataManager.GetCleanupStatus();
		if (legacyFiles == 0) {
			await NexenMsgBox.Show(
				TopLevel.GetTopLevel(this) as Window,
				"NoLegacyFilesToCleanup",
				MessageBoxButtons.OK,
				MessageBoxIcon.Info
			);
			return;
		}

		var result = await NexenMsgBox.Show(
			TopLevel.GetTopLevel(this) as Window,
			"ConfirmCleanupLegacyData",
			MessageBoxButtons.OKCancel,
			MessageBoxIcon.Warning,
			legacyFiles.ToString(),
			gameCount.ToString()
		);

		if (result == DialogResult.OK) {
			int cleaned = GameDataManager.CleanupMigratedLegacyFiles();
			UpdateMigrationStatus();

			await NexenMsgBox.Show(
				TopLevel.GetTopLevel(this) as Window,
				"CleanupComplete",
				MessageBoxButtons.OK,
				MessageBoxIcon.Info,
				cleaned.ToString()
			);
		}
	}

	private PreferencesConfigViewModel? GetViewModel() {
		return DataContext as PreferencesConfigViewModel;
	}

	private async void btnApplyThemeProfile_OnClick(object sender, RoutedEventArgs e) {
		PreferencesConfigViewModel? model = GetViewModel();
		if (model is null) {
			return;
		}

		model.ActivateSelectedThemeProfile();
		await NexenMsgBox.Show(TopLevel.GetTopLevel(this) as Window, "ThemeProfileApplied", MessageBoxButtons.OK, MessageBoxIcon.Info);
	}

	private async void btnSaveThemeProfile_OnClick(object sender, RoutedEventArgs e) {
		PreferencesConfigViewModel? model = GetViewModel();
		if (model is null) {
			return;
		}

		model.SaveCurrentToSelectedThemeProfile();
		await NexenMsgBox.Show(TopLevel.GetTopLevel(this) as Window, "ThemeProfileSaved", MessageBoxButtons.OK, MessageBoxIcon.Info);
	}

	private async void btnDeleteThemeProfile_OnClick(object sender, RoutedEventArgs e) {
		PreferencesConfigViewModel? model = GetViewModel();
		if (model is null) {
			return;
		}

		bool removed = model.DeleteSelectedThemeProfile();
		if (!removed) {
			await NexenMsgBox.Show(TopLevel.GetTopLevel(this) as Window, "ThemeProfileDeleteFailed", MessageBoxButtons.OK, MessageBoxIcon.Info);
		}
	}

	private async void btnRenameThemeProfile_OnClick(object sender, RoutedEventArgs e) {
		PreferencesConfigViewModel? model = GetViewModel();
		if (model is null) {
			return;
		}

		if (!model.RenameSelectedThemeProfile()) {
			await NexenMsgBox.Show(TopLevel.GetTopLevel(this) as Window, "ThemeProfileRenameFailed", MessageBoxButtons.OK, MessageBoxIcon.Info);
		}
	}

	private async void btnDuplicateThemeProfile_OnClick(object sender, RoutedEventArgs e) {
		PreferencesConfigViewModel? model = GetViewModel();
		if (model is null) {
			return;
		}

		if (!model.DuplicateSelectedThemeProfile()) {
			await NexenMsgBox.Show(TopLevel.GetTopLevel(this) as Window, "ThemeProfileDuplicateFailed", MessageBoxButtons.OK, MessageBoxIcon.Info);
		}
	}

	private async void btnResetThemeProfileDefaults_OnClick(object sender, RoutedEventArgs e) {
		PreferencesConfigViewModel? model = GetViewModel();
		if (model is null) {
			return;
		}

		if (model.ResetSelectedThemeProfileToDefaults()) {
			await NexenMsgBox.Show(TopLevel.GetTopLevel(this) as Window, "ThemeProfileResetDefaultsComplete", MessageBoxButtons.OK, MessageBoxIcon.Info);
		} else {
			await NexenMsgBox.Show(TopLevel.GetTopLevel(this) as Window, "ThemeProfileResetDefaultsFailed", MessageBoxButtons.OK, MessageBoxIcon.Info);
		}
	}

	private async void btnImportThemeProfile_OnClick(object sender, RoutedEventArgs e) {
		PreferencesConfigViewModel? model = GetViewModel();
		if (model is null) {
			return;
		}

		string? path = await FileDialogHelper.OpenFile(ConfigManager.HomeFolder, this, "nexen-theme.json", "json");
		if (string.IsNullOrWhiteSpace(path)) {
			return;
		}

		try {
			string json = File.ReadAllText(path);
			ThemeProfileFile? profileFile = (ThemeProfileFile?)JsonSerializer.Deserialize(json, typeof(ThemeProfileFile), NexenSerializerContext.Default);
			if (profileFile?.Profile is null || !profileFile.IsValid()) {
				await NexenMsgBox.Show(TopLevel.GetTopLevel(this) as Window, "ThemeProfileImportInvalid", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			ThemeProfile importProfile = profileFile.Profile;
			ThemeProfile? existing = model.Config.GetThemeProfileByName(importProfile.Name);
			if (existing is not null) {
				DialogResult overwrite = await NexenMsgBox.Show(
					TopLevel.GetTopLevel(this) as Window,
					"ThemeProfileImportConflict",
					MessageBoxButtons.OKCancel,
					MessageBoxIcon.Question,
					importProfile.Name
				);

				if (overwrite != DialogResult.OK) {
					importProfile.Name = model.Config.GenerateUniqueThemeProfileName(importProfile.Name);
				}
			}

			model.UpsertImportedThemeProfile(importProfile, true);
			await NexenMsgBox.Show(TopLevel.GetTopLevel(this) as Window, "ThemeProfileImportComplete", MessageBoxButtons.OK, MessageBoxIcon.Info, importProfile.Name);
		} catch (Exception ex) {
			await NexenMsgBox.Show(TopLevel.GetTopLevel(this) as Window, "ThemeProfileImportFailed", MessageBoxButtons.OK, MessageBoxIcon.Error, ex.Message);
		}
	}

	private async void btnExportThemeProfile_OnClick(object sender, RoutedEventArgs e) {
		PreferencesConfigViewModel? model = GetViewModel();
		if (model is null) {
			return;
		}

		ThemeProfile? profile = model.GetSelectedThemeProfile();
		if (profile is null) {
			await NexenMsgBox.Show(TopLevel.GetTopLevel(this) as Window, "ThemeProfileExportNoSelection", MessageBoxButtons.OK, MessageBoxIcon.Info);
			return;
		}

		string suggested = profile.Name.Replace(' ', '-').ToLowerInvariant() + ".nexen-theme.json";
		string? path = await FileDialogHelper.SaveFile(ConfigManager.HomeFolder, suggested, this, "nexen-theme.json", "json");
		if (string.IsNullOrWhiteSpace(path)) {
			return;
		}

		ThemeProfileFile profileFile = new ThemeProfileFile { Profile = profile };
		string json = JsonSerializer.Serialize(profileFile, typeof(ThemeProfileFile), NexenSerializerContext.Default);
		File.WriteAllText(path, json);
	}

	private async void btnPickThemeBackgroundColor_OnClick(object sender, RoutedEventArgs e) {
		await PickThemeProfileColor(profile => profile.StartupWindowBackgroundColor, (model, colorHex) => {
			model.SetSelectedProfileColors(colorHex, model.SelectedProfileStartupTextColor, model.SelectedProfileStartupPrimaryActionColor);
		});
	}

	private async void btnPickThemeTextColor_OnClick(object sender, RoutedEventArgs e) {
		await PickThemeProfileColor(profile => profile.StartupTextColor, (model, colorHex) => {
			model.SetSelectedProfileColors(model.SelectedProfileStartupBackgroundColor, colorHex, model.SelectedProfileStartupPrimaryActionColor);
		});
	}

	private async void btnPickThemePrimaryActionColor_OnClick(object sender, RoutedEventArgs e) {
		await PickThemeProfileColor(profile => profile.StartupPrimaryActionColor, (model, colorHex) => {
			model.SetSelectedProfileColors(model.SelectedProfileStartupBackgroundColor, model.SelectedProfileStartupTextColor, colorHex);
		});
	}

	private async Task PickThemeProfileColor(Func<ThemeProfile, string> colorSelector, Action<PreferencesConfigViewModel, string> updateAction) {
		PreferencesConfigViewModel? model = GetViewModel();
		ThemeProfile? profile = model?.GetSelectedThemeProfile();
		if (model is null || profile is null) {
			return;
		}

		Color startColor = ParseColorOrDefault(colorSelector(profile), Colors.White);
		ColorPickerWindow wnd = new ColorPickerWindow() {
			DataContext = new ColorPickerViewModel() { Color = startColor }
		};

		if (await wnd.ShowCenteredDialog<bool>(TopLevel.GetTopLevel(this) as Visual) && wnd.DataContext is ColorPickerViewModel colorVm) {
			updateAction(model, ToHexArgb(colorVm.Color));
		}
	}

	private static Color ParseColorOrDefault(string colorValue, Color fallback) {
		try {
			return Color.Parse(colorValue);
		} catch {
			return fallback;
		}
	}

	private static string ToHexArgb(Color color) {
		return $"#{color.A:x2}{color.R:x2}{color.G:x2}{color.B:x2}";
	}
}
