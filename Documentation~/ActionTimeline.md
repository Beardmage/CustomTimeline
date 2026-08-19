# Action Timeline — V1 Clean Export

## Purpose

Action Timeline is a generic ScriptableObject-based authoring tool for timeline-shaped data.

It keeps the useful skeleton from a project-specific feedback timeline tool:

- Timeline asset
- Tracks
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

`ActionTimelineAsset` contains ordered `ActionTimelineTrack` entries.

Each `ActionTimelineTrack` contains ordered `ActionTimelineClip` entries.

Each `ActionTimelineClip` references one `TimelineAction` and stores:

- debug label
- local start time
- action reference
- duration override toggle
- duration override value

`TimelineAction` is an abstract `ScriptableObject` with only:

```csharp
public virtual float NominalDuration => 0f;
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
