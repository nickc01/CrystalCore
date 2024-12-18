using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using WeaverCore.Utilities;
using TMPro;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WeaverCore.Components.Colosseum
{
    public class ColosseumPlatform : MonoBehaviour, IColosseumIdentifier
    {
        [SerializeField, Tooltip("Enable testing mode to repeatedly expand and retract the platform.")]
        bool testing = false;

        [SerializeField, Tooltip("Indicates whether the platform starts in an activated (expanded) state.")]
        bool startActivated = false;

        [SerializeField, Tooltip("The primary collider for the platform.")]
        Collider2D _mainCollider;

        [SerializeField, Tooltip("Animation curve used for expanding the platform.")]
        private AnimationCurve expandCurve = new AnimationCurve(
            new Keyframe(0, 0, 0, 2),
            new Keyframe(1, 1, 0, 0)
        );

        [SerializeField, Tooltip("Duration for the platform expansion.")]
        float expandTime = 0.6f;

        [SerializeField, Tooltip("List of audio clips to play during platform expansion.")]
        List<AudioClip> expandSounds;

        [SerializeField, Tooltip("Animation curve used for retracting the platform.")]
        AnimationCurve retractCurve = new AnimationCurve(
            new Keyframe(0, 0, 2, 0),
            new Keyframe(1, 1, 2, 2)
        );

        [SerializeField, Tooltip("Duration for the platform retraction.")]
        float retractTime = 0.6f;

        [SerializeField, Tooltip("Default delay before retracting the platform.")]
        float defaultRetractAnticDelay = 1.5f;

        [SerializeField, Tooltip("Random jitter applied to the platform during the retraction anticipation phase.")]
        Vector2 retractAnticJitter = new Vector2(0.1f, 0.1f);

        [SerializeField, Tooltip("List of audio clips to play during platform retraction.")]
        List<AudioClip> retractSounds;

        [NonSerialized]
        float _startTime = -1;

        //[SerializeField] private bool resetCurves = false;

        [NonSerialized]
        ColosseumPlatformController platform;


        const float INSTANT_TIME = 0.5f;

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

        string IColosseumIdentifier.Identifier => "Platforms";

        Color IColosseumIdentifier.Color => Color.gray;

        bool IColosseumIdentifier.ShowShortcut => true;

        private void Awake()
        {
            if (_startTime < 0)
            {
                _startTime = Time.time;
            }
            /*_editor_text = GetComponentInChildren<TextMeshPro>();
            if (_editor_text != null)
            {
                _editor_text.gameObject.SetActive(false);
            }*/
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

            if (startActivated)
            {
                Expand();
            }
        }

        /*private void SetDefaultCurves()
        {
            // Initialize expand curve
            expandCurve = new AnimationCurve();
            expandCurve.AddKey(new Keyframe(0.0f, 0.0f, 1.0f, 1.0f)); // Linear start
            expandCurve.AddKey(new Keyframe(0.95f, 1.05f, 0.0f, 0.0f)); // Overshoot at 0.95
            expandCurve.AddKey(new Keyframe(1.0f, 1.0f, -1.0f, -1.0f)); // Back to 1.0

            // Initialize retract curve
            retractCurve = new AnimationCurve();
            retractCurve.AddKey(new Keyframe(0.0f, 0.0f, 1.0f, 1.0f)); // Start at 0.0
            retractCurve.AddKey(new Keyframe(0.05f, -0.05f, 0.0f, 0.0f)); // Dip below at 0.05
            retractCurve.AddKey(new Keyframe(1.0f, 1.0f, 1.0f, 1.0f)); // Linearly return to 1.0

    #if UNITY_EDITOR
            // Force linear tangents
            for (int i = 0; i < expandCurve.keys.Length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(expandCurve, i, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(expandCurve, i, AnimationUtility.TangentMode.Linear);
            }

            for (int i = 0; i < retractCurve.keys.Length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(retractCurve, i, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(retractCurve, i, AnimationUtility.TangentMode.Linear);
            }
    #endif
        }

        private void OnValidate()
        {
            if (resetCurves)
            {
                Debug.Log("Resetting animation curves to default values.");

                SetDefaultCurves();
                resetCurves = false; // Reset the toggle
            }
        }*/

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
            gameObject.SetActive(true);
            if (!Changing)
            {
                Changing = true;
                if (_startTime < 0)
                {
                    _startTime = Time.time;
                }
                StartCoroutine(ExpandRoutine(delay));
            }
        }

        public void RetractWithDelay(float delay) => RetractSlowWithDelay(delay, 0f);

        public void RetractSlowWithDelay(float delay, float anticDelay)
        {
            gameObject.SetActive(true);
            if (!Changing)
            {
                Changing = true;
                if (_startTime < 0)
                {
                    _startTime = Time.time;
                }
                StartCoroutine(RetractRoutine(delay, anticDelay));
            }
        }

        IEnumerator ExpandRoutine(float delay)
        {
            if (platform == null)
            {
                yield return null;
            }

            if (Time.time <= _startTime + INSTANT_TIME)
            {
                MainCollider.enabled = true;
                var frames = platform.Animator.AnimationData.GetClip("Expand").Frames;
                var lastFrame = frames[frames.Count - 1];
                platform.Animator.SpriteRenderer.sprite = lastFrame;
                platform.transform.localPosition = platform.PlatformEndPos;
                Changing = false;
                Expanded = true;
                platform.gameObject.SetActive(true);
                WeaverLog.Log("EXPANDED QUICK");
                yield break;
            }

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
            Expanded = true;
        }

        IEnumerator RetractRoutine(float delay, float anticDelay)
        {
            if (platform == null)
            {
                yield return null;
            }
            
            if (Time.time <= _startTime + INSTANT_TIME)
            {
                MainCollider.enabled = false;
                var frames = platform.Animator.AnimationData.GetClip("Retract").Frames;
                var lastFrame = frames[frames.Count - 1];
                platform.Animator.SpriteRenderer.sprite = lastFrame;
                platform.transform.localPosition = platform.PlatformStartPos;
                Changing = false;
                Expanded = false;
                platform.gameObject.SetActive(false);
                yield break;
            }

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
            Expanded = false;
        }

        IEnumerator TweenObject(Transform obj, Vector3 from, Vector3 to, AnimationCurve curve, float time)
        {
            for (float t = 0; t < time; t += Time.deltaTime)
            {
                obj.localPosition = Vector3.LerpUnclamped(from, to, curve.Evaluate(t / time));
                yield return null;
            }
            obj.localPosition = to;
            yield break;
        }

        /*private void OnValidate() 
        {
            if (PlatformColor == default)
            {
                PlatformColor = Color.HSVToRGB(UnityEngine.Random.Range(0, 1f), 0.5f, 1f);
            }

            if (_editor_text == null)
            {
                _editor_text = GetComponentInChildren<TextMeshPro>();
            }

            if (_editor_text != null)
            {
                _editor_text.color = PlatformColor;
                _editor_text.text = gameObject.name;
            }
        }*/

        /*#if UNITY_EDITOR

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

        #endif*/
    }
}