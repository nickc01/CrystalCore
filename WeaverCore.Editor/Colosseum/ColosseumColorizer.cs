using UnityEngine;
using UnityEditor;
using WeaverCore.Utilities;
using WeaverCore.Components.Colosseum;

[InitializeOnLoad]
public static class ColosseumColorizer
{
    static ColosseumColorizer()
    {
        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
    }

    static ColosseumRoomManager manager;

    private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
    {
        if (manager == null)
        {
            var managerObj = GameObject.FindGameObjectWithTag("Colosseum Manager");
            
            if (managerObj != null)
            {
                manager = managerObj.GetComponent<ColosseumRoomManager>();
            }
        }

        if (manager == null || manager.ColorLabels == false)
        {
            return;
        }

        GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

        if (obj != null)
        {
            if (obj.TryGetComponent<IColosseumIdentifier>(out var identifier))
            {
                EditorGUI.DrawRect(selectionRect, identifier.Color.With(a: 0.25f));

                if (identifier is IColosseumIdentifierExtra extra)
                {
                    var rect = selectionRect;
                    rect.y += rect.height;
                    rect.height /= 20f;
                    rect.y -= rect.height;
                    EditorGUI.DrawRect(rect, extra.UnderlineColor);
                }
            }
        }
    }
}