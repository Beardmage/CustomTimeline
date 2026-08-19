using UnityEngine;

namespace Beardmage.ActionTimeline
{
    /// <summary>
    /// Generic authoring unit referenced by an action timeline clip.
    /// The package intentionally does not define execution semantics; each project owns its runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "Action Timeline/Actions/Timeline Action", fileName = "New TimelineAction")]
    public class TimelineAction : ScriptableObject
    {
        [SerializeField, Min(0f), Tooltip("Nominal authored duration of this action in seconds. Timeline clips may override it locally.")]
        private float nominalDuration = 1f;

        /// <summary>
        /// Nominal authored duration of this action in seconds.
        /// Timeline clips may override it locally for scheduling and authoring readability.
        /// </summary>
        public virtual float NominalDuration => Mathf.Max(0f, nominalDuration);

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            nominalDuration = Mathf.Max(0f, nominalDuration);
        }
#endif
    }
}
