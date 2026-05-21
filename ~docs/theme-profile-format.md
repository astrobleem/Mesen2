# Theme Profile Format

This document describes the Nexen theme profile import and export JSON schema used by the Preferences UI.

## File Extension

- Preferred extension: `.nexen-theme.json`
- Alternate extension: `.json`

## Root Schema

```json
{
	"format": "nexen-theme-profile",
	"version": 1,
	"profile": {
		"name": "Default Dark",
		"theme": "Dark",
		"uiFontFamily": "Microsoft Sans Serif",
		"uiFontSize": 11,
		"menuFontFamily": "Segoe UI",
		"menuFontSize": 12,
		"startupWindowBackgroundColor": "#ff1d130d",
		"startupTextColor": "#ffffffff",
		"startupPrimaryActionColor": "#ffd47a22",
		"setupTitleFontSize": 34,
		"setupPrimaryActionFontSize": 34
	}
}
```

## Notes

- `format` must be `nexen-theme-profile`.
- `version` is currently `1`.
- Color values use ARGB hex format: `#aarrggbb`.
- `theme` supports `Light` or `Dark`.
- Imported profiles are upserted by `profile.name`.

## Runtime Application

When a profile is activated, Nexen applies:

- Base light/dark theme variant.
- UI font family and size.
- Menu font family and size.
- Startup semantic brush overrides for:
	- `NexenStartupWindowBackgroundBrush`
	- `NexenStartupTextBrush`
	- `NexenStartupPrimaryActionBackgroundBrush`
- Setup typography overrides for:
	- `NexenSetupTitleFontSize`
	- `NexenSetupPrimaryActionFontSize`
