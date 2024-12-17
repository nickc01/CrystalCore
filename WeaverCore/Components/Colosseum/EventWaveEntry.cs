using UnityEngine;
using UnityEngine.Events;

namespace WeaverCore.Components.Colosseum
{
    [System.Serializable]
    public class EventWaveEntry
    {
        [Tooltip("Unity event to run for this entry.")]
        public UnityEvent eventsToRun;

        [Tooltip("The delay from the start of the wave before running the event.")]
        public float delayBeforeRun;
    }
}
