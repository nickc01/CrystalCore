using System.Collections.Generic;
using System.Linq;
using RuntimeInspectorNamespace;
using UnityEngine;
using UnityEngine.Serialization;
using WeaverCore;
using WeaverCore.Attributes;
using WeaverCore.Internal;
using WeaverCore.Utilities;

namespace WeaverCore.Components.Colosseum
{
    [CreateAssetMenu(fileName = "ChallengeEnemyPreloads", menuName = "Sanctuary/Challenge Enemy Preloads")]
    public class ColosseumEnemyPreloads : ScriptableObject
    {
        static char[] nameChars = new char[] {':', '/'};

        [Tooltip("Paths for preloading enemy assets.")]
        public string[] preloadPaths;

        [Tooltip("Aliases for mapping enemy names.")]
        public string[] nameAliases;

        public static Dictionary<string, GameObject> LoadedObjects = new Dictionary<string, GameObject>();

        static List<ColosseumEnemyPreloads> loadedPreloads = new List<ColosseumEnemyPreloads>();

        public static IEnumerable<ColosseumEnemyPreloads> LoadedPreloads => loadedPreloads;

        //[HideInInspector]
        //public GameObject[] preloadedObjects;

        public IEnumerator<string> preloadScenes
        {
            get
            {
                foreach (var path in preloadPaths)
                {
                    yield return GetSceneInPath(path);
                }
            }
        }


        public static string GetSceneInPath(string path)
        {
            var sepIndex = path.IndexOf(':');

            if (sepIndex >= 0)
            {
                return path.Substring(0, sepIndex);
            }
            else
            {
                return null;
            }
        }

        public static string GetPathExcludingScene(string path)
        {
            var sepIndex = path.IndexOf(':');

            if (sepIndex >= 0 && sepIndex <= path.Length - 1)
            {
                return path.Substring(sepIndex + 1);
            }
            else
            {
                return null;
            }
        }

        public static string GetObjectNameInPath(string path)
        {
            var sepIndex = path.LastIndexOfAny(nameChars);

            if (sepIndex >= 0 && sepIndex <= path.Length - 1)
            {
                return path.Substring(sepIndex + 1);
            }
            else
            {
                return null;
            }
        }

        public string GetAliasBaseName(string name)
        {
            foreach (var alias in GetAliases(name, true))
            {
                foreach (var path in preloadPaths)
                {
                    if (GetObjectNameInPath(path) == alias)
                    {
                        return alias;
                    }
                }
            }

            return default;
        }


        public IEnumerable<string> GetAliases(string name, bool includeBase = false)
        {
            if (includeBase)
            {
                yield return name;
            }
            foreach (var alias in nameAliases)
            {
                var index = alias.IndexOf(':');
                if (index < 0)
                {
                    continue;
                }
                var entry1 = alias.Substring(0, index);
                var entry2 = alias.Substring(index + 1);

                if (entry1 == name || entry2 == name)
                {
                    if (entry1 != name)
                    {
                        //WeaverLog.Log($"Alias for {name} = {entry1}");
                        yield return entry1;
                    }
                    
                    if (entry2 != name)
                    {
                        //WeaverLog.Log($"Alias for {name} = {entry2}");
                        yield return entry2;
                    }
                }
            }
        }

        [OnRuntimeInit]
        static void OnRuntimeInit()
        {
            WeaverCore_ModClass.OnPreloadNames += GetPreloadNames;
            WeaverCore_ModClass.OnPreloadedObjects += Initialize;
        }

        static List<(string, string)> GetPreloadNames()
        {
            WeaverLog.Log("GETTING PRELOAD NAMES");
            List<(string, string)> preloadNames = new List<(string, string)>();
            foreach (var bundle in WeaverAssets.AllBundles())
            {
                var preloads = WeaverAssets.LoadAssetsOfType<ColosseumEnemyPreloads>(bundle);
                //var preloads = WeaverAssets.LoadAssetsOfType<ChallengeEnemyPreloads>(bundle.GetNameWithType).ToList();
                //WeaverLog.Log("FOUND PRELOAD OBJECTS = " + preloads != null ? preloads.Count.ToString() : "null");
                if (preloads != null)
                {
                    foreach (var p in preloads)
                    {
                        loadedPreloads.Add(p);
                        WeaverLog.Log("Preload = " + p);
                        foreach (var path in p.preloadPaths)
                        {
                            WeaverLog.Log("PATH = " + path);
                            var scene = GetSceneInPath(path);
                            var other = GetPathExcludingScene(path);

                            if (!preloadNames.Contains((scene, other)))
                            {
                                preloadNames.Add((scene, other));
                            }
                        }
                    }
                }
            }
            return preloadNames;
        }

        static void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            if (preloadedObjects != null)
            {
                //var preloads = WeaverAssets.LoadAssetsOfType<ChallengeEnemyPreloads, TheSanctuaryMod>().ToList();

                var preloadParent = new GameObject("PRELOAD_CONTAINER");
                preloadParent.hideFlags = HideFlags.HideAndDontSave;
                GameObject.DontDestroyOnLoad(preloadParent);
                preloadParent.SetActive(false);

                foreach (var p in loadedPreloads)
                {
                    //p.preloadedObjects = new GameObject[p.preloadPaths.Length];

                    for (int i = 0; i < p.preloadPaths.Length; i++)
                    {
                        var scene = GetSceneInPath(p.preloadPaths[i]);
                        var name = GetObjectNameInPath(p.preloadPaths[i]);
                        var other = GetPathExcludingScene(p.preloadPaths[i]);

                        if (preloadedObjects.TryGetValue(scene, out var others) && others.TryGetValue(other, out var loadedObj))
                        {
                            WeaverLog.Log($"Found Loaded Object {loadedObj.name} for {scene}:{other}");
                            //p.preloadedObjects[i] = loadedObj;
                            if (!LoadedObjects.ContainsKey(name))
                            {
                                loadedObj.SetActive(false);
                                loadedObj.transform.SetParent(preloadParent.transform);
                                loadedObj.SetActive(true);
                                LoadedObjects.Add(name, loadedObj);
                            }
                        }
                        else
                        {
                            WeaverLog.Log($"Didnt't find Loaded Object for {scene}:{other}");
                            //p.preloadedObjects[i] = null;
                        }
                        //TODO TODO TODO
                    }
                }
            }
        }
    }
}
