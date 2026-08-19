using System;
using System.Collections.Generic;
using UnityEngine;

namespace Beardmage.ActionTimeline
{
    /// <summary>
    /// Authoring asset describing an ordered action timeline.
    /// It only stores timeline data; execution is intentionally project-owned.
    /// </summary>
    [CreateAssetMenu(menuName = "Action Timeline/Timeline")]
    public sealed class ActionTimelineAsset : ScriptableObject
    {
        [SerializeField, Tooltip("Ordered categories composing this action timeline.")]
        private List<ActionTimelineCategory> categories = new List<ActionTimelineCategory>
        {
            new ActionTimelineCategory()
        };

        [NonSerialized]
        private List<ActionTimelineTrack> flattenedTracks;

        public IReadOnlyList<ActionTimelineCategory> Categories => categories;

        /// <summary>
        /// Flattened read-only view for consumers that do not need authoring categories.
        /// Tracks are serialized exclusively inside their owning category.
        /// </summary>
        public IReadOnlyList<ActionTimelineTrack> Tracks
        {
            get
            {
                if (flattenedTracks == null)
                    flattenedTracks = new List<ActionTimelineTrack>(8);

                flattenedTracks.Clear();
                int categoryCount = categories?.Count ?? 0;
                for (int categoryIndex = 0; categoryIndex < categoryCount; categoryIndex++)
                {
                    ActionTimelineCategory category = categories[categoryIndex];
                    if (category == null)
                        continue;

                    IReadOnlyList<ActionTimelineTrack> categoryTracks = category.Tracks;
                    int trackCount = categoryTracks?.Count ?? 0;
                    for (int trackIndex = 0; trackIndex < trackCount; trackIndex++)
                        flattenedTracks.Add(categoryTracks[trackIndex]);
                }

                return flattenedTracks;
            }
        }

        /// <summary>
        /// Computes the authored read duration of this timeline.
        /// This is a scheduling duration, not a guarantee that project-specific effects have ended.
        /// </summary>
        public float GetDuration()
        {
            float maxDuration = 0f;
            int categoryCount = categories?.Count ?? 0;
            for (int categoryIndex = 0; categoryIndex < categoryCount; categoryIndex++)
            {
                ActionTimelineCategory category = categories[categoryIndex];
                if (category == null || !category.IsEnabled)
                    continue;

                IReadOnlyList<ActionTimelineTrack> trackList = category.Tracks;
                int trackCount = trackList?.Count ?? 0;
                for (int trackIndex = 0; trackIndex < trackCount; trackIndex++)
                {
                    ActionTimelineTrack track = trackList[trackIndex];
                    if (track == null || !track.IsEnabled)
                        continue;

                    IReadOnlyList<ActionTimelineClip> clipList = track.Clips;
                    int clipCount = clipList?.Count ?? 0;
                    for (int clipIndex = 0; clipIndex < clipCount; clipIndex++)
                    {
                        ActionTimelineClip clip = clipList[clipIndex];
                        if (clip == null || !clip.IsValid)
                            continue;

                        float clipEnd = clip.StartTime + clip.GetEffectiveDuration();
                        if (clipEnd > maxDuration)
                            maxDuration = clipEnd;
                    }
                }
            }

            return maxDuration;
        }
    }

    /// <summary>
    /// Authoring group containing an ordered set of timeline tracks.
    /// </summary>
    [Serializable]
    public sealed class ActionTimelineCategory
    {
        [SerializeField, Tooltip("Authoring label for this category.")]
        private string categoryName = "Default Category";

        [SerializeField, Tooltip("If disabled, tracks in this category are ignored by duration calculation.")]
        private bool isEnabled = true;

        [SerializeField, Tooltip("Editor foldout state for this category.")]
        private bool isExpanded = true;

        [SerializeField, Tooltip("Ordered tracks owned by this category.")]
        private List<ActionTimelineTrack> tracks = new List<ActionTimelineTrack>
        {
            new ActionTimelineTrack()
        };

        public string CategoryName => categoryName;
        public bool IsEnabled => isEnabled;
        public bool IsExpanded => isExpanded;
        public IReadOnlyList<ActionTimelineTrack> Tracks => tracks;
    }

    /// <summary>
    /// Ordered collection of timeline clips.
    /// </summary>
    [Serializable]
    public sealed class ActionTimelineTrack
    {
        [SerializeField, Tooltip("Authoring label for this track.")]
        private string trackName = "Track";

        [SerializeField, Tooltip("If disabled, clips on this track are ignored by duration calculation and validation as active content.")]
        private bool isEnabled = true;

        [SerializeField, Tooltip("Ordered clips authored on this track.")]
        private List<ActionTimelineClip> clips = new List<ActionTimelineClip>(8);

        public string TrackName => trackName;
        public bool IsEnabled => isEnabled;
        public IReadOnlyList<ActionTimelineClip> Clips => clips;
    }

    /// <summary>
    /// Smallest scheduled unit of an action timeline.
    /// A clip references one timeline action at a given local timeline time.
    /// </summary>
    [Serializable]
    public sealed class ActionTimelineClip
    {
        [Header("Clip")]
        [SerializeField, Tooltip("Optional debug label used for authoring readability.")]
        private string debugName = "Clip";

        [SerializeField, Min(0f), Tooltip("Local clip start time in seconds.")]
        private float startTime;

        [SerializeField, Tooltip("Action referenced by this clip.")]
        private TimelineAction action;

        [SerializeField, Tooltip("If enabled, this clip overrides the nominal duration authored on the action.")]
        private bool useDurationOverride;

        [SerializeField, Min(0f), Tooltip("Optional local duration override used only for scheduling and authoring readability.")]
        private float durationOverride;

        public string DebugName => debugName;
        public float StartTime => startTime;
        public TimelineAction Action => action;
        public bool UseDurationOverride => useDurationOverride;
        public float DurationOverride => durationOverride;

        public bool IsValid => action;

        public float GetEffectiveDuration()
        {
            if (useDurationOverride)
                return Mathf.Max(0f, durationOverride);

            return action ? Mathf.Max(0f, action.NominalDuration) : 0f;
        }
    }
}
