using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Beardmage.ActionTimeline.Editor
{
    /// <summary>
    /// Visual theme used by the Action Timeline editor window.
    /// Only contains appearance and editor layout tuning values.
    /// Authoring rules such as overlap policy and snap semantics stay in code.
    /// </summary>
    [CreateAssetMenu(
        fileName = "TimelineEditorTheme",
        menuName = "Action Timeline/Editor/Timeline Editor Theme")]
    public sealed class TimelineEditorThemeAsset : ScriptableObject
    {
        [Header("Activation")]
        [SerializeField, HideInInspector, Tooltip("If enabled, this asset is used as the active Timeline Editor theme source.")]
        private bool isActive;

        [Header("Window")]
        [SerializeField, Tooltip("Main background color behind the whole window content.")]
        private Color windowBackground = new Color(0.18f, 0.18f, 0.18f, 1f);

        [SerializeField, Tooltip("Background color used behind the toolbar area when custom drawing is needed.")]
        private Color toolbarBackground = new Color(0.21f, 0.21f, 0.21f, 1f);

        [SerializeField, Tooltip("Default background color for secondary panels.")]
        private Color panelBackground = new Color(0.22f, 0.22f, 0.22f, 1f);

        [SerializeField, Tooltip("Background color used by the inspector column.")]
        private Color inspectorBackground = new Color(0.21f, 0.21f, 0.21f, 1f);

        [SerializeField, Tooltip("Background color for section headers and compact title strips.")]
        private Color sectionHeaderBackground = new Color(0.16f, 0.16f, 0.16f, 1f);

        [SerializeField, Tooltip("Background color of the editable timeline canvas area.")]
        private Color timelineCanvasBackground = new Color(0.13f, 0.13f, 0.13f, 1f);

        [Header("Tracks / Lanes")]
        [SerializeField, Tooltip("Default background color for a track lane.")]
        private Color laneBackground = new Color(0.18f, 0.18f, 0.18f, 1f);

        [SerializeField, Tooltip("Alternate lane background color used for zebra readability.")]
        private Color laneAlternateBackground = new Color(0.20f, 0.20f, 0.20f, 1f);

        [SerializeField, Tooltip("Overlay/background color applied when the mouse hovers a lane.")]
        private Color laneHoverBackground = new Color(0.25f, 0.25f, 0.25f, 1f);

        [SerializeField, Tooltip("Overlay/background color applied to the currently selected lane or track row.")]
        private Color laneSelectedBackground = new Color(0.27f, 0.31f, 0.38f, 1f);

        [SerializeField, Tooltip("Background color of the track header strip.")]
        private Color trackHeaderBackground = new Color(0.24f, 0.24f, 0.24f, 1f);

        [SerializeField, Tooltip("Background color of the selected track header strip.")]
        private Color trackHeaderSelectedBackground = new Color(0.31f, 0.35f, 0.42f, 1f);

        [SerializeField, Tooltip("Tint color used by the bottom add-track button area.")]
        private Color addTrackButtonColor = new Color(0.25f, 0.29f, 0.35f, 1f);

        [Header("Ruler / Grid")]
        [SerializeField, Tooltip("Background color of the time ruler area.")]
        private Color rulerBackground = new Color(0.16f, 0.16f, 0.16f, 1f);

        [SerializeField, Tooltip("Text color used by ruler labels.")]
        private Color rulerTextColor = new Color(0.88f, 0.88f, 0.88f, 1f);

        [SerializeField, Tooltip("Color used by major ruler tick lines.")]
        private Color rulerMajorLineColor = new Color(0.52f, 0.52f, 0.52f, 1f);

        [SerializeField, Tooltip("Color used by minor ruler tick lines.")]
        private Color rulerMinorLineColor = new Color(0.35f, 0.35f, 0.35f, 1f);

        [SerializeField, Tooltip("Color used by major vertical grid lines inside the timeline canvas.")]
        private Color gridMajorLineColor = new Color(1f, 1f, 1f, 0.12f);

        [SerializeField, Tooltip("Color used by minor vertical grid lines inside the timeline canvas.")]
        private Color gridMinorLineColor = new Color(1f, 1f, 1f, 0.05f);

        [Header("Clips")]
        [SerializeField, Tooltip("Default background color used when no action-specific style overrides it.")]
        private Color clipDefaultColor = new Color(0.26f, 0.35f, 0.50f, 1f);

        [SerializeField, Tooltip("Background color of the selected clip.")]
        private Color clipSelectedColor = new Color(0.38f, 0.52f, 0.74f, 1f);

        [SerializeField, Tooltip("Background color of a hovered clip when no stronger state is active.")]
        private Color clipHoverColor = new Color(0.31f, 0.43f, 0.62f, 1f);

        [SerializeField, Tooltip("Background color of a disabled or invalid clip presentation.")]
        private Color clipDisabledColor = new Color(0.35f, 0.35f, 0.35f, 1f);

        [SerializeField, Tooltip("Border color of clip rectangles.")]
        private Color clipBorderColor = new Color(0f, 0f, 0f, 0.35f);

        [SerializeField, Tooltip("Text color used by clip labels.")]
        private Color clipTextColor = Color.white;

        [Header("Drag / Placement")]
        [SerializeField, Tooltip("Color of the drag preview when the placement is valid.")]
        private Color dragPreviewValidColor = new Color(0.24f, 0.65f, 0.38f, 0.9f);

        [SerializeField, Tooltip("Color of the drag preview when the placement is invalid.")]
        private Color dragPreviewInvalidColor = new Color(0.76f, 0.28f, 0.28f, 0.9f);

        [SerializeField, Tooltip("Color of snap guides or snap markers when displayed.")]
        private Color snapGuideColor = new Color(0.95f, 0.82f, 0.33f, 1f);

        [SerializeField, Tooltip("Highlight color used when an overlap or blocked placement must be emphasized.")]
        private Color overlapWarningColor = new Color(0.86f, 0.42f, 0.18f, 1f);

        [Header("Validation")]
        [SerializeField, Tooltip("Informational validation color.")]
        private Color infoColor = new Color(0.30f, 0.60f, 0.90f, 1f);

        [SerializeField, Tooltip("Warning validation color.")]
        private Color warningColor = new Color(0.90f, 0.70f, 0.25f, 1f);

        [SerializeField, Tooltip("Error validation color.")]
        private Color errorColor = new Color(0.88f, 0.34f, 0.34f, 1f);

        [Header("Layout")]
        [SerializeField, Tooltip("Nominal toolbar height used by the custom editor layout.")]
        private float toolbarHeight = 22f;

        [SerializeField, Tooltip("Height of the time ruler strip.")]
        private float rulerHeight = 22f;

        [SerializeField, Tooltip("Visual height of one track lane.")]
        private float laneHeight = 42f;

        [SerializeField, Tooltip("Width of the left track header column.")]
        private float trackHeaderWidth = 220f;

        [SerializeField, Tooltip("Minimum on-screen width of a clip rectangle to keep punctual clips selectable.")]
        private float minClipVisualWidth = 10f;

        [SerializeField, Tooltip("Vertical padding applied inside lane clip rects.")]
        private float clipVerticalPadding = 4f;

        [SerializeField, Tooltip("Default spacing between stacked UI sections.")]
        private float sectionSpacing = 6f;

        [SerializeField, Tooltip("Default padding used by panel containers.")]
        private float panelPadding = 8f;

        [Header("Action Styles")]
        [SerializeField, Tooltip("Optional visual overrides keyed by action type name for clip readability.")]
        private List<TimelineActionStyleEntry> actionStyles = new List<TimelineActionStyleEntry>(8);

        public Color WindowBackground => windowBackground;
        public Color ToolbarBackground => toolbarBackground;
        public Color PanelBackground => panelBackground;
        public Color InspectorBackground => inspectorBackground;
        public Color SectionHeaderBackground => sectionHeaderBackground;
        public Color TimelineCanvasBackground => timelineCanvasBackground;
        public Color LaneBackground => laneBackground;
        public Color LaneAlternateBackground => laneAlternateBackground;
        public Color LaneHoverBackground => laneHoverBackground;
        public Color LaneSelectedBackground => laneSelectedBackground;
        public Color TrackHeaderBackground => trackHeaderBackground;
        public Color TrackHeaderSelectedBackground => trackHeaderSelectedBackground;
        public Color AddTrackButtonColor => addTrackButtonColor;
        public Color RulerBackground => rulerBackground;
        public Color RulerTextColor => rulerTextColor;
        public Color RulerMajorLineColor => rulerMajorLineColor;
        public Color RulerMinorLineColor => rulerMinorLineColor;
        public Color GridMajorLineColor => gridMajorLineColor;
        public Color GridMinorLineColor => gridMinorLineColor;
        public Color ClipDefaultColor => clipDefaultColor;
        public Color ClipSelectedColor => clipSelectedColor;
        public Color ClipHoverColor => clipHoverColor;
        public Color ClipDisabledColor => clipDisabledColor;
        public Color ClipBorderColor => clipBorderColor;
        public Color ClipTextColor => clipTextColor;
        public Color DragPreviewValidColor => dragPreviewValidColor;
        public Color DragPreviewInvalidColor => dragPreviewInvalidColor;
        public Color SnapGuideColor => snapGuideColor;
        public Color OverlapWarningColor => overlapWarningColor;
        public Color InfoColor => infoColor;
        public Color WarningColor => warningColor;
        public Color ErrorColor => errorColor;
        public float ToolbarHeight => toolbarHeight;
        public float RulerHeight => rulerHeight;
        public float LaneHeight => laneHeight;
        public float TrackHeaderWidth => trackHeaderWidth;
        public float MinClipVisualWidth => minClipVisualWidth;
        public float ClipVerticalPadding => clipVerticalPadding;
        public float SectionSpacing => sectionSpacing;
        public float PanelPadding => panelPadding;
        public IReadOnlyList<TimelineActionStyleEntry> ActionStyles => actionStyles;
        public bool IsActive => isActive;

        public bool TryGetActionStyle(string actionTypeName, out TimelineActionStyleEntry style)
        {
            style = null;
            if (string.IsNullOrWhiteSpace(actionTypeName))
                return false;

            int count = actionStyles?.Count ?? 0;
            for (int i = 0; i < count; i++)
            {
                TimelineActionStyleEntry candidate = actionStyles[i];
                if (candidate == null || string.IsNullOrWhiteSpace(candidate.ActionTypeName))
                    continue;

                if (!string.Equals(candidate.ActionTypeName, actionTypeName, StringComparison.Ordinal))
                    continue;

                style = candidate;
                return true;
            }

            return false;
        }

        public Color ResolveClipBackgroundColor(TimelineAction action)
        {
            if (action && TryGetActionStyle(action.GetType().Name, out TimelineActionStyleEntry style))
                return style.BackgroundColor;

            return clipDefaultColor;
        }

        private void OnValidate()
        {
            toolbarHeight = Mathf.Max(0f, toolbarHeight);
            rulerHeight = Mathf.Max(0f, rulerHeight);
            laneHeight = Mathf.Max(12f, laneHeight);
            trackHeaderWidth = Mathf.Max(60f, trackHeaderWidth);
            minClipVisualWidth = Mathf.Max(1f, minClipVisualWidth);
            clipVerticalPadding = Mathf.Max(0f, clipVerticalPadding);
            sectionSpacing = Mathf.Max(0f, sectionSpacing);
            panelPadding = Mathf.Max(0f, panelPadding);
        }
    }

    [CustomEditor(typeof(TimelineEditorThemeAsset))]
    internal sealed class TimelineEditorThemeAssetInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            TimelineEditorThemeAsset theme = (TimelineEditorThemeAsset)target;
            EditorGUILayout.Space(8f);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(theme.IsActive))
                {
                    if (GUILayout.Button(theme.IsActive ? "Active Theme" : "Set as Active"))
                        TimelineEditorConfigLocator.SetActiveTheme(theme);
                }

                if (GUILayout.Button("Ping"))
                    EditorGUIUtility.PingObject(theme);
            }
        }
    }

    [Serializable]
    public sealed class TimelineActionStyleEntry
    {
        [SerializeField, Tooltip("Type name used to identify the timeline action class.")]
        private string actionTypeName;

        [SerializeField, Tooltip("Background color override used for matching clip actions.")]
        private Color backgroundColor = new Color(0.26f, 0.35f, 0.50f, 1f);

        [SerializeField, Tooltip("Text color override used for matching clip actions.")]
        private Color textColor = Color.white;

        [SerializeField, Tooltip("Border color override used for matching clip actions.")]
        private Color borderColor = new Color(0f, 0f, 0f, 0.35f);

        [SerializeField, Tooltip("Optional short label override used instead of the default clip label.")]
        private string shortLabelOverride;

        public string ActionTypeName => actionTypeName;
        public Color BackgroundColor => backgroundColor;
        public Color TextColor => textColor;
        public Color BorderColor => borderColor;
        public string ShortLabelOverride => shortLabelOverride;
    }

    public static class TimelineEditorThemeDefaults
    {
        private static TimelineEditorThemeAsset instance;

        public static TimelineEditorThemeAsset Instance
        {
            get
            {
                if (instance)
                    return instance;

                instance = ScriptableObject.CreateInstance<TimelineEditorThemeAsset>();
                instance.hideFlags = HideFlags.HideAndDontSave;
                return instance;
            }
        }
    }
}
