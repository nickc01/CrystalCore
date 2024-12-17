using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using WeaverCore.Components.Colosseum;

[CustomPropertyDrawer(typeof(EnemyWaveEntry))]
public class EnemyWaveEntryDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Get the Challenge reference
        EnemyWave wave = property.serializedObject.targetObject as EnemyWave;
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

        // Enemy Name dropdown
        SerializedProperty enemyNameProp = property.FindPropertyRelative("enemyName");
        Rect enemyRect = new Rect(position.x, y, position.width, lineHeight);
        string[] enemyOptions = GetEnemyOptions(challenge);
        string[] aliases = GetEnemyAliases(challenge, enemyOptions);
        int enemyIndex = Mathf.Max(0, System.Array.IndexOf(enemyOptions, enemyNameProp.stringValue));
        enemyIndex = EditorGUI.Popup(enemyRect, "Enemy", enemyIndex, aliases);
        enemyNameProp.stringValue = enemyOptions[enemyIndex];
        y += lineHeight + EditorGUIUtility.standardVerticalSpacing;

        // Spawn Location dropdown
        SerializedProperty spawnLocationNameProp = property.FindPropertyRelative("spawnLocationName");
        Rect spawnRect = new Rect(position.x, y, position.width, lineHeight);
        string[] spawnOptions = GetSpawnLocationOptions(challenge);
        int spawnIndex = Mathf.Max(0, System.Array.IndexOf(spawnOptions, spawnLocationNameProp.stringValue));
        spawnIndex = EditorGUI.Popup(spawnRect, "Spawn Location", spawnIndex, spawnOptions);
        spawnLocationNameProp.stringValue = spawnOptions[spawnIndex];
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

        SerializedProperty entryColorProp = property.FindPropertyRelative("entryColor");

        var value = entryColorProp.colorValue;

        var first = wave.entries.FirstOrDefault(e => e.entryColor == value);
        var last = wave.entries.LastOrDefault(e => e.entryColor == value);



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

            if (foundAlias != null)
            {
                aliases[i] = foundAlias + $" (aka {names[i]})";
            }
            else
            {
                aliases[i] = names[i];
            }
        }

        return aliases;
    }

    private string[] GetEnemyOptions(ColosseumRoomManager challenge)
    {
        List<string> options = new List<string>();
        options.Add("Empty");
        if (challenge.enemyPrefabs != null)
        {
            foreach (GameObject prefab in challenge.enemyPrefabs)
            {
                if (prefab != null)
                {
                    options.Add(prefab.name);
                }
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

        //options.AddRange(challenge.preloadedEnemyNames);
        return options.ToArray();
    }

    private string[] GetSpawnLocationOptions(ColosseumRoomManager challenge)
    {
        List<string> options = new List<string>();
        options.Add("Empty");
        foreach (var spawn in challenge.spawnLocations)
        {
            options.Add(spawn.name);
        }
        return options.ToArray();
    }
}
