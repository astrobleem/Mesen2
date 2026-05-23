# Theme Profile Chrome Customization Manual Test

## Goal

Validate that theme profiles can customize centralized menu/chrome/accent tokens through Settings and import/export.

## Preconditions

- Nexen launches normally.
- Preferences window is accessible.
- At least one theme profile exists.

## Steps

1. Open `Settings -> Preferences -> General`.
2. In `Theme Profiles`, select an existing profile (for example `Default Dark`).
3. Change each of these using the color pickers:
	- Menu Background
	- Menu Highlight
	- Accent Color
	- Control Hover Background
	- Control Pressed Background
	- Control Hover Border
	- Control Pressed Border
	- Sidebar Border Color
	- Dock Tab Strip Background
	- Dock Tab Hover Background
	- Dock Tab Active Background
	- Dock Tab Active Border
4. Click `Apply` for the selected profile.
5. Verify the top menu bar and menu selection visuals reflect the new values.
6. Verify button/combo/repeat hover and pressed visuals reflect the updated control semantic tokens.
7. Verify settings left-tab sidebar border reflects the sidebar border token.
8. Verify docked tool tab strip/hover/active visuals reflect the dock tab tokens.
9. Click `Export Profile` and save the `.nexen-theme.json` file.
10. Modify the exported file name and import it back with `Import Profile`.
11. Confirm the imported profile appears in the profile dropdown.
12. Select and apply the imported profile.
13. Confirm menu/chrome/accent/control/sidebar/tab visuals match the imported token values.
14. Use `Reset Defaults` and verify colors return to canonical defaults for the profile theme mode.

## Expected Results

- Picker changes apply to menu/chrome/accent visuals when profile is active.
- Picker changes apply to menu/chrome/accent/control hover+pressed visuals when profile is active.
- Picker changes apply to sidebar/tab strip hover/active visuals when profile is active.
- Exported profile includes UI chrome token values.
- Imported valid profile is accepted and selectable.
- Reset Defaults restores canonical values for light/dark profile mode.

## Failure Notes

Capture these on failure:

- Which token changed and which surface did not update.
- Whether import failed validation for expected-valid files.
- Whether reset failed to restore canonical values.
