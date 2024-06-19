using Modding;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using WeaverCore.Assets.Components;
using WeaverCore.Attributes;
using WeaverCore.Interfaces;

namespace WeaverCore.Utilities
{
    /// <summary>
    /// Contains utility functions related to Charms
    /// </summary>
    public static class CharmUtilities
    {
        static Dictionary<int, IWeaverCharm> addedCustomCharms = new Dictionary<int, IWeaverCharm>();

        static Dictionary<IWeaverCharm, int> charmsToID = new Dictionary<IWeaverCharm, int>();

        static HashSet<IWeaverCharm> disabledCharms = new HashSet<IWeaverCharm>();

#if UNITY_EDITOR
        static int editor_counter = 0;
#else
        static Assembly sfCoreAssembly;
        delegate List<int> AddSpritesDelegate(params Sprite[] charmSprites);

        static AddSpritesDelegate AddSprites;
#endif

        /// <summary>
        /// 
        /// </summary>
        /// <param name="charm">The charm to add</param>
        /// <param name="charmSprite">The sprite of the charm</param>
        /// <param name="acquiredByDefault">Is the charm acquired by the player by default?</param>
        /// <param name="equippedByDefault">Is the charm equipped by the player by default?</param>
        /// <returns>Returns the ID of the registered charm</returns>
        /// <exception cref="System.Exception">Throws if the charm is already registered</exception>
        public static int RegisterCharm(IWeaverCharm charm, Sprite charmSprite)
        {
            if (CharmRegistered(charm))
            {
                throw new System.Exception($"The charm \"{charm.Name}\" : {charm.GetType()} is being added multiple times");
            }
#if UNITY_EDITOR
            int id = ++editor_counter;

            addedCustomCharms.Add(id, charm);
            charmsToID.Add(charm, id);

            WeaverLog.Log($"Registered Charm {charm.GetType().FullName} - {id}");
            return id;
#else
            if (sfCoreAssembly == null)
            {
                sfCoreAssembly = ReflectionUtilities.FindLoadedAssembly("SFCore");
                if (sfCoreAssembly == null)
                {
                    throw new System.Exception("Attempting to add a charm without SFCore installed. Install SFCore to fix this issue");
                }

                var charmHelper = sfCoreAssembly.GetType("SFCore.CharmHelper");

                if (charmHelper == null)
                {
                    throw new System.Exception("Unable to find the CharmHelper class");
                }

                var addSpritesMethod = charmHelper.GetMethod("AddSprites");

                if (addSpritesMethod == null)
                {
                    throw new System.Exception("Unable to find the AddSprites method on the CharmHelper class");
                }

                AddSprites = ReflectionUtilities.MethodToDelegate<AddSpritesDelegate>(addSpritesMethod);
            }

            var ids = AddSprites(charmSprite);

            addedCustomCharms.Add(ids[0], charm);
            charmsToID.Add(charm, ids[0]);
            Debug.Log($"Registered Charm {charm.GetType().FullName} - {ids[0]}");

            return ids[0];
#endif
        }

        public static bool CharmRegistered(IWeaverCharm charm)
        {
            return addedCustomCharms.ContainsValue(charm);
        }

        public static int GetCustomCharmID(IWeaverCharm charm)
        {
            if (charmsToID.TryGetValue(charm, out var id))
            {
                return id;
            }
            return -1;
        }

        public static IEnumerable<IWeaverCharm> GetLoadedCharms()
        {
            foreach (var pair in addedCustomCharms)
            {
                if (!disabledCharms.Contains(pair.Value))
                {
                    yield return pair.Value;
                }
            }
        }
        
        public static int GetLoadedCharmCount()
        {
            int count = 0;
            foreach (var pair in addedCustomCharms)
            {
                if (!disabledCharms.Contains(pair.Value))
                {
                    count++;
                }
            }
            return count;
        }

        [OnFeatureLoad(priority: int.MinValue)]
        static void OnCharmLoad(IWeaverCharm charm)
        {
            //sWeaverLog.Log($"LOADING CHARM {charm.GetType()}");

            if (disabledCharms.Contains(charm))
            {
                disabledCharms.Remove(charm);
            }
            else
            {
                try
                {
                    RegisterCharm(charm, charm.CharmSprite);
                }
                catch (Exception e)
                {
                    WeaverLog.LogError($"Error: Failed to instantiate charm \"{charm.GetType().FullName}\". Make sure it has a default constructor");
                    WeaverLog.LogException(e);
                }
            }
        }

        [OnFeatureUnload(priority: int.MinValue)]
        static void OnCharmUnload(IWeaverCharm charm)
        {
            disabledCharms.Add(charm);
        }

        [OnRuntimeInit]
        static void OnInit()
        {
            ModHooks.LanguageGetHook += ModHooks_LanguageGetHook;
            ModHooks.GetPlayerBoolHook += ModHooks_GetPlayerBoolHook;
            ModHooks.SetPlayerBoolHook += ModHooks_SetPlayerBoolHook;
            ModHooks.GetPlayerIntHook += ModHooks_GetPlayerIntHook;
        }

