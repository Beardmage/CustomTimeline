using System;
using System.Collections.Generic;
using System.IO;
using Beardmage.ActionTimeline;
using UnityEditor;
using UnityEngine;

namespace Beardmage.ActionTimeline.Editor
{
    public sealed class ActionTimelineEditorWindow : EditorWindow
    {
        private sealed class ClipSnapshot
        {
            public string DebugName;
            public float StartTime;
            public TimelineAction Action;
            public bool UseDurationOverride;
            public float DurationOverride;
            public int OriginalTrackIndex;
            public int OriginalClipIndex;

            public float EffectiveDuration => UseDurationOverride
                ? Mathf.Max(0f, DurationOverride)
                : (Action ? Mathf.Max(0f, Action.NominalDuration) : 0f);
        }

        private readonly struct TimelinePointerContext
        {
            public TimelinePointerContext(
                Vector2 canvasMousePosition,
                float timeAtMouse,
                int hoveredLaneIndex,
                bool isInsideCanvas,
                bool isInsideLaneArea)
            {
                CanvasMousePosition = canvasMousePosition;
                TimeAtMouse = timeAtMouse;
                HoveredLaneIndex = hoveredLaneIndex;
                IsInsideCanvas = isInsideCanvas;
                IsInsideLaneArea = isInsideLaneArea;
            }

            public Vector2 CanvasMousePosition { get; }
            public float TimeAtMouse { get; }
            public int HoveredLaneIndex { get; }
            public bool IsInsideCanvas { get; }
            public bool IsInsideLaneArea { get; }
        }

        private static readonly float[] PixelsPerSecondOptions = { 25f, 50f, 75f, 100f, 125f, 150f, 200f, 300f, 400f };
        private static readonly string[] PixelsPerSecondLabels = { "25", "50", "75", "100", "125", "150", "200", "300", "400" };

        private const float TrackFooterButtonHeight = 24f;
        private const float TrackFooterVerticalPadding = 4f;
        private const float ClipResizeHandleWidth = 8f;
        private const float OverlayBadgeHeight = 18f;
        private const float OverlayBadgeHorizontalPadding = 6f;
        private const float OverlayBadgeWidth = 54f;
        private const float ResizeTooltipWidth = 156f;
        private const float ResizeTooltipHeight = 20f;
        private const float ResizeTooltipYOffset = 26f;

        private readonly TimelineValidator validator = new TimelineValidator();
        private TimelineEditorState state;
        private TimelinePointerContext currentPointerContext;
        private TimelineEditorThemeAsset timelineTheme;
        private TimelineEditorSettingsAsset timelineSettings;

        private TimelineEditorThemeAsset ActiveTheme => timelineTheme ? timelineTheme : TimelineEditorThemeDefaults.Instance;
        private TimelineEditorSettingsAsset ActiveSettings => timelineSettings ? timelineSettings : TimelineEditorSettingsDefaults.Instance;
        private float DragStartPixelThreshold => ActiveSettings.DragStartPixelThreshold;
        private float SnapThresholdPixels => ActiveSettings.SnapThresholdPixels;

        [MenuItem("Tools/Action Timeline/Timeline Editor")]
        public static void OpenEmptyWindow()
        {
            ActionTimelineEditorWindow window = GetWindow<ActionTimelineEditorWindow>("Action Timeline");
            window.minSize = new Vector2(1100f, 520f);
            window.Show();
        }

        public static void Open(ActionTimelineAsset timeline)
        {
            ActionTimelineEditorWindow window = GetWindow<ActionTimelineEditorWindow>("Action Timeline");
            window.minSize = new Vector2(1100f, 520f);
            window.Show();
            window.SetTimeline(timeline);
            window.Focus();
        }

        public static void OpenAndFocusClip(ActionTimelineAsset timeline, int trackIndex, int clipIndex)
        {
            ActionTimelineEditorWindow window = GetWindow<ActionTimelineEditorWindow>("Action Timeline");
            window.minSize = new Vector2(1100f, 520f);
            window.Show();
            window.SetTimeline(timeline);
            window.Focus();
            window.FocusClip(trackIndex, clipIndex);
        }

        private void OnEnable()
        {
            EnsureState();
            RefreshEditorConfig();
            ApplyEditorSettingsToState();
        }

        private void OnDisable()
        {
            TimelineEditorStyles.Reset();
        }


