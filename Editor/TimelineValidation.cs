using System.Collections.Generic;
using Beardmage.ActionTimeline;

namespace Beardmage.ActionTimeline.Editor
{
    public enum TimelineValidationSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2
    }

    public enum TimelineValidationRuleId
    {
        None = 0,
        TimelineNull = 1,
        TimelineHasNoTracks = 2,
        TrackNull = 3,
        TrackEmpty = 4,
        TrackDisabledContainsClips = 5,
        TrackOverlap = 6,
        ClipNull = 7,
        ClipMissingAction = 8,
        ClipNegativeDurationOverride = 9,
        TimelineHasNoValidEnabledClips = 10
    }

    public readonly struct TimelineValidationResult
    {
        public TimelineValidationResult(
            TimelineValidationSeverity severity,
            string message,
            int trackIndex = -1,
            int clipIndex = -1)
            : this(TimelineValidationRuleId.None, severity, message, trackIndex, clipIndex, -1)
        {
        }

        public TimelineValidationResult(
            TimelineValidationRuleId ruleId,
            TimelineValidationSeverity severity,
            string message,
            int trackIndex = -1,
            int clipIndex = -1,
            int secondaryClipIndex = -1)
        {
            RuleId = ruleId;
            Severity = severity;
            Message = message;
            TrackIndex = trackIndex;
            ClipIndex = clipIndex;
            SecondaryClipIndex = secondaryClipIndex;
        }

        public TimelineValidationRuleId RuleId { get; }
        public TimelineValidationSeverity Severity { get; }
        public string Message { get; }
        public int TrackIndex { get; }
        public int ClipIndex { get; }
        public int SecondaryClipIndex { get; }

        public bool IsError => Severity == TimelineValidationSeverity.Error;
        public bool IsWarning => Severity == TimelineValidationSeverity.Warning;
        public bool HasTrackContext => TrackIndex >= 0;
        public bool HasClipContext => ClipIndex >= 0;
        public bool HasSecondaryClipContext => SecondaryClipIndex >= 0;
    }

    public sealed class TimelineValidator
    {
        public List<TimelineValidationResult> Validate(ActionTimelineAsset timeline)
        {
            List<TimelineValidationResult> results = new List<TimelineValidationResult>();

            if (!timeline)
            {
                results.Add(new TimelineValidationResult(
                    TimelineValidationRuleId.TimelineNull,
                    TimelineValidationSeverity.Error,
                    "No timeline is selected."));
                return results;
            }

            IReadOnlyList<ActionTimelineTrack> tracks = timeline.Tracks;
            int trackCount = tracks?.Count ?? 0;
            if (trackCount <= 0)
            {
                results.Add(new TimelineValidationResult(
                    TimelineValidationRuleId.TimelineHasNoTracks,
                    TimelineValidationSeverity.Error,
                    "Timeline contains no tracks."));
                return results;
            }

            int validClipCount = 0;

            for (int trackIndex = 0; trackIndex < trackCount; trackIndex++)
            {
                ActionTimelineTrack track = tracks[trackIndex];
                if (track == null)
                {
                    results.Add(new TimelineValidationResult(
                        TimelineValidationRuleId.TrackNull,
                        TimelineValidationSeverity.Error,
                        $"Track {trackIndex + 1} is null.",
                        trackIndex));
                    continue;
                }

                IReadOnlyList<ActionTimelineClip> clips = track.Clips;
                int clipCount = clips?.Count ?? 0;
                string trackName = GetTrackName(track, trackIndex);

                if (clipCount <= 0 && track.IsEnabled)
                {
                    results.Add(new TimelineValidationResult(
                        TimelineValidationRuleId.TrackEmpty,
                        TimelineValidationSeverity.Warning,
                        $"Track '{trackName}' is empty.",
                        trackIndex));
                }

                if (!track.IsEnabled && clipCount > 0)
                {
                    results.Add(new TimelineValidationResult(
                        TimelineValidationRuleId.TrackDisabledContainsClips,
                        TimelineValidationSeverity.Warning,
                        $"Track '{trackName}' is disabled but still contains clips.",
                        trackIndex));
                }

                if (TimelineOverlapUtility.TryFindFirstOverlapPair(track, out int overlapLeftIndex, out int overlapRightIndex))
                {
                    results.Add(new TimelineValidationResult(
                        TimelineValidationRuleId.TrackOverlap,
                        TimelineValidationSeverity.Error,
                        $"Track '{trackName}' contains overlapping clips ({overlapLeftIndex + 1} and {overlapRightIndex + 1}).",
                        trackIndex,
                        overlapLeftIndex,
                        overlapRightIndex));
                }

                for (int clipIndex = 0; clipIndex < clipCount; clipIndex++)
                {
                    ActionTimelineClip clip = clips[clipIndex];
                    if (clip == null)
                    {
                        results.Add(new TimelineValidationResult(
                            TimelineValidationRuleId.ClipNull,
                            TimelineValidationSeverity.Warning,
                            $"Track '{trackName}' contains a null clip reference at slot {clipIndex + 1}.",
                            trackIndex,
                            clipIndex));
                        continue;
                    }

                    if (!clip.Action)
                    {
                        results.Add(new TimelineValidationResult(
                            TimelineValidationRuleId.ClipMissingAction,
                            TimelineValidationSeverity.Error,
                            $"Clip '{GetClipName(clip, clipIndex)}' has no action assigned.",
                            trackIndex,
                            clipIndex));
                        continue;
                    }

                    if (clip.UseDurationOverride && clip.DurationOverride < 0f)
                    {
                        results.Add(new TimelineValidationResult(
                            TimelineValidationRuleId.ClipNegativeDurationOverride,
                            TimelineValidationSeverity.Error,
                            $"Clip '{GetClipName(clip, clipIndex)}' uses a negative duration override.",
                            trackIndex,
                            clipIndex));
                    }

                    if (track.IsEnabled && clip.IsValid)
                        validClipCount++;
                }
            }

            if (validClipCount <= 0)
            {
                results.Add(new TimelineValidationResult(
                    TimelineValidationRuleId.TimelineHasNoValidEnabledClips,
                    TimelineValidationSeverity.Warning,
                    "Timeline has no valid enabled clips."));
            }

            return results;
        }

        public int CountValidClips(ActionTimelineAsset timeline)
        {
            if (!timeline)
                return 0;

            int count = 0;
            IReadOnlyList<ActionTimelineTrack> tracks = timeline.Tracks;
            int trackCount = tracks?.Count ?? 0;
            for (int trackIndex = 0; trackIndex < trackCount; trackIndex++)
            {
                ActionTimelineTrack track = tracks[trackIndex];
                if (track == null || !track.IsEnabled)
                    continue;

                IReadOnlyList<ActionTimelineClip> clips = track.Clips;
                int clipCount = clips?.Count ?? 0;
                for (int clipIndex = 0; clipIndex < clipCount; clipIndex++)
                {
                    ActionTimelineClip clip = clips[clipIndex];
                    if (clip != null && clip.IsValid)
                        count++;
                }
            }

            return count;
        }

        private static string GetTrackName(ActionTimelineTrack track, int index)
        {
            return string.IsNullOrWhiteSpace(track.TrackName) ? $"Track {index + 1}" : track.TrackName;
        }

        private static string GetClipName(ActionTimelineClip clip, int index)
        {
            if (!string.IsNullOrWhiteSpace(clip.DebugName))
                return clip.DebugName;
            if (clip.Action)
                return clip.Action.name;
            return $"Clip {index + 1}";
        }
    }
}
