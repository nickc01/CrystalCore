using UnityEngine;
using System.Collections;
using System;

namespace WeaverCore.Components.Colosseum
{
    public abstract class Wave : MonoBehaviour
    {
        public enum ManualStopType 
        {
            None,
            Gracefully,
            Forcefully
        }

        public virtual bool AutoRun => gameObject.activeInHierarchy && enabled;
        public abstract IEnumerator RunWave(ColosseumRoomManager challenge);

        protected abstract IEnumerator ManuallyRunRoutine(ColosseumRoomManager challenge, Func<ManualStopType> doStop);

        ManualStopType currentStopType = ManualStopType.None;

        IEnumerator ManuallyRunInternal()
        {
            var challenge = gameObject.GetComponentInParent<ColosseumRoomManager>();
            yield return ManuallyRunRoutine(challenge, () => currentStopType);
            manualRoutine = null;
        }

        Coroutine manualRoutine;

        /// <summary>
        /// Manually starts up a wave
        /// </summary>
        public void ManuallyStartWave()
        {
            WeaverLog.Log($"STARTING {name} manually");
            if (manualRoutine != null)
            {
                throw new Exception($"Error: The wave {name} has already been manually started!");
            }

            currentStopType = ManualStopType.None;
            manualRoutine = StartCoroutine(ManuallyRunInternal());
        }

        /// <summary>
        /// Manually stops a wave gracefully. Meaning, the wave will stop once it completes a successful loop
        /// </summary>
        public void ManuallyStopWaveGracefully()
        {
            WeaverLog.Log($"Stopping {name} gracefully");
            currentStopType = ManualStopType.Gracefully;
        }

        /// <summary>
        /// Manually stops a wave forcefully. Meaning, the wave will be stopped regardless of whether it has completed a loop or not.
        /// </summary>
        public void ManuallyStopWaveForcefully()
        {
            WeaverLog.Log($"Stopping {name} forcefully");
            currentStopType = ManualStopType.Forcefully;
        }
    }
}
