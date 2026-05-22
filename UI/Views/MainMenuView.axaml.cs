using Avalonia;
using System.Collections;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Nexen.Config;
using Nexen.Localization;
using Nexen.Utilities;
using Nexen.ViewModels;

namespace Nexen.Views; 
public partial class MainMenuView : UserControl {
	public Menu MainMenu { get; }
	private readonly MenuItem _mnuLanguageEnglish;
	private readonly MenuItem _mnuLanguageSpanish;
	private readonly MenuItem _mnuLanguageJapanese;

	public MainMenuView() {
		InitializeComponent();
		MainMenu = this.GetControl<Menu>("ActionMenu");
		_mnuLanguageEnglish = this.GetControl<MenuItem>("mnuLanguageEnglish");
		_mnuLanguageSpanish = this.GetControl<MenuItem>("mnuLanguageSpanish");
		_mnuLanguageJapanese = this.GetControl<MenuItem>("mnuLanguageJapanese");

		MainMenu.Closed += (s, e) =>                //When an option is selected in the menu (e.g with enter or mouse click)
													//steal focus away from the menu to ensure pressing e.g left/right goes to the
													//game only and doesn't re-activate the main menu
			ApplicationHelper.GetMainWindow()?.Focus();

		Panel panel = this.GetControl<Panel>("MenuPanel");
		panel.PointerPressed += (s, e) => {
			if (s == panel) {
				//Close the menu when the blank space on the right is clicked
				MainMenu.Close();
				ApplicationHelper.GetMainWindow()?.Focus();
			}
		};

		ResourceHelper.LanguageChanged += ResourceHelper_LanguageChanged;
		RefreshLanguageCheckStates();
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e) {
		ResourceHelper.LanguageChanged -= ResourceHelper_LanguageChanged;
		base.OnDetachedFromVisualTree(e);
	}

	private void InitializeComponent() {
		AvaloniaXamlLoader.Load(this);
	}

	private void ResourceHelper_LanguageChanged(object? sender, System.EventArgs e) {
		RefreshLanguageCheckStates();
	}

	private void RefreshLanguageCheckStates() {
		UiLanguage language = ConfigManager.Config.Preferences.UiLanguage;
		_mnuLanguageEnglish.IsChecked = language == UiLanguage.English;
		_mnuLanguageSpanish.IsChecked = language == UiLanguage.Spanish;
		_mnuLanguageJapanese.IsChecked = language == UiLanguage.Japanese;
	}

	private static void ApplyUiLanguage(UiLanguage language) {
		if (ConfigManager.Config.Preferences.UiLanguage == language) {
			return;
		}

		ConfigManager.Config.Preferences.UiLanguage = language;
		ResourceHelper.LoadResources(PreferencesConfig.GetLanguageCode(language));
		ConfigManager.Config.Save();
	}

	private void mnuLanguageEnglish_Click(object sender, RoutedEventArgs e) {
		ApplyUiLanguage(UiLanguage.English);
		RefreshLanguageCheckStates();
	}

	private void mnuLanguageSpanish_Click(object sender, RoutedEventArgs e) {
		ApplyUiLanguage(UiLanguage.Spanish);
		RefreshLanguageCheckStates();
	}

	private void mnuLanguageJapanese_Click(object sender, RoutedEventArgs e) {
		ApplyUiLanguage(UiLanguage.Japanese);
		RefreshLanguageCheckStates();
	}

	private void mnuTools_Opened(object sender, RoutedEventArgs e) {
		if (DataContext is MainMenuViewModel model) {
			if (model.UpdateNetplayMenu() && e.Source is MenuItem item) {
				//Force a refresh of the tools menu to ensure
				//the "Select controller" submenu gets updated
				IEnumerable? items = item.ItemsSource;
				item.ItemsSource = null;
				item.ItemsSource = items;
			}
		}
	}
}
