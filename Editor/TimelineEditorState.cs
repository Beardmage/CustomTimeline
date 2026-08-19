using System.Collections.Generic;
using Beardmage.ActionTimeline;
using UnityEditor;
using UnityEngine;

namespace Beardmage.ActionTimeline.Editor
{
    public sealed class TimelineEditorState
    {
        public const float DefaultPixelsPerSecond = 100f;
        public const float DefaultInspectorWidth = 320f;

        private readonly HashSet<TimelineClipKey> selectedClips = new HashSet<TimelineClipKey>();

        public ActionTimelineAsset Timeline { get; private set; }
        public SerializedObject TimelineSerializedObject { get; private set; }

        public Vector2 CanvasScroll { get; set; } = Vector2.zero;
        public Vector2 InspectorScroll { get; set; } = Vector2.zero;
        public float PixelsPerSecond { get; set; } = DefaultPixelsPerSecond;
        public float InspectorWidth { get; set; } = DefaultInspectorWidth;
        public bool ShowShortcutHints { get; set; }

        public TimelineSelectionKind SelectionKind { get; private set; } = TimelineSelectionKind.None;
        public int SelectedCategoryIndex { get; private set; } = -1;
        public int SelectedTrackIndex { get; private set; } = -1;
        public int SelectedClipIndex { get; private set; } = -1;
        public int SelectedClipCount => selectedClips.Count;
        public IEnumerable<TimelineClipKey> SelectedClips => selectedClips;

        public bool IsDraggingClip { get; private set; }
        public TimelineClipManipulationMode ManipulationMode { get; private set; } = TimelineClipManipulationMode.None;
        public int DragSourceTrackIndex { get; private set; } = -1;
        public int DragSourceClipIndex { get; private set; } = -1;
        public float DragPreviewStartTime { get; private set; }
        public float DragPreviewDuration { get; private set; }
        public int DragPreviewTrackIndex { get; private set; } = -1;
        public bool DragPreviewValid { get; private set; }
        public float DragMouseOffsetTime { get; private set; }
        public float ManipulationInitialStartTime { get; private set; }
        public float ManipulationInitialDuration { get; private set; }
        public float ManipulationFixedEdgeTime { get; private set; }

        public bool HasPendingClipPress { get; private set; }
        public int PendingClipPressTrackIndex { get; private set; } = -1;
        public int PendingClipPressClipIndex { get; private set; } = -1;
        public Vector2 PendingClipPressMousePosition { get; private set; } = Vector2.zero;
        public TimelineClipManipulationMode PendingClipPressManipulationMode { get; private set; } = TimelineClipManipulationMode.None;

        public bool IsDraggingCategory { get; private set; }
        public bool HasPendingCategoryPress { get; private set; }
        public int PendingCategoryPressIndex { get; private set; } = -1;
        public Vector2 PendingCategoryPressMousePosition { get; private set; } = Vector2.zero;
        public float PendingCategoryGrabOffsetTime { get; private set; }
        public int DragCategoryIndex { get; private set; } = -1;
        public float CategoryInitialStartTime { get; private set; }
        public float CategoryInitialEndTime { get; private set; }
        public float CategoryGrabOffsetTime { get; private set; }
        public float CategoryPreviewStartTime { get; private set; }

        public Vector2 LastMouseCanvasPosition { get; set; }

        public bool HasTimeline => Timeline != null;
        public bool HasCategorySelection => SelectionKind == TimelineSelectionKind.Category && SelectedCategoryIndex >= 0;
        public bool HasTrackSelection => SelectionKind == TimelineSelectionKind.Track && SelectedTrackIndex >= 0;
        public bool HasClipSelection => SelectionKind == TimelineSelectionKind.Clip && selectedClips.Count > 0 && SelectedTrackIndex >= 0 && SelectedClipIndex >= 0;

