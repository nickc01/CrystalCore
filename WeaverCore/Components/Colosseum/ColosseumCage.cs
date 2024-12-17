using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WeaverCore.Components.DeathEffects;
using WeaverCore.Enums;
using WeaverCore.Utilities;

namespace WeaverCore.Components.Colosseum
{
    public class ColosseumCage : MonoBehaviour
    {
        [NonSerialized]
        ColosseumRoomManager _roomManager = null;
        public ColosseumRoomManager RoomManager => _roomManager ??= GetComponentInParent<ColosseumRoomManager>();

        public enum CageType
        {
            Small,
            Large
        }

        protected static CachedPrefab<ColosseumCage> _smallPrefab = new CachedPrefab<ColosseumCage>();

        public static ColosseumCage SmallPrefab
        {
            get
            {
                if (_smallPrefab.Value == null)
                {
                    _smallPrefab.Value = WeaverAssets.LoadWeaverAsset<GameObject>("Colosseum Cage Small").GetComponent<ColosseumCage>();
                }
                return _smallPrefab.Value;
            }
        }

        protected static CachedPrefab<ColosseumCage> _largePrefab = new CachedPrefab<ColosseumCage>();

        public static ColosseumCage LargePrefab
        {
            get
            {
                if (_largePrefab.Value == null)
                {
                    _largePrefab.Value = WeaverAssets.LoadWeaverAsset<GameObject>("Colosseum Cage Large").GetComponent<ColosseumCage>();
                }
                return _largePrefab.Value;
            }
        }

        public static ColosseumCage GetDefaultPrefab(CageType cageType) => cageType == CageType.Small ? SmallPrefab : LargePrefab;

        [SerializeField, Tooltip("The SpriteRenderer used to display a preview image.")]
        protected SpriteRenderer previewImage;

        [field: SerializeField, Tooltip("The entity prefab to spawn when the cage activates.")]
        public GameObject EntityToSpawn { get; set; }

        [field: SerializeField, Tooltip("Offset position for spawning the entity.")]
        public Vector3 EntitySpawnOffset { get; private set; } = default;

        [SerializeField, Tooltip("The delay before the entity spawns.")]
        protected float spawnDelay = 0f;

        [SerializeField, Tooltip("Animator for the cage animations.")]
        protected Animator anim;

        [SerializeField, Tooltip("GameObject to represent a strike effect.")]
        protected GameObject strike;

        [SerializeField, Tooltip("The time to wait for the cage animation to finish.")]
        protected float animationWaitTime = 1.25f;

        [SerializeField, Tooltip("Should the entity's geo (drops) be reset when spawned?")]
        protected bool resetEntityGeo = true;

        [SerializeField, Tooltip("Determines the behavior after the cage finishes its task.")]
        protected OnDoneBehaviour onDone = OnDoneBehaviour.DestroyOrPool;

        [SerializeField, Tooltip("Enable to test the cage spawning in debug mode.")]
        bool debugTesting = false;

        [Header("Audio"), Space]
        [SerializeField, Tooltip("Audio clip played when the cage appears.")]
        protected AudioClip appearAudioClip;

        [SerializeField, Tooltip("Delay before playing the appear audio.")]
        protected float appearAudioDelay = 0.6f;

        [SerializeField, Tooltip("Audio clip played when the cage opens.")]
        protected AudioClip openAudioClip;

        [SerializeField, Tooltip("Delay before playing the open audio.")]
        protected float openAudioDelay = 0.4f;

        [SerializeField, Tooltip("Audio clip played when the cage disappears.")]
        protected AudioClip disappearAudioClip;

        [SerializeField, Tooltip("Delay before playing the disappear audio.")]
        protected float disappearAudioDelay = 0.6f;

        Coroutine summonRoutine;

        protected virtual void Awake()
        {
            if (previewImage != null)
            {
                previewImage.enabled = false;
            }

            if (debugTesting)
            {
                DoSummon(null);
            }
        }

