using UnityEngine;
using UnityEditor;

namespace Beardmage.ActionTimeline.Editor
{
    /// <summary>
    /// Interaction tuning settings for the Timeline editor window.
    /// Only contains editor ergonomics and view defaults.
    /// Core authoring rules remain defined in code.
    /// </summary>
    [CreateAssetMenu(
        fileName = "TimelineEditorSettings",
        menuName = "Action Timeline/Editor/Timeline Editor Settings")]
    public sealed class TimelineEditorSettingsAsset : ScriptableObject
    {
        [Header("Activation")]
        [SerializeField, HideInInspector, Tooltip("If enabled, this asset is used as the active Timeline Editor settings source.")]
        private bool isActive;

        [Header("Interaction")]
        [SerializeField, Tooltip("Mouse movement threshold before a clip press becomes an active drag.")]
        private float dragStartPixelThreshold = 4f;

        [SerializeField, Tooltip("Maximum pixel distance allowed to trigger a snap candidate.")]
        private float snapThresholdPixels = 10f;

        [SerializeField, Tooltip("If enabled, the editor may apply placement snap assistance when moving clips.")]
        private bool enableSnap = true;

        [SerializeField, Tooltip("If enabled, selecting timeline content clears focus from the toolbar timeline ObjectField to avoid accidental delete/backspace side effects.")]
        private bool clearObjectFieldFocusOnTimelineSelection = true;

        [SerializeField, Tooltip("If enabled, global editor shortcuts are blocked while a text field or property text editor has focus.")]
        private bool blockShortcutsWhileTextEditing = true;

        [Header("Placement UX")]
        [SerializeField, Tooltip("If enabled, dropping a clip with no compatible existing track may create a new track automatically.")]
        private bool autoCreateTrackWhenDropHasNoFit = true;

        [SerializeField, Tooltip("If enabled, placement resolution prefers the currently hovered track before scanning other tracks.")]
        private bool preferHoveredTrackForDrop = true;

        [SerializeField, Tooltip("If enabled, the contextual 'Create Clip Here' action is available from the timeline canvas.")]
        private bool enableContextCreateClipHere = true;

        [SerializeField, Tooltip("If enabled, contextual clip creation can use snap assistance before writing the start time.")]
        private bool snapCreateClipHere = true;

        [SerializeField, Tooltip("If enabled, negative start times requested by interaction are clamped back to zero.")]
        private bool clampNegativeStartTimesToZero = true;

        [Header("Selection")]
        [SerializeField, Tooltip("If enabled, clicking the canvas background selects the corresponding track lane when possible.")]
        private bool selectTrackOnBackgroundClick = true;

        [SerializeField, Tooltip("If enabled, a newly created track becomes the active selection.")]
        private bool selectNewTrackAfterCreation = true;

        [SerializeField, Tooltip("If enabled, a clip remains selected after a successful move or drop.")]
        private bool selectMovedClipAfterDrop = true;

        [SerializeField, Tooltip("If enabled, cancelling a clip drag keeps the original source clip selected.")]
        private bool keepSelectionOnCancelledDrag = true;

        [SerializeField, Tooltip("If enabled, deleting a selected track falls back to a timeline-level selection state.")]
        private bool selectTimelineAfterDeletingTrack = true;

        [Header("View Defaults")]
        [SerializeField, Tooltip("Default horizontal zoom expressed in pixels per second when the editor state is reset.")]
        private float defaultPixelsPerSecond = 100f;

        [SerializeField, Tooltip("Lower bound allowed for horizontal zoom in pixels per second.")]
        private float minPixelsPerSecond = 25f;

        [SerializeField, Tooltip("Upper bound allowed for horizontal zoom in pixels per second.")]
        private float maxPixelsPerSecond = 400f;

        [SerializeField, Tooltip("If enabled, the inspector panel is shown by default when the editor window initializes its state.")]
        private bool showInspectorByDefault = true;

