using UnityEngine;

namespace Beardmage.ActionTimeline
{
    public static class TimelineDurationUtility
    {
        public static float GetActionNominalDuration(TimelineAction action)
        {
            return action ? Mathf.Max(0f, action.NominalDuration) : 0f;
        }

        public static float GetClipEffectiveDuration(ActionTimelineClip clip)
        {
            return clip == null ? 0f : Mathf.Max(0f, clip.GetEffectiveDuration());
        }

        public static float GetClipEndTime(ActionTimelineClip clip)
        {
            if (clip == null)
                return 0f;

            return Mathf.Max(0f, clip.StartTime) + GetClipEffectiveDuration(clip);
        }

        public static float GetTimelineDuration(ActionTimelineAsset timeline)
        {
            return timeline ? Mathf.Max(0f, timeline.GetDuration()) : 0f;
        }

        public static string FormatSeconds(float value)
        {
            return $"{Mathf.Max(0f, value):0.00}s";
        }
    }
}