        public static bool CharmDisabled(IWeaverCharm charm)
        {
            return disabledCharms.Contains(charm);
        }

        static bool TryParseCharm(string key, string strToFind, out int charmNumber, out IWeaverCharm charm)
        {
            if (key.TryFind(strToFind, out _, out var end) && int.TryParse(key.Substring(end), out charmNumber) && addedCustomCharms.TryGetValue(charmNumber, out charm))
            {
                return true;
            }
            charmNumber = -1;
            charm = null;
            return false;
        }

        private static int ModHooks_GetPlayerIntHook(string name, int orig)
        {
            IWeaverCharm charm;

            if (TryParseCharm(name, "charmCost_", out _, out charm))
            {
                if (CharmDisabled(charm))
                {
                    return 1;
                }
                return charm.NotchCost;
            }

            return orig;
        }

        private static bool ModHooks_SetPlayerBoolHook(string name, bool newValue)
        {
            IWeaverCharm charm;

            if (TryParseCharm(name, "gotCharm_", out _, out charm))
            {
                if (CharmDisabled(charm))
                {
                    return newValue;
                }
                if (charm.Acquired != newValue)
                {
                    charm.Acquired = newValue;
                }
                return newValue;
            }

            if (TryParseCharm(name, "newCharm_", out _, out charm))
            {
                if (CharmDisabled(charm))
                {
                    return newValue;
                }
                if (charm.NewlyCollected != newValue)
                {
                    charm.NewlyCollected = newValue;
                }
                return newValue;
            }

            if (TryParseCharm(name, "equippedCharm_", out _, out charm))
            {
                if (CharmDisabled(charm))
                {
                    return newValue;
                }
                if (charm.Equipped != newValue)
                {
                    charm.Equipped = newValue;
                }
                return newValue;
            }

            return newValue;
        }

        private static bool ModHooks_GetPlayerBoolHook(string name, bool orig)
        {
            IWeaverCharm charm;

            if (TryParseCharm(name, "gotCharm_", out _, out charm))
            {
                if (CharmDisabled(charm))
                {
                    return false;
                }
                return charm.Acquired;
            }

            if (TryParseCharm(name, "newCharm_", out _, out charm))
            {
                if (CharmDisabled(charm))
                {
                    return false;
                }
                return charm.NewlyCollected;
            }

            if (TryParseCharm(name, "equippedCharm_", out _, out charm))
            {
                if (CharmDisabled(charm))
                {
                    return false;
                }
                return charm.Equipped;
            }

            return orig;
        }

        private static string ModHooks_LanguageGetHook(string key, string sheetTitle, string orig)
        {
            IWeaverCharm charm;

            if (TryParseCharm(key, "CHARM_NAME_", out _, out charm))
            {
                if (CharmDisabled(charm))
                {
                    return $"DISABLED - {charm.Name}";
                }
                return charm.Name;
            }

            if (TryParseCharm(key, "CHARM_DESC_", out _, out charm))
            {
                if (CharmDisabled(charm))
                {
                    return $"{charm.Description} - This charm is currently disabled";
                }
                return charm.Description;
            }

            return orig;
        }

        public static bool GiveCharmToPlayer(IWeaverCharm charm, bool displayCollectMessage = true)
        {
            //WeaverLog.Log($"GIVING CHARM {charm} to player");
            return GiveCharmToPlayer(GetCustomCharmID(charm), displayCollectMessage);
        }

        public static bool GiveCharmToPlayer(int charmID, bool displayCollectMessage = true)
        {
            //WeaverLog.Log($"GIVING CHARM ID {charmID} to player");
            bool alreadyCollected = false;
            if (PlayerData.instance.GetBool($"gotCharm_{charmID}") == true)
            {
                alreadyCollected = true;
            }
            else
            {
                GameManager.instance.IncrementPlayerDataInt("charmsOwned");
            }

            //WeaverLog.Log("ALREADY COLLECTED = " + alreadyCollected);

            //WeaverLog.Log("HAS CHARM = " + GameManager.instance.GetPlayerDataBool($"gotCharm_{charmID}"));

            PlayerData.instance.SetBool($"gotCharm_{charmID}", true);
            GameManager.instance.StoryRecord_acquired($"gotCharm_{charmID}");

            if (!GameManager.instance.GetPlayerDataBool("hasCharm"))
            {
                PlayerData.instance.SetBool("hasCharm", true);
            }

            if (displayCollectMessage)
            {
                //WeaverLog.Log("SPAWNING COLLECT MESSAGE");
                ItemGetMessage.SpawnCharm(charmID);
            }
            //else
            //{
                //WeaverLog.Log("NOT SPAWNING COLLECT MESSAGE");
            //}
            return alreadyCollected;
        }
    }

}