        [SerializeField, Tooltip("If enabled, validation details are shown by default when available.")]
        private bool showValidationPanelByDefault = true;

        [SerializeField, Tooltip("If enabled, the bottom add-track button is displayed under the track list.")]
        private bool showBottomAddTrackButton = true;

        [Header("Keyboard Shortcuts")]
        [SerializeField, Tooltip("Master toggle for keyboard shortcuts handled by the timeline editor window.")]
        private bool enableKeyboardShortcuts = true;

        [SerializeField, Tooltip("If enabled, Delete / Backspace can remove the currently selected clip or track.")]
        private bool allowDeleteShortcut = true;

        [SerializeField, Tooltip("If enabled, the add-track shortcut remains active.")]
        private bool allowAddTrackShortcut = true;

        [SerializeField, Tooltip("If enabled, the add-clip shortcut remains active.")]
        private bool allowAddClipShortcut = true;

        [SerializeField, Tooltip("If enabled, the auto-arrange shortcut remains active.")]
        private bool allowAutoArrangeShortcut = true;

        public float DragStartPixelThreshold => dragStartPixelThreshold;
        public float SnapThresholdPixels => snapThresholdPixels;
        public bool EnableSnap => enableSnap;
        public bool ClearObjectFieldFocusOnTimelineSelection => clearObjectFieldFocusOnTimelineSelection;
        public bool BlockShortcutsWhileTextEditing => blockShortcutsWhileTextEditing;
        public bool AutoCreateTrackWhenDropHasNoFit => autoCreateTrackWhenDropHasNoFit;
        public bool PreferHoveredTrackForDrop => preferHoveredTrackForDrop;
        public bool EnableContextCreateClipHere => enableContextCreateClipHere;
        public bool SnapCreateClipHere => snapCreateClipHere;
        public bool ClampNegativeStartTimesToZero => clampNegativeStartTimesToZero;
        public bool SelectTrackOnBackgroundClick => selectTrackOnBackgroundClick;
        public bool SelectNewTrackAfterCreation => selectNewTrackAfterCreation;
        public bool SelectMovedClipAfterDrop => selectMovedClipAfterDrop;
        public bool KeepSelectionOnCancelledDrag => keepSelectionOnCancelledDrag;
        public bool SelectTimelineAfterDeletingTrack => selectTimelineAfterDeletingTrack;
        public float DefaultPixelsPerSecond => defaultPixelsPerSecond;
        public float MinPixelsPerSecond => minPixelsPerSecond;
        public float MaxPixelsPerSecond => maxPixelsPerSecond;
        public bool ShowInspectorByDefault => showInspectorByDefault;
        public bool ShowValidationPanelByDefault => showValidationPanelByDefault;
        public bool ShowBottomAddTrackButton => showBottomAddTrackButton;
        public bool EnableKeyboardShortcuts => enableKeyboardShortcuts;
        public bool AllowDeleteShortcut => allowDeleteShortcut;
        public bool AllowAddTrackShortcut => allowAddTrackShortcut;
        public bool AllowAddClipShortcut => allowAddClipShortcut;
        public bool AllowAutoArrangeShortcut => allowAutoArrangeShortcut;
        public bool IsActive => isActive;

        private void OnValidate()
        {
            dragStartPixelThreshold = Mathf.Max(0f, dragStartPixelThreshold);
            snapThresholdPixels = Mathf.Max(0f, snapThresholdPixels);
            defaultPixelsPerSecond = Mathf.Max(1f, defaultPixelsPerSecond);
            minPixelsPerSecond = Mathf.Max(1f, minPixelsPerSecond);
            maxPixelsPerSecond = Mathf.Max(minPixelsPerSecond, maxPixelsPerSecond);
            defaultPixelsPerSecond = Mathf.Clamp(defaultPixelsPerSecond, minPixelsPerSecond, maxPixelsPerSecond);
        }
    }

