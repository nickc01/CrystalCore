using Modding;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using WeaverCore.Assets.Components;
using WeaverCore.Utilities;

namespace WeaverCore.Components
{
    /// <summary>
    /// Used for creating regions the player can inspect. Useful for picking up items or talking to NPCs.
    /// </summary>
    public abstract class InspectRegion : MonoBehaviour
    {
        public enum HeroSidePositions
        {
            Left,
            Right,
            Both
        }
        /// <summary>
        /// The prefab for the inspection prompt.
        /// </summary>
        [SerializeField]
        [Tooltip("Prefab for the inspection prompt.")]
        public WeaverArrowPrompt promptPrefab;

        private WeaverArrowPrompt prompt;

        private Transform promptMarker;

        /// <summary>
        /// Is set to true if the player is currently within range of inspecting.
        /// </summary>
        public bool PlayerInRange => isActiveAndEnabled && triggerTracker.InsideCount > 0;

        /// <summary>
        /// Is set to true if the player is currently inspecting.
        /// </summary>
        public bool PlayerInspecting { get; private set; } = false;

        [field: SerializeField]
        [field: Tooltip("Whether or not this inspect region can actually be inspected.")]
        public bool Inspectable { get; set; } = true;

        [SerializeField]
        [Tooltip("The name of the event that is triggered when the player is out of range. Leave empty if no event should be fired.")]
        string exitRangeEvent = "";

        [SerializeField]
        [Tooltip("The name of the event that is triggered when the player is in range. Leave empty if no event should be fired.")]
        string enterRangeEvent = "";

        [SerializeField]
        [Tooltip("Is the player able to pick up the item from the left, the right, or both?")]
        HeroSidePositions sidePositions = HeroSidePositions.Both;

        [SerializeField]
        float moveToOffset = 0.25f;

        [SerializeField]
        protected bool heroLooksUp = false;

        [field: SerializeField]
        public bool EnableKnightDamageInterrupt { get; set; } = true;

        /// <summary>
        /// Is set to true if the region has been fully inspected.
        /// </summary>
        public bool FullyInspected { get; protected set; } = false;

        /// <summary>
        /// Event triggered when the region is inspected.
        /// </summary>
        public UnityEvent OnInspect;

        /*/// <summary>
        /// Event triggered when the region is inspected. Subscribers return a Ienumerator routine. And the inspector will not continue until the Ienumerator finishes
        /// </summary>
        public event Func<IEnumerator> OnInspectWaitEvent;*/

        /// <summary>
        /// Event triggered after the Knight takes damage.
        /// </summary>
        public event Modding.Delegates.AfterTakeDamageHandler OnKnightDamaged;

        private TrackTriggerObjects triggerTracker = null;

        protected virtual void Awake()
        {
            triggerTracker = gameObject.GetComponent<TrackTriggerObjects>();

            if (triggerTracker == null)
            {
                triggerTracker = gameObject.AddComponent<TrackTriggerObjects>();
            }
            promptMarker = transform.Find("Prompt Marker");
            gameObject.layer = LayerMask.NameToLayer("Hero Detector");
        }

        private int KnightTookDamage(int hazardType, int damageAmount)
        {
            if (OnKnightDamaged != null)
            {
                damageAmount = OnKnightDamaged.Invoke(hazardType, damageAmount);
            }
            if (PlayerInspecting && EnableKnightDamageInterrupt)
            {
                PlayerInspecting = false;
                StopAllCoroutines();
                StartCoroutine(MainRoutine());
            }
            return damageAmount;
        }

        protected virtual void Reset()
        {
            promptPrefab = WeaverArrowPrompt.DefaultPrefab;
        }

        protected virtual void OnEnable()
        {
            StartCoroutine(InitRoutine());
            ModHooks.AfterTakeDamageHook += KnightTookDamage;
        }

        protected virtual void OnDisable()
        {
            StopAllCoroutines();
            ModHooks.AfterTakeDamageHook -= KnightTookDamage;
        }

        protected virtual void OnDestroy()
        {
            StopAllCoroutines();
            ModHooks.AfterTakeDamageHook -= KnightTookDamage;
        }


        IEnumerator InitRoutine()
        {
            prompt = WeaverArrowPrompt.Spawn(promptPrefab ?? WeaverArrowPrompt.DefaultPrefab, gameObject, promptMarker.position);
            prompt.DestroyOnHide = false;
            prompt.HideInstant();
            prompt.SetLabelTextLang("INSPECT");

            StartCoroutine(MainRoutine());
            yield break;
        }

        IEnumerator MainRoutine()
        {
            yield return new WaitUntil(() => Inspectable);
            prompt.transform.position = promptMarker.position;

            while (true)
            {
                prompt.Hide();

                if (!string.IsNullOrEmpty(exitRangeEvent))
                {
                    EventManager.BroadcastEvent(exitRangeEvent, gameObject);
                }

                yield return new WaitUntil(() => PlayerInRange && Inspectable && !FullyInspected);

                if (!string.IsNullOrEmpty(enterRangeEvent))
                {
                    EventManager.BroadcastEvent(enterRangeEvent, gameObject);
                }

                Coroutine setTargetRoutine = StartCoroutine(SetToPosition());

                try
                {
                    prompt.Show();

                    while (true)
                    {
                        if (HeroController.instance.CanInspect() && (PlayerInput.down.WasPressed || PlayerInput.up.WasPressed))
                        {
                            PlayerInspecting = true;
                            yield return InspectRoutine();
                            PlayerInspecting = false;
                            break;
                        }
                        else if (!PlayerInRange || !Inspectable)
                        {
                            break;
                        }
                        yield return null;
                    }
                }
                finally
                {
                    StopCoroutine(setTargetRoutine);
                }

            }
        }

