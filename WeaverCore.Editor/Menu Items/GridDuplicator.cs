using UnityEngine;
using UnityEditor;

public class GridDuplicator : EditorWindow
{
    // User input fields
    int horizontalAmount = 1;
    int verticalAmount = 1;
    float horizontalOffset = 1.0f;
    float verticalOffset = 1.0f;

    // Add menu item to open the window
    [MenuItem("WeaverCore/Tools/Grid Duplicator")]
    static void ShowWindow()
    {
        GetWindow<GridDuplicator>("Grid Duplicator");
    }

    // GUI layout
    private void OnGUI()
    {
        GUILayout.Label("Grid Settings", EditorStyles.boldLabel);

        horizontalAmount = EditorGUILayout.IntField("Horizontal Amount", horizontalAmount);
        verticalAmount = EditorGUILayout.IntField("Vertical Amount", verticalAmount);
        horizontalOffset = EditorGUILayout.FloatField("Horizontal Offset", horizontalOffset);
        verticalOffset = EditorGUILayout.FloatField("Vertical Offset", verticalOffset);

        // Apply button
        if (GUILayout.Button("Apply"))
        {
            DuplicateObjects();
            Close();
        }
    }

    // Subscribe to the scene GUI event
    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    // Unsubscribe when window is closed
    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    // Draw the green preview box in the scene view
    private void OnSceneGUI(SceneView sceneView)
    {
        GameObject selectedObject = Selection.activeGameObject;

        if (selectedObject == null)
            return;

        Handles.color = Color.green;

        // Get the starting position and transform of the selected object
        Vector3 startPos = selectedObject.transform.position;
        Transform t = selectedObject.transform;

        // Calculate offsets based on the object's orientation
        Vector3 rightOffset = t.right * (horizontalAmount - 1) * horizontalOffset;
        Vector3 forwardOffset = t.forward * (verticalAmount - 1) * verticalOffset;

        // Define the four corners of the rectangle
        Vector3[] verts = new Vector3[4];
        verts[0] = startPos;
        verts[1] = startPos + rightOffset;
        verts[2] = startPos + rightOffset + forwardOffset;
        verts[3] = startPos + forwardOffset;

        // Draw the rectangle
        Handles.DrawSolidRectangleWithOutline(verts, new Color(0, 1, 0, 0.1f), Color.green);

        // Repaint the scene view to update the preview
        SceneView.RepaintAll();
    }

    // Function to duplicate objects
    void DuplicateObjects()
    {
        GameObject selectedObject = Selection.activeGameObject;

        if (selectedObject == null)
        {
            Debug.LogError("No object selected to duplicate.");
            return;
        }

        Vector3 originalPosition = selectedObject.transform.localPosition;
        Transform parentTransform = selectedObject.transform.parent;

        // Check if the selected object is a prefab instance
        GameObject prefabAsset = null;
        if (PrefabUtility.IsPartOfPrefabInstance(selectedObject))
        {
            // Get the prefab asset associated with the selected object
            prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(selectedObject);
        }
        else if (PrefabUtility.IsPartOfPrefabAsset(selectedObject))
        {
            // The selected object is a prefab asset itself
            prefabAsset = selectedObject;
        }

        // Loop through the grid dimensions
        for (int i = 0; i < horizontalAmount; i++)
        {
            for (int j = 0; j < verticalAmount; j++)
            {
                // Skip the original object at (0,0)
                if (i == 0 && j == 0)
                    continue;

                // Calculate the new position using local axes
                //Vector3 offset = selectedObject.transform.right * (i * horizontalOffset) +
                                 //selectedObject.transform.forward * (j * verticalOffset);

                Vector3 offset = new Vector3(i * horizontalOffset, j * verticalOffset);

                Vector3 newPosition = originalPosition + offset;

                GameObject duplicate;

                if (prefabAsset != null)
                {
                    // Instantiate the prefab to maintain prefab connection
                    duplicate = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset, parentTransform);
                }
                else
                {
                    // Duplicate the object if it's not a prefab
                    duplicate = Instantiate(selectedObject, parentTransform);
                }

                // Set transform properties
                duplicate.transform.localPosition = newPosition;
                duplicate.transform.localRotation = selectedObject.transform.localRotation;
                duplicate.transform.localScale = selectedObject.transform.localScale;

                // Register the action for undo functionality
                Undo.RegisterCreatedObjectUndo(duplicate, "Duplicate Object");
            }
        }
    }
}