        /// <summary>
        /// Begins summoning the entity
        /// </summary>
        /// <param name="onSummon">Called when the entity actually spawns</param>
        /// <returns>Returns true if the summon is activated. Returns false if a summon is already taking place, or no EntityToSpawn has been specified</returns>
        public bool DoSummon(Action<GameObject> onSummon)
        {
            return DoSummon(EntityToSpawn, onSummon);
        }

        /// <summary>
        /// Begins summoning the entity
        /// </summary>
        /// <param name="prefab">The prefab to spawn</param>
        /// <param name="onSummon">Called when the entity actually spawns</param>
        /// <returns>Returns true if the summon is activated. Returns false if a summon is already taking place, or no EntityToSpawn has been specified</returns>
        public bool DoSummon(GameObject prefab, Action<GameObject> onSummon)
        {
            if (prefab == null)
            {
                WeaverLog.LogError($"The Colosseum Cage {name} doesn't have an entity to spawn");
                return false;
            }

            if (summonRoutine != null)
            {
                WeaverLog.LogError($"The Colosseum Cage {name} is already summoning something");
                return false;
            }

            gameObject.SetActive(true);

            StartCoroutine(SummonRoutine(prefab, onSummon));

            return true;
        }

        protected GameObject SpawnObject(GameObject prefab)
        {
            GameObject instance;

            if (prefab.TryGetComponent<PoolableObject>(out var p))
            {
                instance = Pooling.Instantiate(prefab,transform.TransformPoint(EntitySpawnOffset), prefab.transform.rotation);
            }
            else
            {
                instance = GameObject.Instantiate(prefab, transform.TransformPoint(EntitySpawnOffset), prefab.transform.rotation);
            }

            if (resetEntityGeo && HealthUtilities.HasHealthComponent(instance))
            {
                HealthUtilities.SetSmallGeo(instance, 0);
                HealthUtilities.SetMediumGeo(instance, 0);
                HealthUtilities.SetLargeGeo(instance, 0);
            }

            return instance;
        }

        protected virtual IEnumerator SummonRoutine(GameObject prefab, Action<GameObject> onSummon)
        {
            yield return new WaitForSeconds(spawnDelay);
            if (anim != null)
            {
                anim.gameObject.SetActive(true);
            }
            ColosseumAudio.TriggerAudio(new List<AudioClip>{ appearAudioClip }, new List<float> { appearAudioDelay });
            
            yield return new WaitForSeconds(animationWaitTime);

            GameObject instance = SpawnObject(prefab);

            if (strike != null)
            {
                strike.gameObject.SetActive(true);
            }

            ColosseumAudio.TriggerAudio(new List<AudioClip> { openAudioClip, disappearAudioClip }, new List<float> { openAudioDelay, disappearAudioDelay });
            try
            {
                onSummon?.Invoke(instance);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                onDone.DoneWithObject(this, 3f);
            }
        }

        public static ColosseumCage Summon(Vector3 position, GameObject prefabToSpawn, Action<GameObject> onSummon, CageType cageType)
        {
            return Summon(position, prefabToSpawn, onSummon, GetDefaultPrefab(cageType));
        }

        public static ColosseumCage Summon(Vector3 position, GameObject prefabToSpawn, Action<GameObject> onSummon, ColosseumCage cagePrefab)
        {
            ColosseumCage instance;
            if (cagePrefab.TryGetComponent<PoolableObject>(out var pool))
            {
                instance = Pooling.Instantiate(cagePrefab, position, Quaternion.identity);
            }
            else
            {
                instance = GameObject.Instantiate(cagePrefab, position, Quaternion.identity);
            }

            instance.gameObject.SetActive(true);

            instance.DoSummon(prefabToSpawn, onSummon);

            return instance;
        }

        protected virtual void OnDrawGizmosSelected() 
        {
            if (RoomManager != null && RoomManager.SpawnLocationLabels)
            {
                Gizmos.color = new Color(0f,1f,0f, 0.25f);
                Gizmos.DrawCube(transform.TransformPoint(EntitySpawnOffset), new Vector3(0.35f, 0.35f, 0.35f));
            }
        }
    }
}