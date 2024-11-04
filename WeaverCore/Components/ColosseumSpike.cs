using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using WeaverCore.Utilities;

namespace WeaverCore.Components
{
    public class ColosseumSpike : MonoBehaviour
    {
        [Space]
        [Header("Animations")]
        [SerializeField]
        protected string idleAnimation = "Idle";
        [SerializeField]
        protected string anticAnimation = "Antic";
        [SerializeField]
        protected string expandAnimation = "Expand";
        [SerializeField]
        protected string retractAnimation = "Retract";

        [Space]
        [Header("Sounds")]
        [SerializeField]
        List<AudioClip> anticSounds = new List<AudioClip>();

        [SerializeField]
        Vector2 anticSoundsPitchRange = new Vector2(0.9f, 1.1f);

        [field: SerializeField]
        public float AnticSoundsVolume { get; private set; } = 1f;

        [SerializeField]
        List<AudioClip> expandSounds = new List<AudioClip>();

        [SerializeField]
        Vector2 expandSoundsPitchRange = new Vector2(0.9f, 1.1f);

        [field: SerializeField]
        public float ExpandSoundsVolume { get; private set; } = 1f;

        [SerializeField]
        List<AudioClip> retractSounds = new List<AudioClip>();

        [SerializeField]
        Vector2 retractSoundsPitchRange = new Vector2(0.9f, 1.1f);

        [field: SerializeField]
        public float RetractSoundsVolume { get; private set; } = 1f;

        [Space]
        [Header("Debugging")]
        [SerializeField]
        bool testFunctionality = false;

        [NonSerialized]
        ParticleSystem _dustParticles;
        public ParticleSystem DustParticles => _dustParticles ??= GetComponentInChildren<ParticleSystem>();

        [NonSerialized]
        WeaverAnimationPlayer _mainAnimator;
        public WeaverAnimationPlayer MainAnimator => _mainAnimator ??= GetComponentInChildren<WeaverAnimationPlayer>();

        [NonSerialized]
        Collider2D _mainCollider;
        public Collider2D MainCollider => _mainCollider ??= GetComponentInChildren<Collider2D>();

        [NonSerialized]
        Coroutine mainRoutine = null;

        public bool Expanded { get; private set; } = false;

        protected void Awake()
        {
            //MainAnimator.SpriteRenderer.enabled = false;
            MainAnimator.PlayAnimation(idleAnimation);
            MainCollider.enabled = false;

            if (testFunctionality)
            {
                StartCoroutine(Test());
            }
        }

        IEnumerator Test()
        {
            while (true)
            {
                yield return new WaitUntil(ExpandAndWait(0f, -1));
                yield return new WaitForSeconds(0.5f);
                yield return new WaitUntil(RetractAndWait(0f));
                yield return new WaitForSeconds(0.5f);
            }
        }

        public void Expand(float anticDuration) => ExpandAndWait(0f, anticDuration);
        public void Retract() => RetractAndWait(0f);

        public Func<bool> ExpandAndWait(float delay, float anticDuration, float audioMultiplier = 1f)
        {
            //WeaverLog.Log("EXPANDING");
            if (anticDuration < 0)
            {
                anticDuration = 2f;
            }
            if (mainRoutine != null)
            {
                StopCoroutine(mainRoutine);
                mainRoutine = null;
            }

            Expanded = true;

            mainRoutine = StartCoroutine(ExpandRoutine(delay, anticDuration, audioMultiplier));
            var id = mainRoutine;

            return () => mainRoutine != id;
        }

        public Func<bool> RetractAndWait(float delay, float audioMultiplier = 1f)
        {
            //WeaverLog.Log("RETRACTING");
            if (mainRoutine != null)
            {
                StopCoroutine(mainRoutine);
                mainRoutine = null;
            }

            mainRoutine = StartCoroutine(RetractRoutine(delay, audioMultiplier));
            var id = mainRoutine;

            return () => mainRoutine != id;
        }

        IEnumerator ExpandRoutine(float delay, float anticDuration, float audioMultiplier)
        {
            WeaverLog.Log("BEGINNING EXPAND DELAY = " + delay);
            if (delay > 0)
            {
                yield return new WaitForSeconds(delay);
            }
            WeaverLog.Log("EXPANDING");
            Expanded = false;
            MainAnimator.SpriteRenderer.enabled = true;
            DustParticles.Play();

            if (audioMultiplier > 0 && anticSounds.Count > 0 && anticDuration > 0f)
            {
                var instance = WeaverAudio.PlayAtPoint(anticSounds.GetRandomElement(), transform.position, AnticSoundsVolume * audioMultiplier);
                instance.AudioSource.pitch = anticSoundsPitchRange.RandomInRange();
            }

            MainAnimator.PlayAnimation(anticAnimation);
            if (anticDuration > 0)
            {
                yield return new WaitForSeconds(anticDuration);
            }

            if (audioMultiplier > 0 && expandSounds.Count > 0)
            {
                var instance = WeaverAudio.PlayAtPoint(expandSounds.GetRandomElement(), transform.position, ExpandSoundsVolume * audioMultiplier);
                instance.AudioSource.pitch = expandSoundsPitchRange.RandomInRange();
            }

            MainCollider.enabled = true;
            DustParticles.Stop();

            yield return MainAnimator.PlayAnimationTillDone(expandAnimation);
            mainRoutine = null;
            yield break;
        }

        IEnumerator RetractRoutine(float delay, float audioMultiplier)
        {
            if (delay > 0)
            {
                yield return new WaitForSeconds(delay);
            }
            MainCollider.enabled = false;
            if (audioMultiplier > 0f && retractSounds.Count > 0)
            {
                var instance = WeaverAudio.PlayAtPoint(retractSounds.GetRandomElement(), transform.position, RetractSoundsVolume * audioMultiplier);
                instance.AudioSource.pitch = retractSoundsPitchRange.RandomInRange();
            }
            yield return MainAnimator.PlayAnimationTillDone(retractAnimation);
            MainAnimator.PlayAnimation(idleAnimation);
            //MainAnimator.SpriteRenderer.enabled = false;
            mainRoutine = null;
            yield break;
        }

        
    }
}