using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using WeaverCore;
using WeaverCore.Components;

namespace WeaverCore.Components.Colosseum
{
    public class AnimatedColosseumGate : ColosseumGate
    {
        [SerializeField, Tooltip("Animation name for opening the gate.")]
        string openAnimation = "Open";

        [SerializeField, Tooltip("Animation name for closing the gate.")]
        string closeAnimation = "Close";

        protected override IEnumerator StateRoutine(bool opening, bool isInstant)
        {
            bool instant = isInstant;
            if (instant)
            {
                Animator.PlaybackSpeed = 9999f;
            }
            
            if (opening)
            {
                if (!instant)
                {
                    PlaySound(openSound, openSoundDelay, openSoundVolume);
                }
                yield return Animator.PlayAnimationTillDone(openAnimation);

                foreach (var obj in enabledOnOpen)
                {
                    obj.SetActive(true);
                }
                foreach (var obj in disabledOnOpen)
                {
                    obj.SetActive(false);
                }
            }
            else
            {
                foreach (var obj in enabledOnClose)
                {
                    obj.SetActive(true);
                }
                foreach (var obj in disabledOnClose)
                {
                    obj.SetActive(false);
                }
                if (!instant)
                {
                    PlaySound(closeSound, closeSoundDelay, closeSoundVolume);
                }
                yield return Animator.PlayAnimationTillDone(closeAnimation);
            }

            if (instant)
            {
                Animator.PlaybackSpeed = 1;
            }
        }
    }
}
