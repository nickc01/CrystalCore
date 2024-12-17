using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using UnityEngine.Events;
using WeaverCore.Editor.Compilation;
using System;
using System.Linq;
using WeaverCore;
using WeaverCore.Components.Colosseum;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Collections;
using WeaverCore.Utilities;

public class ColorJsonConverter : JsonConverter<Color?>
{
    public override void WriteJson(JsonWriter writer, Color? value, JsonSerializer serializer)
    {
        if (value.HasValue)
        {
            // Convert Color to hex string (including alpha)
            Color color = value.Value;
            string hex = ColorUtility.ToHtmlStringRGBA(color);
            // Write as "#RRGGBBAA"
            writer.WriteValue("#" + hex);
        }
        else
        {
            writer.WriteNull();
        }
    }

    public override Color? ReadJson(JsonReader reader, Type objectType, Color? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var token = reader.Value as string;
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }

        // Remove '#' if present
        if (token.StartsWith("#"))
        {
            token = token.Substring(1);
        }

        if (ColorUtility.TryParseHtmlString("#" + token, out Color color))
        {
            return color;
        }

        return null;
    }
}

public static class UnityEventJsonConverter
{
    [Serializable]
    struct ObjectResolutionContainer
    {
        public UnityEngine.Object ResolvedObj;

        public static ObjectResolutionContainer Resolve(int instanceID)
        {
            var json = $"{{\"ResolvedObj\":{{\"instanceID\":{instanceID}}}}}";
            var result = JsonUtility.FromJson<ObjectResolutionContainer>(json);
            WeaverLog.Log($"ID = {instanceID} resolved to {result.ResolvedObj}");
            return result;
        }
    }