        private void OnGUI()
        {
            EnsureState();
            RefreshEditorConfig();
            if (!TimelineEditorStyles.TryEnsureInitialized(ActiveTheme))
            {
                Repaint();
                return;
            }

            ResetPointerContext();
            DrawToolbar();

            if (!state.HasTimeline || state.TimelineSerializedObject == null)
            {
                DrawEmptyState();
                return;
            }

            state.TimelineSerializedObject.Update();
            SanitizeSelection();

            List<TimelineValidationResult> validationResults = validator.Validate(state.Timeline);
            Rect contentRect = GUILayoutUtility.GetRect(0f, 100000f, 0f, 100000f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            DrawMainArea(contentRect, validationResults);
            HandleGlobalShortcuts();
            HandlePendingClipPress();
            HandleGlobalManipulationLifecycle();
            state.TimelineSerializedObject.ApplyModifiedProperties();

            if (state.IsDraggingClip || state.HasPendingClipPress)
                Repaint();
        }

        private void EnsureState()
        {
            if (state != null)
                return;

            state = new TimelineEditorState();
            ApplyEditorSettingsToState();
        }

        private void RefreshEditorConfig()
        {
            timelineTheme = TimelineEditorConfigLocator.GetThemeOrFallback();
            timelineSettings = TimelineEditorConfigLocator.GetSettingsOrFallback();
        }

        private void ApplyEditorSettingsToState()
        {
            if (state == null)
                return;

            state.PixelsPerSecond = Mathf.Clamp(
                ActiveSettings.DefaultPixelsPerSecond,
                ActiveSettings.MinPixelsPerSecond,
                ActiveSettings.MaxPixelsPerSecond);
        }

        private void SetTimeline(ActionTimelineAsset timeline)
        {
            state.SetTimeline(timeline);
            ApplyEditorSettingsToState();
            ResetPointerContext();
            Repaint();
        }

        private void ResetPointerContext()
        {
            currentPointerContext = new TimelinePointerContext(Vector2.zero, 0f, -1, false, false);
            state.LastMouseCanvasPosition = Vector2.zero;
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            EditorGUI.BeginChangeCheck();
            ActionTimelineAsset newTimeline = (ActionTimelineAsset)EditorGUILayout.ObjectField(state.Timeline, typeof(ActionTimelineAsset), false, GUILayout.Width(260f));
            if (EditorGUI.EndChangeCheck())
            {
                SetTimeline(newTimeline);
                EditorGUILayout.EndHorizontal();
                GUIUtility.ExitGUI();
                return;
            }

            if (GUILayout.Button("Create New Timeline", EditorStyles.toolbarButton, GUILayout.Width(125f)))
            {
                ActionTimelineAsset created = ActionTimelineAssetCreationUtility.CreateAndSelectNewTimeline(ActionTimelineAssetCreationUtility.DefaultTimelineDirectory);
                if (created)
                    SetTimeline(created);
                EditorGUILayout.EndHorizontal();
                GUIUtility.ExitGUI();
                return;
            }

            GUI.enabled = state.HasTimeline;

            if (GUILayout.Button("Ping", EditorStyles.toolbarButton, GUILayout.Width(45f)))
                EditorAssetLinkUtility.Ping(state.Timeline);

            if (GUILayout.Button("Inspector", EditorStyles.toolbarButton, GUILayout.Width(60f)))
                EditorAssetLinkUtility.OpenInspector(state.Timeline);

            if (GUILayout.Button("Add Track", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                AddTrack();

            if (GUILayout.Button("Add Clip", EditorStyles.toolbarButton, GUILayout.Width(65f)))
                AddClipToBestTrack();

            if (GUILayout.Button("Delete", EditorStyles.toolbarButton, GUILayout.Width(55f)))
                DeleteSelection();

            if (GUILayout.Button("Auto Arrange", EditorStyles.toolbarButton, GUILayout.Width(90f)))
                AutoArrangeTracks();

            DrawPixelsPerSecondPopup();
            GUILayout.FlexibleSpace();

            bool newShowShortcuts = GUILayout.Toggle(state.ShowShortcutHints, "⌨", EditorStyles.toolbarButton, GUILayout.Width(26f));
            if (newShowShortcuts != state.ShowShortcutHints)
                state.ShowShortcutHints = newShowShortcuts;

            if (state.HasTimeline)
            {
                int trackCount = GetSerializedTrackCount();
                int validClipCount = validator.CountValidClips(state.Timeline);
                float duration = GetCurrentTimelineDuration();
                GUILayout.Label($"Tracks: {trackCount} | Valid Clips: {validClipCount} | Duration: {TimelineDurationUtility.FormatSeconds(duration)}", EditorStyles.miniLabel);
            }

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPixelsPerSecondPopup()
        {
            GUILayout.Label("Scale", EditorStyles.miniLabel, GUILayout.Width(30f));
            int currentIndex = GetClosestPixelsPerSecondIndex(state.PixelsPerSecond);
            int newIndex = EditorGUILayout.Popup(currentIndex, PixelsPerSecondLabels, EditorStyles.toolbarPopup, GUILayout.Width(55f));
            state.PixelsPerSecond = Mathf.Clamp(PixelsPerSecondOptions[Mathf.Clamp(newIndex, 0, PixelsPerSecondOptions.Length - 1)], ActiveSettings.MinPixelsPerSecond, ActiveSettings.MaxPixelsPerSecond);
            GUILayout.Label("px/s", EditorStyles.miniLabel, GUILayout.Width(30f));
        }

        private static int GetClosestPixelsPerSecondIndex(float pixelsPerSecond)
        {
            int bestIndex = 0;
            float bestDistance = Mathf.Abs(PixelsPerSecondOptions[0] - pixelsPerSecond);
            for (int i = 1; i < PixelsPerSecondOptions.Length; i++)
            {
                float distance = Mathf.Abs(PixelsPerSecondOptions[i] - pixelsPerSecond);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = i;
                }
            }
            return bestIndex;
        }

        private void DrawEmptyState()
        {
            GUILayout.FlexibleSpace();
            Rect rect = GUILayoutUtility.GetRect(320f, 110f, GUILayout.ExpandWidth(true));
            float width = Mathf.Min(420f, rect.width - 40f);
            Rect centeredRect = new Rect(rect.center.x - (width * 0.5f), rect.y + 10f, width, 90f);
            GUI.Box(centeredRect, "Select a ActionTimelineAsset asset in the toolbar or create a new one.", TimelineEditorStyles.EmptyStateStyle);
            GUILayout.FlexibleSpace();
        }

        private void DrawMainArea(Rect rect, List<TimelineValidationResult> validationResults)
        {
            float inspectorWidth = Mathf.Clamp(state.InspectorWidth, 280f, 420f);
            Rect trackColumnRect = new Rect(rect.x, rect.y, TimelineEditorStyles.TrackColumnWidth, rect.height);
            Rect inspectorRect = new Rect(rect.xMax - inspectorWidth, rect.y, inspectorWidth, rect.height);
            Rect canvasRect = new Rect(trackColumnRect.xMax + 2f, rect.y, Mathf.Max(120f, inspectorRect.xMin - trackColumnRect.xMax - 4f), rect.height);

            EditorGUI.DrawRect(rect, TimelineEditorStyles.WindowBackground);
            DrawTrackColumn(trackColumnRect, validationResults);
            DrawTimelineCanvas(canvasRect, validationResults);
            EditorGUI.DrawRect(new Rect(inspectorRect.x, inspectorRect.y, 1f, inspectorRect.height), TimelineEditorStyles.MajorGridLineColor);
            DrawInspectorPanel(inspectorRect, validationResults);
        }

        private void DrawTrackColumn(Rect rect, List<TimelineValidationResult> validationResults)
        {
            EditorGUI.DrawRect(rect, TimelineEditorStyles.PanelBackground);
            Rect headerRect = new Rect(rect.x, rect.y, rect.width, TimelineEditorStyles.RulerHeight);
            EditorGUI.DrawRect(headerRect, TimelineEditorStyles.RulerBackground);
            GUI.Label(new Rect(headerRect.x + 8f, headerRect.y, headerRect.width - 16f, headerRect.height), "Tracks", TimelineEditorStyles.TrackLabelStyle);

            Rect scrollRect = new Rect(rect.x, rect.y + TimelineEditorStyles.RulerHeight, rect.width, rect.height - TimelineEditorStyles.RulerHeight);
            SerializedProperty tracksProperty = GetTracksProperty();
            int trackCount = tracksProperty != null ? tracksProperty.arraySize : 0;
            float footerHeight = ActiveSettings.ShowBottomAddTrackButton ? TrackFooterButtonHeight + (TrackFooterVerticalPadding * 2f) : 0f;
            float contentHeight = Mathf.Max(scrollRect.height - 1f, (trackCount * TimelineEditorStyles.LaneHeight) + footerHeight);

            Vector2 trackScroll = GUI.BeginScrollView(scrollRect, new Vector2(0f, state.CanvasScroll.y), new Rect(0f, 0f, rect.width - 16f, contentHeight));
            if (!Mathf.Approximately(trackScroll.y, state.CanvasScroll.y))
                state.CanvasScroll = new Vector2(state.CanvasScroll.x, trackScroll.y);

            for (int trackIndex = 0; trackIndex < trackCount; trackIndex++)
            {
                SerializedProperty trackProperty = tracksProperty.GetArrayElementAtIndex(trackIndex);
                Rect rowRect = new Rect(0f, trackIndex * TimelineEditorStyles.LaneHeight, rect.width - 16f, TimelineEditorStyles.LaneHeight);
                DrawTrackHeaderRow(rowRect, trackProperty, trackIndex, validationResults);
            }

            if (ActiveSettings.ShowBottomAddTrackButton)
            {
                Rect footerRect = new Rect(0f, trackCount * TimelineEditorStyles.LaneHeight, rect.width - 16f, footerHeight);
                DrawAddTrackFooterRow(footerRect);
            }

            GUI.EndScrollView();
        }

        private void DrawTrackHeaderRow(Rect rowRect, SerializedProperty trackProperty, int trackIndex, List<TimelineValidationResult> validationResults)
        {
            bool isSelected = (state.SelectionKind == TimelineSelectionKind.Track && state.SelectedTrackIndex == trackIndex) ||
                              (state.SelectionKind == TimelineSelectionKind.Clip && state.SelectedTrackIndex == trackIndex);
            EditorGUI.DrawRect(rowRect, trackIndex % 2 == 0 ? TimelineEditorStyles.LaneEvenBackground : TimelineEditorStyles.LaneOddBackground);
            if (isSelected)
                EditorGUI.DrawRect(new Rect(rowRect.x, rowRect.y, 3f, rowRect.height), TimelineEditorStyles.ClipSelectedColor);

            SerializedProperty enabledProperty = trackProperty.FindPropertyRelative("isEnabled");
            SerializedProperty nameProperty = trackProperty.FindPropertyRelative("trackName");
            SerializedProperty clipsProperty = trackProperty.FindPropertyRelative("clips");

            Rect toggleRect = new Rect(rowRect.x + 6f, rowRect.y + 5f, 16f, 16f);
            Rect labelRect = new Rect(rowRect.x + 26f, rowRect.y, rowRect.width - 60f, rowRect.height);
            Rect badgeRect = new Rect(rowRect.xMax - 28f, rowRect.y + 5f, 20f, 16f);

            EditorGUI.BeginChangeCheck();
            bool newEnabled = EditorGUI.Toggle(toggleRect, enabledProperty.boolValue);
            if (EditorGUI.EndChangeCheck())
                enabledProperty.boolValue = newEnabled;

            string trackName = string.IsNullOrWhiteSpace(nameProperty.stringValue) ? $"Track {trackIndex + 1}" : nameProperty.stringValue;
            GUI.Label(labelRect, trackName, TimelineEditorStyles.TrackLabelStyle);
            GUI.Label(badgeRect, (clipsProperty != null ? clipsProperty.arraySize : 0).ToString(), TimelineEditorStyles.MiniBadgeStyle);

            if (HasTrackValidationIssue(validationResults, trackIndex))
                EditorGUI.DrawRect(new Rect(rowRect.xMax - 6f, rowRect.y + 4f, 3f, rowRect.height - 8f), TimelineEditorStyles.WarningColor);

            Event current = Event.current;
            if (current.type == EventType.MouseDown && current.button == 0 && rowRect.Contains(current.mousePosition))
            {
                ClearEditorKeyboardFocusIfConfigured();
                state.SelectTrack(trackIndex);
                state.ClearPendingClipPress();
                current.Use();
                Repaint();
            }

            if (current.type == EventType.ContextClick && rowRect.Contains(current.mousePosition))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Add Clip"), false, () =>
                {
                    ClearEditorKeyboardFocusIfConfigured();
                    state.SelectTrack(trackIndex);
                    AddClipToTrack(trackIndex, 0f);
                });
                menu.AddSeparator(string.Empty);
                menu.AddItem(new GUIContent("Delete Track"), false, () =>
                {
                    ClearEditorKeyboardFocusIfConfigured();
                    state.SelectTrack(trackIndex);
                    DeleteSelection();
                });
                menu.ShowAsContext();
                current.Use();
            }
        }

        private void DrawAddTrackFooterRow(Rect rowRect)
        {
            EditorGUI.DrawRect(rowRect, TimelineEditorStyles.AddTrackButtonColor);
            Rect separatorRect = new Rect(rowRect.x, rowRect.y, rowRect.width, 1f);
            EditorGUI.DrawRect(separatorRect, TimelineEditorStyles.MajorGridLineColor);

            Rect buttonRect = new Rect(rowRect.x + 8f, rowRect.y + TrackFooterVerticalPadding, Mathf.Max(60f, rowRect.width - 16f), TrackFooterButtonHeight);
            GUIContent buttonContent = new GUIContent("+ Add Track");
            if (GUI.Button(buttonRect, buttonContent, EditorStyles.miniButton))
            {
                AddTrack();
                GUIUtility.ExitGUI();
            }
        }

        private void DrawTimelineCanvas(Rect rect, List<TimelineValidationResult> validationResults)
        {
            EditorGUI.DrawRect(rect, TimelineEditorStyles.PanelBackground);
            Rect rulerRect = new Rect(rect.x, rect.y, rect.width, TimelineEditorStyles.RulerHeight);
            Rect scrollRect = new Rect(rect.x, rect.y + TimelineEditorStyles.RulerHeight, rect.width, rect.height - TimelineEditorStyles.RulerHeight);

            SerializedProperty tracksProperty = GetTracksProperty();
            int trackCount = tracksProperty != null ? tracksProperty.arraySize : 0;
            float duration = GetCurrentTimelineDuration();
            float contentWidth = Mathf.Max(scrollRect.width - 16f, (duration * state.PixelsPerSecond) + 120f);
            float contentHeight = Mathf.Max(scrollRect.height - 1f, trackCount * TimelineEditorStyles.LaneHeight);

            DrawTimeRuler(rulerRect);
            Rect contentRect = new Rect(0f, 0f, contentWidth, contentHeight);
            state.CanvasScroll = GUI.BeginScrollView(scrollRect, state.CanvasScroll, contentRect);
            currentPointerContext = BuildPointerContext(contentRect, trackCount);
            state.LastMouseCanvasPosition = currentPointerContext.CanvasMousePosition;

            if (TryHandleImmediateCanvasInteractionBeforeDraw(trackCount))
            {
                GUI.EndScrollView();
                GUIUtility.ExitGUI();
                return;
            }

            DrawCanvasBackground(contentRect, trackCount);
            DrawGrid(contentRect, duration);
            DrawTimelineEndMarker(duration, contentRect.height);

            bool clickedClip = false;
            for (int trackIndex = 0; trackIndex < trackCount; trackIndex++)
            {
                SerializedProperty trackProperty = tracksProperty.GetArrayElementAtIndex(trackIndex);
                SerializedProperty clipsProperty = trackProperty.FindPropertyRelative("clips");
                Rect laneRect = new Rect(0f, trackIndex * TimelineEditorStyles.LaneHeight, contentWidth, TimelineEditorStyles.LaneHeight);
                clickedClip |= DrawTrackLane(trackIndex, laneRect, clipsProperty, validationResults);
            }

            if (state.IsDraggingClip)
            {
                UpdateActiveManipulationPreview(trackCount);
                DrawManipulationPreview();
            }

            HandleCanvasBackgroundSelection(clickedClip, currentPointerContext);
            GUI.EndScrollView();
        }

        private void DrawTimeRuler(Rect rect)
        {
            EditorGUI.DrawRect(rect, TimelineEditorStyles.RulerBackground);
            float visibleStartTime = state.CanvasScroll.x / Mathf.Max(0.0001f, state.PixelsPerSecond);
            float visibleEndTime = (state.CanvasScroll.x + rect.width) / Mathf.Max(0.0001f, state.PixelsPerSecond);
            float majorStep = state.PixelsPerSecond >= 150f ? 0.5f : 1f;
            float start = Mathf.Floor(visibleStartTime / majorStep) * majorStep;

            for (float time = start; time <= visibleEndTime + majorStep; time += majorStep)
            {
                float x = rect.x + (time * state.PixelsPerSecond) - state.CanvasScroll.x;
                EditorGUI.DrawRect(new Rect(x, rect.y, 1f, rect.height), TimelineEditorStyles.MajorGridLineColor);
                GUI.Label(new Rect(x + 4f, rect.y + 2f, 48f, rect.height - 4f), TimelineDurationUtility.FormatSeconds(time), EditorStyles.miniLabel);
            }
        }

        private void DrawCanvasBackground(Rect contentRect, int trackCount)
        {
            for (int trackIndex = 0; trackIndex < trackCount; trackIndex++)
            {
                Rect laneRect = new Rect(0f, trackIndex * TimelineEditorStyles.LaneHeight, contentRect.width, TimelineEditorStyles.LaneHeight);
                EditorGUI.DrawRect(laneRect, trackIndex % 2 == 0 ? TimelineEditorStyles.LaneEvenBackground : TimelineEditorStyles.LaneOddBackground);
            }
        }

        private void DrawGrid(Rect contentRect, float duration)
        {
            float maxTime = Mathf.Max(duration, contentRect.width / Mathf.Max(0.0001f, state.PixelsPerSecond));
            float minorStep = state.PixelsPerSecond >= 150f ? 0.25f : 0.5f;
            float majorStep = state.PixelsPerSecond >= 150f ? 0.5f : 1f;

            for (float time = 0f; time <= maxTime + majorStep; time += minorStep)
            {
                bool isMajor = Mathf.Approximately(Mathf.Repeat(time, majorStep), 0f);
                float x = time * state.PixelsPerSecond;
                EditorGUI.DrawRect(new Rect(x, 0f, 1f, contentRect.height), isMajor ? TimelineEditorStyles.MajorGridLineColor : TimelineEditorStyles.GridLineColor);
            }
        }

        private void DrawTimelineEndMarker(float duration, float contentHeight)
        {
            float x = duration * state.PixelsPerSecond;
            EditorGUI.DrawRect(new Rect(x, 0f, 2f, contentHeight), TimelineEditorStyles.TimelineEndColor);
        }

        private bool DrawTrackLane(int trackIndex, Rect laneRect, SerializedProperty clipsProperty, List<TimelineValidationResult> validationResults)
        {
            bool clickedClip = false;
            int clipCount = clipsProperty != null ? clipsProperty.arraySize : 0;
            for (int clipIndex = 0; clipIndex < clipCount; clipIndex++)
            {
                if (state.IsDraggingClip && trackIndex == state.DragSourceTrackIndex && clipIndex == state.DragSourceClipIndex)
                    continue;

                SerializedProperty clipProperty = clipsProperty.GetArrayElementAtIndex(clipIndex);
                clickedClip |= DrawClip(trackIndex, clipIndex, laneRect, clipProperty, validationResults);
            }

            Event current = Event.current;
            if (current.type == EventType.MouseDown && current.button == 0 && laneRect.Contains(current.mousePosition) && !clickedClip)
            {
                if (ActiveSettings.SelectTrackOnBackgroundClick)
                    state.SelectTrack(trackIndex);
                else
                    state.SelectTimeline();

                ClearEditorKeyboardFocusIfConfigured();
                state.ClearPendingClipPress();
                current.Use();
                Repaint();
            }

            if (ActiveSettings.EnableContextCreateClipHere && current.type == EventType.ContextClick && laneRect.Contains(current.mousePosition) && !clickedClip)
            {
                float startTime = currentPointerContext.TimeAtMouse;
                if (ActiveSettings.SnapCreateClipHere)
                    startTime = GetSnappedStartTimeForTrack(trackIndex, startTime, 0f);

                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Create Clip Here"), false, () => AddClipToTrack(trackIndex, startTime));
                menu.ShowAsContext();
                current.Use();
            }

            if (HasTrackOverlapIssue(validationResults, trackIndex))
                EditorGUI.DrawRect(new Rect(laneRect.x, laneRect.yMax - 2f, laneRect.width, 2f), TimelineEditorStyles.ErrorColor);

            return clickedClip;
        }

        private bool DrawClip(int trackIndex, int clipIndex, Rect laneRect, SerializedProperty clipProperty, List<TimelineValidationResult> validationResults)
        {
            string debugName = clipProperty.FindPropertyRelative("debugName").stringValue;
            float startTime = Mathf.Max(0f, clipProperty.FindPropertyRelative("startTime").floatValue);
            TimelineAction action = (TimelineAction)clipProperty.FindPropertyRelative("action").objectReferenceValue;
            bool useDurationOverride = clipProperty.FindPropertyRelative("useDurationOverride").boolValue;
            float durationOverride = Mathf.Max(0f, clipProperty.FindPropertyRelative("durationOverride").floatValue);
            float logicalDuration = useDurationOverride ? durationOverride : (action ? Mathf.Max(0f, action.NominalDuration) : 0f);

            Rect clipRect = TimelineRectUtility.GetClipRect(startTime, logicalDuration, state.PixelsPerSecond, 0f, laneRect.y, laneRect.height, TimelineEditorStyles.MinClipVisualWidth);
            Rect leftResizeRect = new Rect(clipRect.x, clipRect.y, Mathf.Min(ClipResizeHandleWidth, clipRect.width * 0.5f), clipRect.height);
            Rect rightResizeRect = new Rect(clipRect.xMax - Mathf.Min(ClipResizeHandleWidth, clipRect.width * 0.5f), clipRect.y, Mathf.Min(ClipResizeHandleWidth, clipRect.width * 0.5f), clipRect.height);

            bool isSelected = state.SelectionKind == TimelineSelectionKind.Clip && state.SelectedTrackIndex == trackIndex && state.SelectedClipIndex == clipIndex;
            bool hasClipError = HasClipValidationIssue(validationResults, trackIndex, clipIndex, TimelineValidationSeverity.Error);
            bool hasClipWarning = HasClipValidationIssue(validationResults, trackIndex, clipIndex, TimelineValidationSeverity.Warning);

            Color clipColor = ResolveClipBackgroundColor(action, isSelected, hasClipError);
            EditorGUI.DrawRect(clipRect, clipColor);
            EditorGUI.DrawRect(new Rect(clipRect.x, clipRect.y, clipRect.width, 1f), TimelineEditorStyles.ClipBorderColor);
            EditorGUI.DrawRect(new Rect(clipRect.x, clipRect.yMax - 1f, clipRect.width, 1f), TimelineEditorStyles.ClipBorderColor);

            if (hasClipError)
            {
                EditorGUI.DrawRect(new Rect(clipRect.x, clipRect.y, clipRect.width, 2f), TimelineEditorStyles.ErrorColor);
                EditorGUI.DrawRect(new Rect(clipRect.x, clipRect.yMax - 2f, clipRect.width, 2f), TimelineEditorStyles.ErrorColor);
            }
            else if (hasClipWarning)
            {
                EditorGUI.DrawRect(new Rect(clipRect.x, clipRect.yMax - 3f, clipRect.width, 3f), TimelineEditorStyles.WarningColor);
            }

            EditorGUIUtility.AddCursorRect(leftResizeRect, MouseCursor.ResizeHorizontal);
            EditorGUIUtility.AddCursorRect(rightResizeRect, MouseCursor.ResizeHorizontal);

            GUI.Label(clipRect, BuildClipDisplayName(debugName, action, clipIndex), TimelineEditorStyles.ClipLabelStyle);

            Event current = Event.current;
            if (current.type == EventType.MouseDown && current.button == 0 && clipRect.Contains(current.mousePosition))
            {
                TimelineClipManipulationMode manipulationMode = TimelineClipManipulationMode.Move;
                if (rightResizeRect.Contains(current.mousePosition))
                    manipulationMode = TimelineClipManipulationMode.ResizeRight;
                else if (leftResizeRect.Contains(current.mousePosition))
                    manipulationMode = TimelineClipManipulationMode.ResizeLeft;

                ClearEditorKeyboardFocus();
                state.SelectClip(trackIndex, clipIndex);
                state.BeginPendingClipPress(trackIndex, clipIndex, current.mousePosition, manipulationMode);
                current.Use();
                Repaint();
                return true;
            }

            if (current.type == EventType.ContextClick && clipRect.Contains(current.mousePosition))
            {
                ClearEditorKeyboardFocus();
                state.SelectClip(trackIndex, clipIndex);
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Set Start Time..."), false, () =>
                {
                    ClearEditorKeyboardFocusIfConfigured();
                    float currentStartTime = Mathf.Max(0f, clipProperty.FindPropertyRelative("startTime").floatValue);
                    OpenSetStartTimePrompt(trackIndex, clipIndex, currentStartTime);
                });
                menu.AddSeparator(string.Empty);
                menu.AddItem(new GUIContent("Duplicate"), false, () =>
                {
                    ClearEditorKeyboardFocus();
                    state.SelectClip(trackIndex, clipIndex);
                    DuplicateSelectedClip();
                });
                menu.AddItem(new GUIContent("Delete"), false, () =>
                {
                    ClearEditorKeyboardFocus();
                    state.SelectClip(trackIndex, clipIndex);
                    DeleteSelection();
                });
                menu.ShowAsContext();
                current.Use();
                Repaint();
                return true;
            }

            return false;
        }

