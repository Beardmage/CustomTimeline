namespace Beardmage.ActionTimeline.Editor
{
    /// <summary>
    /// Package-local editor configuration entry point.
    /// The V1 export intentionally uses hidden fallback assets so projects can decide if/where
    /// persistent theme and settings assets should live.
    /// </summary>
    public static class TimelineEditorConfigLocator
    {
        public static TimelineEditorThemeAsset GetThemeOrFallback()
        {
            return TimelineEditorThemeDefaults.Instance;
        }

        public static TimelineEditorSettingsAsset GetSettingsOrFallback()
        {
            return TimelineEditorSettingsDefaults.Instance;
        }
    }
}
