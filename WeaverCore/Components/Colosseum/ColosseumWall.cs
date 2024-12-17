using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using WeaverCore.Utilities;

namespace WeaverCore.Components.Colosseum
{
    public class ColosseumWall : MonoBehaviour, IColosseumIdentifier
    {
        [SerializeField, Tooltip("Starting position of the wall.")]
        Vector3 startPosition;

        [SerializeField, Tooltip("Shake intensity of the wall during movement.")]
        Vector2 shakeIntensity = new Vector2(0.075f, 0.075f);

        [SerializeField, Tooltip("Duration of the initial shake phase.")]
        float beginShakeDuration = 0.3f;

        [SerializeField, Tooltip("Duration of the impact shake phase.")]
        float impactShakeDuration = 0.6f;

        [SerializeField, Tooltip("Speed at which the wall moves.")]
        float movementSpeed = 28f;

        [SerializeField, Tooltip("Audio clip played while the wall is moving.")]
        AudioClip movingSound;

        [SerializeField, Tooltip("Pitch range for the wall's moving sound.")]
        Vector2 movingSoundPitchRange = new Vector2(0.75f, 1.25f);

        [SerializeField, Tooltip("Audio clip played upon impact.")]
        AudioClip impactSound;

        [SerializeField, Tooltip("Pitch range for the impact sound.")]
        Vector2 impactSoundPitchRange = new Vector2(0.75f, 1.25f);

        [SerializeField]
        [Tooltip("If set to true, the wall will continously move in and out")]
        bool debugTesting = false;


        [NonSerialized]
        Rigidbody2D wall;

        [NonSerialized]
        ParticleSystem wholeDust;
        
        //[NonSerialized]
        //Vector3 jitterEndPos = default;

        //[NonSerialized]
        //float distance = default;

        //[NonSerialized]
        //float wallPos = default;

        public Vector2 StoredPosition { get; private set; }

        public bool Moving => currentRoutine != null;

        string IColosseumIdentifier.Identifier => "Walls";

        Color IColosseumIdentifier.Color => Color.blue;

        bool IColosseumIdentifier.ShowShortcut => true;

        Coroutine currentRoutine = null;

        void Awake()
        {
            wall = transform.Find("Wall").GetComponent<Rigidbody2D>();
            wholeDust = wall.transform.Find("Whole Dust").GetComponent<ParticleSystem>();
            wall.transform.localPosition = startPosition;
            StoredPosition = new Vector2(float.NaN, float.NaN);
            if (debugTesting)
            {
                StartCoroutine(DebugRoutine());
            }
        }

        IEnumerator DebugRoutine()
        {
            while (true)
            {
                MoveWallTo(15.4f);
                yield return new WaitForSeconds(2f);
                ResetWall();
                yield return new WaitForSeconds(2f);
            }
        }


        void Reset()
        {
            var wall = transform.Find("Wall");
            if (wall != null)
            {
                startPosition = wall.transform.localPosition;
            }
        }

        public void SetLocalX(float x)
        {
            StoredPosition = StoredPosition.With(x: x);

            if (!Moving)
            {
                MoveWallToLocalPos(new Vector2(float.NaN, float.NaN));
            }
        }

        public void SetLocalY(float y)
        {
            StoredPosition = StoredPosition.With(y: y);

            if (!Moving)
            {
                MoveWallToLocalPos(new Vector2(float.NaN, float.NaN));
            }
        }

        public void MoveWallTo(float localX)
        {
            SetLocalX(localX);
        }

        public void MoveWallToWorldPos(Vector2 worldPosition)
        {
            MoveWallToLocalPos(wall.transform.InverseTransformPoint(worldPosition));
        }

