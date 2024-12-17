using System.Collections;
using UnityEngine;

namespace WeaverCore.Components.Colosseum
{
    public class SimpleColosseumGate : ColosseumGate
    {
        [Tooltip("The local position of the gate when opened.")]
        public Vector3 OpenPosition;

        [SerializeField, Tooltip("Duration for the gate to open.")]
        float openMoveTime = 0.25f;

        [SerializeField, Tooltip("Animation curve for the gate's opening movement.")]
        AnimationCurve openMoveCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [Tooltip("The local position of the gate when closed.")]
        public Vector3 ClosePosition;

        [SerializeField, Tooltip("Duration for the gate to close.")]
        float closeMoveTime = 0.25f;

        [SerializeField, Tooltip("Animation curve for the gate's closing movement.")]
        AnimationCurve closeMoveCurve = AnimationCurve.Linear(0, 0, 1, 1);

        protected override IEnumerator StateRoutine(bool opening, bool isInstant)
        {
            float instant = isInstant ? 0f : 1f;
            WeaverLog.Log("OPENING = " + opening);
            if (opening)
            {
                return Move(transform.localPosition, OpenPosition, openMoveTime * instant, openMoveCurve);
            }
            else
            {
                return Move(transform.localPosition, ClosePosition, closeMoveTime * instant, closeMoveCurve);
            }
        }

        void Reset()
        {
            ClosePosition = transform.localPosition;
            OpenPosition = transform.localPosition + new Vector3(0f, -10.43f, 0f);
        }

        IEnumerator Move(Vector3 source, Vector3 dest, float time, AnimationCurve curve)
        {
            for (float t = 0; t < time; t += Time.deltaTime)
            {
                transform.localPosition = Vector3.Lerp(source, dest, curve.Evaluate(t / time));
                yield return null;
            }
            transform.localPosition = dest;
        }
    }

    /*public class AnimatedColosseumGate : MonoBehaviour
    {
        public bool IsOpened { get; private set; }
        
        [SerializeField]
        bool startOpen = true;

        [SerializeField]
        string openAnimation = "Open";

        [SerializeField]
        string closeAnimation = "Close";

        [SerializeField]
        AudioClip openSound;

        [SerializeField]
        AudioClip closeSound;

        [SerializeField]
        List<GameObject> enabledOnOpen;

        [SerializeField]
        List<GameObject> enabledOnClose;

        [SerializeField]
        List<GameObject> disabledOnOpen;

        [SerializeField]
        List<GameObject> disabledOnClose;

        [SerializeField]
        UnityEvent test;

        [SerializeField]
        float openSoundDelay = 0;

        [SerializeField]
        float closeSoundDelay = 0f;

        Coroutine stateRoutine;

        float _startTime = -1f;

        [NonSerialized]
        WeaverAnimationPlayer _animator;
        public WeaverAnimationPlayer Animator => _animator ??= GetComponent<WeaverAnimationPlayer>();

        void Awake()
        {
            if (_startTime < 0)
            {
                _startTime = Time.time;

                if (startOpen)
                {
                    IsOpened = false;
                    Open();
                }
                else
                {
                    IsOpened = true;
                    Close();
                }
            }

            //StartCoroutine(TEST());
        }

        IEnumerator TEST()
        {
            yield return new WaitForSeconds(2f);
            while (true)
            {
                Open();
                yield return new WaitForSeconds(1);
                Close();
                yield return new WaitForSeconds(1);
            }
        }

        public void Open()
        {
            //WeaverLog.Log("A - ISOPENED = " + IsOpened);
            if (IsOpened)
            {
                return;
            }

            IsOpened = true;

            //WeaverLog.Log("OPENING");

            if (_startTime < 0)
            {
                _startTime = Time.time;
            }

            if (stateRoutine != null)
            {
                StopCoroutine(stateRoutine);
                stateRoutine = null;
            }

            stateRoutine = StartCoroutine(StateRoutine(true));
        }

        public void Close()
        {
            //WeaverLog.Log("B - ISOPENED = " + IsOpened);
            if (!IsOpened)
            {
                return;
            }

            IsOpened = false;

            //WeaverLog.Log("CLOSING");

            if (_startTime < 0)
            {
                _startTime = Time.time;
            }

            if (stateRoutine != null)
            {
                StopCoroutine(stateRoutine);
                stateRoutine = null;
            }

            stateRoutine = StartCoroutine(StateRoutine(false));
        }


        IEnumerator StateRoutine(bool open)
        {
            bool instant = Time.time < _startTime + 1f;
            if (instant)
            {
                Animator.PlaybackSpeed = 9999f;
            }
            
            if (open)
            {
                PlaySound(openSound, openSoundDelay);
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
                PlaySound(closeSound, closeSoundDelay);
                yield return Animator.PlayAnimationTillDone(closeAnimation);
            }

            if (instant)
            {
                Animator.PlaybackSpeed = 1;
            }

            stateRoutine = null;
        }

        void PlaySound(AudioClip sound, float delay)
        {
            if (Time.time >= _startTime + 1f)
            {
                if (delay > 0)
                {
                    WeaverAudio.PlayAtPointDelayed(delay, sound, transform.position);
                }
                else
                {
                    WeaverAudio.PlayAtPoint(sound, transform.position);
                }
            }
        }
    }*/
}
