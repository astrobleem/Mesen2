# Theme Customization Guide

This guide explains where Nexen theme values are defined, how they are applied at runtime, and how to update them safely.

## Theme Sources

Nexen theme values come from two layers:

1. Base resource dictionaries:
	- UI/Styles/NexenBrandTheme.xaml
	- UI/App.axaml
	- UI/Styles/NexenStyles.xaml
2. User-overridable theme profile values:
	- UI/Config/ThemeProfile.cs
	- UI/Config/PreferencesConfig.cs

## Runtime Theme Flow

Theme resources are applied in this order:

1. Avalonia loads base resources from UI/App.axaml and style includes.
2. UI/Styles/NexenBrandTheme.xaml provides Dark/Light brand dictionaries.
3. UI/Config/PreferencesConfig.cs resolves the active theme profile.
4. UI/Utilities/NexenThemeManager.cs writes profile overrides into Application resources.

This keeps static defaults in XAML while allowing user profile overrides at runtime.

## Key Resource Tokens

Primary startup and splash tokens are in UI/Styles/NexenBrandTheme.xaml:

- NexenStartupWindowBackgroundBrush
- NexenStartupSplashBackgroundBrush
- NexenStartupSplashStatusBrush
- NexenStartupProgressForegroundBrush
- NexenStartupProgressBackgroundBrush

Global typography defaults are in UI/App.axaml:

- NexenFont / NexenFontSize
- NexenMenuFont / NexenMenuFontSize

Touch sizing defaults are in UI/Styles/NexenStyles.xaml through control styles for Button, TextBox, ComboBox, and CheckBox.

## Theme Profile Import and Export

Theme profile import/export uses JSON-backed profile models:

- Schema and validation:
	- UI/Config/ThemeProfile.cs
- Persistence and profile lifecycle:
	- UI/Config/PreferencesConfig.cs
- Settings UI actions:
	- UI/ViewModels/PreferencesConfigViewModel.cs
	- UI/Views/PreferencesConfigView.axaml
	- UI/Views/PreferencesConfigView.axaml.cs

When importing, values are validated and then applied through the same profile pipeline used for in-app edits.

## Updating Default Theme Values Safely

When changing default colors or sizes:

1. Update Dark and Light entries in UI/Styles/NexenBrandTheme.xaml for parity.
2. Keep contrast high for startup text/progress tokens.
3. If changing sizing defaults, update both UI/App.axaml (font tokens) and UI/Styles/NexenStyles.xaml (control metrics).
4. Build and run UI checks in both Dark and Light themes.
5. Verify startup splash readability and progress visibility.

## Language and Theme Interaction

Display language resources are loaded from:

- UI/Localization/resources.en.xml
- UI/Localization/resources.es.xml
- UI/Localization/resources.ja.xml

Language changes in settings now apply immediately without restarting the process.

## Validation Checklist

- Build passes for Release x64.
- Theme profile import/export still works.
- Splash colors stay in brown/orange branding.
- Progress bar remains visible in both themes.
- Default text/menu/control sizes are touch-friendly.