        public void MoveWallToLocalPos(Vector2 localPosition)
        {
            gameObject.SetActive(true);
            if (currentRoutine != null)
            {
                StopCoroutine(currentRoutine);
                currentRoutine = null;
            }
            currentRoutine = StartCoroutine(MoveWallRoutine(localPosition));

            /*wall.transform.localPosition = jitterEndPos;
            wall.velocity = default;
            wall.transform.localPosition = new Vector3(distance, 0f, 0f);
            jitterEndPos = wall.transform.localPosition;
            wholeDust.Play();
            var instance = WeaverAudio.PlayAtPoint(movingSound, transform.position);
            instance.AudioSource.pitch = movingSoundPitchRange.RandomInRange();

            currentRoutine = StartCoroutine(MoveWallRoutine(moveRelativePosition));*/
        }

        IEnumerator MoveWallRoutine(Vector3 localPosition)
        {
            yield return Shake(shakeIntensity, beginShakeDuration);

            if (float.IsNaN(localPosition.x))
            {
                localPosition.x = StoredPosition.x;

                if (float.IsNaN(localPosition.x))
                {
                    localPosition.x = wall.transform.localPosition.x;
                }
            }

            if (float.IsNaN(localPosition.y))
            {
                localPosition.y = StoredPosition.y;

                if (float.IsNaN(localPosition.y))
                {
                    localPosition.y = wall.transform.localPosition.y;
                }
            }

            StoredPosition = new Vector2(float.NaN, float.NaN);

            WeaverLog.Log($"{wall.transform.parent.name} WALL BEGIN LOCAL POSITION = " + wall.transform.localPosition);
            WeaverLog.Log($"{wall.transform.parent.name} MOVING TO LOCAL POSITION = " + localPosition);

            {
                var instance = WeaverAudio.PlayAtPoint(movingSound, transform.position);
                instance.AudioSource.pitch = movingSoundPitchRange.RandomInRange();
            }

            //yield return VelocityMoveInDirection((localPosition - wall.transform.localPosition).normalized * movementSpeed, Vector2.Distance(localPosition, wall.transform.localPosition));
            yield return VelocityMoveInDirection(wall.transform.localPosition, localPosition, movementSpeed);

            wholeDust.Play();

            {
                var instance = WeaverAudio.PlayAtPoint(impactSound, transform.position);
                instance.AudioSource.pitch = impactSoundPitchRange.RandomInRange();
            }


            yield return Shake(shakeIntensity, impactShakeDuration);

            /*if (wallPos < distance)
            {
                //OUT
            }
            else if (wallPos > distance)
            {
                //IN
            }*/

            if (currentRoutine != null)
            {
                StopCoroutine(currentRoutine);
                currentRoutine = null;
            }
        }

        public void ResetWall()
        {
            MoveWallToLocalPos(startPosition);
        }

        IEnumerator Shake(Vector2 intensity, float time)
        {
            var start = wall.transform.localPosition;
            for (float t = 0; t < time; t += Time.deltaTime)
            {
                wall.transform.localPosition = start + new Vector3(intensity.x * UnityEngine.Random.value, intensity.y * UnityEngine.Random.value);
                yield return null;
            }

            wall.transform.localPosition = start;
        }

        IEnumerator VelocityMoveInDirection(Vector2 startPos, Vector2 endPos, float speed)
        {
            WeaverLog.Log($"{wall.transform.parent.name} START POS = " + startPos);
            WeaverLog.Log($"{wall.transform.parent.name} END POS = " + endPos);
            wall.transform.localPosition = startPos;
            var startWorld = wall.transform.position;

            wall.transform.localPosition = endPos;
            var endWorld = wall.transform.position;

            wall.transform.localPosition = startPos;

            wall.velocity = (endWorld - startWorld).normalized * speed;

            var maxDistance = Vector2.Distance(startPos, endPos);

            while (Vector2.Distance(startPos, wall.transform.localPosition) < maxDistance)
            {
                yield return null;
            }

            wall.velocity = default;
            wall.transform.localPosition = endPos;

            /*wall.velocity = velocity;
            var startPos = wall.transform.localPosition;



            while (Vector2.Distance(startPos, wall.transform.localPosition) < maxDistance)
            {
                yield return null;
            }
            wall.velocity = default;
            wall.transform.localPosition = startPos + (Vector3)(velocity.normalized * maxDistance);*/
        }

    }
}
