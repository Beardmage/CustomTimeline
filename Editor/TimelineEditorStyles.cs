using UnityEditor;
using UnityEngine;

namespace Beardmage.ActionTimeline.Editor
{
    /// <summary>
    /// Shared GUI styles and visual accessors for the Action Timeline editor window.
    /// Values are sourced from the active editor theme asset when available.
    /// </summary>
    public static class TimelineEditorStyles
    {
        private static bool initialized;
        private static TimelineEditorThemeAsset activeTheme;
        private static int lastThemeSignature;

        public static GUIStyle TrackLabelStyle { get; private set; }
        public static GUIStyle CategoryLabelStyle { get; private set; }
        public static GUIStyle TrackChildLabelStyle { get; private set; }
        public static GUIStyle TrackChildBadgeStyle { get; private set; }
        public static GUIStyle ClipLabelStyle { get; private set; }
        public static GUIStyle InspectorHeaderStyle { get; private set; }
        public static GUIStyle EmptyStateStyle { get; private set; }
        public static GUIStyle MiniBadgeStyle { get; private set; }

        public static float TrackColumnWidth => ActiveTheme.TrackHeaderWidth;
        public static float RulerHeight => ActiveTheme.RulerHeight;
        public static float LaneHeight => ActiveTheme.LaneHeight;
        public static float MinClipVisualWidth => ActiveTheme.MinClipVisualWidth;
        public static float ClipVerticalPadding => ActiveTheme.ClipVerticalPadding;

        public static Color WindowBackground => ActiveTheme.WindowBackground;
        public static Color PanelBackground => ActiveTheme.PanelBackground;
        public static Color InspectorBackground => ActiveTheme.InspectorBackground;
        public static Color RulerBackground => ActiveTheme.RulerBackground;
        public static Color LaneOddBackground => ActiveTheme.LaneBackground;
        public static Color LaneEvenBackground => ActiveTheme.LaneAlternateBackground;
        public static Color GridLineColor => ActiveTheme.GridMinorLineColor;
        public static Color MajorGridLineColor => ActiveTheme.GridMajorLineColor;
        public static Color TimelineEndColor => ActiveTheme.RulerMajorLineColor;
        public static Color ClipColor => ActiveTheme.ClipDefaultColor;
        public static Color ClipSelectedColor => ActiveTheme.ClipSelectedColor;
        public static Color ClipPreviewValidColor => ActiveTheme.DragPreviewValidColor;
        public static Color ClipPreviewInvalidColor => ActiveTheme.DragPreviewInvalidColor;
        public static Color ClipInvalidColor => ActiveTheme.OverlapWarningColor;
        public static Color ClipBorderColor => ActiveTheme.ClipBorderColor;
        public static Color DisabledClipColor => ActiveTheme.ClipDisabledColor;
        public static Color WarningColor => ActiveTheme.WarningColor;
        public static Color ErrorColor => ActiveTheme.ErrorColor;
        public static Color AddTrackButtonColor => ActiveTheme.AddTrackButtonColor;
        public static Color CategoryRowColor => Color.Lerp(ActiveTheme.PanelBackground, ActiveTheme.SectionHeaderBackground, 0.72f);
        public static Color TrackChildRowColor => Color.Lerp(ActiveTheme.LaneAlternateBackground, Color.white, 0.14f);
        public static Color CategoryActivityColor => new Color(0.28f, 0.48f, 0.72f, 0.72f);
        public static Color CategoryActivitySelectedColor => new Color(0.20f, 0.58f, 0.95f, 0.9f);
        public static Color SelectionOutlineColor => new Color(ActiveTheme.ClipSelectedColor.r, ActiveTheme.ClipSelectedColor.g, ActiveTheme.ClipSelectedColor.b, 0.58f);
        public static Color PlayheadColor => new Color(1f, 1f, 1f, 0.86f);

        private static TimelineEditorThemeAsset ActiveTheme => activeTheme ? activeTheme : TimelineEditorThemeDefaults.Instance;

        /// <summary>
        /// Initializes the shared IMGUI styles using the provided theme asset.
        /// Returns false while Unity editor styles are not ready yet.
        /// </summary>
        public static bool TryEnsureInitialized(TimelineEditorThemeAsset theme)
        {
            activeTheme = theme ? theme : TimelineEditorThemeDefaults.Instance;

            if (EditorStyles.label == null ||
                EditorStyles.miniLabel == null ||
                EditorStyles.boldLabel == null ||
                EditorStyles.helpBox == null)
            {
                return false;
            }

            int signature = ComputeThemeSignature(ActiveTheme);

            if (!initialized)
            {
                TrackLabelStyle = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    clipping = TextClipping.Clip,
                    fontStyle = FontStyle.Bold
                };

                CategoryLabelStyle = new GUIStyle(TrackLabelStyle);

                TrackChildLabelStyle = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    clipping = TextClipping.Clip,
                    fontStyle = FontStyle.Normal
                };

                TrackChildBadgeStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold
                };

                ClipLabelStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    clipping = TextClipping.Clip
                };

                InspectorHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 12
                };

                EmptyStateStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true,
                    fontSize = 12
                };

                MiniBadgeStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold
                };

                initialized = true;
            }

            if (lastThemeSignature != signature)
            {
                lastThemeSignature = signature;
                ApplyThemeToStyles();
            }

            return true;
        }

        /// <summary>
        /// Backward-compatible overload that uses the default theme fallback.
        /// </summary>
        public static bool TryEnsureInitialized()
        {
            return TryEnsureInitialized(TimelineEditorThemeDefaults.Instance);
        }

        /// <summary>
        /// Backward-compatible helper retained for existing call sites.
        /// </summary>
        public static void EnsureInitialized()
        {
            TryEnsureInitialized(TimelineEditorThemeDefaults.Instance);
        }

        /// <summary>
        /// Clears cached GUIStyle instances so they can be rebuilt after reload or theme changes.
        /// </summary>
        public static void Reset()
        {
            initialized = false;
            activeTheme = null;
            lastThemeSignature = 0;
            TrackLabelStyle = null;
            CategoryLabelStyle = null;
            TrackChildLabelStyle = null;
            TrackChildBadgeStyle = null;
            ClipLabelStyle = null;
            InspectorHeaderStyle = null;
            EmptyStateStyle = null;
            MiniBadgeStyle = null;
        }

        private static int ComputeThemeSignature(TimelineEditorThemeAsset theme)
        {
            if (!theme)
                return 0;

            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + theme.GetInstanceID();
                hash = (hash * 31) + theme.RulerTextColor.GetHashCode();
                hash = (hash * 31) + theme.ClipTextColor.GetHashCode();
                hash = (hash * 31) + theme.PanelBackground.GetHashCode();
                hash = (hash * 31) + theme.RulerBackground.GetHashCode();
                hash = (hash * 31) + theme.LaneHeight.GetHashCode();
                hash = (hash * 31) + theme.TrackHeaderWidth.GetHashCode();
                hash = (hash * 31) + theme.MinClipVisualWidth.GetHashCode();
                hash = (hash * 31) + theme.PanelPadding.GetHashCode();
                hash = (hash * 31) + theme.SectionSpacing.GetHashCode();
                return hash;
            }
        }

        private static void ApplyThemeToStyles()
        {
            if (TrackLabelStyle == null || CategoryLabelStyle == null || TrackChildLabelStyle == null || TrackChildBadgeStyle == null ||
                ClipLabelStyle == null || InspectorHeaderStyle == null || EmptyStateStyle == null || MiniBadgeStyle == null)
                return;

            TrackLabelStyle.normal.textColor = ActiveTheme.RulerTextColor;
            TrackLabelStyle.hover.textColor = ActiveTheme.RulerTextColor;
            TrackLabelStyle.active.textColor = ActiveTheme.RulerTextColor;
            TrackLabelStyle.focused.textColor = ActiveTheme.RulerTextColor;

            CategoryLabelStyle.normal.textColor = ActiveTheme.RulerTextColor;
            CategoryLabelStyle.hover.textColor = ActiveTheme.RulerTextColor;
            CategoryLabelStyle.active.textColor = ActiveTheme.RulerTextColor;
            CategoryLabelStyle.focused.textColor = ActiveTheme.RulerTextColor;

            Color childTextColor = Color.Lerp(ActiveTheme.RulerTextColor, Color.white, 0.28f);
            TrackChildLabelStyle.normal.textColor = childTextColor;
            TrackChildLabelStyle.hover.textColor = childTextColor;
            TrackChildLabelStyle.active.textColor = childTextColor;
            TrackChildLabelStyle.focused.textColor = childTextColor;

            TrackChildBadgeStyle.normal.textColor = childTextColor;
            TrackChildBadgeStyle.hover.textColor = childTextColor;
            TrackChildBadgeStyle.active.textColor = childTextColor;
            TrackChildBadgeStyle.focused.textColor = childTextColor;

            ClipLabelStyle.normal.textColor = ActiveTheme.ClipTextColor;
            ClipLabelStyle.hover.textColor = ActiveTheme.ClipTextColor;
            ClipLabelStyle.active.textColor = ActiveTheme.ClipTextColor;
            ClipLabelStyle.focused.textColor = ActiveTheme.ClipTextColor;

            InspectorHeaderStyle.normal.textColor = ActiveTheme.RulerTextColor;
            InspectorHeaderStyle.hover.textColor = ActiveTheme.RulerTextColor;
            InspectorHeaderStyle.active.textColor = ActiveTheme.RulerTextColor;
            InspectorHeaderStyle.focused.textColor = ActiveTheme.RulerTextColor;

            MiniBadgeStyle.normal.textColor = ActiveTheme.RulerTextColor;
            MiniBadgeStyle.hover.textColor = ActiveTheme.RulerTextColor;
            MiniBadgeStyle.active.textColor = ActiveTheme.RulerTextColor;
            MiniBadgeStyle.focused.textColor = ActiveTheme.RulerTextColor;

            EmptyStateStyle.normal.textColor = ActiveTheme.RulerTextColor;
            EmptyStateStyle.hover.textColor = ActiveTheme.RulerTextColor;
            EmptyStateStyle.active.textColor = ActiveTheme.RulerTextColor;
            EmptyStateStyle.focused.textColor = ActiveTheme.RulerTextColor;
        }
    }
}