        IEnumerator SetToPosition()
        {
            while (true)
            {
                if (prompt != null && promptMarker != null)
                {
                    prompt.transform.position = promptMarker.transform.position;
                }
                //obj.transform.position = target.transform.position;
                yield return null;
            }
        }

        IEnumerator InspectRoutine()
        {
            PlayerData.instance.SetBool("disablePause", true);
            HeroController.instance.RelinquishControl();
            HeroController.instance.StopAnimationControl();
            prompt.Hide();

            var selfX = transform.position.x;
            var playerX = Player.Player1.transform.position.x;

            float leftProx = selfX - moveToOffset;
            float rightProx = selfX + moveToOffset;

            bool forceTurnLeft = false;
            bool forceTurnRight = false;

            var heroRB = HeroController.instance.GetComponent<Rigidbody2D>();

            bool canGoLeft = true;
            bool canGoRight = true;
            
            if (sidePositions == HeroSidePositions.Left)
            {
                canGoRight = false;
            }
            else if (sidePositions == HeroSidePositions.Right)
            {
                canGoLeft = false;
            }
            else
            {
                canGoRight = playerX >= selfX && playerX < rightProx;
                canGoLeft = playerX >= leftProx && playerX < selfX;
            }

            if (canGoRight)
            {
                //MOVE RIGHT
                Player.Player1.transform.SetScaleX(-1f);
                HeroController.instance.FaceRight();
                HeroUtilities.PlayPlayerClip("Walk");
                heroRB.velocity = new Vector2(6f, 0f);
                for (float t = 0; t < 1; t += Time.deltaTime)
                {
                    if (Player.Player1.transform.position.x >= rightProx)
                    {
                        break;
                    }
                    yield return null;
                }
            }
            else if (canGoLeft)
            {
                //MOVE LEFT
                Player.Player1.transform.SetScaleX(1f);
                HeroController.instance.FaceLeft();
                HeroUtilities.PlayPlayerClip("Walk");
                heroRB.velocity = new Vector2(-6f, 0f);
                for (float t = 0; t < 1; t += Time.deltaTime)
                {
                    if (Player.Player1.transform.position.x <= leftProx)
                    {
                        break;
                    }
                    yield return null;
                }
            }

            var playerScaleX = Player.Player1.transform.localScale.x;

            var playerFacingRight = playerScaleX < 0;
            var playerOnRight = playerX >= selfX;

            if (forceTurnLeft || (playerFacingRight && playerOnRight))
            {
                //TURN HERO LEFT
                heroRB.velocity = default;
                HeroController.instance.FaceLeft();
                Player.Player1.transform.SetScaleX(1f);
                yield return HeroUtilities.PlayPlayerClipTillDone("Turn");
            }
            else if (forceTurnRight || (!playerFacingRight && !playerOnRight))
            {
                //TURN HERO RIGHT
                heroRB.velocity = default;
                HeroController.instance.FaceRight();
                Player.Player1.transform.SetScaleX(-1f);
                yield return HeroUtilities.PlayPlayerClipTillDone("Turn");
            }

            HeroUtilities.PlayPlayerClip("Idle");

            if (heroLooksUp)
            {
                HeroUtilities.PlayPlayerClip("LookUp");
            }

            HashSet<Guid> runningRoutines = new HashSet<Guid>();

            void RunRoutine(IEnumerator routine)
            {

                var id = Guid.NewGuid();

                runningRoutines.Add(id);
                IEnumerator Runner(IEnumerator routine)
                {
                    try
                    {
                        yield return routine;
                    }
                    finally
                    {
                        runningRoutines.Remove(id);
                    }
                }

                StartCoroutine(Runner(routine));
            }

            RunRoutine(OnInspectRoutine());

            /*if (OnInspectWaitEvent != null)
            {
                foreach (var d in OnInspectWaitEvent.GetInvocationList())
                {
                    var routine = ((Func<IEnumerator>)d).Invoke();

                    if (routine != null)
                    {
                        RunRoutine(routine);
                    }
                }
            }*/

            yield return new WaitUntil(() => runningRoutines.Count == 0);

            HeroController.instance.RegainControl();
            PlayerData.instance.SetBool("disablePause", false);
            HeroController.instance.StartAnimationControl();
            yield return new WaitForSeconds(1f);
        }

        /// <summary>
        /// Called to play the default item pickup animation and trigger the OnInspect UnityEvent. You can override this to provide your own custom animations
        /// </summary>
        protected abstract IEnumerator OnInspectRoutine();

        /*protected virtual void OnTriggerEnter2D(Collider2D collision)
        {
            PlayerInRange = true;
        }

        protected virtual void OnTriggerExit2D(Collider2D collision)
        {
            PlayerInRange = false;
        }*/
    }
}
