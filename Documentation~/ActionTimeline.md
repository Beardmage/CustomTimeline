# Action Timeline — V1f ergonomics pass

## Purpose

Action Timeline is a generic ScriptableObject-based authoring tool for timeline-shaped data.

It keeps the useful skeleton from a project-specific feedback timeline tool:

- Timeline asset
- Categories and tracks
- Clips
- ScriptableObject actions
- Start times
- Nominal durations
- Clip duration overrides
- Editor window
- Validation
- Action usage navigation

It intentionally does **not** define runtime execution.
Each project decides how to interpret and execute timeline actions.

## Core model

`ActionTimelineAsset` contains ordered `ActionTimelineCategory` entries.

Each category owns ordered `ActionTimelineTrack` entries, and each track owns ordered `ActionTimelineClip` entries.

Each `ActionTimelineClip` references one `TimelineAction` and stores:

- debug label
- local start time
- action reference
- duration override toggle
- duration override value

`TimelineAction` is a concrete, extensible `ScriptableObject` with a nominal duration:

```csharp
public virtual float NominalDuration => nominalDuration;
```

There is no `Execute` method in this package.

## Resize policy

V1 uses the most portable resize policy:

```text
Resize clip => write only the clip duration override
```

The editor never modifies or duplicates the action asset during resize.
Projects that want action-specific duration editing can add their own policy later.

## Editor tools

The package provides:

- `Tools/Action Timeline/Timeline Editor`
- `Assets/Create/Action Timeline/Timeline`
- lightweight `ActionTimelineAsset` inspector
- shared `TimelineAction` inspector
- action usage index and clip focus navigation

The editor window preserves the original V1d canvas geometry and interaction model:

- category foldouts and indented child tracks
- category activity boxes spanning the first to last child clip
- category box drag moves every child clip while preserving the initial mouse grab offset
- Ctrl/Cmd + click toggles clips in a multi-selection
- Shift + click on a track selects every clip on that track
- dragging a selected clip body moves the entire selection
- clip move, resize-left, and resize-right remain mutually exclusive
- clip snapping and last-valid-hovered-track behavior remain active during moves
- selected track and category outlines stay outside the clip bodies
- the white playhead can be clicked and dragged from the timestamp ruler
- clip and category movement can snap to the playhead
- Add Clip uses the playhead for an explicit selection, or the hovered timeline cursor otherwise
- Ctrl/Cmd + C/V/D copy, paste, and duplicate categories, tracks, and clips
- disabled categories darken their activity box and child tracks; disabled tracks darken their clips
- project Settings and Theme assets can be activated and pinged from the editor toolbar

Category proportional resize is intentionally deferred.

## Package boundaries

Included:

- authoring data
- editor tooling
- generic validation
- generic usage index
- theme/settings fallbacks

Excluded:

- concrete actions
- runtime playback
- presentation services
- feedback signal/context types
- project-specific renderer registry
- project-specific config locator

## Suggested extension pattern

In a project, create concrete actions by subclassing `TimelineAction`:

```csharp
[CreateAssetMenu(menuName = "My Project/Timeline Actions/My Action")]
public sealed class MyTimelineAction : TimelineAction
{
    [SerializeField] private float duration = 1f;
    public override float NominalDuration => duration;
}
```

Then implement a project-owned runtime that reads `ActionTimelineAsset`, evaluates track/clip timing, and dispatches actions however the project needs.
