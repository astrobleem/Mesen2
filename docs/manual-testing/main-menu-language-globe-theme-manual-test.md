# Main Menu Globe Language and Theme Manual Test

## Purpose

Validate the right-aligned globe language selector and brown/orange default menu-theme updates.

## Preconditions

- Build uses latest local `master` branch.
- Nexen launches successfully.
- A ROM can be loaded to keep full menus active.

## Test Steps

1. Launch Nexen and confirm a globe icon appears on the right side of the top menu bar.
2. Open the globe menu with mouse.
3. Confirm language entries exist for English, Spanish, and Japanese.
4. Select Spanish.
5. Verify top-level menu labels update immediately without restart.
6. Open Settings and verify labels are translated there too.
7. Select Japanese from the globe menu.
8. Verify menu and settings labels update immediately.
9. Select English and verify labels return to English.
10. Close Nexen.
11. Reopen Nexen and verify the last selected language persisted.
12. In both light and dark theme profiles, visually verify menu/background/highlight accents are brown/orange-forward and readable.
13. Hover menu and button surfaces to confirm pointer-over accents use orange highlight tones.

## Pass Criteria

- Globe menu is visible, accessible, and right aligned.
- Language changes apply immediately and persist across restart.
- Menu/theme accents visibly reflect brown/orange branding in both light and dark themes.
- Text and focus/hover states remain readable.

## Evidence to Capture

- Screenshot of globe menu expanded.
- Screenshot of Spanish or Japanese main menu labels.
- Screenshot of light theme menu hover state.
- Screenshot of dark theme menu hover state.