        private void HandlePendingClipPress()
        {
            if (!state.HasPendingClipPress || state.IsDraggingClip)
                return;

            Event current = Event.current;
            if (current.type == EventType.MouseDrag)
            {
                float distance = Vector2.Distance(current.mousePosition, state.PendingClipPressMousePosition);
                if (distance >= DragStartPixelThreshold)
                {
                    BeginClipManipulation(
                        state.PendingClipPressTrackIndex,
                        state.PendingClipPressClipIndex,
                        state.PendingClipPressManipulationMode,
                        state.PendingClipPressMousePosition);
                    state.ClearPendingClipPress();
                    current.Use();
                    Repaint();
                }
                return;
            }

            if (current.type == EventType.MouseUp && current.button == 0)
            {
                state.ClearPendingClipPress();
                current.Use();
                Repaint();
            }
        }

        private void BeginClipManipulation(int trackIndex, int clipIndex, TimelineClipManipulationMode manipulationMode, Vector2 mousePosition)
        {
            SerializedProperty clipProperty = GetClipProperty(trackIndex, clipIndex);
            if (clipProperty == null)
                return;

            float clipStartTime = GetClipStartTime(clipProperty);
            float clipDuration = GetClipEffectiveDuration(clipProperty);
            float clipEndTime = clipStartTime + clipDuration;
            float mouseTime = currentPointerContext.IsInsideCanvas
                ? currentPointerContext.TimeAtMouse
                : TimelineRectUtility.PixelToTimeClamped(mousePosition.x, state.PixelsPerSecond, 0f);

            switch (manipulationMode)
            {
                case TimelineClipManipulationMode.ResizeLeft:
                    state.BeginClipManipulation(
                        manipulationMode,
                        trackIndex,
                        clipIndex,
                        clipStartTime,
                        clipDuration,
                        0f,
                        clipStartTime,
                        clipDuration,
                        clipEndTime);
                    break;

                case TimelineClipManipulationMode.ResizeRight:
                    state.BeginClipManipulation(
                        manipulationMode,
                        trackIndex,
                        clipIndex,
                        clipStartTime,
                        clipDuration,
                        0f,
                        clipStartTime,
                        clipDuration,
                        clipStartTime);
                    break;

                default:
                    state.BeginClipManipulation(
                        TimelineClipManipulationMode.Move,
                        trackIndex,
                        clipIndex,
                        clipStartTime,
                        clipDuration,
                        Mathf.Max(0f, mouseTime - clipStartTime),
                        clipStartTime,
                        clipDuration,
                        clipEndTime);
                    break;
            }

            state.SelectClip(trackIndex, clipIndex);
        }

        private void UpdateActiveManipulationPreview(int trackCount)
        {
            if (!state.IsDraggingClip)
                return;

            SerializedProperty clipProperty = GetDragSourceClipProperty();
            if (clipProperty == null)
                return;

            switch (state.ManipulationMode)
            {
                case TimelineClipManipulationMode.ResizeLeft:
                    UpdateResizeLeftPreview(clipProperty);
                    break;

                case TimelineClipManipulationMode.ResizeRight:
                    UpdateResizeRightPreview(clipProperty);
                    break;

                default:
                    UpdateMovePreview(trackCount);
                    break;
            }
        }

        private void UpdateMovePreview(int trackCount)
        {
            SerializedProperty clipProperty = GetDragSourceClipProperty();
            if (clipProperty == null)
                return;

            float rawStart = Mathf.Max(0f, currentPointerContext.TimeAtMouse - state.DragMouseOffsetTime);
            int previewTrackIndex = state.DragPreviewTrackIndex >= 0 ? state.DragPreviewTrackIndex : state.DragSourceTrackIndex;

            if (trackCount > 0 && currentPointerContext.IsInsideLaneArea)
            {
                previewTrackIndex = Mathf.Clamp(currentPointerContext.HoveredLaneIndex, 0, Mathf.Max(0, trackCount - 1));
            }
            else if (previewTrackIndex < 0 && trackCount > 0)
            {
                previewTrackIndex = Mathf.Clamp(state.DragSourceTrackIndex, 0, Mathf.Max(0, trackCount - 1));
            }

            float movingDuration = state.ManipulationInitialDuration;
            float snappedStart = GetSnappedStartTimeForTrack(previewTrackIndex, rawStart, movingDuration);
            bool isValid = CanPlaceSourceClipAt(previewTrackIndex, snappedStart, movingDuration);
            state.SetDragPreview(previewTrackIndex, snappedStart, movingDuration, isValid);
        }

        private void UpdateResizeLeftPreview(SerializedProperty clipProperty)
        {
            float fixedEndTime = state.ManipulationFixedEdgeTime;
            float rawStart = Mathf.Clamp(currentPointerContext.TimeAtMouse, 0f, fixedEndTime);
            float tentativeDuration = Mathf.Max(0f, fixedEndTime - rawStart);
            float snappedStart = GetSnappedStartTimeForTrack(state.DragSourceTrackIndex, rawStart, tentativeDuration);
            snappedStart = Mathf.Clamp(snappedStart, 0f, fixedEndTime);
            float previewDuration = Mathf.Max(0f, fixedEndTime - snappedStart);
            bool isValid = CanPlaceSourceClipAt(state.DragSourceTrackIndex, snappedStart, previewDuration);

            state.SetDragPreview(state.DragSourceTrackIndex, snappedStart, previewDuration, isValid);
        }