    [CustomEditor(typeof(TimelineEditorSettingsAsset))]
    internal sealed class TimelineEditorSettingsAssetInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            TimelineEditorSettingsAsset settings = (TimelineEditorSettingsAsset)target;
            EditorGUILayout.Space(8f);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(settings.IsActive))
                {
                    if (GUILayout.Button(settings.IsActive ? "Active Settings" : "Set as Active"))
                        TimelineEditorConfigLocator.SetActiveSettings(settings);
                }

                if (GUILayout.Button("Ping"))
                    EditorGUIUtility.PingObject(settings);
            }
        }
    }

    /// <summary>
    /// Hidden in-memory fallback instance used when the persisted Timeline Editor settings asset
    /// does not exist yet or could not be created.
    /// </summary>
    public static class TimelineEditorSettingsDefaults
    {
        private static TimelineEditorSettingsAsset instance;

        public static TimelineEditorSettingsAsset Instance
        {
            get
            {
                if (instance)
                    return instance;

                instance = ScriptableObject.CreateInstance<TimelineEditorSettingsAsset>();
                instance.hideFlags = HideFlags.HideAndDontSave;
                Apply(instance);
                return instance;
            }
        }

        /// <summary>
        /// Applies the canonical default Timeline Editor settings values to the provided asset instance.
        /// </summary>
        public static void Apply(TimelineEditorSettingsAsset asset)
        {
            if (!asset)
                return;

#if UNITY_EDITOR
            UnityEditor.SerializedObject serializedObject = new UnityEditor.SerializedObject(asset);

            SetFloat(serializedObject, "dragStartPixelThreshold", 4f);
            SetFloat(serializedObject, "snapThresholdPixels", 10f);
            SetBool(serializedObject, "enableSnap", true);
            SetBool(serializedObject, "clearObjectFieldFocusOnTimelineSelection", true);
            SetBool(serializedObject, "blockShortcutsWhileTextEditing", true);

            SetBool(serializedObject, "autoCreateTrackWhenDropHasNoFit", true);
            SetBool(serializedObject, "preferHoveredTrackForDrop", true);
            SetBool(serializedObject, "enableContextCreateClipHere", true);
            SetBool(serializedObject, "snapCreateClipHere", true);
            SetBool(serializedObject, "clampNegativeStartTimesToZero", true);

            SetBool(serializedObject, "selectTrackOnBackgroundClick", true);
            SetBool(serializedObject, "selectNewTrackAfterCreation", true);
            SetBool(serializedObject, "selectMovedClipAfterDrop", true);
            SetBool(serializedObject, "keepSelectionOnCancelledDrag", true);
            SetBool(serializedObject, "selectTimelineAfterDeletingTrack", true);

            SetFloat(serializedObject, "defaultPixelsPerSecond", 100f);
            SetFloat(serializedObject, "minPixelsPerSecond", 25f);
            SetFloat(serializedObject, "maxPixelsPerSecond", 400f);
            SetBool(serializedObject, "showInspectorByDefault", true);
            SetBool(serializedObject, "showValidationPanelByDefault", true);
            SetBool(serializedObject, "showBottomAddTrackButton", true);

            SetBool(serializedObject, "enableKeyboardShortcuts", true);
            SetBool(serializedObject, "allowDeleteShortcut", true);
            SetBool(serializedObject, "allowAddTrackShortcut", true);
            SetBool(serializedObject, "allowAddClipShortcut", true);
            SetBool(serializedObject, "allowAutoArrangeShortcut", true);

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
#endif
        }

#if UNITY_EDITOR
        private static void SetBool(UnityEditor.SerializedObject serializedObject, string propertyName, bool value)
        {
            UnityEditor.SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
                property.boolValue = value;
        }

        private static void SetFloat(UnityEditor.SerializedObject serializedObject, string propertyName, float value)
        {
            UnityEditor.SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
                property.floatValue = value;
        }
#endif
    }

}
