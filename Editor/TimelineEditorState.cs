using Beardmage.ActionTimeline;
using UnityEditor;
using UnityEngine;

namespace Beardmage.ActionTimeline.Editor
{
    public sealed class TimelineEditorState
    {
        public const float DefaultPixelsPerSecond = 100f;
        public const float DefaultInspectorWidth = 320f;

        public ActionTimelineAsset Timeline { get; private set; }
        public SerializedObject TimelineSerializedObject { get; private set; }

        public Vector2 CanvasScroll { get; set; } = Vector2.zero;
        public Vector2 InspectorScroll { get; set; } = Vector2.zero;
        public float PixelsPerSecond { get; set; } = DefaultPixelsPerSecond;
        public float InspectorWidth { get; set; } = DefaultInspectorWidth;
        public bool ShowShortcutHints { get; set; }

        public TimelineSelectionKind SelectionKind { get; private set; } = TimelineSelectionKind.None;
        public int SelectedTrackIndex { get; private set; } = -1;
        public int SelectedClipIndex { get; private set; } = -1;

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

        public Vector2 LastMouseCanvasPosition { get; set; }

        public bool HasTimeline => Timeline != null;
        public bool HasTrackSelection => SelectionKind == TimelineSelectionKind.Track && SelectedTrackIndex >= 0;
        public bool HasClipSelection => SelectionKind == TimelineSelectionKind.Clip && SelectedTrackIndex >= 0 && SelectedClipIndex >= 0;

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
            ClearDragState();
            ClearSelection();
            if (timeline != null)
                SelectTimeline();
        }

        public void ClearSelection()
        {
            bool changed = SelectionKind != TimelineSelectionKind.None || SelectedTrackIndex != -1 || SelectedClipIndex != -1;

            SelectionKind = TimelineSelectionKind.None;
            SelectedTrackIndex = -1;
            SelectedClipIndex = -1;

            if (changed)
                InspectorScroll = Vector2.zero;
        }

        public void SelectTimeline()
        {
            bool changed = SelectionKind != TimelineSelectionKind.Timeline || SelectedTrackIndex != -1 || SelectedClipIndex != -1;

            SelectionKind = TimelineSelectionKind.Timeline;
            SelectedTrackIndex = -1;
            SelectedClipIndex = -1;

            if (changed)
                InspectorScroll = Vector2.zero;
        }

        public void SelectTrack(int trackIndex)
        {
            bool changed = SelectionKind != TimelineSelectionKind.Track || SelectedTrackIndex != trackIndex || SelectedClipIndex != -1;

            SelectionKind = TimelineSelectionKind.Track;
            SelectedTrackIndex = trackIndex;
            SelectedClipIndex = -1;

            if (changed)
                InspectorScroll = Vector2.zero;
        }

        public void SelectClip(int trackIndex, int clipIndex)
        {
            bool changed = SelectionKind != TimelineSelectionKind.Clip || SelectedTrackIndex != trackIndex || SelectedClipIndex != clipIndex;

            SelectionKind = TimelineSelectionKind.Clip;
            SelectedTrackIndex = trackIndex;
            SelectedClipIndex = clipIndex;

            if (changed)
                InspectorScroll = Vector2.zero;
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
            DragMouseOffsetTime = Mathf.Max(0f, mouseOffsetTime);
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


        public void ClearPendingClipPress()
        {
            HasPendingClipPress = false;
            PendingClipPressTrackIndex = -1;
            PendingClipPressClipIndex = -1;
            PendingClipPressMousePosition = Vector2.zero;
            PendingClipPressManipulationMode = TimelineClipManipulationMode.None;
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
    }
}
