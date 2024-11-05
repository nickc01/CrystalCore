using Modding;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using WeaverCore.Assets.Components;
using WeaverCore.Attributes;
using WeaverCore.Settings;
using WeaverCore.Utilities;

namespace WeaverCore.Components
{

    /// <summary>
    /// Base class for any items dropped on the floor that the player can collect
    /// </summary>
    public abstract class DroppedItem : MonoBehaviour
    {
        [field: SerializeField]
        public SaveSpecificSettings SettingsStorage { get; set; } = null;

        [field: SerializeField]
        [field: Tooltip("The bool field in Settings Storage that will store whether this item has been collected. Leave this field empty if this item can always be collected.")]
        public string SettingsField { get; set; } = "";

        [field: SerializeField]
        [field: Tooltip("If set to false, then this item will not spawn in the world")]
        public bool ItemActive { get; set; } = true;

        [field: SerializeField]
        [field: Tooltip("If set to true, the item will start by flinging itself upwards")]
        public bool FlingOnStart { get; set; } = true;

        [field: SerializeField]
        [field: Tooltip("The speed range the object will be flung at")]
        public Vector2 FlingSpeedMinMax = new Vector2(20, 20);

        [field: SerializeField]
        [field: Tooltip("The angle range the object will be flung at")]
        public Vector2 FlingAngleMinMax = new Vector2(82, 86);

        [field: SerializeField]
        public UnityEvent OnPickup;

        [NonSerialized]
        InspectRegion _inspectionRegion;

        public InspectRegion InspectionRegion => _inspectionRegion ??= GetComponentInChildren<InspectRegion>();

        [NonSerialized]
        Rigidbody2D rb;

        public Rigidbody2D RB => rb ??= GetComponent<Rigidbody2D>();

        protected virtual void Awake()
        {
            if (InspectionRegion == null)
            {
                throw new Exception("A dropped item requires an inspect region");
            }
            InspectionRegion.Inspectable = false;

            InspectionRegion.OnInspect.AddListener(GiveItem);

            StartCoroutine(DroppedItemRoutine());
        }

        IEnumerator DroppedItemRoutine()
        {
            if (SettingsStorage != null)
            {
                //WeaverLog.Log("OLD SETTINGS DUMP = " + JsonUtility.ToJson(SettingsStorage, true));
                var result = SaveSpecificSettings.GetSaveSettings(SettingsStorage.GetType());
                if (result != null)
                {
                    SettingsStorage = result;
                    //WeaverLog.Log("NEW SETTINGS DUMP = " + JsonUtility.ToJson(result, true));
                }
            }
            transform.rotation = Quaternion.identity;

            //WeaverLog.Log("CAN SPAWN = " + CanSpawn());

            if (!CanSpawn())
            {
                Destroy(gameObject);
                yield break;
            }

            OnActive();

            if (FlingOnStart)
            {
                var particleTrail = GetComponentInChildren<ParticleSystem>();
                particleTrail.Play();
                RB.gravityScale = 0.85f;

                float speed = FlingSpeedMinMax.RandomInRange();
                float angle = FlingAngleMinMax.RandomInRange();
                Vector2 velocity = new Vector2(speed * Mathf.Cos(angle * Mathf.Deg2Rad), speed * Mathf.Sin(angle * Mathf.Deg2Rad));
                RB.velocity = velocity;

                float idleCounter = 0f;

                while (true)
                {
                    if (RB.velocity.y == 0f)
                    {
                        idleCounter += Time.deltaTime;
                    }
                    if (idleCounter >= 0.25f)
                    {
                        break;
                    }
                    yield return null;
                }

                RB.gravityScale = 0f;
                RB.velocity = default;
                particleTrail.Stop();
            }

            if (gameObject.TryGetComponent<ObjectBounce>(out var bouncer))
            {
                bouncer.StopBounce();
            }

            InspectionRegion.Inspectable = true;
        }

        /// <summary>
        /// Gives the item to the player. This is automatically called when the player goes near the item and inspects it
        /// </summary>
        public void GiveItem()
        {
            try
            {
                ItemActive = false;
                GetComponent<SpriteRenderer>().enabled = false;
                if (TryGetComponent<AudioSource>(out var audio))
                {
                    audio.Stop();
                }
                OnGiveItem();
                WeaverLog.Log("ON PICKUP");
                OnPickup?.Invoke();

                if (SettingsStorage.HasField<bool>(SettingsField))
                {
                    SettingsStorage.SetFieldValue(SettingsField, true);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error collecting item {gameObject.name} : {e}");
            }
        }

        /// <summary>
        /// Called when the player picks up an item. Implement your custom item behavior in here
        /// </summary>
        protected abstract void OnGiveItem();

        /// <summary>
        /// Called when the item is spawned
        /// </summary>
        protected virtual void OnActive()
        {

        }

        /// <summary>
        /// Returns whether or not this item can spawn.
        /// </summary>
        /// <returns>Returns true if the item can spawn. If false, then the item will get destroyed</returns>
        protected virtual bool CanSpawn()
        {
            if (ItemActive)
            {
                if (SettingsStorage.HasField<bool>(SettingsField))
                {
                    var result = SettingsStorage.GetFieldValue<bool>(SettingsField);
                    //WeaverLog.Log($"{SettingsField} = {result}");
                    //WeaverLog.Log("CAN SPAWN RESULT = " + !result);
                    return !SettingsStorage.GetFieldValue<bool>(SettingsField);
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return false;
            }
        }
    }
}
