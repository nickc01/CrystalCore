using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using WeaverCore.Components.Colosseum;

/*[CustomPropertyDrawer(typeof(EventWaveEntry))]
public class EventWaveEntryDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var rect = new Rect(position.x, position.y, position.width, 0f);
        foreach (var prop in property)
        {
            var height = EditorGUI.GetPropertyHeight(prop as SerializedProperty);
            rect.height += height;
            EditorGUI.PropertyField(rect, prop as SerializedProperty);
            rect.y += height;
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = 0;
        foreach (var prop in property)
        {
            height += EditorGUI.GetPropertyHeight(prop as SerializedProperty);
        }

        return height;
        //return EditorGUI.GetPropertyHeight(property, label);
        //return base.GetPropertyHeight(property, label);
    }
}*/

