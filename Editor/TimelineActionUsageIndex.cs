using System;
using System.Collections.Generic;
using Beardmage.ActionTimeline;
using UnityEditor;

namespace Beardmage.ActionTimeline.Editor
{
    /// <summary>
    /// Editor-only cached index of TimelineAction usages across ActionTimelineAsset assets.
    /// This cache is intentionally rebuilt only on demand or after explicit invalidation.
    /// </summary>
    public static class TimelineActionUsageIndex
    {
        private sealed class UsageData
        {
            public int Count;
            public readonly List<string> TimelinePaths = new List<string>(4);
            public readonly List<TimelineActionUsageOccurrence> Occurrences = new List<TimelineActionUsageOccurrence>(8);
        }

        private static readonly Dictionary<string, UsageData> usageByActionKey = new Dictionary<string, UsageData>(128);
        private static bool isDirty = true;

        public static void Invalidate()
        {
            isDirty = true;
        }

        public static int GetUsageCount(TimelineAction action)
        {
            EnsureFresh();
            if (!TryGetActionKey(action, out string actionKey))
                return 0;

            return usageByActionKey.TryGetValue(actionKey, out UsageData usageData)
                ? usageData.Count
                : 0;
        }

        public static bool IsShared(TimelineAction action)
        {
            return GetUsageCount(action) > 1;
        }

        public static IReadOnlyList<string> GetUsageTimelinePaths(TimelineAction action)
        {
            EnsureFresh();
            if (!TryGetActionKey(action, out string actionKey))
                return Array.Empty<string>();

            return usageByActionKey.TryGetValue(actionKey, out UsageData usageData)
                ? usageData.TimelinePaths
                : Array.Empty<string>();
        }

        public static IReadOnlyList<TimelineActionUsageOccurrence> GetUsageOccurrences(TimelineAction action)
        {
            EnsureFresh();
            if (!TryGetActionKey(action, out string actionKey))
                return Array.Empty<TimelineActionUsageOccurrence>();

            return usageByActionKey.TryGetValue(actionKey, out UsageData usageData)
                ? usageData.Occurrences
                : Array.Empty<TimelineActionUsageOccurrence>();
        }

        private static void EnsureFresh()
        {
            if (!isDirty)
                return;

            Rebuild();
        }

        private static void Rebuild()
        {
            usageByActionKey.Clear();

            string[] timelineGuids = AssetDatabase.FindAssets("t:ActionTimelineAsset");
            int timelineCount = timelineGuids?.Length ?? 0;
            for (int timelineIndex = 0; timelineIndex < timelineCount; timelineIndex++)
            {
                string timelinePath = AssetDatabase.GUIDToAssetPath(timelineGuids[timelineIndex]);
                if (string.IsNullOrWhiteSpace(timelinePath))
                    continue;

                ActionTimelineAsset timeline = AssetDatabase.LoadAssetAtPath<ActionTimelineAsset>(timelinePath);
                if (!timeline)
                    continue;

                IReadOnlyList<ActionTimelineTrack> tracks = timeline.Tracks;
                int trackCount = tracks?.Count ?? 0;
                for (int trackIndex = 0; trackIndex < trackCount; trackIndex++)
                {
                    ActionTimelineTrack track = tracks[trackIndex];
                    if (track == null)
                        continue;

                    IReadOnlyList<ActionTimelineClip> clips = track.Clips;
                    int clipCount = clips?.Count ?? 0;

                    string trackName = string.IsNullOrWhiteSpace(track.TrackName)
                        ? $"Track {trackIndex + 1}"
                        : track.TrackName;

                    for (int clipIndex = 0; clipIndex < clipCount; clipIndex++)
                    {
                        ActionTimelineClip clip = clips[clipIndex];
                        if (clip == null || !clip.Action)
                            continue;

                        if (!TryGetActionKey(clip.Action, out string actionKey))
                            continue;

                        if (!usageByActionKey.TryGetValue(actionKey, out UsageData usageData))
                        {
                            usageData = new UsageData();
                            usageByActionKey[actionKey] = usageData;
                        }

                        usageData.Count++;
                        if (!usageData.TimelinePaths.Contains(timelinePath))
                            usageData.TimelinePaths.Add(timelinePath);

                        string clipDebugName = string.IsNullOrWhiteSpace(clip.DebugName)
                            ? $"Clip {clipIndex + 1}"
                            : clip.DebugName;

                        usageData.Occurrences.Add(new TimelineActionUsageOccurrence(
                            timelinePath,
                            trackIndex,
                            clipIndex,
                            trackName,
                            clipDebugName,
                            clip.StartTime,
                            clip.UseDurationOverride));
                    }
                }
            }

            isDirty = false;
        }

        private static bool TryGetActionKey(TimelineAction action, out string actionKey)
        {
            actionKey = string.Empty;
            if (!action)
                return false;

            GlobalObjectId globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(action);
            actionKey = globalObjectId.ToString();
            return !string.IsNullOrWhiteSpace(actionKey);
        }
    }

    /// <summary>
    /// One concrete editor-only occurrence of a TimelineAction inside a timeline.
    /// This is used for navigation from an action asset back to the exact authored clip.
    /// </summary>
    public readonly struct TimelineActionUsageOccurrence
    {
        public string TimelinePath { get; }
        public int TrackIndex { get; }
        public int ClipIndex { get; }
        public string TrackName { get; }
        public string ClipDebugName { get; }
        public float ClipStartTime { get; }
        public bool UseDurationOverride { get; }

        public TimelineActionUsageOccurrence(
            string timelinePath,
            int trackIndex,
            int clipIndex,
            string trackName,
            string clipDebugName,
            float clipStartTime,
            bool useDurationOverride)
        {
            TimelinePath = timelinePath ?? string.Empty;
            TrackIndex = trackIndex;
            ClipIndex = clipIndex;
            TrackName = trackName ?? string.Empty;
            ClipDebugName = clipDebugName ?? string.Empty;
            ClipStartTime = clipStartTime;
            UseDurationOverride = useDurationOverride;
        }
    }

    public sealed class TimelineActionUsageIndexInvalidationPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            bool shouldInvalidate = ContainsRelevantAsset(importedAssets) ||
                                    ContainsRelevantAsset(deletedAssets) ||
                                    ContainsRelevantAsset(movedAssets) ||
                                    ContainsRelevantAsset(movedFromAssetPaths);

            if (shouldInvalidate)
                TimelineActionUsageIndex.Invalidate();
        }

        private static bool ContainsRelevantAsset(string[] assetPaths)
        {
            int assetCount = assetPaths?.Length ?? 0;
            for (int assetIndex = 0; assetIndex < assetCount; assetIndex++)
            {
                string assetPath = assetPaths[assetIndex];
                if (string.IsNullOrWhiteSpace(assetPath))
                    continue;

                Type mainType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
                if (mainType == typeof(ActionTimelineAsset) ||
                    typeof(TimelineAction).IsAssignableFrom(mainType))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
