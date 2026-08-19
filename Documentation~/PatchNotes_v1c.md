# Action Timeline V1c Patch Notes

## Inspector panel rendering fix

- Reworked the right-side inspector panel wrapper to isolate its local GUI coordinate space with `GUI.BeginGroup` and a local `GUILayout.BeginArea`.
- Added `DrawSelectedInspectorContent` so the panel always has an explicit fallback for timeline, track, clip, and invalid selections.
- Resets invalid/NaN inspector scroll values defensively.
- Uses stock `EditorStyles.boldLabel` for inspector section headers to avoid theme text-color edge cases.

## Menu conflict cleanup

- Removed the manual `Assets/Create/Action Timeline/Timeline` menu item from the editor utility.
- Timeline asset creation from the Assets/Create menu remains provided by `ActionTimelineAsset` via `CreateAssetMenu`.
- The explicit tool shortcut remains under `Tools/Action Timeline/Create Timeline`.
