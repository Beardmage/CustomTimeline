using System.Collections.Generic;
using Beardmage.ActionTimeline;
using UnityEditor;
using UnityEngine;

namespace Beardmage.ActionTimeline.Editor
{
    /// <summary>
    /// Lightweight inspector for ActionTimelineAsset assets.
    /// It is intentionally not a full authoring surface:
    /// the dedicated Timeline Editor window remains the primary tool.
    /// </summary>
    [CustomEditor(typeof(ActionTimelineAsset))]
    public sealed class ActionTimelineInspector : UnityEditor.Editor
    {
        private SerializedProperty categoriesProperty;
        private readonly TimelineValidator validator = new TimelineValidator();

        private bool showTrackOverview = true;
        private bool showValidationDetails = true;

        private void OnEnable()
        {
            categoriesProperty = serializedObject.FindProperty("categories");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            ActionTimelineAsset timeline = (ActionTimelineAsset)target;
            List<TimelineValidationResult> validationResults = validator.Validate(timeline);

            DrawPrimaryActions(timeline);
            EditorGUILayout.Space(6f);

            DrawSummary(timeline, validationResults);
            EditorGUILayout.Space(6f);

            DrawTrackOverview(timeline);
            EditorGUILayout.Space(6f);

            DrawValidation(validationResults);

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawPrimaryActions(ActionTimelineAsset timeline)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open Timeline Editor", GUILayout.Height(24f)))
                    EditorAssetLinkUtility.OpenTimelineEditor(timeline);

                if (GUILayout.Button("Ping", GUILayout.Width(70f), GUILayout.Height(24f)))
                    EditorAssetLinkUtility.Ping(timeline);
            }
        }

        private void DrawSummary(ActionTimelineAsset timeline, List<TimelineValidationResult> validationResults)
        {
            EditorGUILayout.LabelField("Summary", EditorStyles.boldLabel);

            int trackCount = timeline.Tracks?.Count ?? 0;
            int categoryCount = timeline.Categories?.Count ?? 0;
            int validClipCount = validator.CountValidClips(timeline);
            float duration = TimelineDurationUtility.GetTimelineDuration(timeline);

            int warningCount = 0;
            int errorCount = 0;

            for (int i = 0; i < validationResults.Count; i++)
            {
                if (validationResults[i].IsError)
                    errorCount++;
                else if (validationResults[i].IsWarning)
                    warningCount++;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Categories", categoryCount.ToString());
                EditorGUILayout.LabelField("Tracks", trackCount.ToString());
                EditorGUILayout.LabelField("Valid Clips", validClipCount.ToString());
                EditorGUILayout.LabelField("Duration", TimelineDurationUtility.FormatSeconds(duration));
                EditorGUILayout.LabelField("Warnings", warningCount.ToString());
                EditorGUILayout.LabelField("Errors", errorCount.ToString());
            }

            if (trackCount <= 0)
            {
                EditorGUILayout.HelpBox(
                    "This timeline has no tracks yet.",
                    MessageType.Info);
            }
        }

        private void DrawTrackOverview(ActionTimelineAsset timeline)
        {
            showTrackOverview = EditorGUILayout.BeginFoldoutHeaderGroup(showTrackOverview, "Track Overview");

            if (showTrackOverview)
            {
                if (categoriesProperty == null || categoriesProperty.arraySize <= 0)
                {
                    EditorGUILayout.HelpBox(
                        "No categories to display.",
                        MessageType.None);
                }
                else
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    int flatTrackIndex = 0;
                    for (int categoryIndex = 0; categoryIndex < categoriesProperty.arraySize; categoryIndex++)
                    {
                        SerializedProperty categoryProperty = categoriesProperty.GetArrayElementAtIndex(categoryIndex);
                        SerializedProperty categoryNameProperty = categoryProperty.FindPropertyRelative("categoryName");
                        SerializedProperty categoryEnabledProperty = categoryProperty.FindPropertyRelative("isEnabled");
                        SerializedProperty tracksProperty = categoryProperty.FindPropertyRelative("tracks");
                        string categoryName = string.IsNullOrWhiteSpace(categoryNameProperty.stringValue)
                            ? $"Category {categoryIndex + 1}"
                            : categoryNameProperty.stringValue;

                        EditorGUILayout.LabelField(
                            $"{categoryName} ({(categoryEnabledProperty.boolValue ? "Enabled" : "Disabled")})",
                            EditorStyles.boldLabel);

                        int categoryTrackCount = tracksProperty != null ? tracksProperty.arraySize : 0;
                        for (int localTrackIndex = 0; localTrackIndex < categoryTrackCount; localTrackIndex++, flatTrackIndex++)
                        {
                            SerializedProperty trackProperty = tracksProperty.GetArrayElementAtIndex(localTrackIndex);
                            SerializedProperty trackNameProperty = trackProperty.FindPropertyRelative("trackName");
                            SerializedProperty enabledProperty = trackProperty.FindPropertyRelative("isEnabled");
                            SerializedProperty clipsProperty = trackProperty.FindPropertyRelative("clips");
                            string trackName = string.IsNullOrWhiteSpace(trackNameProperty.stringValue)
                                ? $"Track {flatTrackIndex + 1}"
                                : trackNameProperty.stringValue;
                            int clipCount = clipsProperty?.arraySize ?? 0;
                            ActionTimelineTrack runtimeTrack = GetRuntimeTrack(timeline, flatTrackIndex);
                            bool hasOverlap = runtimeTrack != null && TimelineOverlapUtility.TrackHasOverlap(runtimeTrack);

                            using (new EditorGUILayout.HorizontalScope())
                            {
                                GUILayout.Space(14f);
                                string enabledLabel = enabledProperty.boolValue ? "Enabled" : "Disabled";
                                string overlapLabel = hasOverlap ? " | Overlap" : string.Empty;
                                EditorGUILayout.LabelField(trackName, GUILayout.MinWidth(126f));
                                EditorGUILayout.LabelField(
                                    $"{enabledLabel} | Clips: {clipCount}{overlapLabel}",
                                    EditorStyles.miniLabel);
                            }
                        }
                    }

                    EditorGUILayout.EndVertical();
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawValidation(List<TimelineValidationResult> validationResults)
        {
            showValidationDetails = EditorGUILayout.BeginFoldoutHeaderGroup(showValidationDetails, "Validation");

            if (showValidationDetails)
            {
                if (validationResults.Count <= 0)
                {
                    EditorGUILayout.HelpBox(
                        "No validation issues found.",
                        MessageType.Info);
                }
                else
                {
                    for (int i = 0; i < validationResults.Count; i++)
                    {
                        TimelineValidationResult result = validationResults[i];
                        EditorGUILayout.HelpBox(result.Message, ToMessageType(result.Severity));
                    }
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private ActionTimelineTrack GetRuntimeTrack(ActionTimelineAsset timeline, int trackIndex)
        {
            if (!timeline)
                return null;

            IReadOnlyList<ActionTimelineTrack> tracks = timeline.Tracks;
            if (tracks == null || trackIndex < 0 || trackIndex >= tracks.Count)
                return null;

            return tracks[trackIndex];
        }

        private static MessageType ToMessageType(TimelineValidationSeverity severity)
        {
            return severity switch
            {
                TimelineValidationSeverity.Error => MessageType.Error,
                TimelineValidationSeverity.Warning => MessageType.Warning,
                _ => MessageType.Info
            };
        }
    }
}
