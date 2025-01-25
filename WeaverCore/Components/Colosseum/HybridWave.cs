using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using WeaverCore.Utilities;
using System;
using TMPro;
using UnityEngine.Events;

namespace WeaverCore.Components.Colosseum
{
    public class HybridWave : Wave, ISerializationCallbackReceiver, IColosseumIdentifier
    {
        public enum LoopKind {
            None,
            EnemiesOnly,
            EventsOnly,
            Both
        }

        [Serializable]
        class EnemyDataContainer
        {
            public List<EnemyWaveEntry> Entries;
        }

        [Tooltip("Minimum duration the wave can last.")]
        public float minimumDuration = 10f;

        [Tooltip("How many times to loop the hybrid wave.")]
        public int loopCount = 0;

        [Tooltip("The delay between each loop. This doesn't nothing if the Loop Count is set to 0")]
        public float loopDelay = 0;

        [Tooltip("What the additional loops will run. \"None\" disables looping. \"Enemies Only\" will only repeat enemy entries when looping. \"Events Only\" will only repeat event entries when looping. \"Both\" will repeat everything upon looping.")]
        public LoopKind LoopingKind = LoopKind.Both;

        [Tooltip("Delay before the wave ends.")]
        public float endingDelay = 0f;

        [Tooltip("If set to true, then this wave can only be run if manually triggered. Be sure to check this box to prevent the wave from being automatically triggered")]
        public bool ManuallyTriggered = false;

        [Tooltip("List of hybrid wave entries defining events to run or enemies to spawn during the wave.")]
        public List<HybridWaveEntry> entries = new List<HybridWaveEntry>();

        public string Identifier => "Hybrid Waves";

        public Color Color => Color.Lerp(new Color(0.0f, 1.0f, 1.0f), Color.yellow, 0.5f);

        public bool ShowShortcut => true;

        [SerializeField]
        [HideInInspector]
        List<UnityEvent> event_entries_eventsToRun = new List<UnityEvent>();

        [SerializeField]
        [HideInInspector]
        List<float> event_entries_delayBeforeRun = new List<float>();

        [SerializeField]
        [HideInInspector]
        string enemy_entries_json;

        [SerializeField]
        [HideInInspector]
        List<UnityEngine.Object> enemy_entries_references;

        [SerializeField]
        [HideInInspector]
        List<HybridWaveEntry.HybridWaveType> entries_types = new List<HybridWaveEntry.HybridWaveType>();

        public override bool AutoRun => base.AutoRun && !ManuallyTriggered;

        static UnityEvent Clone(UnityEvent source)
        {
            return JsonUtility.FromJson<UnityEvent>(JsonUtility.ToJson(source));
            /*var copy = new UnityEvent();
            foreach (var pair in copiers)
            {
                pair.Value(copy, source);
            }
            return copy;*/
        }

        #if UNITY_EDITOR
        private void OnValidate() 
        {
            event_entries_eventsToRun.Clear();
            event_entries_eventsToRun.AddRange(entries.Select(e => Clone(e.eventData.eventsToRun)));

            event_entries_delayBeforeRun.Clear();
            event_entries_delayBeforeRun.AddRange(entries.Select(e => e.eventData.delayBeforeRun));

            var enemyEntries = new EnemyDataContainer {
                Entries = new List<EnemyWaveEntry>(entries.Select(e => e.enemyData))
            };

            entries_types.Clear();
            entries_types.AddRange(entries.Select(e => e.Type));

            WeaverSerializer.Serialize(enemyEntries, out enemy_entries_json, out enemy_entries_references);
        }
        #endif

        public void OnBeforeSerialize()
        {
            
        }

        public void OnAfterDeserialize()
        {
            #if !UNITY_EDITOR
            //entries = WeaverSerializer.Deserialize<List<EnemyWaveEntry>>(entries_json, entries_references);
            entries.Clear();

            var enemyEntries = WeaverSerializer.Deserialize<EnemyDataContainer>(enemy_entries_json, enemy_entries_references);

            for (int i = 0; i < event_entries_eventsToRun.Count; i++)
            {
                entries.Add(new HybridWaveEntry {
                    Type = entries_types[i],
                    enemyData = enemyEntries.Entries[i],
                    eventData = new EventWaveEntry {
                        eventsToRun = event_entries_eventsToRun[i],
                        delayBeforeRun = event_entries_delayBeforeRun[i]
                    }
                });
                /*entries.Add(new EventWaveEntry {
                    eventsToRun = event_entries_eventsToRun[i],
                    delayBeforeRun = event_entries_delayBeforeRun[i]
                });*/
            }
            #endif
        }

