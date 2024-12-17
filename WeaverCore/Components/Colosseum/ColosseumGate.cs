using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WeaverCore.Interfaces;

namespace WeaverCore.Components.Colosseum
{
    public abstract class ColosseumGate : MonoBehaviour, IGate
    {
        public bool IsOpened { get; private set; }
        
        [SerializeField, Tooltip("Indicates if the gate starts in an open state.")]
        protected bool startOpen = true;

        [SerializeField, Tooltip("Audio clip to play when the gate opens.")]
        protected AudioClip openSound;

        [SerializeField, Tooltip("The volume of the open sound")]
        protected float openSoundVolume = 1f;

        [SerializeField, Tooltip("Audio clip to play when the gate closes.")]
        protected AudioClip closeSound;

        [SerializeField, Tooltip("The volume of the close sound")]
        protected float closeSoundVolume = 1f;

        [SerializeField, Tooltip("Game objects to enable when the gate opens.")]
        protected List<GameObject> enabledOnOpen;

        [SerializeField, Tooltip("Game objects to enable when the gate closes.")]
        protected List<GameObject> enabledOnClose;

        [SerializeField, Tooltip("Game objects to disable when the gate opens.")]
        protected List<GameObject> disabledOnOpen;

        [SerializeField, Tooltip("Game objects to disable when the gate closes.")]
        protected List<GameObject> disabledOnClose;

        [SerializeField, Tooltip("Delay before playing the gate opening sound.")]
        protected float openSoundDelay = 0;

        [SerializeField, Tooltip("Delay before playing the gate closing sound.")]
        protected float closeSoundDelay = 0f;

        Coroutine stateRoutine;

        float _startTime = -1f;
        public float StartTime
        {
            get
            {
                if (_startTime < 0)
                {
                    _startTime = Time.time;
                }
                return _startTime;
            }
        }

        public bool IsInstant => Time.time < _startTime + 1f;

        [NonSerialized]
        WeaverAnimationPlayer _animator;
        public WeaverAnimationPlayer Animator => _animator ??= GetComponent<WeaverAnimationPlayer>();

        protected virtual void Awake()
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

            stateRoutine = StartCoroutine(RoutineRunner(true, IsInstant));
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

            stateRoutine = StartCoroutine(RoutineRunner(false, IsInstant));
        }

        IEnumerator RoutineRunner(bool opening, bool instant)
        {
            if (opening)
            {
                PlaySound(openSound, openSoundDelay, openSoundVolume);
            }
            else
            {
                PlaySound(closeSound, closeSoundDelay, closeSoundVolume);
            }
            yield return StateRoutine(opening, instant);
            stateRoutine = null;
        }

        protected abstract IEnumerator StateRoutine(bool opening, bool isInstant);

        protected void PlaySound(AudioClip sound, float delay, float volume)
        {
            if (!IsInstant)
            {
                if (delay > 0)
                {
                    WeaverAudio.PlayAtPointDelayed(delay, sound, transform.position, volume);
                }
                else
                {
                    WeaverAudio.PlayAtPoint(sound, transform.position, volume);
                }
            }
        }

        public void OpenInstant()
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

            stateRoutine = StartCoroutine(RoutineRunner(true, true));
        }

        public void CloseInstant()
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

            stateRoutine = StartCoroutine(RoutineRunner(false, true));
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
