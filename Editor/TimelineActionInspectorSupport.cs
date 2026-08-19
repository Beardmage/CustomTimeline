using Beardmage.ActionTimeline;
using UnityEditor;
using UnityEngine;

namespace Beardmage.ActionTimeline.Editor
{
    /// <summary>
    /// Base renderer used by TimelineActionInspector to specialize the Authoring section for action families.
    /// Projects may copy/extend this pattern with their own inspectors.
    /// </summary>
    public abstract class TimelineActionInspectorRenderer
    {
        public abstract bool CanRender(TimelineAction action);

        public abstract void DrawAuthoring(
            TimelineAction action,
            SerializedObject serializedObject);
    }

    public static class TimelineActionInspectorRendererRegistry
    {
        private static readonly TimelineActionInspectorRenderer genericRenderer = new GenericTimelineActionInspectorRenderer();

        public static TimelineActionInspectorRenderer Resolve(TimelineAction action)
        {
            return genericRenderer;
        }
    }

    /// <summary>
    /// Safe fallback renderer used for all actions without a specialized inspector.
    /// </summary>
    public sealed class GenericTimelineActionInspectorRenderer : TimelineActionInspectorRenderer
    {
        public override bool CanRender(TimelineAction action)
        {
            return true;
        }

        public override void DrawAuthoring(TimelineAction action, SerializedObject serializedObject)
        {
            if (serializedObject == null)
                return;

            EditorGUILayout.LabelField(
                "Generic authoring view.",
                EditorStyles.miniLabel);

            EditorGUILayout.Space(2f);
            DrawAllPropertiesExcludingScript(serializedObject);
        }

        private static void DrawAllPropertiesExcludingScript(SerializedObject serializedObject)
        {
            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (iterator.propertyPath == "m_Script")
                    continue;

                EditorGUILayout.PropertyField(iterator, true);
            }
        }
    }

    /// <summary>
    /// Small shared IMGUI helpers used by action inspector renderers.
    /// Keeps renderer code compact while staying independent from heavier editor frameworks.
    /// </summary>
    public static class TimelineActionInspectorBlocks
    {
        private static readonly Color SubBlockHeaderColor = new Color(0.20f, 0.20f, 0.20f, 1f);

        public static void BeginSubBlock(string label)
        {
            Rect headerRect = GUILayoutUtility.GetRect(1f, 18f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(headerRect, SubBlockHeaderColor);

            Rect labelRect = new Rect(
                headerRect.x + 6f,
                headerRect.y + 1f,
                headerRect.width - 12f,
                headerRect.height - 2f);

            EditorGUI.LabelField(labelRect, label, EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        }

        public static void EndSubBlock()
        {
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2f);
        }

        public static void DrawPropertyIfPresent(SerializedObject serializedObject, string propertyName, bool includeChildren = false)
        {
            if (serializedObject == null || string.IsNullOrWhiteSpace(propertyName))
                return;

            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
                return;

            EditorGUILayout.PropertyField(property, includeChildren);
        }
    }
}
