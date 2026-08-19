using System;
using System.IO;
using Beardmage.ActionTimeline;
using UnityEditor;
using UnityEngine;

namespace Beardmage.ActionTimeline.Editor
{
    public static class ActionTimelineAssetCreationUtility
    {
        public const string DefaultTimelineDirectory = "Assets/ActionTimelines";

        [MenuItem("Assets/Create/Action Timeline/Timeline", priority = 210)]
        public static void CreateTimelineFromAssetsMenu()
        {
            CreateAndSelectNewTimeline();
        }

        [MenuItem("Tools/Action Timeline/Create Timeline")]
        public static void CreateTimelineFromToolsMenu()
        {
            CreateAndSelectNewTimeline();
        }

        public static ActionTimelineAsset CreateAndSelectNewTimeline(string targetDirectory = null)
        {
            string directory = ResolveTargetDirectory(targetDirectory);
            if (string.IsNullOrWhiteSpace(directory))
                directory = DefaultTimelineDirectory;

            EnsureFolderExists(directory);
            if (!AssetDatabase.IsValidFolder(directory))
            {
                Debug.LogError($"Failed to create or resolve ActionTimeline target folder '{directory}'.");
                return null;
            }

            string assetPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(directory, "New ActionTimeline.asset").Replace("\\", "/"));
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                Debug.LogError("Failed to create ActionTimeline asset because the generated asset path is empty.");
                return null;
            }

            ActionTimelineAsset timeline = ScriptableObject.CreateInstance<ActionTimelineAsset>();
            AssetDatabase.CreateAsset(timeline, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = timeline;
            EditorGUIUtility.PingObject(timeline);
            EditorUtility.FocusProjectWindow();
            return timeline;
        }

        private static string ResolveTargetDirectory(string explicitDirectory)
        {
            if (!string.IsNullOrWhiteSpace(explicitDirectory))
                return explicitDirectory.Replace("\\", "/");

            UnityEngine.Object activeObject = Selection.activeObject;
            if (!activeObject)
                return DefaultTimelineDirectory;

            string selectedPath = AssetDatabase.GetAssetPath(activeObject);
            if (string.IsNullOrWhiteSpace(selectedPath))
                return DefaultTimelineDirectory;

            if (AssetDatabase.IsValidFolder(selectedPath))
                return selectedPath;

            string directory = Path.GetDirectoryName(selectedPath);
            return string.IsNullOrWhiteSpace(directory) ? DefaultTimelineDirectory : directory.Replace("\\", "/");
        }

        private static void EnsureFolderExists(string folderPath)
        {
            string normalized = (folderPath ?? string.Empty).Replace("\\", "/").Trim();
            if (string.IsNullOrWhiteSpace(normalized))
                normalized = DefaultTimelineDirectory;

            if (!normalized.StartsWith("Assets"))
                normalized = DefaultTimelineDirectory;

            if (AssetDatabase.IsValidFolder(normalized))
                return;

            string[] parts = normalized.Split('/');
            if (parts.Length == 0 || parts[0] != "Assets")
                return;

            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }

    public static class TimelineRectUtility
    {
        public static float TimeToPixel(float time, float pixelsPerSecond, float originX)
        {
            float safeTime = Mathf.Max(0f, time);
            float safePixelsPerSecond = Mathf.Max(0.0001f, pixelsPerSecond);
            return originX + (safeTime * safePixelsPerSecond);
        }

        public static float PixelToTime(float pixelX, float pixelsPerSecond, float originX)
        {
            float safePixelsPerSecond = Mathf.Max(0.0001f, pixelsPerSecond);
            return Mathf.Max(0f, (pixelX - originX) / safePixelsPerSecond);
        }

        public static float PixelToTimeClamped(float pixelX, float pixelsPerSecond, float originX)
        {
            return Mathf.Max(0f, PixelToTime(pixelX, pixelsPerSecond, originX));
        }

        public static float DurationToWidth(float logicalDuration, float pixelsPerSecond)
        {
            float safeDuration = Mathf.Max(0f, logicalDuration);
            float safePixelsPerSecond = Mathf.Max(0.0001f, pixelsPerSecond);
            return safeDuration * safePixelsPerSecond;
        }

        public static float GetVisualWidth(float logicalDuration, float pixelsPerSecond, float minVisualWidth)
        {
            return Mathf.Max(minVisualWidth, DurationToWidth(logicalDuration, pixelsPerSecond));
        }

        public static Rect GetClipRect(
            float startTime,
            float logicalDuration,
            float pixelsPerSecond,
            float originX,
            float laneY,
            float laneHeight,
            float minVisualWidth,
            float verticalPadding = 2f)
        {
            float x = TimeToPixel(startTime, pixelsPerSecond, originX);
            float width = GetVisualWidth(logicalDuration, pixelsPerSecond, minVisualWidth);
            float y = laneY + verticalPadding;
            float height = Mathf.Max(1f, laneHeight - (verticalPadding * 2f));
            return new Rect(x, y, width, height);
        }

        public static int CanvasYToLaneIndex(float canvasY, float laneHeight, int trackCount)
        {
            if (trackCount <= 0 || laneHeight <= 0f)
                return -1;

            int laneIndex = Mathf.FloorToInt(canvasY / laneHeight);
            if (laneIndex < 0 || laneIndex >= trackCount)
                return -1;

            return laneIndex;
        }
    }

    public static class EditorAssetLinkUtility
    {
        public static void Ping(UnityEngine.Object obj)
        {
            if (!obj)
                return;
            EditorGUIUtility.PingObject(obj);
        }

        public static void Select(UnityEngine.Object obj)
        {
            if (!obj)
                return;
            Selection.activeObject = obj;
        }

        public static void SelectAndPing(UnityEngine.Object obj)
        {
            if (!obj)
                return;
            Selection.activeObject = obj;
            EditorGUIUtility.PingObject(obj);
            EditorUtility.FocusProjectWindow();
        }

        public static void OpenInspector(UnityEngine.Object obj)
        {
            if (!obj)
                return;
            Selection.activeObject = obj;
            EditorGUIUtility.PingObject(obj);
            EditorUtility.FocusProjectWindow();
        }

        public static void OpenTimelineEditor(ActionTimelineAsset timeline)
        {
            if (!timeline)
                return;
            ActionTimelineEditorWindow.Open(timeline);
        }
    }

    public sealed class FloatValuePromptWindow : EditorWindow
    {
        private Action<float> onConfirm;
        private string valueLabel;
        private float value;

        public static void Open(string title, string label, float initialValue, Action<float> onConfirm)
        {
            FloatValuePromptWindow window = CreateInstance<FloatValuePromptWindow>();
            window.titleContent = new GUIContent(title);
            window.valueLabel = label;
            window.value = Mathf.Max(0f, initialValue);
            window.onConfirm = onConfirm;
            window.minSize = new Vector2(260f, 80f);
            window.maxSize = new Vector2(260f, 80f);
            window.ShowUtility();
            window.Focus();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8f);
            EditorGUI.BeginChangeCheck();
            float newValue = EditorGUILayout.FloatField(valueLabel, value);
            if (EditorGUI.EndChangeCheck())
                value = Mathf.Max(0f, newValue);

            GUILayout.FlexibleSpace();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Cancel"))
                {
                    Close();
                    GUIUtility.ExitGUI();
                    return;
                }

                if (GUILayout.Button("Apply"))
                {
                    onConfirm?.Invoke(Mathf.Max(0f, value));
                    Close();
                    GUIUtility.ExitGUI();
                }
            }

            EditorGUILayout.Space(8f);
        }
    }
}