        public void SetTimeline(ActionTimelineAsset timeline)
        {
            Timeline = timeline;
            TimelineSerializedObject = timeline ? new SerializedObject(timeline) : null;
            CanvasScroll = Vector2.zero;
            InspectorScroll = Vector2.zero;
            PixelsPerSecond = DefaultPixelsPerSecond;
            InspectorWidth = DefaultInspectorWidth;
            ShowShortcutHints = false;
            ClearPendingClipPress();
            ClearPendingCategoryPress();
            ClearDragState();
            ClearCategoryDragState();
            ClearSelection();
            if (timeline != null)
                SelectTimeline();
        }

        public void ClearSelection()
        {
            bool changed = SelectionKind != TimelineSelectionKind.None || SelectedCategoryIndex != -1 ||
                           SelectedTrackIndex != -1 || SelectedClipIndex != -1 || selectedClips.Count > 0;
            SelectionKind = TimelineSelectionKind.None;
            SelectedCategoryIndex = -1;
            SelectedTrackIndex = -1;
            SelectedClipIndex = -1;
            selectedClips.Clear();
            if (changed)
                InspectorScroll = Vector2.zero;
        }

        public void SelectTimeline()
        {
            bool changed = SelectionKind != TimelineSelectionKind.Timeline || SelectedCategoryIndex != -1 ||
                           SelectedTrackIndex != -1 || SelectedClipIndex != -1 || selectedClips.Count > 0;
            SelectionKind = TimelineSelectionKind.Timeline;
            SelectedCategoryIndex = -1;
            SelectedTrackIndex = -1;
            SelectedClipIndex = -1;
            selectedClips.Clear();
            if (changed)
                InspectorScroll = Vector2.zero;
        }

        public void SelectCategory(int categoryIndex)
        {
            bool changed = SelectionKind != TimelineSelectionKind.Category || SelectedCategoryIndex != categoryIndex;
            SelectionKind = TimelineSelectionKind.Category;
            SelectedCategoryIndex = categoryIndex;
            SelectedTrackIndex = -1;
            SelectedClipIndex = -1;
            selectedClips.Clear();
            if (changed)
                InspectorScroll = Vector2.zero;
        }

        public void SelectTrack(int trackIndex)
        {
            bool changed = SelectionKind != TimelineSelectionKind.Track || SelectedTrackIndex != trackIndex;
            SelectionKind = TimelineSelectionKind.Track;
            SelectedCategoryIndex = -1;
            SelectedTrackIndex = trackIndex;
            SelectedClipIndex = -1;
            selectedClips.Clear();
            if (changed)
                InspectorScroll = Vector2.zero;
        }

        public void SelectClip(int trackIndex, int clipIndex)
        {
            TimelineClipKey key = new TimelineClipKey(trackIndex, clipIndex);
            bool changed = SelectionKind != TimelineSelectionKind.Clip || SelectedTrackIndex != trackIndex ||
                           SelectedClipIndex != clipIndex || selectedClips.Count != 1 || !selectedClips.Contains(key);
            SelectionKind = TimelineSelectionKind.Clip;
            SelectedCategoryIndex = -1;
            SelectedTrackIndex = trackIndex;
            SelectedClipIndex = clipIndex;
            selectedClips.Clear();
            selectedClips.Add(key);
            if (changed)
                InspectorScroll = Vector2.zero;
        }

        public void SetPrimaryClip(int trackIndex, int clipIndex)
        {
            TimelineClipKey key = new TimelineClipKey(trackIndex, clipIndex);
            if (!selectedClips.Contains(key))
            {
                SelectClip(trackIndex, clipIndex);
                return;
            }

            SelectionKind = TimelineSelectionKind.Clip;
            SelectedCategoryIndex = -1;
            SelectedTrackIndex = trackIndex;
            SelectedClipIndex = clipIndex;
        }