    // Entry point to convert a UnityEvent to a custom JSON format
    public static string UnityEventToJson(UnityEvent unityEvent)
    {
        var persistentCallsField = typeof(UnityEventBase).GetField("m_PersistentCalls", BindingFlags.NonPublic | BindingFlags.Instance);
        var persistentCalls = persistentCallsField.GetValue(unityEvent);

        var callsField = persistentCalls.GetType().GetField("m_Calls", BindingFlags.NonPublic | BindingFlags.Instance);
        var callsList = callsField.GetValue(persistentCalls) as IList;

        var root = new JObject();
        var persistentCallsObj = new JObject();
        var callsArray = new JArray();

        foreach (var call in callsList)
        {
            Type persistentCallType = call.GetType();
            var target = (UnityEngine.Object)persistentCallType.GetField("m_Target", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(call);
            var targetAssemblyTypeName = (string)persistentCallType.GetField("m_TargetAssemblyTypeName", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(call);
            var methodName = (string)persistentCallType.GetField("m_MethodName", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(call);
            var mode = (PersistentListenerMode)persistentCallType.GetField("m_Mode", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(call);
            var arguments = persistentCallType.GetField("m_Arguments", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(call);
            var callState = (int)persistentCallType.GetField("m_CallState", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(call);

            var callObj = new JObject();
            var targetObj = ObjectToJObject(target);
            if (targetObj != null && targetObj.HasValues) 
            {
                callObj["m_Target"] = targetObj;
            }
            else
            {
                // If target is null or empty, still add as empty object to distinguish it
                callObj["m_Target"] = new JObject();
            }

            callObj["m_TargetAssemblyTypeName"] = targetAssemblyTypeName;
            callObj["m_MethodName"] = methodName;
            callObj["m_Mode"] = (int)mode;

            var argObj = ArgumentsToJObject(arguments, mode);
            if (argObj.HasValues)
            {
                callObj["m_Arguments"] = argObj;
            }

            callObj["m_CallState"] = callState;

            callsArray.Add(callObj);
        }

        persistentCallsObj["m_Calls"] = callsArray;
        root["m_PersistentCalls"] = persistentCallsObj;

        return root.ToString(Formatting.Indented);
    }

    // Convert back from the JSON to a UnityEvent
    public static UnityEvent JsonToUnityEvent(string json)
    {
        var unityEvent = new UnityEvent();

        var root = JObject.Parse(json);
        var callsArray = root["m_PersistentCalls"]?["m_Calls"] as JArray;
        if (callsArray == null)
        {
            return unityEvent; // no calls
        }

        var persistentCallsField = typeof(UnityEventBase).GetField("m_PersistentCalls", BindingFlags.NonPublic | BindingFlags.Instance);
        var persistentCalls = persistentCallsField.GetValue(unityEvent);
        var callsField = persistentCalls.GetType().GetField("m_Calls", BindingFlags.NonPublic | BindingFlags.Instance);
        var callsList = (IList)callsField.GetValue(persistentCalls);

        // Clear existing calls
        callsList.Clear();

        foreach (var callToken in callsArray)
        {
            var callObj = (JObject)callToken;

            Type persistentCallType = typeof(UnityEngine.Events.UnityEventBase).Assembly.GetType("UnityEngine.Events.PersistentCall");
            var newCall = Activator.CreateInstance(persistentCallType);

            var target = JObjectToObject(callObj["m_Target"] as JObject);
            var targetAssemblyTypeName = (string)callObj["m_TargetAssemblyTypeName"];
            var methodName = (string)callObj["m_MethodName"];
            var mode = (int)callObj["m_Mode"];
            var callState = (int)callObj["m_CallState"];

            persistentCallType.GetField("m_Target", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(newCall, target);
            persistentCallType.GetField("m_TargetAssemblyTypeName", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(newCall, targetAssemblyTypeName);
            persistentCallType.GetField("m_MethodName", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(newCall, methodName);
            persistentCallType.GetField("m_Mode", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(newCall, mode);
            persistentCallType.GetField("m_CallState", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(newCall, callState);

            var argumentsToken = callObj["m_Arguments"] as JObject;
            if (argumentsToken != null)
            {
                var argumentCacheType = typeof(UnityEngine.Events.UnityEventBase).Assembly.GetType("UnityEngine.Events.ArgumentCache");
                var argumentsObj = Activator.CreateInstance(argumentCacheType);

                JObjectToArguments(argumentsToken, argumentsObj);
                persistentCallType.GetField("m_Arguments", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(newCall, argumentsObj);
            }

            callsList.Add(newCall);
        }

        var callsDirtyField = typeof(UnityEventBase).GetField("m_CallsDirty", BindingFlags.NonPublic | BindingFlags.Instance);
        callsDirtyField.SetValue(unityEvent, true);

        return unityEvent;
    }

    private static JObject ObjectToJObject(UnityEngine.Object obj)
    {
        if (obj == null)
        {
            return new JObject();
        }

        var objJson = new JObject();
        if (IsSceneObject(obj))
        {
            string path = GetScenePath(obj);
            if (!string.IsNullOrEmpty(path))
            {
                objJson["scenePath"] = path;
            }
        }
        else
        {
            objJson["instanceID"] = obj.GetInstanceID();
        }

        return objJson;
    }

    private static JObject ArgumentsToJObject(object arguments, PersistentListenerMode mode)
    {
        var argumentCacheType = arguments.GetType();

        var objArgument = (UnityEngine.Object)argumentCacheType.GetField("m_ObjectArgument", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(arguments);
        var objectArgumentAssemblyTypeName = (string)argumentCacheType.GetField("m_ObjectArgumentAssemblyTypeName", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(arguments);
        var intArgument = (int)argumentCacheType.GetField("m_IntArgument", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(arguments);
        var floatArgument = (float)argumentCacheType.GetField("m_FloatArgument", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(arguments);
        var stringArgument = (string)argumentCacheType.GetField("m_StringArgument", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(arguments);
        var boolArgument = (bool)argumentCacheType.GetField("m_BoolArgument", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(arguments);

        var argsObj = new JObject();

        if (mode != PersistentListenerMode.EventDefined)
        {
            if (mode != PersistentListenerMode.Bool)
            {
                //argsObj["m_BoolArgument"] = false;
                boolArgument = false;
            }
            
            if (mode != PersistentListenerMode.Float)
            {
                //argsObj["m_FloatArgument"] = 0.0;
                floatArgument = 0.0f;
            }

            if (mode != PersistentListenerMode.Int)
            {
                //argsObj["m_IntArgument"] = 0;
                intArgument = 0;
            }

            if (mode != PersistentListenerMode.Object)
            {
                //argsObj["m_ObjectArgument"] = null;
                //argsObj["m_ObjectArgumentAssemblyTypeName"] = "";
                objArgument = null;
                objectArgumentAssemblyTypeName = "";
            }

            if (mode != PersistentListenerMode.String)
            {
                //argsObj["m_StringArgument"] = "";
                stringArgument = "";
            }
        }

        // Only include m_ObjectArgument if it is not null and has valid data
        if (objArgument != null)
        {
            var objJ = ObjectToJObject(objArgument);
            if (objJ.HasValues) 
            {
                argsObj["m_ObjectArgument"] = objJ;
                if (!string.IsNullOrEmpty(objectArgumentAssemblyTypeName))
                {
                    argsObj["m_ObjectArgumentAssemblyTypeName"] = objectArgumentAssemblyTypeName;
                }
            }
        }

        // Only include intArgument if not 0
        if (intArgument != 0)
        {
            argsObj["m_IntArgument"] = intArgument;
        }

        // Only include floatArgument if not 0
        if (Math.Abs(floatArgument) > float.Epsilon)
        {
            argsObj["m_FloatArgument"] = floatArgument;
        }

        // Only include stringArgument if not null or empty
        if (!string.IsNullOrEmpty(stringArgument))
        {
            argsObj["m_StringArgument"] = stringArgument;
        }

        // Only include boolArgument if true
        if (boolArgument)
        {
            argsObj["m_BoolArgument"] = boolArgument;
        }

        return argsObj;
    }

    private static UnityEngine.Object JObjectToObject(JObject objJson)
    {
        if (objJson == null || (objJson["scenePath"] == null && objJson["instanceID"] == null))
        {
            return null;
        }

        if (objJson["scenePath"] != null)
        {
            var parameters = ((string)objJson["scenePath"]).Split(':');
            string scenePath = parameters[0];
            string assemblyName = parameters[1];
            string typeName = parameters[2];

            var foundObj = GameObject.Find(scenePath);
            if (foundObj != null)
            {
                var componentType = TypeUtilities.NameToType(typeName, assemblyName);

                if (componentType == typeof(GameObject))
                {
                    return foundObj;
                }
                else
                {
                    return foundObj.GetComponent(componentType);
                }
            }
            return foundObj;
        }
        else
        {
            int instanceID = (int)objJson["instanceID"];
            return ObjectResolutionContainer.Resolve(instanceID).ResolvedObj;
        }
    }

    private static void JObjectToArguments(JObject argsJson, object argumentsObj)
    {
        var argumentCacheType = argumentsObj.GetType();

        UnityEngine.Object restoredObject = null;
        if (argsJson["m_ObjectArgument"] != null)
        {
            restoredObject = JObjectToObject(argsJson["m_ObjectArgument"] as JObject);
        }

        argumentCacheType.GetField("m_ObjectArgument", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(argumentsObj, restoredObject);
        argumentCacheType.GetField("m_ObjectArgumentAssemblyTypeName", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(argumentsObj, (string)argsJson["m_ObjectArgumentAssemblyTypeName"]);
        argumentCacheType.GetField("m_IntArgument", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(argumentsObj, (int?)argsJson["m_IntArgument"] ?? 0);
        argumentCacheType.GetField("m_FloatArgument", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(argumentsObj, (float?)argsJson["m_FloatArgument"] ?? 0f);
        argumentCacheType.GetField("m_StringArgument", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(argumentsObj, (string)argsJson["m_StringArgument"] ?? "");
        argumentCacheType.GetField("m_BoolArgument", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(argumentsObj, (bool?)argsJson["m_BoolArgument"] ?? false);
    }

    private static bool IsSceneObject(UnityEngine.Object obj)
    {
        if (obj == null)
            return false;
        var go = GetGameObject(obj);
        if (go == null)
            return false;
        return go.scene.IsValid();
    }

    private static GameObject GetGameObject(UnityEngine.Object obj)
    {
        if (obj is GameObject go)
            return go;
        if (obj is Component comp)
            return comp.gameObject;
        return null;
    }

    private static string GetScenePath(UnityEngine.Object obj)
    {
        var go = GetGameObject(obj);
        if (go == null)
            return null;

        string path = go.name;
        Transform current = go.transform;
        while (current.parent != null)
        {
            current = current.parent;
            path = current.name + "/" + path;
        }
        return path + ":" + obj.GetType().Assembly.GetName().Name + ":" + obj.GetType().FullName;
    }
}

public abstract class WaveData {}

[System.Serializable]
public class EnemyWaveData : WaveData
{
    public string enemyName;
    public string spawnLocationName;
    public float? delayBeforeSpawn;
    public bool? isPrioritized;

    [JsonConverter(typeof(ColorJsonConverter))]
    public Color? entryColor;
}

public class EventWaveData : WaveData
{
    public JObject functionToCall;
    public float? delayBeforeRun;
}

[System.Serializable]
public class WaveDataList
{
    public string name;
    public string type;  // "enemy" or "event"
    public List<WaveData> entries;  // Can be either EnemyEntry or EventEntry based on the type
}

[System.Serializable]
public class WaveContainer
{
    public List<WaveDataList> waves;
}

[CustomEditor(typeof(ColosseumRoomManager))]
public class ColosseumRoomManagerEditor : Editor
{
    private string jsonFilePath = "";
    private bool showShortcuts = false;
    private Dictionary<string, List<(GameObject obj, IColosseumIdentifier ident, IColosseumIdentifierExtra extra)>> shortcutGroups;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ColosseumRoomManager roomManager = (ColosseumRoomManager)target;

        if (GUILayout.Button("Import Waves from JSON"))
        {
            jsonFilePath = EditorUtility.OpenFilePanel("Select JSON file", "", "json");

            if (!string.IsNullOrEmpty(jsonFilePath))
            {
                ImportWavesFromJson(roomManager, jsonFilePath);
            }
        }

        if (GUILayout.Button("Export Waves to JSON"))
        {
            jsonFilePath = EditorUtility.SaveFilePanel("Save JSON file", "", "waves.json", "json");

            if (!string.IsNullOrEmpty(jsonFilePath))
            {
                ExportWavesToJson(roomManager, jsonFilePath);
            }
        }
        
        EditorGUILayout.Space();
        // Debug Info Button
        if (GUILayout.Button("Print Debug Info"))
        {
            PrintDebugInfo(roomManager);
        }

        // Print all possible enemies button
        if (GUILayout.Button("Print All Possible Enemies"))
        {
            PrintAllPossibleEnemies(roomManager);
        }

        EditorGUILayout.Space();
        showShortcuts = EditorGUILayout.Foldout(showShortcuts, "Shortcuts", true);
        if (showShortcuts)
        {
            EditorGUILayout.LabelField("Click on any of these shortcut buttons below to quickly jump to an object");
            EnsureShortcutGroups(roomManager);
            DrawShortcutsGUI();
        }
    }

    private void PrintAllPossibleEnemies(ColosseumRoomManager roomManager)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("---------- All Possible Enemies ----------");

        // 1. Enemy Prefabs
        sb.AppendLine("---- Enemy Prefabs ----");
        foreach (var prefab in roomManager.enemyPrefabs)
        {
            if (prefab != null)
            {
                // Using the prefab's real name
                sb.AppendLine(prefab.name);
            }
        }

        // 2. Preloaded Enemies
        sb.AppendLine("---- Preloaded Enemies ----");
        foreach (var preloadSet in roomManager.preloadedEnemies)
        {
            if (preloadSet == null)
                continue;

            foreach (var path in preloadSet.preloadPaths)
            {
                // Extract the real name from the path
                string realName = ColosseumEnemyPreloads.GetObjectNameInPath(path);
                if (!string.IsNullOrEmpty(realName))
                {
                    sb.AppendLine(realName);
                }
            }
        }

        sb.AppendLine("---------- End of List ----------");

        // Print all at once
        Debug.Log(sb.ToString());
    }

    private void PrintDebugInfo(ColosseumRoomManager roomManager)
    {
        //var allChildren = roomManager.GetComponentsInChildren<Transform>(true);

        // Use a StringBuilder to accumulate all debug information
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("---------- Debug Information ----------");

        // Enemy spawn positions
        sb.AppendLine("---- Enemy Spawn Positions ----");
        foreach (var location in roomManager.spawnLocations)
        {
            if (location != null)
            {
                sb.AppendLine($"Name: {location.name}, World Pos: {location.transform.position}, Local Pos: {location.transform.localPosition}");
            }
        }

        // Colosseum walls
        sb.AppendLine("---- Colosseum Walls ----");
        foreach (var wall in roomManager.GetComponentsInChildren<ColosseumWall>())
        {
            sb.AppendLine($"Wall Name: {wall.name}, World Pos: {wall.transform.position}, Local Pos: {wall.transform.localPosition}");
            var wallChild = wall.transform.Find("Wall");
            if (wallChild != null)
            {
                sb.AppendLine($"   Inner 'Wall' object: {wallChild.name}, Local Pos: {wallChild.localPosition}");
            }
        }

        // Platforms
        sb.AppendLine("---- Platforms ----");
        foreach (var plat in roomManager.GetComponentsInChildren<ColosseumPlatform>())
        {
            sb.AppendLine($"Platform Name: {plat.name}, World Pos: {plat.transform.position}, Local Pos: {plat.transform.localPosition}");
        }

        sb.AppendLine("---------- End of Debug Information ----------");

        // Print all at once
        Debug.Log(sb.ToString());
    }


    private void ImportWavesFromJson(ColosseumRoomManager roomManager, string jsonFilePath)
    {
        string jsonContent = File.ReadAllText(jsonFilePath);

        var waveContainerJson = JObject.Parse(jsonContent);

        var waveContainer = new WaveContainer();
        waveContainer.waves = new List<WaveDataList>();

        var wavesArray = waveContainerJson["waves"] as JArray;
        if (wavesArray != null)
        {
            foreach (var waveToken in wavesArray)
            {
                var w = new WaveDataList();
                w.name = (string)waveToken["name"];
                w.type = (string)waveToken["type"];
                w.entries = new List<WaveData>();

                var entriesArray = waveToken["entries"] as JArray;
                if (entriesArray != null)
                {
                    if (w.type == "enemy")
                    {
                        foreach (var entryToken in entriesArray)
                        {
                            var ene = entryToken.ToObject<EnemyWaveData>();
                            w.entries.Add(ene);
                        }
                    }
                    else if (w.type == "event")
                    {
                        foreach (var entryToken in entriesArray)
                        {
                            var eve = entryToken.ToObject<EventWaveData>();
                            w.entries.Add(eve);
                        }
                    }
                }

                waveContainer.waves.Add(w);
            }
        }

        var waveTransform = new GameObject("Waves " + Guid.NewGuid()).transform;
        waveTransform.SetParent(roomManager.transform);
        waveTransform.localPosition = Vector3.zero;

        foreach (var wave in waveContainer.waves)
        {
            var waveObj = new GameObject(wave.name);
            waveObj.transform.SetParent(waveTransform, false);
            waveObj.transform.localPosition = Vector3.zero;

            if (wave.type == "enemy")
            {
                var enemyWave = waveObj.AddComponent<EnemyWave>();
                enemyWave.entries = wave.entries.Cast<EnemyWaveData>().Select(ene => new EnemyWaveEntry
                {
                    enemyName = ene.enemyName,
                    spawnLocationName = ene.spawnLocationName,
                    delayBeforeSpawn = ene.delayBeforeSpawn ?? 0,
                    isPrioritized = ene.isPrioritized ?? false,
                    entryColor = ene.entryColor ?? default(Color)
                }).ToList();
            }
            else if (wave.type == "event")
            {
                var eventWave = waveObj.AddComponent<EventWave>();
                eventWave.entries = wave.entries.Cast<EventWaveData>().Select(eve =>
                {
                    var eventEntry = new EventWaveEntry();
                    eventEntry.delayBeforeRun = eve.delayBeforeRun ?? 0;
                    eventEntry.eventsToRun = UnityEventJsonConverter.JsonToUnityEvent(eve.functionToCall.ToString());
                    return eventEntry;
                }).ToList();
            }
        }

        Debug.Log("Waves imported successfully from: " + jsonFilePath);
    }

    private void ExportWavesToJson(ColosseumRoomManager roomManager, string jsonFilePath)
    {
        var settings = new JsonSerializerSettings
        {
            ContractResolver = new WeaverSerializer.IgnorePropertiesResolver(),
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore
        };

        var waves = roomManager.GetComponentsInChildren<Wave>().Where(w => w.AutoRun).OrderBy(w => w.transform.GetSiblingIndex()).ToList();

        var container = new WaveContainer();
        container.waves = new List<WaveDataList>();

        foreach (var wave in waves)
        {
            var data = new WaveDataList();
            data.name = wave.name;
            if (wave is EventWave eventWave)
            {
                data.type = "event";
                data.entries = new List<WaveData>();

                foreach (var entry in eventWave.entries)
                {
                    var unityEventSerialized = UnityEventJsonConverter.UnityEventToJson(entry.eventsToRun);
                    data.entries.Add(new EventWaveData
                    {
                        functionToCall = JObject.Parse(unityEventSerialized),
                        delayBeforeRun = entry.delayBeforeRun == 0 ? (float?)null : entry.delayBeforeRun
                    });
                }

                container.waves.Add(data);
            }
            else if (wave is EnemyWave enemyWave)
            {
                data.type = "enemy";
                data.entries = new List<WaveData>();

                foreach (var entry in enemyWave.entries)
                {
                    data.entries.Add(new EnemyWaveData
                    {
                        enemyName = entry.enemyName,
                        spawnLocationName = entry.spawnLocationName,
                        delayBeforeSpawn = entry.delayBeforeSpawn == 0 ? (float?)null : entry.delayBeforeSpawn,
                        isPrioritized = entry.isPrioritized == false ? (bool?)null : entry.isPrioritized,
                        entryColor = entry.entryColor == default ? (Color?)null : entry.entryColor
                    });
                }

                container.waves.Add(data);
            }
        }

        var jsonOutput = JsonConvert.SerializeObject(container, settings);
        File.WriteAllText(jsonFilePath, jsonOutput);
        Debug.Log("Waves exported to: " + jsonFilePath);
    }

    private void EnsureShortcutGroups(ColosseumRoomManager roomManager)
    {
        if (shortcutGroups == null)
        {
            shortcutGroups = new Dictionary<string, List<(GameObject obj, IColosseumIdentifier ident, IColosseumIdentifierExtra extra)>>();

            var identifiers = roomManager.GetComponentsInChildren<MonoBehaviour>(true)
                .Where(c => c is IColosseumIdentifier)
                .Select(c => (c.gameObject, (IColosseumIdentifier)c, c as IColosseumIdentifierExtra));

            foreach (var (obj, ident, extra) in identifiers)
            {
                if (!shortcutGroups.ContainsKey(ident.Identifier))
                {
                    shortcutGroups[ident.Identifier] = new List<(GameObject, IColosseumIdentifier, IColosseumIdentifierExtra)>();
                }
                shortcutGroups[ident.Identifier].Add((obj, ident, extra));
            }
        }
    }

    private Dictionary<string, bool> subgroupFoldouts = new Dictionary<string, bool>();

    private void DrawShortcutsGUI()
    {
        if (shortcutGroups == null || shortcutGroups.Count == 0)
        {
            EditorGUILayout.LabelField("No shortcuts available.");
            return;
        }

        foreach (var kvp in shortcutGroups)
        {
            string identifierName = kvp.Key;
            var list = kvp.Value;

            if (!subgroupFoldouts.ContainsKey(identifierName))
            {
                subgroupFoldouts[identifierName] = false;
            }

            var firstIdent = list[0].ident;
            Color groupColor = Color.Lerp(firstIdent.Color, Color.white, 0.45f);
            var style = new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = groupColor },
                onNormal = { textColor = groupColor },
                focused = { textColor = groupColor },
                onFocused = { textColor = groupColor },
                active = { textColor = groupColor },
                onActive = { textColor = groupColor }
            };

            subgroupFoldouts[identifierName] = EditorGUILayout.Foldout(subgroupFoldouts[identifierName], identifierName, true, style);
            if (subgroupFoldouts[identifierName])
            {
                EditorGUI.indentLevel++;
                foreach (var (obj, ident, extra) in list.OrderBy(item => item.obj.name))
                {
                    DrawShortcutButton(obj, ident, extra);
                }
                EditorGUI.indentLevel--;
            }
        }
    }

    private void DrawShortcutButton(GameObject obj, IColosseumIdentifier ident, IColosseumIdentifierExtra extra)
    {
        Color oldBackgroundColor = GUI.backgroundColor;
        Color oldContentColor = GUI.contentColor;

        GUI.contentColor = Color.Lerp(ident.Color, Color.white, 0.7f);
        GUI.backgroundColor = Color.Lerp(oldBackgroundColor, Color.black, 0.7f);

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.normal.textColor = GUI.contentColor;
        buttonStyle.alignment = TextAnchor.MiddleLeft;

        if (GUILayout.Button(obj.name, buttonStyle))
        {
            Selection.activeObject = obj;
            EditorGUIUtility.PingObject(obj);
        }

        if (extra != null)
        {
            Rect lastRect = GUILayoutUtility.GetLastRect();
            Handles.BeginGUI();
            Handles.color = extra.UnderlineColor;
            float underlineY = lastRect.yMax - 2f;
            Handles.DrawLine(new Vector3(lastRect.x, underlineY), new Vector3(lastRect.xMax, underlineY));
            Handles.EndGUI();
        }

        GUI.backgroundColor = oldBackgroundColor;
        GUI.contentColor = oldContentColor;
    }
}
