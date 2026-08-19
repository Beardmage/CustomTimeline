# Extraction Notes

This package is a clean generic extraction from a project-specific feedback timeline tool.

## Renamed concepts

- `FeedbackTimeline` -> `ActionTimelineAsset`
- `FeedbackTrack` -> `ActionTimelineTrack`
- `FeedbackClip` -> `ActionTimelineClip`
- `FeedbackAction` -> `TimelineAction`
- `FeedbackTimelineEditorWindow` -> `ActionTimelineEditorWindow`
- `FeedbackTimelineInspector` -> `ActionTimelineInspector`
- `FeedbackActionInspector` -> `TimelineActionInspector`

## Removed project-specific concepts

- feedback runtime execution
- signal/context execution parameters
- presentation runtime requests
- concrete audio/camera/UI/VFX/tween actions
- specialized action inspector renderers
- project config locator
- action duration mutation/duplication during clip resize

## V1 resize policy

Clip resize always writes:

- `startTime`
- `useDurationOverride = true`
- `durationOverride`

The package never edits a referenced action asset during resize.

## Known integration note

This package was generated as a clean source export and has not been compiled inside a Unity project in this environment. Import it in a Unity project as a local UPM package or copy the folder into `Packages/` for compile validation.
