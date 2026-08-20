using UnityEditor;
using UnityEngine;

namespace Beardmage.ActionTimeline.Editor
{
    /// <summary>
    /// Resolves the persisted project-wide Timeline Editor configuration once per editor session.
    /// Explicit activation and creation invalidate the cache; normal OnGUI repainting does not scan
    /// the AssetDatabase.
    /// </summary>
    public static class TimelineEditorConfigLocator
    {
        private static bool resolved;
        private static TimelineEditorThemeAsset activeTheme;
        private static TimelineEditorSettingsAsset activeSettings;

        public static TimelineEditorThemeAsset GetThemeOrFallback()
        {
            ResolveIfNeeded();
            return activeTheme ? activeTheme : TimelineEditorThemeDefaults.Instance;
        }

        public static TimelineEditorSettingsAsset GetSettingsOrFallback()
        {
            ResolveIfNeeded();
            return activeSettings ? activeSettings : TimelineEditorSettingsDefaults.Instance;
        }

        public static TimelineEditorThemeAsset GetOrCreateAndActivateTheme()
        {
            ResolveIfNeeded();
            if (!activeTheme)
            {
                string path = AssetDatabase.GenerateUniqueAssetPath("Assets/TimelineEditorTheme.asset");
                TimelineEditorThemeAsset created = ScriptableObject.CreateInstance<TimelineEditorThemeAsset>();
                AssetDatabase.CreateAsset(created, path);
                AssetDatabase.SaveAssets();
                SetActiveTheme(created);
            }

            return activeTheme;
        }

        public static TimelineEditorSettingsAsset GetOrCreateAndActivateSettings()
        {
            ResolveIfNeeded();
            if (!activeSettings)
            {
                string path = AssetDatabase.GenerateUniqueAssetPath("Assets/TimelineEditorSettings.asset");
                TimelineEditorSettingsAsset created = ScriptableObject.CreateInstance<TimelineEditorSettingsAsset>();
                AssetDatabase.CreateAsset(created, path);
                TimelineEditorSettingsDefaults.Apply(created);
                AssetDatabase.SaveAssets();
                SetActiveSettings(created);
            }

            return activeSettings;
        }

        public static void SetActiveTheme(TimelineEditorThemeAsset theme)
        {
            if (!theme)
                return;

            SetActiveAsset("t:TimelineEditorThemeAsset", "isActive", theme);
            activeTheme = theme;
            resolved = true;
            TimelineEditorStyles.Reset();
        }

        public static void SetActiveSettings(TimelineEditorSettingsAsset settings)
        {
            if (!settings)
                return;

            SetActiveAsset("t:TimelineEditorSettingsAsset", "isActive", settings);
            activeSettings = settings;
            resolved = true;
        }

        public static void Invalidate()
        {
            resolved = false;
            activeTheme = null;
            activeSettings = null;
            TimelineEditorStyles.Reset();
        }

        private static void ResolveIfNeeded()
        {
            if (resolved)
                return;

            activeTheme = ResolveAsset<TimelineEditorThemeAsset>("t:TimelineEditorThemeAsset");
            activeSettings = ResolveAsset<TimelineEditorSettingsAsset>("t:TimelineEditorSettingsAsset");
            resolved = true;
        }

        private static T ResolveAsset<T>(string filter) where T : UnityEngine.Object
        {
            string[] guids = AssetDatabase.FindAssets(filter);
            T first = null;
            for (int index = 0; index < guids.Length; index++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[index]);
                T candidate = AssetDatabase.LoadAssetAtPath<T>(path);
                if (!candidate)
                    continue;

                if (!first)
                    first = candidate;

                SerializedObject serializedObject = new SerializedObject(candidate);
                SerializedProperty activeProperty = serializedObject.FindProperty("isActive");
                if (activeProperty != null && activeProperty.boolValue)
                    return candidate;
            }

            if (first)
            {
                SetActiveAsset(filter, "isActive", first);
                return first;
            }

            return null;
        }

        private static void SetActiveAsset(string filter, string propertyName, UnityEngine.Object activeObject)
        {
            string[] guids = AssetDatabase.FindAssets(filter);
            for (int index = 0; index < guids.Length; index++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[index]);
                UnityEngine.Object candidate = AssetDatabase.LoadAssetAtPath(path, activeObject.GetType());
                if (!candidate)
                    continue;

                SerializedObject serializedObject = new SerializedObject(candidate);
                SerializedProperty activeProperty = serializedObject.FindProperty(propertyName);
                if (activeProperty == null)
                    continue;

                bool shouldBeActive = candidate == activeObject;
                if (activeProperty.boolValue == shouldBeActive)
                    continue;

                Undo.RecordObject(candidate, shouldBeActive ? "Set Timeline Editor Asset Active" : "Deactivate Timeline Editor Asset");
                activeProperty.boolValue = shouldBeActive;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(candidate);
            }

            AssetDatabase.SaveAssets();
        }
    }
}
