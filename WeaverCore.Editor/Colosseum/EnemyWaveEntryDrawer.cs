using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using WeaverCore;
using WeaverCore.Components.Colosseum;
using WeaverCore.Utilities;

[CustomPropertyDrawer(typeof(EnemyWaveEntry))]
public class EnemyWaveEntryDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Get the Challenge reference
        Wave wave = property.serializedObject.targetObject as Wave;

        if (wave == null || !(wave is EnemyWave || wave is HybridWave))
        {
            base.OnGUI(position, property, label);
            return;
        }

        ColosseumRoomManager challenge = wave.GetComponentInParent<ColosseumRoomManager>();
        if (challenge == null)
        {
            EditorGUI.LabelField(position, "Challenge reference not found.");
            return;
        }

        // Begin property
        EditorGUI.BeginProperty(position, label, property);

        // Calculate rects
        var indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        float lineHeight = EditorGUIUtility.singleLineHeight;
        float y = position.y;

        // Enemy Name field
        SerializedProperty enemyNameProp = property.FindPropertyRelative("enemyName");
        Rect enemyRect = new Rect(position.x, y, position.width, lineHeight);
        string[] enemyOptions = GetEnemyOptions(challenge);
        string[] aliases = GetEnemyAliases(challenge, enemyOptions);

        if (string.IsNullOrEmpty(enemyNameProp.stringValue))
        {
            enemyNameProp.stringValue = "Empty";
        }
        int enemyIndex = System.Array.IndexOf(enemyOptions, enemyNameProp.stringValue);

        if (enemyIndex < 0) // Not in the options
        {
            DrawInvalidTextField(enemyRect, enemyNameProp, "Enemy not found in Colosseum");
        }
        else
        {
            enemyIndex = EditorGUI.Popup(enemyRect, "Enemy", enemyIndex, aliases);
            enemyNameProp.stringValue = enemyOptions[enemyIndex];
        }
        y += lineHeight + EditorGUIUtility.standardVerticalSpacing;

        // Spawn Location field
        SerializedProperty spawnLocationNameProp = property.FindPropertyRelative("spawnLocationName");
        Rect spawnRect = new Rect(position.x, y, position.width, lineHeight);
        string[] spawnOptions = GetSpawnLocationOptions(challenge);
        if (string.IsNullOrEmpty(spawnLocationNameProp.stringValue))
        {
            spawnLocationNameProp.stringValue = "Empty";
        }
        int spawnIndex = System.Array.IndexOf(spawnOptions, spawnLocationNameProp.stringValue);

        if (spawnIndex < 0) // Not in the options
        {
            DrawInvalidTextField(spawnRect, spawnLocationNameProp, "Location not found in Colosseum");
        }
        else
        {
            spawnIndex = EditorGUI.Popup(spawnRect, "Spawn Location", spawnIndex, spawnOptions);
            spawnLocationNameProp.stringValue = spawnOptions[spawnIndex];
        }
        y += lineHeight + EditorGUIUtility.standardVerticalSpacing;

        // Delay Before Spawn
        SerializedProperty delayProp = property.FindPropertyRelative("delayBeforeSpawn");
        Rect delayRect = new Rect(position.x, y, position.width, lineHeight);
        EditorGUI.PropertyField(delayRect, delayProp);
        y += lineHeight + EditorGUIUtility.standardVerticalSpacing;

        // Is Prioritized
        SerializedProperty isPrioritizedProp = property.FindPropertyRelative("isPrioritized");
        Rect prioritizedRect = new Rect(position.x, y, position.width, lineHeight);
        EditorGUI.PropertyField(prioritizedRect, isPrioritizedProp);
        y += lineHeight + EditorGUIUtility.standardVerticalSpacing;

        // Entry Color
        SerializedProperty entryColorProp = property.FindPropertyRelative("entryColor");
        var value = entryColorProp.colorValue;

        EnemyWaveEntry first = null;
        EnemyWaveEntry last = null;

        if (wave is EnemyWave enemyWave)
        {
            first = enemyWave.entries.FirstOrDefault(e => e.entryColor == value);
            last = enemyWave.entries.LastOrDefault(e => e.entryColor == value);
        }
        else if (wave is HybridWave hybridWave)
        {
            //WeaverLog.Log("HYBRID WAVE = " + hybridWave);
            //WeaverLog.Log("ENTRIES = " + hybridWave.entries);
            first = hybridWave.entries.FirstOrDefault(e => e.Type == HybridWaveEntry.HybridWaveType.Enemy && e.enemyData.entryColor == value)?.enemyData;
            last = hybridWave.entries.LastOrDefault(e => e.Type == HybridWaveEntry.HybridWaveType.Enemy && e.enemyData.entryColor == value)?.enemyData;
        }

        //var test = wave as EnemyWave;
        //var first = test.entries.FirstOrDefault(e => e.entryColor == value);
        //var last = test.entries.LastOrDefault(e => e.entryColor == value);

        if (value == default || (last != first && value == last.entryColor && value == first.entryColor))
        {
            value = new Color(Random.value, Random.value, Random.value);
            entryColorProp.colorValue = value;
        }

        Rect entryColorRect = new Rect(position.x, y, position.width, lineHeight);
        EditorGUI.PropertyField(entryColorRect, entryColorProp);
        y += lineHeight + EditorGUIUtility.standardVerticalSpacing;

        // Set indent back
        EditorGUI.indentLevel = indent;

        // End property
        EditorGUI.EndProperty();
    }

    private void DrawInvalidTextField(Rect rect, SerializedProperty prop, string errorMessage)
    {
        // Set color to red
        Color previousColor = GUI.color;
        GUI.color = Color.red;

        // Draw the text field
        Rect fieldRect = new Rect(rect.x, rect.y, rect.width * 0.7f, rect.height);
        prop.stringValue = EditorGUI.TextField(fieldRect, prop.displayName, prop.stringValue);

        // Reset color
        GUI.color = previousColor;

        // Draw the error message
        Rect labelRect = new Rect(rect.x + fieldRect.width + 5, rect.y, rect.width * 0.3f - 5, rect.height);
        EditorGUI.LabelField(labelRect, errorMessage, EditorStyles.miniLabel);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 5;
    }

    private string[] GetEnemyAliases(ColosseumRoomManager challenge, string[] names)
    {
        string[] aliases = new string[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            string foundAlias = null;
            foreach (var preloadList in challenge.preloadedEnemies)
            {
                foreach (var alias in preloadList.GetAliases(names[i]))
                {
                    foundAlias = alias;
                    goto Outer;
                }
            }
        Outer:
            aliases[i] = foundAlias != null ? $"{foundAlias} (aka {names[i]})" : names[i];
        }
        return aliases;
    }

    private string[] GetEnemyOptions(ColosseumRoomManager challenge)
    {
        List<string> options = new List<string> { "Empty" };
        if (challenge.enemyPrefabs != null)
        {
            foreach (GameObject prefab in challenge.enemyPrefabs)
            {
                if (prefab != null) options.Add(prefab.name);
            }
        }
        if (challenge.preloadedEnemies != null)
        {
            foreach (var preloadedObj in challenge.preloadedEnemies)
            {
                foreach (var path in preloadedObj.preloadPaths)
                {
                    options.Add(ColosseumEnemyPreloads.GetObjectNameInPath(path));
                }
            }
        }
        return options.ToArray();
    }

    private string[] GetSpawnLocationOptions(ColosseumRoomManager challenge)
    {
        List<string> options = new List<string> { "Empty" };
        foreach (var spawn in challenge.spawnLocations)
        {
            options.Add(spawn.name);
        }
        return options.ToArray();
    }
}

