using UnityEditor;
using UnityEngine;
using WeaverCore.Components.Colosseum;

[CustomEditor(typeof(ColosseumEnemyPreloads))]
public class ChallengeEnemyPreloadsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        // Get the path of the asset
        var obj = target as ColosseumEnemyPreloads;
        string path = AssetDatabase.GetAssetPath(obj);

        // Get the asset importer for the asset
        AssetImporter importer = AssetImporter.GetAtPath(path);

        // Check if the asset is assigned to an asset bundle
        if (string.IsNullOrEmpty(importer.assetBundleName))
        {
            // Display a warning message
            EditorGUILayout.HelpBox("This scriptable object is not assigned to any AssetBundle. Please assign it to an AssetBundle.", MessageType.Warning);
        }
    }
}
