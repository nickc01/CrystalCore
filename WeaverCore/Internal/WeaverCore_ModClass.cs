using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using WeaverCore.Assets;
using WeaverCore.Utilities;

namespace WeaverCore.Internal
{

    /// <summary>
    /// The mod class for WeaverCore
    /// </summary>
    public sealed class WeaverCore_ModClass : WeaverMod
    {
        public WeaverCore_ModClass() : base("WeaverCore") { }
        /*public override void Initialize()
        {
            base.Initialize();
        }*/

        internal static event Func<List<(string, string)>> OnPreloadNames;
        internal static event Action<Dictionary<string, Dictionary<string, GameObject>>> OnPreloadedObjects;

        List<(string, string)> GetBasePreloadNames()
        {
            return new System.Collections.Generic.List<(string, string)>
            {
                ("GG_Workshop", "GG_Statue_Mage_Knight"),/*,
                ("End_Game_Completion", "credits object")*/
                ("Tutorial_01", "_Enemies/Crawler 1"),
                ("Tutorial_01", "_Props/Health Cocoon"),
                ("Tutorial_01", "_Props/Chest"),
                ("Tutorial_01", "_Enemies/Buzzer"),
                ("Town", "_NPCs/Elderbug/Dream Dialogue")
            };
            /*var preloads = WeaverAssets.LoadAssetsOfType<ChallengeEnemyPreloads, TheSanctuaryMod>().ToList();
            WeaverLog.Log("FOUND PRELOAD OBJECTS = " + preloads != null ? preloads.Count.ToString() : "null");

            if (preloads != null)
            {
                foreach (var p in preloads)
                {
                    WeaverLog.Log("Preload = " + p);
                    foreach (var path in p.preloadPaths)
                    {
                        WeaverLog.Log("PATH = " + path);
                    }
                }
            }
            return base.GetPreloadNames();*/
        }

        /*public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            
        }*/


        public override string GetVersion()
        {
            return "2.2.0.7";
        }

        public override System.Collections.Generic.List<(string, string)> GetPreloadNames()
        {
            List<(string, string)> extraPreloads = new List<(string, string)>();
            if (OnPreloadNames != null)
            {
                foreach (var func in OnPreloadNames.GetInvocationList())
                {
                    if (func is Func<List<(string, string)>> funcCasted)
                    {
                        var funcResults = funcCasted.Invoke();
                        if (funcResults != null)
                        {
                            extraPreloads.AddRange(funcResults);
                        }
                    }
                }
            }

            var baseList = base.GetPreloadNames();

            if (baseList == null)
            {
                baseList = new List<(string, string)>();
            }

            baseList.AddRange(GetBasePreloadNames());

            foreach (var extra in extraPreloads)
            {
                WeaverLog.Log("Adding Extra Preload = " + extra.Item1 + ":" + extra.Item2);
            }

            baseList.AddRange(extraPreloads);

            return baseList;
        }

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            if (preloadedObjects.TryGetValue("GG_Workshop", out var ggSceneDict) && ggSceneDict.TryGetValue("GG_Statue_Mage_Knight", out var mageKnightStatue))
            {
                GG_Preloads.SetMageKnightStatue(mageKnightStatue);
            }

