using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using WeaverCore.Utilities;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WeaverCore.Components
{
    public class ColosseumPlatform : MonoBehaviour
    {
        [SerializeField]
        bool testing = false;

        public Color PlatformColor = default;

        [NonSerialized]
        ColosseumPlatformController platform;

        [SerializeField]
        Collider2D _mainCollider;

        [SerializeField]
        private AnimationCurve expandCurve = new AnimationCurve(
            new Keyframe(0, 0, 0, 2),
            new Keyframe(1, 1, 0, 0)
        );

        [SerializeField]
        float expandTime = 0.6f;

        [SerializeField]
        List<AudioClip> expandSounds;

        [SerializeField]
        AnimationCurve retractCurve = new AnimationCurve(
            new Keyframe(0, 0, 2, 0),
            new Keyframe(1, 1, 2, 2)
        );

        [SerializeField]
        float retractTime = 0.6f;

        [SerializeField]
        float defaultRetractAnticDelay = 1.5f;

        [SerializeField]
        Vector2 retractAnticJitter = new Vector2(0.1f, 0.1f);

        [SerializeField]
        List<AudioClip> retractSounds;

        public Collider2D MainCollider
        {
            get
            {
                if (_mainCollider == null)
                {
                    _mainCollider = GetComponentInChildren<Collider2D>();
                }
                return _mainCollider;
            }
        }

        public bool Changing { get; private set; } = false;
        public bool Expanded { get; private set; } = false;

        private void Awake()
        {
            platform = transform.Find("Platform").GetComponent<ColosseumPlatformController>();
            if (MainCollider != null)
            {
                MainCollider.enabled = false;
            }

            if (TryGetComponent<SpriteRenderer>(out var renderer))
            {
                renderer.enabled = false;
            }

            platform.gameObject.SetActive(false);

            if (testing)
            {
                StartCoroutine(TestingRoutine());
            }
        }

        IEnumerator TestingRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(4f);
                Expand();
                yield return new WaitForSeconds(4f);
                Retract();
            }
        }

        public void Expand() => ExpandWithDelay(0f);
        public void Retract() => RetractWithDelay(0f);
        public void ExpandWithRand() => ExpandWithDelay(UnityEngine.Random.value);
        public void RetractWithRand() => RetractWithDelay(UnityEngine.Random.value);

        public void RetractSlow() => RetractSlowWithDelay(0f, defaultRetractAnticDelay);
        public void RetractSlowWithRand() => RetractSlowWithDelay(UnityEngine.Random.value, defaultRetractAnticDelay);

        public void ExpandWithDelay(float delay)
        {
            if (!Changing)
            {
                Changing = true;
                StartCoroutine(ExpandRoutine(delay));
            }
        }

        public void RetractWithDelay(float delay) => RetractSlowWithDelay(delay, 0f);

        public void RetractSlowWithDelay(float delay, float anticDelay)
        {
            if (!Changing)
            {
                Changing = true;
                StartCoroutine(RetractRoutine(delay, anticDelay));
            }
        }

        IEnumerator ExpandRoutine(float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            platform.gameObject.SetActive(true);
            platform.transform.localPosition = platform.PlatformStartPos;
            platform.Animator.PlayAnimation("Retracted");

            ColosseumAudio.TriggerAudio(expandSounds.GetRandomElement(), 0f);

            yield return TweenObject(platform.transform, platform.PlatformStartPos, platform.PlatformEndPos, expandCurve, expandTime);

            platform.transform.localPosition = platform.PlatformActivePos;
            platform.Animator.PlayAnimation("Expand");
            MainCollider.enabled = true;
            Changing = false;
        }

        IEnumerator RetractRoutine(float delay, float anticDelay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            if (anticDelay > 0f)
            {
                var origPos = platform.transform.localPosition;
                for (float t = 0; t < anticDelay; t += Time.deltaTime)
                {
                    platform.transform.localPosition = origPos + new Vector3(UnityEngine.Random.Range(-retractAnticJitter.x, retractAnticJitter.x), UnityEngine.Random.Range(-retractAnticJitter.y, retractAnticJitter.y));
                    yield return null;
                }
                platform.transform.localPosition = origPos;
            }

            MainCollider.enabled = false;
            platform.transform.localPosition = platform.PlatformEndPos;
            ColosseumAudio.TriggerAudio(retractSounds.GetRandomElement(), 0f);
            yield return platform.Animator.PlayAnimationTillDone("Retract");
            yield return TweenObject(platform.transform, platform.PlatformEndPos, platform.PlatformStartPos, retractCurve, retractTime);
            platform.gameObject.SetActive(false);
            Changing = false;
        }

        IEnumerator TweenObject(Transform obj, Vector3 from, Vector3 to, AnimationCurve curve, float time)
        {
            for (float t = 0; t < time; t += Time.deltaTime)
            {
                obj.localPosition = Vector3.Lerp(from, to, curve.Evaluate(t / time));
                yield return null;
            }
            yield break;
        }

        private void OnValidate() 
        {
            if (PlatformColor == default)
            {
                PlatformColor = Color.HSVToRGB(UnityEngine.Random.Range(0, 1f), 0.5f, 1f);
            }
        }

        #if UNITY_EDITOR

        private void OnDrawGizmos()
        {
            // Set the label color
            Handles.color = PlatformColor;

            // Center the label on the object
            Vector3 labelPosition = transform.position;

            // Draw the label in the Scene view
            Handles.Label(labelPosition - new Vector3(1f, 0.65f), gameObject.name, new GUIStyle
            {
                alignment = TextAnchor.MiddleCenter,   // Center the text
                fontStyle = FontStyle.Bold,            // Make it bold (optional)
                normal = new GUIStyleState { textColor = PlatformColor }  // Set color
            });
        }

        #endif
    }
}