using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using WeaverCore.Utilities;

namespace WeaverCore.Components.Colosseum
{
    public class ColosseumCutscene : Wave
    {
        public override bool AutoRun => false;
        
        [SerializeField, Tooltip("The delay before the cutscene begins.")]
        float beginDelay = 0f;

        [SerializeField, Tooltip("Area to lock the camera during the cutscene.")]
        CameraLockArea cutsceneLockArea;

        [SerializeField, Tooltip("Determines whether to play cutscene sound.")]
        bool playCutsceneSound = true;

        [SerializeField, Tooltip("Delay before triggering the main cutscene event.")]
        float preEventDelay = 1f;

        [Tooltip("Unity Event triggered during the cutscene.")]
        public UnityEvent TriggerEvent;

        [SerializeField, Tooltip("Delay after the main cutscene event.")]
        float postEventDelay = 1f;

        [SerializeField, Tooltip("Delay before the cutscene ends.")]
        float endDelay = 0.5f;

        public IEnumerator CutsceneRoutine()
        {
            yield return new WaitForSeconds(beginDelay);

            if (cutsceneLockArea != null)
            {
                cutsceneLockArea.gameObject.SetActive(true);
                //HeroUtilities.BeginInGameCutscene(playCutsceneSound);
                ColosseumRoomManager.BeginInGameCutsceneBasic(playCutsceneSound);
            }

            yield return new WaitForSeconds(preEventDelay);

            TriggerEvent?.Invoke();

            yield return new WaitForSeconds(postEventDelay);

            if (cutsceneLockArea != null)
            {
                cutsceneLockArea.gameObject.SetActive(false);
                //HeroUtilities.EndInGameCutscene();
                ColosseumRoomManager.EndInGameCutsceneBasic();
            }

            yield return new WaitForSeconds(endDelay);
        }

        public override IEnumerator RunWave(ColosseumRoomManager challenge)
        {
            yield return CutsceneRoutine();
        }

        protected override IEnumerator ManuallyRunRoutine(ColosseumRoomManager challenge, Func<ManualStopType> doStop)
        {
            yield return CutsceneRoutine();
        }
    }
}
