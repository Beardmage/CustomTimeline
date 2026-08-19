using System;

namespace Beardmage.ActionTimeline.Editor
{
    public enum TimelineSelectionKind
    {
        None = 0,
        Timeline = 1,
        Category = 2,
        Track = 3,
        Clip = 4
    }

    [Serializable]
    public readonly struct TimelineClipKey : IEquatable<TimelineClipKey>
    {
        public TimelineClipKey(int trackIndex, int clipIndex)
        {
            TrackIndex = trackIndex;
            ClipIndex = clipIndex;
        }

        public int TrackIndex { get; }
        public int ClipIndex { get; }

        public bool Equals(TimelineClipKey other)
        {
            return TrackIndex == other.TrackIndex && ClipIndex == other.ClipIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is TimelineClipKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (TrackIndex * 397) ^ ClipIndex;
            }
        }
    }

    /// <summary>
    /// Current pointer manipulation mode on a timeline clip.
    /// </summary>
    public enum TimelineClipManipulationMode
    {
        None = 0,
        Move = 1,
        ResizeLeft = 2,
        ResizeRight = 3
    }
}