        private void UpdateResizeRightPreview(SerializedProperty clipProperty)
        {
            float fixedStartTime = state.ManipulationFixedEdgeTime;
            float rawEnd = Mathf.Max(fixedStartTime, currentPointerContext.TimeAtMouse);
            float snappedEnd = GetSnappedEndTimeForTrack(state.DragSourceTrackIndex, rawEnd, fixedStartTime);
            snappedEnd = Mathf.Max(fixedStartTime, snappedEnd);
            float previewDuration = Mathf.Max(0f, snappedEnd - fixedStartTime);
            bool isValid = CanPlaceSourceClipAt(state.DragSourceTrackIndex, fixedStartTime, previewDuration);

            state.SetDragPreview(state.DragSourceTrackIndex, fixedStartTime, previewDuration, isValid);
        }

        private float GetSnappedStartTimeForTrack(int trackIndex, float rawStartTime, float movingDuration)
        {
            SerializedProperty trackProperty = GetTrackProperty(trackIndex);
            if (trackProperty == null)
                return rawStartTime;

            SerializedProperty clipsProperty = trackProperty.FindPropertyRelative("clips");
            if (clipsProperty == null)
                return rawStartTime;

            float snapThresholdTime = SnapThresholdPixels / Mathf.Max(0.0001f, state.PixelsPerSecond);
            float bestSnappedStart = rawStartTime;
            float bestDistance = float.MaxValue;

            int clipCount = clipsProperty.arraySize;
            for (int clipIndex = 0; clipIndex < clipCount; clipIndex++)
            {
                if (trackIndex == state.DragSourceTrackIndex && clipIndex == state.DragSourceClipIndex)
                    continue;

                SerializedProperty otherClipProperty = clipsProperty.GetArrayElementAtIndex(clipIndex);
                if (otherClipProperty == null)
                    continue;

                float otherStart = GetClipStartTime(otherClipProperty);
                float otherDuration = GetClipEffectiveDuration(otherClipProperty);
                float otherEnd = otherStart + otherDuration;

                float candidateStartFromEndToStart = Mathf.Max(0f, otherStart - Mathf.Max(0f, movingDuration));
                TryAcceptStartSnapCandidate(trackIndex, rawStartTime, movingDuration, candidateStartFromEndToStart, snapThresholdTime, ref bestSnappedStart, ref bestDistance);

                if (otherDuration > 0f)
                    TryAcceptStartSnapCandidate(trackIndex, rawStartTime, movingDuration, Mathf.Max(0f, otherEnd), snapThresholdTime, ref bestSnappedStart, ref bestDistance);
            }

            return bestSnappedStart;
        }

        private float GetSnappedEndTimeForTrack(int trackIndex, float rawEndTime, float fixedStartTime)
        {
            SerializedProperty trackProperty = GetTrackProperty(trackIndex);
            if (trackProperty == null)
                return rawEndTime;

            SerializedProperty clipsProperty = trackProperty.FindPropertyRelative("clips");
            if (clipsProperty == null)
                return rawEndTime;

            float snapThresholdTime = SnapThresholdPixels / Mathf.Max(0.0001f, state.PixelsPerSecond);
            float bestSnappedEnd = rawEndTime;
            float bestDistance = float.MaxValue;

            int clipCount = clipsProperty.arraySize;
            for (int clipIndex = 0; clipIndex < clipCount; clipIndex++)
            {
                if (trackIndex == state.DragSourceTrackIndex && clipIndex == state.DragSourceClipIndex)
                    continue;

                SerializedProperty otherClipProperty = clipsProperty.GetArrayElementAtIndex(clipIndex);
                if (otherClipProperty == null)
                    continue;

                float otherStart = GetClipStartTime(otherClipProperty);
                float candidateDuration = Mathf.Max(0f, otherStart - fixedStartTime);
                float distance = Mathf.Abs(otherStart - rawEndTime);
                if (distance > snapThresholdTime || distance >= bestDistance)
                    continue;

                if (!CanPlaceSourceClipAt(trackIndex, fixedStartTime, candidateDuration))
                    continue;

                bestDistance = distance;
                bestSnappedEnd = otherStart;
            }

            return bestSnappedEnd;
        }

        private void TryAcceptStartSnapCandidate(int trackIndex, float rawStartTime, float movingDuration, float candidateStartTime, float snapThresholdTime, ref float bestSnappedStart, ref float bestDistance)
        {
            float distance = Mathf.Abs(candidateStartTime - rawStartTime);
            if (distance > snapThresholdTime || distance >= bestDistance)
                return;

            if (!CanPlaceSourceClipAt(trackIndex, candidateStartTime, movingDuration))
                return;

            bestDistance = distance;
            bestSnappedStart = candidateStartTime;
        }

        private void DrawManipulationPreview()
        {
            SerializedProperty clipProperty = GetDragSourceClipProperty();
            if (clipProperty == null)
                return;

            int previewTrackIndex = Mathf.Max(0, state.DragPreviewTrackIndex);
            Rect laneRect = new Rect(0f, previewTrackIndex * TimelineEditorStyles.LaneHeight, 100000f, TimelineEditorStyles.LaneHeight);
            Rect previewRect = TimelineRectUtility.GetClipRect(
                state.DragPreviewStartTime,
                state.DragPreviewDuration,
                state.PixelsPerSecond,
                0f,
                laneRect.y,
                laneRect.height,
                TimelineEditorStyles.MinClipVisualWidth);

            Color previewColor = state.DragPreviewValid ? TimelineEditorStyles.ClipPreviewValidColor : TimelineEditorStyles.ClipPreviewInvalidColor;
            EditorGUI.DrawRect(previewRect, previewColor);

            string debugName = clipProperty.FindPropertyRelative("debugName").stringValue;
            TimelineAction action = (TimelineAction)clipProperty.FindPropertyRelative("action").objectReferenceValue;
            GUI.Label(previewRect, BuildClipDisplayName(debugName, action, state.DragSourceClipIndex), TimelineEditorStyles.ClipLabelStyle);

            DrawManipulationTimeBadges(previewRect, state.DragPreviewStartTime, state.DragPreviewStartTime + state.DragPreviewDuration);

            if (state.ManipulationMode == TimelineClipManipulationMode.ResizeLeft || state.ManipulationMode == TimelineClipManipulationMode.ResizeRight)
                DrawResizeDurationTooltip(state.DragPreviewDuration, currentPointerContext.CanvasMousePosition, previewRect);
        }

        private void DrawManipulationTimeBadges(Rect previewRect, float startTime, float endTime)
        {
            Rect leftBadgeRect = new Rect(previewRect.x + 4f, previewRect.y + 2f, OverlayBadgeWidth, OverlayBadgeHeight);
            Rect rightBadgeRect = new Rect(previewRect.xMax - OverlayBadgeWidth - 4f, previewRect.y + 2f, OverlayBadgeWidth, OverlayBadgeHeight);

            DrawOverlayBadge(leftBadgeRect, TimelineDurationUtility.FormatSeconds(startTime), TextAnchor.MiddleLeft);
            DrawOverlayBadge(rightBadgeRect, TimelineDurationUtility.FormatSeconds(endTime), TextAnchor.MiddleRight);
        }

        private void DrawResizeDurationTooltip(float previewDuration, Vector2 canvasMousePosition, Rect previewRect)
        {
            float tooltipX = Mathf.Clamp(canvasMousePosition.x - (ResizeTooltipWidth * 0.5f), previewRect.xMin, previewRect.xMax - ResizeTooltipWidth);
            float tooltipY = Mathf.Max(previewRect.yMin - ResizeTooltipYOffset, 0f);
            Rect tooltipRect = new Rect(tooltipX, tooltipY, ResizeTooltipWidth, ResizeTooltipHeight);
            string tooltipText = TimelineDurationUtility.FormatSeconds(previewDuration) + " • Override";
            DrawOverlayBadge(tooltipRect, tooltipText, TextAnchor.MiddleCenter);
        }

