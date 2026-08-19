namespace Beardmage.ActionTimeline.Editor
{
    public enum TimelineSelectionKind
    {
        None = 0,
        Timeline = 1,
        Track = 2,
        Clip = 3
    }

    /// <summary>
    /// Current pointer manipulation mode on a timeline clip.
    /// </summary>
    public enum TimelineClipManipulationMode
    {
        None = 0,
        Move = 1,
        ResizeLeft = 2,
        ResizeRight = 3
    }
}
