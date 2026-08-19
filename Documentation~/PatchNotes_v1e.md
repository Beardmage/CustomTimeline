# Action Timeline 0.1.4 — V1e

## Focus

Adds hierarchical category authoring to the Action Timeline editor. This version intentionally assumes test assets only and does not preserve the previous flat track-only timeline asset schema.

## Data model

- `ActionTimelineAsset` now serializes ordered categories.
- `ActionTimelineCategory` owns ordered tracks.
- `ActionTimelineTrack` still owns ordered clips.
- A default category with one default track is created for new timeline assets.

## Editor

- Added category rows above child tracks.
- Added category foldout state.
- Added category creation from toolbar and inspector.
- Added track creation inside a selected category.
- Category rows draw an aggregate activity box spanning from the earliest child clip start to the latest child clip end.
- Dragging a category activity box moves every child clip while preserving relative offsets.
- Category moves are clamped so the earliest child clip cannot move before 0 seconds.

## Selection

- Simple click selects one clip.
- Ctrl/Cmd + click toggles a clip in the current clip selection.
- Shift + click on a track row or lane selects all clips in that track.
- Dragging a selected clip in a multi-selection moves the whole selected set.

## Deferred

- Category proportional resize/scale is intentionally not included in this pass.
