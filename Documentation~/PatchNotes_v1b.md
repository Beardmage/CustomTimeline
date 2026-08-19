# Action Timeline Clean V1b Patch Notes

This patch addresses two issues reported after the first Unity import.

## Fixes

- Reworked the right-side timeline editor inspector panel to use a pure layout-based IMGUI area instead of a mixed absolute `GUI.BeginGroup` + `GUILayout.BeginArea` stack. This avoids clipped inspector labels/content on some Unity editor layouts and skins.
- Resets the inspector scroll when selection changes between timeline, track, and clip.
- Made `TimelineAction` concrete and directly creatable from `Assets/Create/Action Timeline/Actions/Timeline Action`.
- Added a serialized `nominalDuration` field to the generic `TimelineAction`, while keeping execution semantics completely project-owned.

## Still intentionally absent

- No runtime execution API.
- No `Execute(...)`.
- No concrete feedback/audio/camera/UI actions.
- Clip resize still writes only to the clip duration override.