        private void DrawOverlayBadge(Rect rect, string text, TextAnchor alignment)
        {
            EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.65f));
            GUIStyle style = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = alignment,
                padding = new RectOffset((int)OverlayBadgeHorizontalPadding, (int)OverlayBadgeHorizontalPadding, 0, 0),
                normal = { textColor = Color.white },
                fontStyle = FontStyle.Bold
            };
            GUI.Label(rect, text, style);
        }

        private bool TryHandleImmediateCanvasInteractionBeforeDraw(int trackCount)
        {
            if (!state.IsDraggingClip)
                return false;

            Event current = Event.current;
            if (current.type == EventType.MouseUp && current.button == 0)
            {
                UpdateActiveManipulationPreview(trackCount);
                CommitClipManipulation();
                current.Use();
                return true;
            }

            if (current.type == EventType.KeyDown && current.keyCode == KeyCode.Escape)
            {
                CancelClipManipulation();
                current.Use();
                return true;
            }

            return false;
        }

        private void HandleGlobalManipulationLifecycle()
        {
            if (!state.IsDraggingClip)
                return;

            Event current = Event.current;
            if (current.type == EventType.MouseDrag)
            {
                Repaint();
                return;
            }

            if (current.type == EventType.MouseUp && current.button == 0)
            {
                CommitClipManipulation();
                current.Use();
                return;
            }

            if (current.type == EventType.KeyDown && current.keyCode == KeyCode.Escape)
            {
                CancelClipManipulation();
                current.Use();
            }
        }

        private void CommitClipManipulation()
        {
            if (!state.IsDraggingClip)
                return;

            if (state.ManipulationMode == TimelineClipManipulationMode.Move)
            {
                CommitMoveManipulation();
                return;
            }

            CommitResizeManipulation();
        }

        private void CommitMoveManipulation()
        {
            SerializedProperty sourceClipProperty = GetDragSourceClipProperty();
            if (sourceClipProperty == null)
            {
                CancelClipManipulation();
                return;
            }

            int sourceTrackIndex = state.DragSourceTrackIndex;
            int sourceClipIndex = state.DragSourceClipIndex;
            ClipSnapshot snapshot = CreateSnapshotFromProperty(sourceClipProperty, sourceTrackIndex, sourceClipIndex);
            snapshot.StartTime = state.DragPreviewStartTime;

            int targetTrackIndex = ResolveCommittedDropTrack(snapshot);
            Undo.RecordObject(state.Timeline, "Move Timeline Clip");

            if (targetTrackIndex == sourceTrackIndex)
            {
                sourceClipProperty.FindPropertyRelative("startTime").floatValue = Mathf.Max(0f, state.DragPreviewStartTime);
                state.TimelineSerializedObject.ApplyModifiedProperties();
                state.TimelineSerializedObject.Update();
                ClampCanvasScrollToCurrentContent();
                EditorUtility.SetDirty(state.Timeline);
                TimelineActionUsageIndex.Invalidate();
                state.SelectClip(sourceTrackIndex, sourceClipIndex);
                state.ClearDragState();
                Repaint();
                return;
            }

            MoveSelectedClipToTrack(targetTrackIndex, snapshot);
            state.ClearDragState();
            Repaint();
        }

        private void CommitResizeManipulation()
        {
            SerializedProperty clipProperty = GetDragSourceClipProperty();
            if (clipProperty == null)
            {
                CancelClipManipulation();
                return;
            }

            if (!state.DragPreviewValid)
            {
                CancelClipManipulation();
                return;
            }

            Undo.RecordObject(state.Timeline, "Resize Timeline Clip");

            SerializedProperty startTimeProperty = clipProperty.FindPropertyRelative("startTime");
            SerializedProperty useDurationOverrideProperty = clipProperty.FindPropertyRelative("useDurationOverride");
            SerializedProperty durationOverrideProperty = clipProperty.FindPropertyRelative("durationOverride");

            if (startTimeProperty != null)
                startTimeProperty.floatValue = Mathf.Max(0f, state.DragPreviewStartTime);

            if (useDurationOverrideProperty != null)
                useDurationOverrideProperty.boolValue = true;

            if (durationOverrideProperty != null)
                durationOverrideProperty.floatValue = Mathf.Max(0f, state.DragPreviewDuration);

            state.TimelineSerializedObject.ApplyModifiedProperties();
            state.TimelineSerializedObject.Update();
            ClampCanvasScrollToCurrentContent();
            EditorUtility.SetDirty(state.Timeline);
            TimelineActionUsageIndex.Invalidate();

            state.SelectClip(state.DragSourceTrackIndex, state.DragSourceClipIndex);
            state.ClearDragState();
            Repaint();
        }

        private void CancelClipManipulation()
        {
            state.ClearDragState();
            Repaint();
        }

        private int ResolveCommittedDropTrack(ClipSnapshot snapshot)
        {
            if (state.DragPreviewValid && state.DragPreviewTrackIndex >= 0)
                return state.DragPreviewTrackIndex;

            return ResolveBestDropTrack(snapshot);
        }

        private int ResolveBestDropTrack(ClipSnapshot snapshot)
        {
            int trackCount = GetSerializedTrackCount();
            int previewTrack = Mathf.Clamp(state.DragPreviewTrackIndex, 0, Mathf.Max(0, trackCount - 1));
            if (trackCount > 0 && CanPlaceSnapshotAt(previewTrack, snapshot))
                return previewTrack;

            for (int trackIndex = 0; trackIndex < trackCount; trackIndex++)
            {
                if (CanPlaceSnapshotAt(trackIndex, snapshot))
                    return trackIndex;
            }

            return AddTrackInternal(BuildAutoTrackNameFromSnapshot(snapshot, trackCount));
        }

        private void MoveSelectedClipToTrack(int targetTrackIndex, ClipSnapshot snapshot)
        {
            SerializedProperty tracksProperty = GetTracksProperty();
            if (tracksProperty == null)
                return;

            int sourceTrackIndex = state.DragSourceTrackIndex;
            int sourceClipIndex = state.DragSourceClipIndex;
            if (sourceTrackIndex < 0 || sourceTrackIndex >= tracksProperty.arraySize)
                return;

            SerializedProperty targetTrackProperty = tracksProperty.GetArrayElementAtIndex(targetTrackIndex);
            SerializedProperty targetClipsProperty = targetTrackProperty.FindPropertyRelative("clips");
            int newClipIndex = targetClipsProperty.arraySize;
            targetClipsProperty.arraySize++;
            SerializedProperty newClipProperty = targetClipsProperty.GetArrayElementAtIndex(newClipIndex);
            ApplySnapshotToClipProperty(snapshot, newClipProperty);

            SerializedProperty sourceTrackProperty = tracksProperty.GetArrayElementAtIndex(sourceTrackIndex);
            SerializedProperty sourceClipsProperty = sourceTrackProperty.FindPropertyRelative("clips");
            sourceClipsProperty.DeleteArrayElementAtIndex(sourceClipIndex);

            state.TimelineSerializedObject.ApplyModifiedProperties();
            state.TimelineSerializedObject.Update();
            ClampCanvasScrollToCurrentContent();
            EditorUtility.SetDirty(state.Timeline);
            TimelineActionUsageIndex.Invalidate();
            state.SelectClip(targetTrackIndex, newClipIndex);
        }

        private bool CanPlaceSourceClipAt(int targetTrackIndex, float startTime, float duration)
        {
            SerializedProperty clipProperty = GetDragSourceClipProperty();
            if (clipProperty == null)
                return false;

            ClipSnapshot snapshot = CreateSnapshotFromProperty(clipProperty, state.DragSourceTrackIndex, state.DragSourceClipIndex);
            snapshot.StartTime = Mathf.Max(0f, startTime);
            snapshot.DurationOverride = Mathf.Max(0f, duration);
            snapshot.UseDurationOverride = true;
            return CanPlaceSnapshotAt(targetTrackIndex, snapshot);
        }

        private bool CanPlaceSnapshotAt(int targetTrackIndex, ClipSnapshot snapshot)
        {
            SerializedProperty trackProperty = GetTrackProperty(targetTrackIndex);
            if (trackProperty == null)
                return false;

            SerializedProperty clipsProperty = trackProperty.FindPropertyRelative("clips");
            if (clipsProperty == null)
                return true;

            int clipCount = clipsProperty.arraySize;
            for (int clipIndex = 0; clipIndex < clipCount; clipIndex++)
            {
                if (targetTrackIndex == state.DragSourceTrackIndex && clipIndex == state.DragSourceClipIndex)
                    continue;

                SerializedProperty otherClipProperty = clipsProperty.GetArrayElementAtIndex(clipIndex);
                if (otherClipProperty == null)
                    continue;

                float otherStart = GetClipStartTime(otherClipProperty);
                float otherDuration = GetClipEffectiveDuration(otherClipProperty);
                if (TimelineOverlapUtility.Overlaps(snapshot.StartTime, snapshot.EffectiveDuration, otherStart, otherDuration))
                    return false;
            }

            return true;
        }

        private void HandleCanvasBackgroundSelection(bool clickedClip, TimelinePointerContext pointerContext)
        {
            Event current = Event.current;
            if (current.type != EventType.MouseDown || current.button != 0 || clickedClip || state.IsDraggingClip || state.HasPendingClipPress)
                return;

            if (!pointerContext.IsInsideCanvas)
                return;

            if (pointerContext.IsInsideLaneArea && ActiveSettings.SelectTrackOnBackgroundClick)
                state.SelectTrack(pointerContext.HoveredLaneIndex);
            else
                state.SelectTimeline();

            ClearEditorKeyboardFocusIfConfigured();
            current.Use();
            Repaint();
        }

        private void DrawInspectorPanel(Rect rect, List<TimelineValidationResult> validationResults)
        {
            EditorGUI.DrawRect(rect, TimelineEditorStyles.PanelBackground);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), TimelineEditorStyles.MajorGridLineColor);

            Rect areaRect = new Rect(
                rect.x + 8f,
                rect.y + 8f,
                Mathf.Max(10f, rect.width - 16f),
                Mathf.Max(10f, rect.height - 16f));

            GUILayout.BeginArea(areaRect);
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Inspector", TimelineEditorStyles.InspectorHeaderStyle, GUILayout.ExpandWidth(true));

                bool newShowShortcutHints = GUILayout.Toggle(
                    state.ShowShortcutHints,
                    "⌨ Shortcuts",
                    EditorStyles.miniButton,
                    GUILayout.Width(92f),
                    GUILayout.Height(20f));

                if (newShowShortcutHints != state.ShowShortcutHints)
                    state.ShowShortcutHints = newShowShortcutHints;
            }

            EditorGUILayout.LabelField("Selection", GetSelectionSummary(), EditorStyles.miniLabel);

            if (state.ShowShortcutHints)
                DrawShortcutHintsPanelLayout();

            EditorGUILayout.Space(4f);

            state.InspectorScroll = EditorGUILayout.BeginScrollView(
                state.InspectorScroll,
                false,
                true,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));

            if (state.HasClipSelection)
            {
                if (GetSelectedClipProperty() != null)
                    DrawClipInspector();
                else
                    EditorGUILayout.HelpBox("Clip selection is active but the selected clip property could not be resolved.", MessageType.Warning);
            }
            else if (state.HasTrackSelection)
            {
                if (GetSelectedTrackProperty() != null)
                    DrawTrackInspector();
                else
                    EditorGUILayout.HelpBox("Track selection is active but the selected track property could not be resolved.", MessageType.Warning);
            }
            else
            {
                DrawTimelineInspector(validationResults);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void DrawShortcutHintsPanelLayout()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Shortcuts", EditorStyles.boldLabel);
                DrawShortcutRowLayout("Delete / Backspace", "Delete selected track or clip");
                DrawShortcutRowLayout("T", "Add track");
                DrawShortcutRowLayout("A", "Add clip");
                DrawShortcutRowLayout("F", "Frame timeline");
                DrawShortcutRowLayout("Esc", "Cancel manipulation / clear selection");
            }
        }

        private static void DrawShortcutRowLayout(string key, string description)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(key, EditorStyles.centeredGreyMiniLabel, GUILayout.Width(86f));
                GUILayout.Label(description, EditorStyles.miniLabel, GUILayout.ExpandWidth(true));
            }
        }

        private string GetSelectionSummary()
        {
            if (state.HasClipSelection)
                return $"Clip — Track {state.SelectedTrackIndex + 1}, Clip {state.SelectedClipIndex + 1}";

            if (state.HasTrackSelection)
                return $"Track {state.SelectedTrackIndex + 1}";

            if (state.HasTimeline)
                return "Timeline";

            return "None";
        }

        private void DrawTimelineInspector(List<TimelineValidationResult> validationResults)
        {
            GUILayout.Label("Timeline", TimelineEditorStyles.InspectorHeaderStyle);
            EditorGUILayout.ObjectField("Asset", state.Timeline, typeof(ActionTimelineAsset), false);
            int trackCount = state.Timeline.Tracks?.Count ?? 0;
            int validClipCount = validator.CountValidClips(state.Timeline);
            float duration = GetCurrentTimelineDuration();
            EditorGUILayout.LabelField("Tracks", trackCount.ToString());
            EditorGUILayout.LabelField("Valid Clips", validClipCount.ToString());
            EditorGUILayout.LabelField("Duration", TimelineDurationUtility.FormatSeconds(duration));
            EditorGUILayout.Space(8f);
            GUILayout.Label("Validation", TimelineEditorStyles.InspectorHeaderStyle);
            if (validationResults.Count <= 0)
            {
                EditorGUILayout.HelpBox("No validation issues found.", MessageType.Info);
                return;
            }

            for (int i = 0; i < validationResults.Count; i++)
                EditorGUILayout.HelpBox(validationResults[i].Message, ToMessageType(validationResults[i].Severity));
        }

        private void DrawTrackInspector()
        {
            SerializedProperty trackProperty = GetSelectedTrackProperty();
            if (trackProperty == null)
            {
                EditorGUILayout.HelpBox("Selected track is no longer valid.", MessageType.Warning);
                return;
            }

            GUILayout.Label("Track", TimelineEditorStyles.InspectorHeaderStyle);
            SerializedProperty trackNameProperty = trackProperty.FindPropertyRelative("trackName");
            SerializedProperty enabledProperty = trackProperty.FindPropertyRelative("isEnabled");
            SerializedProperty clipsProperty = trackProperty.FindPropertyRelative("clips");
            EditorGUILayout.PropertyField(trackNameProperty);
            EditorGUILayout.PropertyField(enabledProperty);
            EditorGUILayout.LabelField("Clip Count", (clipsProperty != null ? clipsProperty.arraySize : 0).ToString());
            EditorGUILayout.Space(8f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Clip"))
                    AddClipToTrack(state.SelectedTrackIndex, 0f);
                if (GUILayout.Button("Delete Track"))
                    DeleteSelection();
            }
        }

        private void DrawClipInspector()
        {
            SerializedProperty clipProperty = GetSelectedClipProperty();
            if (clipProperty == null)
            {
                EditorGUILayout.HelpBox("Selected clip is no longer valid.", MessageType.Warning);
                return;
            }

            GUILayout.Label("Clip", TimelineEditorStyles.InspectorHeaderStyle);
            SerializedProperty debugNameProperty = clipProperty.FindPropertyRelative("debugName");
            SerializedProperty startTimeProperty = clipProperty.FindPropertyRelative("startTime");
            SerializedProperty actionProperty = clipProperty.FindPropertyRelative("action");
            SerializedProperty useDurationOverrideProperty = clipProperty.FindPropertyRelative("useDurationOverride");
            SerializedProperty durationOverrideProperty = clipProperty.FindPropertyRelative("durationOverride");

            TimelineAction action = (TimelineAction)actionProperty.objectReferenceValue;
            int usageCount = TimelineActionUsageIndex.GetUsageCount(action);
            string usageLabel = usageCount <= 1 ? "Single usage" : $"{usageCount} usages";
            EditorGUILayout.LabelField("Action Usage", usageLabel, EditorStyles.miniLabel);

            EditorGUILayout.PropertyField(debugNameProperty);
            EditorGUILayout.PropertyField(startTimeProperty);
            startTimeProperty.floatValue = Mathf.Max(0f, startTimeProperty.floatValue);
            EditorGUILayout.PropertyField(actionProperty);
            EditorGUILayout.PropertyField(useDurationOverrideProperty);
            if (useDurationOverrideProperty.boolValue)
            {
                EditorGUILayout.PropertyField(durationOverrideProperty);
                durationOverrideProperty.floatValue = Mathf.Max(0f, durationOverrideProperty.floatValue);
            }
            else
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.FloatField("Duration Override", durationOverrideProperty.floatValue);
            }

            float effectiveDuration = GetClipEffectiveDuration(clipProperty);
            float endTime = GetClipEndTime(clipProperty);
            EditorGUILayout.Space(8f);
            GUILayout.Label("Computed", TimelineEditorStyles.InspectorHeaderStyle);
            EditorGUILayout.LabelField("Nominal Duration", TimelineDurationUtility.FormatSeconds(TimelineDurationUtility.GetActionNominalDuration(action)));
            EditorGUILayout.LabelField("Effective Duration", TimelineDurationUtility.FormatSeconds(effectiveDuration));
            EditorGUILayout.LabelField("End Time", TimelineDurationUtility.FormatSeconds(endTime));

            EditorGUILayout.Space(8f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Set Start Time..."))
                    OpenSetStartTimePrompt(state.SelectedTrackIndex, state.SelectedClipIndex, Mathf.Max(0f, startTimeProperty.floatValue));
                if (GUILayout.Button("Duplicate"))
                    DuplicateSelectedClip();
            }

            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = action;
                if (GUILayout.Button("Ping Action"))
                    EditorAssetLinkUtility.Ping(action);
                if (GUILayout.Button("Open Action"))
                    EditorAssetLinkUtility.OpenInspector(action);
                GUI.enabled = true;
            }

            EditorGUILayout.Space(4f);
            if (GUILayout.Button("Delete"))
                DeleteSelection();
        }

        private void OpenSetStartTimePrompt(int trackIndex, int clipIndex, float initialValue)
        {
            FloatValuePromptWindow.Open("Set Clip Start Time", "Start Time", initialValue, value =>
            {
                if (!state.HasTimeline || state.TimelineSerializedObject == null)
                    return;

                state.TimelineSerializedObject.Update();
                SerializedProperty tracksProperty = GetTracksProperty();
                if (tracksProperty == null || trackIndex < 0 || trackIndex >= tracksProperty.arraySize)
                    return;

                SerializedProperty trackProperty = tracksProperty.GetArrayElementAtIndex(trackIndex);
                SerializedProperty clipsProperty = trackProperty.FindPropertyRelative("clips");
                if (clipsProperty == null || clipIndex < 0 || clipIndex >= clipsProperty.arraySize)
                    return;

                Undo.RecordObject(state.Timeline, "Set Timeline Clip Start Time");
                SerializedProperty clipProperty = clipsProperty.GetArrayElementAtIndex(clipIndex);
                clipProperty.FindPropertyRelative("startTime").floatValue = Mathf.Max(0f, value);
                state.TimelineSerializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(state.Timeline);
                state.SelectClip(trackIndex, clipIndex);
                Repaint();
            });
        }

        private void AddTrack()
        {
            int newIndex = AddTrackInternal($"Track {GetSerializedTrackCount() + 1}");
            if (newIndex < 0)
                return;

            ClearEditorKeyboardFocusIfConfigured();
            if (ActiveSettings.SelectNewTrackAfterCreation)
                state.SelectTrack(newIndex);
            else
                state.SelectTimeline();

            ScrollTrackRowIntoView(newIndex);
            Repaint();
        }

        private int AddTrackInternal(string trackName)
        {
            SerializedProperty tracksProperty = GetTracksProperty();
            if (tracksProperty == null)
                return -1;

            Undo.RecordObject(state.Timeline, "Add Timeline Track");
            int newIndex = tracksProperty.arraySize;
            tracksProperty.arraySize++;
            SerializedProperty newTrackProperty = tracksProperty.GetArrayElementAtIndex(newIndex);
            ResetTrackProperty(newTrackProperty, trackName);
            state.TimelineSerializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(state.Timeline);
            TimelineActionUsageIndex.Invalidate();
            return newIndex;
        }

        private void AddClipToBestTrack()
        {
            SerializedProperty tracksProperty = GetTracksProperty();
            if (tracksProperty == null)
                return;
            if (tracksProperty.arraySize <= 0)
                AddTrack();
            int targetTrackIndex = (state.HasTrackSelection || state.HasClipSelection) ? Mathf.Clamp(state.SelectedTrackIndex, 0, tracksProperty.arraySize - 1) : 0;
            float startTime = GetCurrentTimelineDuration();
            AddClipToTrack(targetTrackIndex, startTime);
        }

        private void AddClipToTrack(int trackIndex, float startTime)
        {
            SerializedProperty tracksProperty = GetTracksProperty();
            if (tracksProperty == null)
                return;
            if (tracksProperty.arraySize <= 0)
                AddTrack();
            trackIndex = Mathf.Clamp(trackIndex, 0, tracksProperty.arraySize - 1);
            Undo.RecordObject(state.Timeline, "Add Timeline Clip");
            SerializedProperty trackProperty = tracksProperty.GetArrayElementAtIndex(trackIndex);
            SerializedProperty clipsProperty = trackProperty.FindPropertyRelative("clips");
            int newIndex = clipsProperty.arraySize;
            clipsProperty.arraySize++;
            SerializedProperty newClipProperty = clipsProperty.GetArrayElementAtIndex(newIndex);
            ResetClipProperty(newClipProperty, $"Clip {newIndex + 1}", Mathf.Max(0f, startTime));
            state.TimelineSerializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(state.Timeline);
            TimelineActionUsageIndex.Invalidate();
            state.SelectClip(trackIndex, newIndex);
            Repaint();
        }

        private void DuplicateSelectedClip()
        {
            SerializedProperty clipProperty = GetSelectedClipProperty();
            if (clipProperty == null)
                return;

            int trackIndex = state.SelectedTrackIndex;
            SerializedProperty tracksProperty = GetTracksProperty();
            if (tracksProperty == null || trackIndex < 0 || trackIndex >= tracksProperty.arraySize)
                return;

            Undo.RecordObject(state.Timeline, "Duplicate Timeline Clip");
            SerializedProperty trackProperty = tracksProperty.GetArrayElementAtIndex(trackIndex);
            SerializedProperty clipsProperty = trackProperty.FindPropertyRelative("clips");
            int newIndex = clipsProperty.arraySize;
            clipsProperty.arraySize++;
            SerializedProperty newClipProperty = clipsProperty.GetArrayElementAtIndex(newIndex);
            CopyClipProperty(clipProperty, newClipProperty);
            SerializedProperty startTimeProperty = newClipProperty.FindPropertyRelative("startTime");
            startTimeProperty.floatValue = Mathf.Max(0f, startTimeProperty.floatValue + 0.05f);
            state.TimelineSerializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(state.Timeline);
            TimelineActionUsageIndex.Invalidate();
            state.SelectClip(trackIndex, newIndex);
            Repaint();
        }

        private static void RemoveArrayElementAtIndexCompletely(SerializedProperty arrayProperty, int index)
        {
            if (arrayProperty == null || !arrayProperty.isArray)
                return;
            if (index < 0 || index >= arrayProperty.arraySize)
                return;

            int previousSize = arrayProperty.arraySize;
            arrayProperty.DeleteArrayElementAtIndex(index);
            if (arrayProperty.arraySize == previousSize)
                arrayProperty.DeleteArrayElementAtIndex(index);
        }

        private void ClampCanvasScrollToCurrentContent()
        {
            float inspectorWidth = Mathf.Clamp(state.InspectorWidth, 280f, 420f);
            float visibleCanvasWidth = Mathf.Max(120f, position.width - inspectorWidth - TimelineEditorStyles.TrackColumnWidth - 4f);
            float visibleCanvasHeight = Mathf.Max(1f, position.height - TimelineEditorStyles.RulerHeight);
            float contentWidth = Mathf.Max(visibleCanvasWidth - 16f, (GetCurrentTimelineDuration() * state.PixelsPerSecond) + 120f);
            float contentHeight = Mathf.Max(visibleCanvasHeight - 1f, GetSerializedTrackCount() * TimelineEditorStyles.LaneHeight);
            float maxScrollX = Mathf.Max(0f, contentWidth - visibleCanvasWidth);
            float maxScrollY = Mathf.Max(0f, contentHeight - visibleCanvasHeight);
            state.CanvasScroll = new Vector2(Mathf.Clamp(state.CanvasScroll.x, 0f, maxScrollX), Mathf.Clamp(state.CanvasScroll.y, 0f, maxScrollY));
        }

        private void FocusClip(int trackIndex, int clipIndex)
        {
            EnsureState();

            if (!state.HasTimeline || state.TimelineSerializedObject == null)
                return;

            state.TimelineSerializedObject.Update();

            SerializedProperty clipProperty = GetClipProperty(trackIndex, clipIndex);
            if (clipProperty == null)
                return;

            state.SelectClip(trackIndex, clipIndex);
            ScrollTrackRowIntoView(trackIndex);
            ScrollClipIntoView(clipProperty);
            Repaint();
        }

        private void ScrollClipIntoView(SerializedProperty clipProperty)
        {
            if (clipProperty == null)
                return;

            float clipStart = GetClipStartTime(clipProperty);
            float clipDuration = GetClipEffectiveDuration(clipProperty);

            float clipLeft = TimelineRectUtility.TimeToPixel(clipStart, state.PixelsPerSecond, 0f);
            float clipWidth = TimelineRectUtility.GetVisualWidth(
                clipDuration,
                state.PixelsPerSecond,
                TimelineEditorStyles.MinClipVisualWidth);
            float clipRight = clipLeft + clipWidth;

            float visibleCanvasWidth = Mathf.Max(
                120f,
                position.width - Mathf.Clamp(state.InspectorWidth, 280f, 420f) - TimelineEditorStyles.TrackColumnWidth - 4f);

            float scrollX = state.CanvasScroll.x;
            const float HorizontalPadding = 24f;

            if (clipLeft - HorizontalPadding < scrollX)
            {
                scrollX = Mathf.Max(0f, clipLeft - HorizontalPadding);
            }
            else if (clipRight + HorizontalPadding > scrollX + visibleCanvasWidth)
            {
                scrollX = Mathf.Max(0f, clipRight + HorizontalPadding - visibleCanvasWidth);
            }

            state.CanvasScroll = new Vector2(scrollX, state.CanvasScroll.y);
            ClampCanvasScrollToCurrentContent();
        }

        private void ScrollTrackRowIntoView(int trackIndex)
        {
            if (trackIndex < 0)
                return;

            float laneHeight = TimelineEditorStyles.LaneHeight;
            float rowTop = trackIndex * laneHeight;
            float rowBottom = rowTop + laneHeight;
            float visibleHeight = Mathf.Max(1f, position.height - TimelineEditorStyles.RulerHeight);

            float scrollY = state.CanvasScroll.y;
            if (rowTop < scrollY)
                scrollY = rowTop;
            else if (rowBottom > scrollY + visibleHeight)
                scrollY = rowBottom - visibleHeight;

            state.CanvasScroll = new Vector2(state.CanvasScroll.x, Mathf.Max(0f, scrollY));
        }

        private static void ClearEditorKeyboardFocus()
        {
            GUI.FocusControl(string.Empty);
            EditorGUIUtility.editingTextField = false;
        }

        private void ClearEditorKeyboardFocusIfConfigured()
        {
            if (!ActiveSettings.ClearObjectFieldFocusOnTimelineSelection)
                return;

            ClearEditorKeyboardFocus();
        }

        private void DeleteSelection()
        {
            if (state.SelectionKind == TimelineSelectionKind.Track)
                DeleteSelectedTrack();
            else if (state.SelectionKind == TimelineSelectionKind.Clip)
                DeleteSelectedClip();
        }

        private void DeleteSelectedTrack()
        {
            SerializedProperty tracksProperty = GetTracksProperty();
            if (tracksProperty == null || !state.HasTrackSelection)
                return;

            int trackIndex = state.SelectedTrackIndex;
            if (trackIndex < 0 || trackIndex >= tracksProperty.arraySize)
                return;

            SerializedProperty trackProperty = tracksProperty.GetArrayElementAtIndex(trackIndex);
            SerializedProperty clipsProperty = trackProperty.FindPropertyRelative("clips");
            int clipCount = clipsProperty != null ? clipsProperty.arraySize : 0;
            if (clipCount > 0)
            {
                bool confirmed = EditorUtility.DisplayDialog("Delete Track", $"Track contains {clipCount} clip(s). Delete it anyway?", "Delete", "Cancel");
                if (!confirmed)
                    return;
            }

            Undo.RecordObject(state.Timeline, "Delete Timeline Track");
            RemoveArrayElementAtIndexCompletely(tracksProperty, trackIndex);
            state.TimelineSerializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(state.Timeline);
            TimelineActionUsageIndex.Invalidate();
            state.SelectTimeline();
            Repaint();
        }

        private void DeleteSelectedClip()
        {
            SerializedProperty tracksProperty = GetTracksProperty();
            if (tracksProperty == null || !state.HasClipSelection)
                return;

            int trackIndex = state.SelectedTrackIndex;
            int clipIndex = state.SelectedClipIndex;
            if (trackIndex < 0 || trackIndex >= tracksProperty.arraySize)
                return;

            SerializedProperty trackProperty = tracksProperty.GetArrayElementAtIndex(trackIndex);
            SerializedProperty clipsProperty = trackProperty.FindPropertyRelative("clips");
            if (clipsProperty == null || clipIndex < 0 || clipIndex >= clipsProperty.arraySize)
                return;

            Undo.RecordObject(state.Timeline, "Delete Timeline Clip");
            RemoveArrayElementAtIndexCompletely(clipsProperty, clipIndex);
            state.TimelineSerializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(state.Timeline);
            TimelineActionUsageIndex.Invalidate();
            state.SelectTrack(trackIndex);
            Repaint();
        }

        private void AutoArrangeTracks()
        {
            SerializedProperty tracksProperty = GetTracksProperty();
            if (tracksProperty == null)
                return;
            int existingTrackCount = tracksProperty.arraySize;
            if (existingTrackCount <= 0)
                return;

            List<string> existingTrackNames = new List<string>(existingTrackCount);
            List<bool> existingTrackEnabled = new List<bool>(existingTrackCount);
            List<ClipSnapshot> allClips = new List<ClipSnapshot>(16);

            for (int trackIndex = 0; trackIndex < existingTrackCount; trackIndex++)
            {
                SerializedProperty trackProperty = tracksProperty.GetArrayElementAtIndex(trackIndex);
                existingTrackNames.Add(trackProperty.FindPropertyRelative("trackName").stringValue);
                existingTrackEnabled.Add(trackProperty.FindPropertyRelative("isEnabled").boolValue);
                SerializedProperty clipsProperty = trackProperty.FindPropertyRelative("clips");
                int clipCount = clipsProperty != null ? clipsProperty.arraySize : 0;
                for (int clipIndex = 0; clipIndex < clipCount; clipIndex++)
                    allClips.Add(CreateSnapshotFromProperty(clipsProperty.GetArrayElementAtIndex(clipIndex), trackIndex, clipIndex));
            }

            allClips.Sort(CompareSnapshotsStable);
            List<List<ClipSnapshot>> lanes = new List<List<ClipSnapshot>>(4);
            for (int i = 0; i < allClips.Count; i++)
            {
                ClipSnapshot candidate = allClips[i];
                int compatibleLaneIndex = FindCompatibleSnapshotLane(lanes, candidate);
                if (compatibleLaneIndex < 0)
                    lanes.Add(new List<ClipSnapshot> { candidate });
                else
                    lanes[compatibleLaneIndex].Add(candidate);
            }

            Undo.RecordObject(state.Timeline, "Auto Arrange Timeline Tracks");
            tracksProperty.arraySize = lanes.Count;
            for (int laneIndex = 0; laneIndex < lanes.Count; laneIndex++)
            {
                SerializedProperty trackProperty = tracksProperty.GetArrayElementAtIndex(laneIndex);
                string existingName = laneIndex < existingTrackNames.Count ? existingTrackNames[laneIndex] : string.Empty;
                string trackName = !string.IsNullOrWhiteSpace(existingName) ? existingName : BuildAutoTrackName(lanes[laneIndex], laneIndex);
                bool isEnabled = laneIndex < existingTrackEnabled.Count ? existingTrackEnabled[laneIndex] : true;
                ResetTrackProperty(trackProperty, trackName);
                trackProperty.FindPropertyRelative("isEnabled").boolValue = isEnabled;
                SerializedProperty clipsProperty = trackProperty.FindPropertyRelative("clips");
                List<ClipSnapshot> lane = lanes[laneIndex];
                clipsProperty.arraySize = lane.Count;
                for (int localClipIndex = 0; localClipIndex < lane.Count; localClipIndex++)
                    ApplySnapshotToClipProperty(lane[localClipIndex], clipsProperty.GetArrayElementAtIndex(localClipIndex));
            }

            state.TimelineSerializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(state.Timeline);
            TimelineActionUsageIndex.Invalidate();
            state.SelectTimeline();
            Repaint();
        }

        private void HandleGlobalShortcuts()
        {
            if (!ActiveSettings.EnableKeyboardShortcuts)
                return;

            Event current = Event.current;
            if (current.type != EventType.KeyDown)
                return;

            bool shouldBlockStructuralShortcuts = ShouldBlockStructuralShortcuts();
            if ((current.keyCode == KeyCode.Delete || current.keyCode == KeyCode.Backspace) && ActiveSettings.AllowDeleteShortcut)
            {
                if (shouldBlockStructuralShortcuts)
                    return;

                ClearEditorKeyboardFocusIfConfigured();
                DeleteSelection();
                current.Use();
                return;
            }

            if (current.keyCode == KeyCode.T && ActiveSettings.AllowAddTrackShortcut)
            {
                if (shouldBlockStructuralShortcuts)
                    return;

                ClearEditorKeyboardFocusIfConfigured();
                AddTrack();
                current.Use();
                return;
            }

            if (current.keyCode == KeyCode.A && ActiveSettings.AllowAddClipShortcut)
            {
                if (shouldBlockStructuralShortcuts)
                    return;

                ClearEditorKeyboardFocusIfConfigured();
                AddClipToBestTrack();
                current.Use();
                return;
            }

            if (current.keyCode == KeyCode.F && ActiveSettings.AllowAutoArrangeShortcut)
            {
                if (shouldBlockStructuralShortcuts)
                    return;

                ClearEditorKeyboardFocusIfConfigured();
                FrameTimeline();
                current.Use();
                return;
            }

            if (current.keyCode == KeyCode.Escape && !state.IsDraggingClip)
            {
                if (state.HasPendingClipPress)
                {
                    state.ClearPendingClipPress();
                    current.Use();
                    Repaint();
                    return;
                }

                if (shouldBlockStructuralShortcuts)
                    return;

                state.ClearPendingClipPress();
                state.SelectTimeline();
                current.Use();
                Repaint();
            }
        }

        private TimelinePointerContext BuildPointerContext(Rect contentRect, int trackCount)
        {
            Vector2 canvasMousePosition = Event.current.mousePosition;
            bool isInsideCanvas = contentRect.Contains(canvasMousePosition);
            int hoveredLaneIndex = TimelineRectUtility.CanvasYToLaneIndex(canvasMousePosition.y, TimelineEditorStyles.LaneHeight, trackCount);
            bool isInsideLaneArea = hoveredLaneIndex >= 0;
            float timeAtMouse = TimelineRectUtility.PixelToTimeClamped(canvasMousePosition.x, state.PixelsPerSecond, 0f);
            return new TimelinePointerContext(canvasMousePosition, timeAtMouse, hoveredLaneIndex, isInsideCanvas, isInsideLaneArea);
        }

        private bool ShouldBlockStructuralShortcuts()
        {
            return ActiveSettings.BlockShortcutsWhileTextEditing && EditorGUIUtility.editingTextField;
        }

        private void FrameTimeline()
        {
            float duration = GetCurrentTimelineDuration();
            float targetWidth = position.width - TimelineEditorStyles.TrackColumnWidth - state.InspectorWidth - 40f;
            if (duration <= 0f || targetWidth <= 40f)
            {
                state.PixelsPerSecond = Mathf.Clamp(ActiveSettings.DefaultPixelsPerSecond, ActiveSettings.MinPixelsPerSecond, ActiveSettings.MaxPixelsPerSecond);
                state.CanvasScroll = Vector2.zero;
                Repaint();
                return;
            }

            state.PixelsPerSecond = Mathf.Clamp(targetWidth / Mathf.Max(0.1f, duration), ActiveSettings.MinPixelsPerSecond, ActiveSettings.MaxPixelsPerSecond);
            state.CanvasScroll = Vector2.zero;
            Repaint();
        }

        private void SanitizeSelection()
        {
            if (!state.HasTimeline)
            {
                state.ClearSelection();
                return;
            }

            SerializedProperty tracksProperty = GetTracksProperty();
            if (tracksProperty == null)
            {
                state.ClearSelection();
                return;
            }

            int trackCount = tracksProperty.arraySize;
            if (state.SelectionKind == TimelineSelectionKind.Track)
            {
                if (state.SelectedTrackIndex < 0 || state.SelectedTrackIndex >= trackCount)
                    state.SelectTimeline();
                return;
            }

            if (state.SelectionKind == TimelineSelectionKind.Clip)
            {
                if (state.SelectedTrackIndex < 0 || state.SelectedTrackIndex >= trackCount)
                {
                    state.SelectTimeline();
                    return;
                }

                SerializedProperty trackProperty = tracksProperty.GetArrayElementAtIndex(state.SelectedTrackIndex);
                SerializedProperty clipsProperty = trackProperty.FindPropertyRelative("clips");
                int clipCount = clipsProperty != null ? clipsProperty.arraySize : 0;
                if (state.SelectedClipIndex < 0 || state.SelectedClipIndex >= clipCount)
                    state.SelectTrack(state.SelectedTrackIndex);
            }
        }

        private SerializedProperty GetTracksProperty()
        {
            return state.TimelineSerializedObject != null ? state.TimelineSerializedObject.FindProperty("tracks") : null;
        }

        private SerializedProperty GetTrackProperty(int trackIndex)
        {
            SerializedProperty tracksProperty = GetTracksProperty();
            if (tracksProperty == null)
                return null;
            if (trackIndex < 0 || trackIndex >= tracksProperty.arraySize)
                return null;
            return tracksProperty.GetArrayElementAtIndex(trackIndex);
        }

        private SerializedProperty GetClipProperty(int trackIndex, int clipIndex)
        {
            SerializedProperty trackProperty = GetTrackProperty(trackIndex);
            if (trackProperty == null)
                return null;

            SerializedProperty clipsProperty = trackProperty.FindPropertyRelative("clips");
            if (clipsProperty == null)
                return null;
            if (clipIndex < 0 || clipIndex >= clipsProperty.arraySize)
                return null;

            return clipsProperty.GetArrayElementAtIndex(clipIndex);
        }

        private static float GetClipStartTime(SerializedProperty clipProperty)
        {
            if (clipProperty == null)
                return 0f;

            SerializedProperty startTimeProperty = clipProperty.FindPropertyRelative("startTime");
            return startTimeProperty != null ? Mathf.Max(0f, startTimeProperty.floatValue) : 0f;
        }

        private static float GetClipEffectiveDuration(SerializedProperty clipProperty)
        {
            if (clipProperty == null)
                return 0f;

            SerializedProperty useDurationOverrideProperty = clipProperty.FindPropertyRelative("useDurationOverride");
            SerializedProperty durationOverrideProperty = clipProperty.FindPropertyRelative("durationOverride");
            SerializedProperty actionProperty = clipProperty.FindPropertyRelative("action");

            bool useDurationOverride = useDurationOverrideProperty != null && useDurationOverrideProperty.boolValue;
            float durationOverride = durationOverrideProperty != null ? Mathf.Max(0f, durationOverrideProperty.floatValue) : 0f;
            TimelineAction action = actionProperty != null ? (TimelineAction)actionProperty.objectReferenceValue : null;

            return useDurationOverride ? durationOverride : (action ? Mathf.Max(0f, action.NominalDuration) : 0f);
        }

        private static float GetClipEndTime(SerializedProperty clipProperty)
        {
            return GetClipStartTime(clipProperty) + GetClipEffectiveDuration(clipProperty);
        }

        private int GetSerializedTrackCount()
        {
            SerializedProperty tracksProperty = GetTracksProperty();
            return tracksProperty != null ? tracksProperty.arraySize : 0;
        }

        private float GetCurrentTimelineDuration()
        {
            SerializedProperty tracksProperty = GetTracksProperty();
            if (tracksProperty == null)
                return 0f;

            float duration = 0f;
            for (int trackIndex = 0; trackIndex < tracksProperty.arraySize; trackIndex++)
            {
                SerializedProperty trackProperty = tracksProperty.GetArrayElementAtIndex(trackIndex);
                if (trackProperty == null)
                    continue;

                SerializedProperty clipsProperty = trackProperty.FindPropertyRelative("clips");
                if (clipsProperty == null)
                    continue;

                for (int clipIndex = 0; clipIndex < clipsProperty.arraySize; clipIndex++)
                {
                    SerializedProperty clipProperty = clipsProperty.GetArrayElementAtIndex(clipIndex);
                    if (clipProperty == null)
                        continue;

                    duration = Mathf.Max(duration, GetClipEndTime(clipProperty));
                }
            }

            return duration;
        }

        private SerializedProperty GetSelectedTrackProperty()
        {
            if (!state.HasTrackSelection && !state.HasClipSelection)
                return null;
            SerializedProperty tracksProperty = GetTracksProperty();
            if (tracksProperty == null)
                return null;
            int trackIndex = state.SelectedTrackIndex;
            if (trackIndex < 0 || trackIndex >= tracksProperty.arraySize)
                return null;
            return tracksProperty.GetArrayElementAtIndex(trackIndex);
        }

        private SerializedProperty GetSelectedClipProperty()
        {
            if (!state.HasClipSelection)
                return null;
            SerializedProperty trackProperty = GetSelectedTrackProperty();
            if (trackProperty == null)
                return null;
            SerializedProperty clipsProperty = trackProperty.FindPropertyRelative("clips");
            if (clipsProperty == null)
                return null;
            int clipIndex = state.SelectedClipIndex;
            if (clipIndex < 0 || clipIndex >= clipsProperty.arraySize)
                return null;
            return clipsProperty.GetArrayElementAtIndex(clipIndex);
        }

        private SerializedProperty GetDragSourceClipProperty()
        {
            if (!state.TryGetDragSourceIndices(out int trackIndex, out int clipIndex))
                return null;
            return GetClipProperty(trackIndex, clipIndex);
        }

        private static void ResetTrackProperty(SerializedProperty trackProperty, string trackName)
        {
            trackProperty.FindPropertyRelative("trackName").stringValue = trackName;
            trackProperty.FindPropertyRelative("isEnabled").boolValue = true;
            SerializedProperty clipsProperty = trackProperty.FindPropertyRelative("clips");
            clipsProperty.ClearArray();
        }

        private static void ResetClipProperty(SerializedProperty clipProperty, string debugName, float startTime)
        {
            clipProperty.FindPropertyRelative("debugName").stringValue = debugName;
            clipProperty.FindPropertyRelative("startTime").floatValue = Mathf.Max(0f, startTime);
            clipProperty.FindPropertyRelative("action").objectReferenceValue = null;
            clipProperty.FindPropertyRelative("useDurationOverride").boolValue = false;
            clipProperty.FindPropertyRelative("durationOverride").floatValue = 0f;
        }

        private static void CopyClipProperty(SerializedProperty source, SerializedProperty destination)
        {
            destination.FindPropertyRelative("debugName").stringValue = source.FindPropertyRelative("debugName").stringValue;
            destination.FindPropertyRelative("startTime").floatValue = source.FindPropertyRelative("startTime").floatValue;
            destination.FindPropertyRelative("action").objectReferenceValue = source.FindPropertyRelative("action").objectReferenceValue;
            destination.FindPropertyRelative("useDurationOverride").boolValue = source.FindPropertyRelative("useDurationOverride").boolValue;
            destination.FindPropertyRelative("durationOverride").floatValue = source.FindPropertyRelative("durationOverride").floatValue;
        }

        private static ClipSnapshot CreateSnapshotFromProperty(SerializedProperty clipProperty, int originalTrackIndex, int originalClipIndex)
        {
            return new ClipSnapshot
            {
                DebugName = clipProperty.FindPropertyRelative("debugName").stringValue,
                StartTime = Mathf.Max(0f, clipProperty.FindPropertyRelative("startTime").floatValue),
                Action = (TimelineAction)clipProperty.FindPropertyRelative("action").objectReferenceValue,
                UseDurationOverride = clipProperty.FindPropertyRelative("useDurationOverride").boolValue,
                DurationOverride = Mathf.Max(0f, clipProperty.FindPropertyRelative("durationOverride").floatValue),
                OriginalTrackIndex = originalTrackIndex,
                OriginalClipIndex = originalClipIndex
            };
        }

        private static void ApplySnapshotToClipProperty(ClipSnapshot snapshot, SerializedProperty clipProperty)
        {
            clipProperty.FindPropertyRelative("debugName").stringValue = snapshot.DebugName;
            clipProperty.FindPropertyRelative("startTime").floatValue = snapshot.StartTime;
            clipProperty.FindPropertyRelative("action").objectReferenceValue = snapshot.Action;
            clipProperty.FindPropertyRelative("useDurationOverride").boolValue = snapshot.UseDurationOverride;
            clipProperty.FindPropertyRelative("durationOverride").floatValue = snapshot.DurationOverride;
        }

        private static int CompareSnapshotsStable(ClipSnapshot left, ClipSnapshot right)
        {
            int byTime = left.StartTime.CompareTo(right.StartTime);
            if (byTime != 0)
                return byTime;
            int byTrack = left.OriginalTrackIndex.CompareTo(right.OriginalTrackIndex);
            if (byTrack != 0)
                return byTrack;
            return left.OriginalClipIndex.CompareTo(right.OriginalClipIndex);
        }

        private static int FindCompatibleSnapshotLane(List<List<ClipSnapshot>> lanes, ClipSnapshot candidate)
        {
            for (int laneIndex = 0; laneIndex < lanes.Count; laneIndex++)
            {
                if (CanPlaceSnapshotInLane(lanes[laneIndex], candidate))
                    return laneIndex;
            }
            return -1;
        }

        private static bool CanPlaceSnapshotInLane(List<ClipSnapshot> lane, ClipSnapshot candidate)
        {
            for (int i = 0; i < lane.Count; i++)
            {
                ClipSnapshot other = lane[i];
                if (TimelineOverlapUtility.Overlaps(candidate.StartTime, candidate.EffectiveDuration, other.StartTime, other.EffectiveDuration))
                    return false;
            }
            return true;
        }

        private static string BuildAutoTrackName(List<ClipSnapshot> lane, int laneIndex)
        {
            if (lane == null || lane.Count == 0)
                return $"Track {laneIndex + 1}";
            return BuildAutoTrackNameFromSnapshot(lane[0], laneIndex);
        }

        private static string BuildAutoTrackNameFromSnapshot(ClipSnapshot snapshot, int laneIndex)
        {
            if (snapshot == null)
                return $"Track {laneIndex + 1}";
            string actionToken = BuildTrackTokenFromAction(snapshot.Action);
            if (!string.IsNullOrWhiteSpace(actionToken))
                return actionToken + "Track";
            string debugToken = SanitizeTrackToken(snapshot.DebugName);
            if (!string.IsNullOrWhiteSpace(debugToken))
                return debugToken + "Track";
            return $"Track {laneIndex + 1}";
        }

        private static string BuildTrackTokenFromAction(TimelineAction action)
        {
            if (!action)
                return string.Empty;
            string typeName = action.GetType().Name;
            if (string.IsNullOrWhiteSpace(typeName))
                typeName = action.name;
            if (typeName.EndsWith("TimelineAction", StringComparison.Ordinal))
                typeName = typeName.Substring(0, typeName.Length - "TimelineAction".Length);
            else if (typeName.EndsWith("Action", StringComparison.Ordinal))
                typeName = typeName.Substring(0, typeName.Length - "Action".Length);
            return SanitizeTrackToken(typeName);
        }

        private static string SanitizeTrackToken(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;
            List<char> chars = new List<char>(raw.Length);
            for (int index = 0; index < raw.Length; index++)
            {
                char c = raw[index];
                if (char.IsLetterOrDigit(c))
                    chars.Add(c);
            }
            return chars.Count == 0 ? string.Empty : new string(chars.ToArray());
        }

        private Color ResolveClipBackgroundColor(TimelineAction action, bool isSelected, bool hasClipError)
        {
            if (isSelected)
                return TimelineEditorStyles.ClipSelectedColor;
            if (hasClipError)
                return TimelineEditorStyles.ClipInvalidColor;
            return ActiveTheme.ResolveClipBackgroundColor(action);
        }

        private static string BuildClipDisplayName(string debugName, TimelineAction action, int clipIndex)
        {
            if (!string.IsNullOrWhiteSpace(debugName))
                return debugName;
            if (action)
                return action.name;
            return $"Clip {clipIndex + 1}";
        }

        private static MessageType ToMessageType(TimelineValidationSeverity severity)
        {
            switch (severity)
            {
                case TimelineValidationSeverity.Error:
                    return MessageType.Error;
                case TimelineValidationSeverity.Warning:
                    return MessageType.Warning;
                default:
                    return MessageType.Info;
            }
        }

        private static bool HasTrackValidationIssue(List<TimelineValidationResult> validationResults, int trackIndex)
        {
            for (int i = 0; i < validationResults.Count; i++)
            {
                if (validationResults[i].TrackIndex == trackIndex)
                    return true;
            }
            return false;
        }

        private static bool HasTrackOverlapIssue(List<TimelineValidationResult> validationResults, int trackIndex)
        {
            for (int i = 0; i < validationResults.Count; i++)
            {
                TimelineValidationResult result = validationResults[i];
                if (result.TrackIndex == trackIndex && result.Message.IndexOf("overlapping", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        private static bool HasClipValidationIssue(List<TimelineValidationResult> validationResults, int trackIndex, int clipIndex, TimelineValidationSeverity severity)
        {
            for (int i = 0; i < validationResults.Count; i++)
            {
                TimelineValidationResult result = validationResults[i];
                if (result.TrackIndex != trackIndex || result.Severity != severity)
                    continue;

                if (result.ClipIndex == clipIndex || result.SecondaryClipIndex == clipIndex)
                    return true;
            }

            return false;
        }
    }
}
