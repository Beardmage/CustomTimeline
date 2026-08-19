using System.Collections.Generic;
using UnityEngine;

namespace Beardmage.ActionTimeline
{
    public static class TimelineOverlapUtility
    {
        private const float TimeEpsilon = 0.0001f;

        public static bool Overlaps(float startA, float durationA, float startB, float durationB)
        {
            float safeStartA = Mathf.Max(0f, startA);
            float safeStartB = Mathf.Max(0f, startB);
            float safeDurationA = Mathf.Max(0f, durationA);
            float safeDurationB = Mathf.Max(0f, durationB);

            bool aIsPoint = safeDurationA <= TimeEpsilon;
            bool bIsPoint = safeDurationB <= TimeEpsilon;

            if (aIsPoint && bIsPoint)
                return Mathf.Abs(safeStartA - safeStartB) <= TimeEpsilon;

            if (aIsPoint)
                return PointOverlapsIntervalClosed(safeStartA, safeStartB, safeStartB + safeDurationB);

            if (bIsPoint)
                return PointOverlapsIntervalClosed(safeStartB, safeStartA, safeStartA + safeDurationA);

            float endA = safeStartA + safeDurationA;
            float endB = safeStartB + safeDurationB;
            return safeStartA < endB - TimeEpsilon && safeStartB < endA - TimeEpsilon;
        }

        public static bool Overlaps(ActionTimelineClip left, ActionTimelineClip right)
        {
            if (left == null || right == null)
                return false;

            return Overlaps(
                left.StartTime,
                TimelineDurationUtility.GetClipEffectiveDuration(left),
                right.StartTime,
                TimelineDurationUtility.GetClipEffectiveDuration(right));
        }

        public static bool CanPlaceClipInTrack(ActionTimelineTrack track, ActionTimelineClip candidate, int ignoredClipIndex = -1)
        {
            if (track == null || candidate == null)
                return false;

            IReadOnlyList<ActionTimelineClip> clips = track.Clips;
            int clipCount = clips?.Count ?? 0;

            for (int i = 0; i < clipCount; i++)
            {
                if (i == ignoredClipIndex)
                    continue;

                ActionTimelineClip other = clips[i];
                if (other == null)
                    continue;

                if (Overlaps(candidate, other))
                    return false;
            }

            return true;
        }

        public static bool TrackHasOverlap(ActionTimelineTrack track)
        {
            return TryFindFirstOverlapPair(track, out _, out _);
        }

        public static bool TryFindFirstOverlapPair(ActionTimelineTrack track, out int leftIndex, out int rightIndex)
        {
            leftIndex = -1;
            rightIndex = -1;

            if (track == null)
                return false;

            IReadOnlyList<ActionTimelineClip> clips = track.Clips;
            int clipCount = clips?.Count ?? 0;

            for (int i = 0; i < clipCount; i++)
            {
                ActionTimelineClip left = clips[i];
                if (left == null)
                    continue;

                for (int j = i + 1; j < clipCount; j++)
                {
                    ActionTimelineClip right = clips[j];
                    if (right == null)
                        continue;

                    if (!Overlaps(left, right))
                        continue;

                    leftIndex = i;
                    rightIndex = j;
                    return true;
                }
            }

            return false;
        }

        private static bool PointOverlapsIntervalClosed(float point, float intervalStart, float intervalEnd)
        {
            if (intervalEnd - intervalStart <= TimeEpsilon)
                return Mathf.Abs(point - intervalStart) <= TimeEpsilon;

            return point >= intervalStart - TimeEpsilon && point <= intervalEnd + TimeEpsilon;
        }
    }
}
