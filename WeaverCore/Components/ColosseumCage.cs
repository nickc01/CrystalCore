using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using WeaverCore.Attributes;
using WeaverCore.Components.DeathEffects;
using WeaverCore.Enums;
using WeaverCore.Utilities;

namespace WeaverCore.Components
{
    public class ColosseumCage : MonoBehaviour
    {
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

        protected static Dictionary<string, UnboundCoroutine> audioRoutines = new Dictionary<string, UnboundCoroutine>();

        [SerializeField]
        protected SpriteRenderer previewImage;

        [field: SerializeField]
        public GameObject EntityToSpawn { get; set; }

        [field: SerializeField]
        public Vector3 EntitySpawnOffset { get; private set; } = default;

        [SerializeField]
        protected float spawnDelay = 0f;

        [SerializeField]
        protected Animator anim;

        [SerializeField]
        protected GameObject strike;

        [SerializeField]
        protected float animationWaitTime = 1.25f;

        [SerializeField]
        protected bool resetEntityGeo = true;

        [SerializeField]
        protected OnDoneBehaviour onDone = OnDoneBehaviour.DestroyOrPool;

        [SerializeField]
        bool debugTesting = false;

        [Header("Audio")]
        [Space]
        [SerializeField]
        protected AudioClip appearAudioClip;

        [SerializeField]
        protected float appearAudioDelay = 0.6f;

        [SerializeField]
        protected AudioClip openAudioClip;

        [SerializeField]
        protected float openAudioDelay = 0.4f;

        [SerializeField]
        protected AudioClip disappearAudioClip;

        [SerializeField]
        protected float disappearAudioDelay = 0.6f;

        Coroutine summonRoutine;

        [OnRuntimeInit]
        static void OnRuntimeInit()
        {
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnSceneChange;
        }

        static void OnSceneChange(Scene from, Scene to)
        {
            audioRoutines.Clear();
        }

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
            TriggerAudio(new List<AudioClip>{ appearAudioClip }, new List<float> { appearAudioDelay });
            
            yield return new WaitForSeconds(animationWaitTime);

            GameObject instance = SpawnObject(prefab);

            if (strike != null)
            {
                strike.gameObject.SetActive(true);
            }

            TriggerAudio(new List<AudioClip> { openAudioClip, disappearAudioClip }, new List<float> { openAudioDelay, disappearAudioDelay });
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

        public static void TriggerAudio(List<AudioClip> clips, List<float> delays)
        {
            string combinedName = "";
            for (int i = 0; i < clips.Count - 1; i++)
            {
                combinedName += clips[i].name + "_-_";
            }
            combinedName += clips[clips.Count - 1].name;
            if (audioRoutines.ContainsKey(combinedName))
            {
                //WeaverLog.Log("NOT TRIGGERING Clip = " + combinedName);
                return;
            }

            //WeaverLog.Log("TRIGGERING Clip = " + combinedName);

            UnboundCoroutine audioRoutine = null;
            
            audioRoutine = UnboundCoroutine.Start(TriggerAudioRoutine(combinedName, clips, delays, () => audioRoutine));

            audioRoutines.Add(combinedName, audioRoutine);
        }

        static IEnumerator TriggerAudioRoutine(string combinedName, List<AudioClip> clips, List<float> delays, Func<UnboundCoroutine> routine)
        {
            yield return null;
            for (int i = 0; i < clips.Count; i++)
            {
                if (!audioRoutines.ContainsKey(combinedName))
                {
                    //WeaverLog.Log("NOT Playing Clip = " + combinedName);
                    yield break;
                }

                //WeaverLog.Log("Playing Clip = " + combinedName);

                WeaverAudio.PlayAtPoint(clips[i], Player.Player1.transform.position);
                yield return new WaitForSeconds(delays[i]);
            }

            if (audioRoutines.ContainsKey(combinedName))
            {
                audioRoutines.Remove(combinedName);
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
            Gizmos.color = new Color(0f,1f,0f, 0.25f);
            Gizmos.DrawCube(transform.TransformPoint(EntitySpawnOffset), new Vector3(0.35f, 0.35f, 0.35f));
        }
    }
}