            if (preloadedObjects.TryGetValue("Tutorial_01", out var tutorialSceneDict))
            {
                if (tutorialSceneDict.TryGetValue("_Enemies/Crawler 1", out var crawler))
                {
                    var enemyDeathEffects = crawler.GetComponent("EnemyDeathEffects");

                    var journalPrefab = enemyDeathEffects.ReflectGetField("journalUpdateMessagePrefab") as GameObject;

                    Other_Preloads.JournalUpdateMessagePrefab = journalPrefab;

                    GameObject.Destroy(crawler);
                }

                if (tutorialSceneDict.TryGetValue("_Props/Health Cocoon", out var healthCocoonObj))
                {
                    var cocoon = healthCocoonObj.GetComponent<HealthCocoon>();
                    foreach (var fling in cocoon.flingPrefabs)
                    {
                        if (fling.prefab.name == "Health Scuttler")
                        {
                            var scuttlerControl = fling.prefab.GetComponent<ScuttlerControl>();
                            Other_Preloads.HealthCocoonFlashPrefab = scuttlerControl.screenFlash;
                            break;
                        }
                    }

                    GameObject.Destroy(healthCocoonObj);
                }

                if (tutorialSceneDict.TryGetValue("_Enemies/Buzzer", out var buzzerObj))
                {
                    var deathEffects = buzzerObj.GetComponent("EnemyDeathEffects");

                    var deathEffectsType = deathEffects.GetType();

                    Other_Preloads.JournalUpdateMessagePrefab = (GameObject)deathEffectsType.GetField("journalUpdateMessagePrefab", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(deathEffects);

                    var journalUpdateMessageSpawnedField = deathEffectsType.GetField("journalUpdateMessageSpawned", BindingFlags.NonPublic | BindingFlags.Static);

                    Other_Preloads.GetJournalUpdateMessageSpawnedFunc = () =>
                    {
                        return journalUpdateMessageSpawnedField.GetValue(null) as GameObject;
                    };

                    Other_Preloads.SetJournalUpdateMessageSpawnedFunc = val =>
                    {
                        journalUpdateMessageSpawnedField.SetValue(null, val);
                    };

                    GameObject.Destroy(buzzerObj);
                }

                if (tutorialSceneDict.TryGetValue("_Props/Chest", out var chestObj))
                {
                    var pmFSM = PlayMakerUtilities.GetPlaymakerFSMOnObject(chestObj, "Chest Control");
                    var fsm = PlayMakerUtilities.GetFSMOnPlayMakerComponent(pmFSM);
                    var spawnItemState = PlayMakerUtilities.FindStateOnFSM(fsm, "Spawn Items");
                    var actionData = PlayMakerUtilities.GetActionData(spawnItemState);

                    var fsmObjects = (IList)actionData.GetType().GetField("fsmGameObjectParams", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(actionData);
                    Type fsmObjectType = null;
                    PropertyInfo valueProperty = null;

                    foreach (var fsmObject in fsmObjects)
                    {
                        if (fsmObjectType == null)
                        {
                            fsmObjectType = fsmObject.GetType();
                            valueProperty = fsmObjectType.GetProperty("Value");
                        }

                        var gm = valueProperty.GetValue(fsmObject) as GameObject;
                        if (gm != null)
                        {
                            if (gm.name == "Geo Small")
                            {
                                Other_Preloads.smallGeoPrefab = gm;
                            }

                            if (gm.name == "Geo Med")
                            {
                                Other_Preloads.mediumGeoPrefab = gm;
                            }

                            if (gm.name == "Geo Large")
                            {
                                Other_Preloads.largeGeoPrefab = gm;
                            }
                        }
                    }

                    GameObject.Destroy(chestObj);
                }
            }

            if (preloadedObjects.TryGetValue("Town", out var townSceneDict))
            {
                if (townSceneDict.TryGetValue("_NPCs/Elderbug/Dream Dialogue", out var dreamDialogueObj))
                {
                    var npcPlayMakerFSM = PlayMakerUtilities.GetPlaymakerFSMOnObject(dreamDialogueObj, "npc_dream_dialogue");

                    var impactState = PlayMakerUtilities.FindStateOnFSM(PlayMakerUtilities.GetFSMOnPlayMakerComponent(npcPlayMakerFSM), "Impact");

                    var actionData = PlayMakerUtilities.GetActionData(impactState);

                    var fsmObjects = (IList)actionData.GetType().GetField("fsmGameObjectParams", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(actionData);

                    Type fsmObjectType = null;
                    PropertyInfo valueProperty = null;

                    foreach (var fsmObject in fsmObjects)
                    {
                        if (fsmObjectType == null)
                        {
                            fsmObjectType = fsmObject.GetType();
                            valueProperty = fsmObjectType.GetProperty("Value");
                        }

                        var gm = valueProperty.GetValue(fsmObject) as GameObject;
                        if (gm != null)
                        {
                            if (gm.name == "dream_area_effect")
                            {
                                Other_Preloads.dream_area_effectPrefab = gm;
                                break;
                            }
                        }
                    }

                    GameObject.Destroy(dreamDialogueObj);
                }
            }



            /*if (preloadedObjects.TryGetValue("End_Game_Completion", out var endSceneDict) && endSceneDict.TryGetValue("credits object", out var creditsObject))
            {
                var completionTitle = creditsObject.transform.Find("game completion").Find("game completion title");

                var tmp = completionTitle.GetComponent<TextMeshPro>();
                var mainFont = tmp.font;

                var otherFonts = GameObject.FindObjectsOfType<TMP_FontAsset>();


                if (FontAssetContainer.InGameFonts == null)
                {
                    FontAssetContainer.InGameFonts = new HashSet<TMP_FontAsset>();
                }

                AddFont(mainFont);

                //WeaverLog.Log("CURRENT SCENE = " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);

                //UnityEngine.SceneManagement.SceneManager.sceneLoaded += SceneManager_sceneLoaded;

                foreach (var tmpComponent in GameObject.FindObjectsOfType<TextMeshPro>())
                {
                    AddFont(tmpComponent.font);
                }

                foreach (var font in FontAssetContainer.InGameFonts)
                {
                    WeaverLog.Log("FOUND FONT = " + font.name);
                }

                var assetContainer = FontAssetContainer.Load();
                assetContainer.ReplaceFonts();
            }*/

            OnPreloadedObjects?.Invoke(preloadedObjects);

            base.Initialize(preloadedObjects);
        }

        /*private void SceneManager_sceneLoaded(UnityEngine.SceneManagement.Scene arg0, UnityEngine.SceneManagement.LoadSceneMode arg1)
        {
            WeaverLog.Log("NEW SCENE = " + arg0.path);

            if (arg0.path.Contains("Menu_Title"))
            {
                UnityEngine.SceneManagement.SceneManager.sceneLoaded -= SceneManager_sceneLoaded;

                foreach (var gm in arg0.GetRootGameObjects())
                {
                    CheckGMForFonts(gm);
                }

                foreach (var font in FontAssetContainer.InGameFonts)
                {
                    WeaverLog.Log("FOUND FONT = " + font.name);
                }

                var assetContainer = FontAssetContainer.Load();
                assetContainer.ReplaceFonts();
            }
        }*/
    }
}
