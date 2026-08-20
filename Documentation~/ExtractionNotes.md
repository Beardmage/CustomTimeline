# Extraction and integration notes — V1f

This package is a generic extraction from a project-specific feedback timeline tool. The extraction keeps authoring and editor ergonomics while leaving project execution semantics outside the package.

## Generic concepts

| Project-specific concept | Generic package concept |
| --- | --- |
| Feedback timeline | `ActionTimelineAsset` |
| Feedback category | `ActionTimelineCategory` |
| Feedback track | `ActionTimelineTrack` |
| Feedback clip | `ActionTimelineClip` |
| Feedback action | `TimelineAction` |
| Feedback editor window | `ActionTimelineEditorWindow` |
| Feedback timeline inspector | `ActionTimelineInspector` |
| Feedback action inspector | `TimelineActionInspector` |

## Intentionally removed

- feedback runtime execution;
- signal/context execution parameters;
- presentation runtime requests;
- concrete audio, camera, UI, VFX or tween actions;
- specialized action inspector renderers;
- project-specific action registry or config locator;
- action duration mutation/duplication during clip resize.

## Current package boundaries

The runtime assembly contains only the serializable model, `TimelineAction`, duration helpers and overlap helpers. The editor assembly contains the window, selection state, inspectors, validation, Settings/Theme assets and usage navigation.

The active configuration source is project-owned through `TimelineEditorSettingsAsset` and `TimelineEditorThemeAsset`. The editor resolves an active asset once and uses an in-memory fallback only when no asset exists.

## Resize policy

Clip resize writes only clip-local data:

- `startTime`;
- `useDurationOverride = true`;
- `durationOverride`.

The referenced `TimelineAction` asset is never edited or duplicated by this operation.

## Serialization note

Tracks are serialized under categories. `ActionTimelineAsset.Tracks` is a flattened, non-serialized convenience view. Any project code that needs stable identity should use its own GUID or asset-level identifier rather than a flat track index.

## Documentation map

- `ActionTimeline.md` — guide for authors and users of the editor;
- `TechnicalArchitecture.md` — architecture, state machine, extension points and Mermaid diagrams;
- `PatchNotes_v1b.md`, `PatchNotes_v1c.md` — historical extraction and inspector notes.

## Verification

The package is checked by compiling the Runtime and Editor source against installed Unity assemblies. A consuming project should still perform a Unity import smoke test, then exercise asset creation, domain reload, Undo/Redo and the interaction checklist in `TechnicalArchitecture.md`.
