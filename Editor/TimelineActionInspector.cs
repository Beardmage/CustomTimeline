using System;
using System.Collections.Generic;
using Beardmage.ActionTimeline;
using UnityEditor;
using UnityEngine;

namespace Beardmage.ActionTimeline.Editor
{
    /// <summary>
    /// Compact shared inspector for all <see cref="TimelineAction"/> assets.
    /// Specialized renderers may customize the Authoring section by family while
    /// Summary and Usage stay shared across all actions.
    /// </summary>
    [CustomEditor(typeof(TimelineAction), true)]
    public sealed class TimelineActionInspector : UnityEditor.Editor
    {
        private const string SummarySectionId = "summary";
        private const string AuthoringSectionId = "authoring";
        private const string UsageSectionId = "usage";

        private sealed class TimelineUsageGroup
        {
            public string Path;
            public string DisplayName;
            public ActionTimelineAsset Timeline;
            public readonly List<TimelineActionUsageOccurrence> Occurrences = new List<TimelineActionUsageOccurrence>(4);
        }

        private readonly List<TimelineUsageGroup> cachedUsageGroups = new List<TimelineUsageGroup>(8);

        private string lastUsageSignature = string.Empty;
        private TimelineAction cachedAction;

        private static readonly Color UnusedBadgeColor = new Color(0.30f, 0.30f, 0.30f);
        private static readonly Color SingleUseBadgeColor = new Color(0.18f, 0.32f, 0.52f);
        private static readonly Color SharedBadgeColor = new Color(0.44f, 0.34f, 0.14f);

        private void OnEnable()
        {
            cachedAction = target as TimelineAction;
            lastUsageSignature = string.Empty;
            cachedUsageGroups.Clear();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            TimelineAction action = target as TimelineAction;
            RefreshUsageCacheIfNeeded(action);

            DrawSummarySection(action);
            DrawAuthoringSection(action);
            DrawUsageSection(action);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSummarySection(TimelineAction action)
        {
            bool expanded = DrawSectionHeader(SummarySectionId, "Summary", true);
            if (expanded)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    string typeName = action != null ? ObjectNames.NicifyVariableName(action.GetType().Name) : "None";
                    float nominalDuration = action != null ? Mathf.Max(0f, action.NominalDuration) : 0f;
                    int usageCount = action != null ? TimelineActionUsageIndex.GetUsageCount(action) : 0;
                    int timelineCount = cachedUsageGroups.Count;

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Type", typeName);
                        GUILayout.FlexibleSpace();
                        DrawUsageBadgeLabel(usageCount);
                    }

                    EditorGUILayout.LabelField("Nominal Duration", FormatSeconds(nominalDuration));
                    EditorGUILayout.LabelField("Usage", BuildUsageSummary(usageCount, timelineCount));
                }
            }

