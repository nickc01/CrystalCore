using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using WeaverCore.Components;
using WeaverCore.Utilities;
using System.Linq;
using WeaverCore;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WeaverCore.Components.Colosseum
{
    public class EnemyWave : Wave, ISerializationCallbackReceiver, IColosseumIdentifier
    {
        [Tooltip("Minimum duration the wave can last.")]
        public float minimumDuration = 5f;

        [Tooltip("Delay before the wave ends.")]
        public float endingDelay = 0.5f;

        [Tooltip("List of entries defining enemies in the wave.")]
        public List<EnemyWaveEntry> entries = new List<EnemyWaveEntry>();

        [SerializeField]
        [HideInInspector]
        string entries_json;

        [SerializeField]
        [HideInInspector]
        List<UnityEngine.Object> entries_references;

        string IColosseumIdentifier.Identifier => "Enemy Waves";

        Color IColosseumIdentifier.Color => Color.yellow;

        bool IColosseumIdentifier.ShowShortcut => true;

        public override IEnumerator RunWave(ColosseumRoomManager challenge)
        {
            //WeaverLog.Log("RUNNING WAVE = " + gameObject.name);
            float waveStartTime = Time.time;
            List<MonoBehaviour> prioritizedEnemies = new List<MonoBehaviour>();
            Dictionary<MonoBehaviour, (Vector3, float)> lastPositions = new Dictionary<MonoBehaviour, (Vector3, float)>();

            List<int> awaitingSummons = new List<int>(entries.Select((_,i) => i));

            // Start spawning enemies
            //foreach (WaveEntry entry in entries)

            int count = -1;

            //WeaverLog.Log("ENTRIES = " + entries.Count);
            //for (int i = 0; i < entries.Count; i++)
            foreach (EnemyWaveEntry entry in entries.OrderBy(e => e.delayBeforeSpawn))
            {
                count++;
                //var entry = entries[i];
                // Find the enemy prefab
                GameObject enemyPrefab = null;

                // Try to find in enemyPrefabs list
                foreach (GameObject prefab in challenge.enemyPrefabs)
                {
                    if (prefab.name == entry.enemyName)
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
                                if (name == entry.enemyName)
                                {
                                    preloadPath = path;
                                    enemyPrefab = challenge.PreloadPlaceHolder.gameObject;
                                    goto Outer;
                                }
                            }
                            #endif

                            WeaverLog.Log("FINDING ENEMY = " + entry.enemyName);

                            if (ColosseumEnemyPreloads.LoadedObjects.ContainsKey(entry.enemyName))
                            {
                                enemyPrefab = ColosseumEnemyPreloads.LoadedObjects[entry.enemyName];
                                WeaverLog.Log("FOUND ENEMY = " + enemyPrefab);
                                goto Outer;
                            }

                            /*for (int i = 0; i < preloadObj.preloadPaths.Length; i++)
                            {
                                var path = preloadObj.preloadPaths[i];
                                WeaverLog.Log("CHECKING PATH = " + path);
                                WeaverLog.Log("CHECKING NAME = " + ChallengeEnemyPreloads.GetObjectNameInPath(path));
                                if (entry.enemyName == ChallengeEnemyPreloads.GetObjectNameInPath(path))
                                {
                                    //WeaverLog.Log("FOUND ENEMY = " + preloadObj.preloadedObjects[i]);
                                    //enemyPrefab = preloadObj.preloadedObjects[i];
                                    goto Outer;
                                }                                
                            }*/
                            /*foreach (var obj in preloadObj.preloadedObjects)
                            {
                                if (obj != null && obj.name == entry.enemyName)
                                {
                                    enemyPrefab = obj;
                                    goto Outer;
                                }
                            }*/
                        }
                    }
                }

                Outer:

                if (enemyPrefab == null)
                {
                    count--;
                    Debug.LogError("Enemy prefab not found: " + entry.enemyName);
                    continue;
                }

                // Find spawn location
                //SpawnLocation spawnLocation = challenge.spawnLocations.Find(s => s.name == entry.spawnLocationName);
                ColosseumEnemySpawner spawnLocation = challenge.spawnLocations.Find(s => s != null && s.name == entry.spawnLocationName);
                if (spawnLocation == null)
                {
                    count--;
                    Debug.LogError("Spawn location not found: " + entry.spawnLocationName);
                    continue;
                }

                // Wait for delay
                yield return new WaitForSeconds(entry.delayBeforeSpawn - (Time.time - waveStartTime));

                int currentSummon = count;

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
                    if (entry.isPrioritized && hComponent != null)
                    {
                        prioritizedEnemies.Add(hComponent);
                        lastPositions.Add(hComponent, (hComponent.transform.position, Time.time));
                    }

                    WeaverLog.Log("SUMMONED = " + currentSummon + " : " + gm);
                });
            }

            yield return new WaitUntil(() => awaitingSummons.Count == 0);

            // Wait for minimum duration
            while (Time.time - waveStartTime < minimumDuration)
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
                            /*if (e.TryGetComponent<PoolableObject>(out var pool))
                            {
                                //return health > 0 && !pool.InPool;
                                isAlive = !(health <= 0 || pool.InPool);
                            }
                            else
                            {
                                
                            }*/
                        }

                        return isAlive;
                    }


                    if (!prioritizedEnemies.Exists(e => IsAlive(e)))
                    {
                        break;
                    }
                    yield return null;
                }
            }

            yield return new WaitForSeconds(endingDelay);
        }

        private void OnDrawGizmosSelected()
        {
            #if UNITY_EDITOR
                if (Selection.gameObjects.IndexOf(gameObject) < 0)
                {
                    return;
                }
            #endif

            ColosseumRoomManager challenge = GetComponentInParent<ColosseumRoomManager>();

            if (challenge != null && challenge.EnemySpawnPointsLabels)
            {
                // Group entries by spawn location
                Dictionary<ColosseumEnemySpawner, List<EnemyWaveEntry>> entriesBySpawnLocation = new Dictionary<ColosseumEnemySpawner, List<EnemyWaveEntry>>();

                foreach (EnemyWaveEntry entry in entries)
                {
                    ColosseumEnemySpawner spawnLocation = challenge.spawnLocations.Find(s => s.name == entry.spawnLocationName);
                    if (spawnLocation != null)
                    {
                        if (!entriesBySpawnLocation.ContainsKey(spawnLocation))
                        {
                            entriesBySpawnLocation[spawnLocation] = new List<EnemyWaveEntry>();
                        }
                        entriesBySpawnLocation[spawnLocation].Add(entry);
                    }
                }

                float offsetAmount = 1f; // Adjust this value as needed

                // Draw entries with offset
                foreach (var kvp in entriesBySpawnLocation)
                {
                    ColosseumEnemySpawner spawnLocation = kvp.Key;
                    List<EnemyWaveEntry> entriesAtLocation = kvp.Value;

                    var spawnPoint = spawnLocation.transform.position;

                    for (int i = 0; i < entriesAtLocation.Count; i++)
                    {
                        EnemyWaveEntry entry = entriesAtLocation[i];

                        // Calculate offset position
                        Vector3 offset = Vector3.down * i * offsetAmount;

                        // Draw the location where the enemy will spawn
                        Gizmos.color = entry.entryColor;
                        Gizmos.DrawSphere(spawnPoint, 0.3f);

                        Gizmos.color = spawnLocation.EditorColor;
                        Gizmos.DrawLine(transform.position, spawnPoint);

                        #if UNITY_EDITOR

                        var style = new GUIStyle(EditorStyles.textField);
                        style.normal.textColor = Color.Lerp(entry.entryColor, Color.white, 0.5f);
                        style.fontSize = 20;
                        Handles.Label(spawnPoint + offset, new GUIContent(entry.enemyName), style);

                        #endif
                    }
                }
            }
        }


        #if UNITY_EDITOR
        private void OnValidate() 
        {
            WeaverSerializer.Serialize(entries, out entries_json, out entries_references);        
        }
        #endif

        public void OnBeforeSerialize() {}

        public void OnAfterDeserialize()
        {
            #if !UNITY_EDITOR
            entries = WeaverSerializer.Deserialize<List<EnemyWaveEntry>>(entries_json, entries_references);
            #endif
        }
    }
}