        public bool ToggleClipSelection(int trackIndex, int clipIndex)
        {
            TimelineClipKey key = new TimelineClipKey(trackIndex, clipIndex);
            if (!selectedClips.Add(key))
                selectedClips.Remove(key);

            if (selectedClips.Count <= 0)
            {
                SelectTrack(trackIndex);
                return false;
            }

            SelectionKind = TimelineSelectionKind.Clip;
            SelectedCategoryIndex = -1;
            if (selectedClips.Contains(key))
            {
                SelectedTrackIndex = trackIndex;
                SelectedClipIndex = clipIndex;
            }
            else if (!selectedClips.Contains(new TimelineClipKey(SelectedTrackIndex, SelectedClipIndex)))
            {
                SelectFirstClipAsPrimary();
            }

            InspectorScroll = Vector2.zero;
            return selectedClips.Contains(key);
        }

        public void SelectAllClipsOnTrack(int trackIndex, int clipCount)
        {
            selectedClips.Clear();
            for (int clipIndex = 0; clipIndex < clipCount; clipIndex++)
                selectedClips.Add(new TimelineClipKey(trackIndex, clipIndex));

            if (selectedClips.Count <= 0)
            {
                SelectTrack(trackIndex);
                return;
            }

            SelectionKind = TimelineSelectionKind.Clip;
            SelectedCategoryIndex = -1;
            SelectedTrackIndex = trackIndex;
            SelectedClipIndex = 0;
            InspectorScroll = Vector2.zero;
        }

        public bool IsClipSelected(int trackIndex, int clipIndex)
        {
            return selectedClips.Contains(new TimelineClipKey(trackIndex, clipIndex));
        }

        public bool IsPrimaryClip(int trackIndex, int clipIndex)
        {
            return HasClipSelection && SelectedTrackIndex == trackIndex && SelectedClipIndex == clipIndex;
        }

        public void ReplaceClipSelection(IEnumerable<TimelineClipKey> clips, TimelineClipKey primary)
        {
            selectedClips.Clear();
            if (clips != null)
            {
                foreach (TimelineClipKey clip in clips)
                    selectedClips.Add(clip);
            }

            if (selectedClips.Count <= 0)
            {
                SelectTimeline();
                return;
            }

            SelectionKind = TimelineSelectionKind.Clip;
            SelectedCategoryIndex = -1;
            if (selectedClips.Contains(primary))
            {
                SelectedTrackIndex = primary.TrackIndex;
                SelectedClipIndex = primary.ClipIndex;
            }
            else
            {
                SelectFirstClipAsPrimary();
            }
        }

        private void SelectFirstClipAsPrimary()
        {
            TimelineClipKey best = default;
            bool hasBest = false;
            foreach (TimelineClipKey candidate in selectedClips)
            {
                if (!hasBest || candidate.TrackIndex < best.TrackIndex ||
                    (candidate.TrackIndex == best.TrackIndex && candidate.ClipIndex < best.ClipIndex))
                {
                    best = candidate;
                    hasBest = true;
                }
            }

            SelectedTrackIndex = hasBest ? best.TrackIndex : -1;
            SelectedClipIndex = hasBest ? best.ClipIndex : -1;
        }

        public void BeginPendingClipPress(int trackIndex, int clipIndex, Vector2 mousePosition, TimelineClipManipulationMode manipulationMode)
        {
            HasPendingClipPress = true;
            PendingClipPressTrackIndex = trackIndex;
            PendingClipPressClipIndex = clipIndex;
            PendingClipPressMousePosition = mousePosition;
            PendingClipPressManipulationMode = manipulationMode;
        }

        public bool TryGetDragSourceIndices(out int trackIndex, out int clipIndex)
        {
            trackIndex = DragSourceTrackIndex;
            clipIndex = DragSourceClipIndex;
            return IsDraggingClip && trackIndex >= 0 && clipIndex >= 0;
        }