            EndSectionSpacing(expanded);
        }

        private void DrawAuthoringSection(TimelineAction action)
        {
            bool expanded = DrawSectionHeader(AuthoringSectionId, "Authoring", true);
            if (expanded)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    TimelineActionInspectorRenderer renderer = TimelineActionInspectorRendererRegistry.Resolve(action);
                    renderer.DrawAuthoring(action, serializedObject);
                }
            }

            EndSectionSpacing(expanded);
        }

        private void DrawUsageSection(TimelineAction action)
        {
            int usageCount = action != null ? TimelineActionUsageIndex.GetUsageCount(action) : 0;
            bool defaultExpanded = usageCount > 0;
            bool expanded = DrawSectionHeader(UsageSectionId, "Usage", defaultExpanded);
            if (expanded)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    if (action == null)
                    {
                        EditorGUILayout.LabelField("No action selected.", EditorStyles.miniLabel);
                    }
                    else
                    {
                        int timelineCount = cachedUsageGroups.Count;
                        EditorGUILayout.LabelField("Summary", BuildUsageSummary(usageCount, timelineCount), EditorStyles.miniLabel);

                        if (usageCount <= 0 || timelineCount <= 0)
                        {
                            EditorGUILayout.Space(2f);
                            EditorGUILayout.LabelField("Unused by any timeline.", EditorStyles.miniLabel);
                        }
                        else
                        {
                            EditorGUILayout.Space(4f);
                            for (int index = 0; index < cachedUsageGroups.Count; index++)
                            {
                                DrawTimelineUsageGroup(cachedUsageGroups[index]);
                            }
                        }
                    }
                }
            }

            EndSectionSpacing(expanded);
        }

        private void DrawTimelineUsageGroup(TimelineUsageGroup group)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUIContent label = new GUIContent(group.DisplayName, group.Path);
                    if (GUILayout.Button(label, EditorStyles.label, GUILayout.ExpandWidth(true)))
                    {
                        if (group.Timeline)
                        {
                            Selection.activeObject = group.Timeline;
                            EditorGUIUtility.PingObject(group.Timeline);
                        }
                    }

                    GUI.enabled = group.Timeline != null;

                    if (GUILayout.Button("Ping", GUILayout.Width(46f)))
                        EditorGUIUtility.PingObject(group.Timeline);

                    if (GUILayout.Button("Open", GUILayout.Width(46f)))
                    {
                        Selection.activeObject = group.Timeline;
                        EditorGUIUtility.PingObject(group.Timeline);
                        EditorUtility.FocusProjectWindow();
                    }

                    if (GUILayout.Button("Editor", GUILayout.Width(52f)))
                        ActionTimelineEditorWindow.Open(group.Timeline);

                    GUI.enabled = true;
                }

                int occurrenceCount = group.Occurrences.Count;
                for (int occurrenceIndex = 0; occurrenceIndex < occurrenceCount; occurrenceIndex++)
                {
                    DrawOccurrenceRow(group, group.Occurrences[occurrenceIndex], occurrenceIndex + 1);
                }
            }
        }

        private void DrawOccurrenceRow(TimelineUsageGroup group, TimelineActionUsageOccurrence occurrence, int displayIndex)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                string trackLabel = string.IsNullOrWhiteSpace(occurrence.TrackName)
                    ? $"Track {occurrence.TrackIndex + 1}"
                    : occurrence.TrackName;

                string clipLabel = string.IsNullOrWhiteSpace(occurrence.ClipDebugName)
                    ? $"Clip {occurrence.ClipIndex + 1}"
                    : occurrence.ClipDebugName;

                string overrideToken = occurrence.UseDurationOverride ? " • Override" : string.Empty;
                string line = $"{displayIndex}. {trackLabel} • {clipLabel} • {FormatSeconds(occurrence.ClipStartTime)}{overrideToken}";
                EditorGUILayout.LabelField(line, EditorStyles.miniLabel, GUILayout.ExpandWidth(true));

                GUI.enabled = group.Timeline != null;
                if (GUILayout.Button("Focus", GUILayout.Width(52f)))
                    ActionTimelineEditorWindow.OpenAndFocusClip(group.Timeline, occurrence.TrackIndex, occurrence.ClipIndex);
                GUI.enabled = true;
            }
        }

        private void RefreshUsageCacheIfNeeded(TimelineAction action)
        {
            if (action == null)
            {
                cachedAction = null;
                lastUsageSignature = string.Empty;
                cachedUsageGroups.Clear();
                return;
            }

            int usageCount = TimelineActionUsageIndex.GetUsageCount(action);
            IReadOnlyList<TimelineActionUsageOccurrence> occurrences = TimelineActionUsageIndex.GetUsageOccurrences(action);
            string signature = BuildUsageSignature(action, usageCount, occurrences);
            if (cachedAction == action && signature == lastUsageSignature)
                return;

            cachedAction = action;
            lastUsageSignature = signature;
            cachedUsageGroups.Clear();

            Dictionary<string, TimelineUsageGroup> groupsByPath = new Dictionary<string, TimelineUsageGroup>(8);
            int occurrenceCount = occurrences?.Count ?? 0;
            for (int index = 0; index < occurrenceCount; index++)
            {
                TimelineActionUsageOccurrence occurrence = occurrences[index];
                string timelinePath = occurrence.TimelinePath;
                if (string.IsNullOrWhiteSpace(timelinePath))
                    continue;

                if (!groupsByPath.TryGetValue(timelinePath, out TimelineUsageGroup group))
                {
                    ActionTimelineAsset timeline = AssetDatabase.LoadAssetAtPath<ActionTimelineAsset>(timelinePath);
                    string displayName = timeline ? timeline.name : System.IO.Path.GetFileNameWithoutExtension(timelinePath);

                    group = new TimelineUsageGroup
                    {
                        Path = timelinePath,
                        DisplayName = string.IsNullOrWhiteSpace(displayName) ? "Timeline" : displayName,
                        Timeline = timeline
                    };

                    groupsByPath[timelinePath] = group;
                    cachedUsageGroups.Add(group);
                }

                group.Occurrences.Add(occurrence);
            }
        }

        private static string BuildUsageSignature(TimelineAction action, int usageCount, IReadOnlyList<TimelineActionUsageOccurrence> occurrences)
        {
            unchecked
            {
                int hash = action ? action.GetInstanceID() : 0;
                hash = (hash * 397) ^ usageCount;

                int occurrenceCount = occurrences?.Count ?? 0;
                hash = (hash * 397) ^ occurrenceCount;

                for (int index = 0; index < occurrenceCount; index++)
                {
                    TimelineActionUsageOccurrence occurrence = occurrences[index];
                    hash = (hash * 397) ^ (occurrence.TimelinePath ?? string.Empty).GetHashCode();
                    hash = (hash * 397) ^ occurrence.TrackIndex;
                    hash = (hash * 397) ^ occurrence.ClipIndex;
                    hash = (hash * 397) ^ occurrence.ClipStartTime.GetHashCode();
                    hash = (hash * 397) ^ occurrence.UseDurationOverride.GetHashCode();
                }

                return hash.ToString();
            }
        }

        private bool DrawSectionHeader(string sectionId, string label, bool defaultExpanded)
        {
            string key = BuildSectionStateKey(sectionId);
            bool expanded = SessionState.GetBool(key, defaultExpanded);

            Rect rect = GUILayoutUtility.GetRect(1f, 22f, GUILayout.ExpandWidth(true));
            Rect foldoutRect = new Rect(rect.x + 4f, rect.y + 1f, rect.width - 8f, rect.height - 2f);

            EditorGUI.DrawRect(rect, new Color(0.16f, 0.16f, 0.16f, 1f));
            expanded = EditorGUI.Foldout(foldoutRect, expanded, label, true);

            SessionState.SetBool(key, expanded);
            return expanded;
        }

        private static void EndSectionSpacing(bool expanded)
        {
            EditorGUILayout.Space(expanded ? 4f : 2f);
        }

        private string BuildSectionStateKey(string sectionId)
        {
            string assetPath = target ? AssetDatabase.GetAssetPath(target) : string.Empty;
            return $"Beardmage.ActionTimeline.TimelineActionInspector.{assetPath}.{sectionId}";
        }

        private static string BuildUsageSummary(int usageCount, int timelineCount)
        {
            if (usageCount <= 0)
                return "Unused";

            string usageLabel = usageCount == 1 ? "1 use" : $"{usageCount} uses";
            string timelineLabel = timelineCount == 1 ? "1 timeline" : $"{timelineCount} timelines";
            return $"{usageLabel} • {timelineLabel}";
        }

        private static string FormatSeconds(float value)
        {
            return $"{Mathf.Max(0f, value):0.00}s";
        }

        private static void DrawUsageBadgeLabel(int usageCount)
        {
            string label;
            Color color;

            if (usageCount <= 0)
            {
                label = "Unused";
                color = UnusedBadgeColor;
            }
            else if (usageCount == 1)
            {
                label = "Single Use";
                color = SingleUseBadgeColor;
            }
            else
            {
                label = "Shared";
                color = SharedBadgeColor;
            }

            Rect rect = GUILayoutUtility.GetRect(78f, 18f, GUILayout.Width(78f));
            EditorGUI.DrawRect(rect, color);

            GUIStyle style = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            style.normal.textColor = Color.white;

            GUI.Label(rect, label, style);
        }
    }
}