        public override IEnumerator RunWave(ColosseumRoomManager challenge)
        {
            yield return RunWaveInternal(challenge, () => ManualStopType.None);
        }

        IEnumerator RunWaveInternal(ColosseumRoomManager challenge, Func<ManualStopType> doStop)
        {
            LoopKind currentLoopKind = LoopKind.Both;
            for (int l = 0; l <= loopCount; l++)
            {
                float waveStartTime = Time.time;
                List<MonoBehaviour> prioritizedEnemies = new List<MonoBehaviour>();
                Dictionary<MonoBehaviour, (Vector3, float)> lastPositions = new Dictionary<MonoBehaviour, (Vector3, float)>();

                bool isEnemyLoop = currentLoopKind == LoopKind.EnemiesOnly || currentLoopKind == LoopKind.Both;
                bool isEventLoop = currentLoopKind == LoopKind.EventsOnly || currentLoopKind == LoopKind.Both;

                List<int> awaitingSummons = new List<int>(entries.Where(e => e.Type == HybridWaveEntry.HybridWaveType.Enemy && isEnemyLoop).Select((_,i) => i));
                int enemyCount = -1;
                int entryCount = 0;
                //foreach (EnemyWaveEntry entry in entries.OrderBy(e => e.delayBeforeSpawn)
                foreach (var entry in entries.Where(e => (e.Type == HybridWaveEntry.HybridWaveType.Enemy && isEnemyLoop) || (e.Type == HybridWaveEntry.HybridWaveType.Event && isEventLoop)).OrderBy(e => e.DelayBeforeSpawn))
                {
                    entryCount++;
                    if (entry.Type == HybridWaveEntry.HybridWaveType.Enemy)
                    {
                        var enemyEntry = entry.enemyData;
                        enemyCount++;
                        GameObject enemyPrefab = null;

                        // Try to find in enemyPrefabs list
                        foreach (GameObject prefab in challenge.enemyPrefabs)
                        {
                            if (prefab.name == enemyEntry.enemyName)
                            {
                                enemyPrefab = prefab;
                                break;
                            }
                        }

                        string preloadPath = null;

                        if (enemyPrefab == null)
                        {
                            foreach (var preloadObj in challenge.preloadedEnemies)
                            {
                                if (preloadObj != null)
                                {
                                    #if UNITY_EDITOR
                                    foreach (var path in preloadObj.preloadPaths)
                                    {
                                        var name = ColosseumEnemyPreloads.GetObjectNameInPath(path);
                                        if (name == enemyEntry.enemyName)
                                        {
                                            preloadPath = path;
                                            enemyPrefab = challenge.PreloadPlaceHolder.gameObject;
                                            goto Outer;
                                        }
                                    }
                                    #endif

                                    WeaverLog.Log("FINDING ENEMY = " + enemyEntry.enemyName);

                                    if (ColosseumEnemyPreloads.LoadedObjects.ContainsKey(enemyEntry.enemyName))
                                    {
                                        enemyPrefab = ColosseumEnemyPreloads.LoadedObjects[enemyEntry.enemyName];
                                        WeaverLog.Log("FOUND ENEMY = " + enemyPrefab);
                                        goto Outer;
                                    }
                                }
                            }
                        }

                        Outer:

                        if (enemyPrefab == null)
                        {
                            enemyCount--;
                            Debug.LogError("Enemy prefab not found: " + enemyEntry.enemyName);
                            continue;
                        }

                        // Find spawn location
                        //SpawnLocation spawnLocation = challenge.spawnLocations.Find(s => s.name == entry.spawnLocationName);
                        ColosseumEnemySpawner spawnLocation = challenge.spawnLocations.Find(s => s != null && s.name == enemyEntry.spawnLocationName);
                        if (spawnLocation == null)
                        {
                            enemyCount--;
                            Debug.LogError("Spawn location not found: " + enemyEntry.spawnLocationName);
                            continue;
                        }

                        // Wait for delay
                        //yield return new WaitForSeconds(enemyEntry.delayBeforeSpawn - (Time.time - waveStartTime));
                        yield return CoroutineUtilities.WaitForTimeOrPredicate(enemyEntry.delayBeforeSpawn - (Time.time - waveStartTime), () => doStop() == ManualStopType.Forcefully);

                        if (doStop() == ManualStopType.Forcefully)
                        {
                            yield break;
                        }

                        int currentSummon = enemyCount;

                        // Use the spawner to spawn the enemy
                        spawnLocation.SpawnEnemy(enemyPrefab, gm => {

                            #if UNITY_EDITOR
                            if (preloadPath != null && gm.TryGetComponent<TextMeshPro>(out var tmPro))
                            {
                                tmPro.text = tmPro.text.Replace("{x}", ColosseumEnemyPreloads.GetObjectNameInPath(preloadPath));
                            }
                            #endif

                            awaitingSummons.Remove(currentSummon);

                            var hComponent = HealthUtilities.GetHealthComponent(gm);

                            // If prioritized, add to the list
                            if (enemyEntry.isPrioritized && hComponent != null)
                            {
                                prioritizedEnemies.Add(hComponent);
                                lastPositions.Add(hComponent, (hComponent.transform.position, Time.time));
                            }
                        });
                    }
                    else if (entry.Type == HybridWaveEntry.HybridWaveType.Event)
                    {
                        var eventEntry = entry.eventData;
                        //yield return new WaitForSeconds(eventEntry.delayBeforeRun - (Time.time - waveStartTime));
                        yield return CoroutineUtilities.WaitForTimeOrPredicate(eventEntry.delayBeforeRun - (Time.time - waveStartTime), () => doStop() == ManualStopType.Forcefully);

                        if (doStop() == ManualStopType.Forcefully)
                        {
                            yield break;
                        }

                        eventEntry.eventsToRun?.Invoke();
                    }
                    else
                    {
                        //Do Nothing
                    }


                }

                WeaverLog.Log("ENTRY COUNT = " + entryCount);

                if (entryCount == 0)
                {
                    break;
                }


                yield return new WaitUntil(() => awaitingSummons.Count == 0 || doStop() != ManualStopType.None);

                // Wait for minimum duration
                while (Time.time - waveStartTime < minimumDuration && doStop() != ManualStopType.None)
                {
                    yield return null;
                }

                var sceneManager = GameObject.FindObjectOfType<WeaverSceneManager>();

                Rect sceneBounds = default;

                if (sceneManager != null)
                {
                    sceneBounds = sceneManager.SceneDimensions;
                }

                // Wait for prioritized enemies to be destroyed (i.e., their health reaches 0)
                if (prioritizedEnemies.Count > 0)
                {
                    while (true)
                    //while (prioritizedEnemies.Exists(e => HealthUtilities.TryGetHealth(e, out var health) && health > 0 || (e.TryGetComponent<PoolableObject>(out var pool) && pool.InPool)))
                    {
                        bool IsAlive(MonoBehaviour e)
                        {
                            bool isAlive = true;
                            if (e == null || e.gameObject == null)
                            {
                                isAlive = false;
                            }

                            if (isAlive && e.TryGetComponent<PoolableObject>(out var poolableObject))
                            {
                                isAlive = !poolableObject.InPool;
                            }

                            if (isAlive && lastPositions.TryGetValue(e, out var pair))
                            {
                                if (Vector2.Distance(e.transform.position, pair.Item1) > 0.1)
                                {
                                    pair = (e.transform.position, Time.time);
                                    lastPositions[e] = pair;
                                }

                                if (Time.time - pair.Item2 >= 10f)
                                {
                                    isAlive = false;
                                }
                            }

                            if (isAlive && sceneManager != null)
                            {
                                isAlive = sceneBounds.IsWithin(e.transform.position);
                            }

                            if (isAlive && HealthUtilities.TryGetHealth(e, out var health))
                            {
                                isAlive = health > 0;
                            }

                            return isAlive;
                        }


                        if (!prioritizedEnemies.Exists(e => IsAlive(e)) || doStop() != ManualStopType.None)
                        {
                            break;
                        }
                        yield return null;
                    }
                }

                if (l != loopCount)
                {
                    yield return CoroutineUtilities.WaitForTimeOrPredicate(loopDelay, () => doStop() != ManualStopType.None);
                    //yield return new WaitForSeconds(loopDelay);
                }

                if (LoopingKind == LoopKind.EnemiesOnly)
                {
                    currentLoopKind = LoopKind.EnemiesOnly;
                }
                else if (LoopingKind == LoopKind.EventsOnly)
                {
                    currentLoopKind = LoopKind.EventsOnly;
                }

                WeaverLog.Log("STOP STATE = " + doStop());

                if (doStop() == ManualStopType.Gracefully || doStop() == ManualStopType.Forcefully)
                {
                    yield break;
                }
            }

            yield return new WaitForSeconds(endingDelay);
        }

        protected override IEnumerator ManuallyRunRoutine(ColosseumRoomManager challenge, Func<ManualStopType> doStop)
        {
            yield return RunWaveInternal(challenge, doStop);
        }
    }
}