        public void BeginClipManipulation(
            TimelineClipManipulationMode manipulationMode,
            int trackIndex,
            int clipIndex,
            float previewStartTime,
            float previewDuration,
            float mouseOffsetTime,
            float initialStartTime,
            float initialDuration,
            float fixedEdgeTime)
        {
            IsDraggingClip = true;
            ManipulationMode = manipulationMode;
            DragSourceTrackIndex = trackIndex;
            DragSourceClipIndex = clipIndex;
            DragPreviewTrackIndex = trackIndex;
            DragPreviewStartTime = Mathf.Max(0f, previewStartTime);
            DragPreviewDuration = Mathf.Max(0f, previewDuration);
            DragMouseOffsetTime = mouseOffsetTime;
            ManipulationInitialStartTime = Mathf.Max(0f, initialStartTime);
            ManipulationInitialDuration = Mathf.Max(0f, initialDuration);
            ManipulationFixedEdgeTime = Mathf.Max(0f, fixedEdgeTime);
            DragPreviewValid = true;
        }

        public void SetDragPreview(int trackIndex, float previewStartTime, float previewDuration, bool isValid)
        {
            DragPreviewTrackIndex = trackIndex;
            DragPreviewStartTime = Mathf.Max(0f, previewStartTime);
            DragPreviewDuration = Mathf.Max(0f, previewDuration);
            DragPreviewValid = isValid;
        }

        public void BeginPendingCategoryPress(int categoryIndex, Vector2 mousePosition, float grabOffsetTime)
        {
            HasPendingCategoryPress = true;
            PendingCategoryPressIndex = categoryIndex;
            PendingCategoryPressMousePosition = mousePosition;
            PendingCategoryGrabOffsetTime = Mathf.Max(0f, grabOffsetTime);
        }

        public void BeginCategoryManipulation(int categoryIndex, float initialStartTime, float initialEndTime, float grabOffsetTime)
        {
            IsDraggingCategory = true;
            DragCategoryIndex = categoryIndex;
            CategoryInitialStartTime = Mathf.Max(0f, initialStartTime);
            CategoryInitialEndTime = Mathf.Max(CategoryInitialStartTime, initialEndTime);
            CategoryGrabOffsetTime = Mathf.Max(0f, grabOffsetTime);
            CategoryPreviewStartTime = CategoryInitialStartTime;
        }

        public void SetCategoryPreviewStart(float startTime)
        {
            CategoryPreviewStartTime = Mathf.Max(0f, startTime);
        }

        public void ClearPendingClipPress()
        {
            HasPendingClipPress = false;
            PendingClipPressTrackIndex = -1;
            PendingClipPressClipIndex = -1;
            PendingClipPressMousePosition = Vector2.zero;
            PendingClipPressManipulationMode = TimelineClipManipulationMode.None;
        }

        public void ClearPendingCategoryPress()
        {
            HasPendingCategoryPress = false;
            PendingCategoryPressIndex = -1;
            PendingCategoryPressMousePosition = Vector2.zero;
            PendingCategoryGrabOffsetTime = 0f;
        }

        public void ClearDragState()
        {
            IsDraggingClip = false;
            ManipulationMode = TimelineClipManipulationMode.None;
            DragSourceTrackIndex = -1;
            DragSourceClipIndex = -1;
            DragPreviewStartTime = 0f;
            DragPreviewDuration = 0f;
            DragPreviewTrackIndex = -1;
            DragPreviewValid = false;
            DragMouseOffsetTime = 0f;
            ManipulationInitialStartTime = 0f;
            ManipulationInitialDuration = 0f;
            ManipulationFixedEdgeTime = 0f;
        }

        public void ClearCategoryDragState()
        {
            IsDraggingCategory = false;
            DragCategoryIndex = -1;
            CategoryInitialStartTime = 0f;
            CategoryInitialEndTime = 0f;
            CategoryGrabOffsetTime = 0f;
            CategoryPreviewStartTime = 0f;
        }
    }
}
