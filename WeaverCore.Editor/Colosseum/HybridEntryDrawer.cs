using UnityEditor;
using UnityEngine;
using WeaverCore.Components.Colosseum;

[CustomPropertyDrawer(typeof(HybridWaveEntry))]
public class HybridWaveEntryDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var typeProp = property.FindPropertyRelative(nameof(HybridWaveEntry.Type));

        var type = (HybridWaveEntry.HybridWaveType)typeProp.intValue;

        float lineHeight = EditorGUIUtility.singleLineHeight;
        float y = position.y;

        Rect enumRect = new Rect(position.x, position.y, position.width, lineHeight);

        var newType = (HybridWaveEntry.HybridWaveType)EditorGUI.EnumPopup(enumRect, new GUIContent("TYPE", "The type of event to be triggered (Enemy or Event Wave)"), type);

        if (newType != type)
        {
            type = newType;
            typeProp.intValue = (int)type;
        }


        switch (type)   
        {
            case HybridWaveEntry.HybridWaveType.Enemy:
                Rect enemyRect = new Rect(position.x, y + lineHeight + EditorGUIUtility.standardVerticalSpacing, position.width, lineHeight);
                var enemyData = property.FindPropertyRelative(nameof(HybridWaveEntry.enemyData));
                EditorGUI.PropertyField(enemyRect, enemyData, label);

                break;
            case HybridWaveEntry.HybridWaveType.Event:
                var eventData = property.FindPropertyRelative(nameof(HybridWaveEntry.eventData));
                var eventsToRun_prop = eventData.FindPropertyRelative(nameof(EventWaveEntry.eventsToRun));
                var delayBeforeRun_prop = eventData.FindPropertyRelative(nameof(EventWaveEntry.delayBeforeRun));

                var eventsToRun_rect = new Rect(position.x, y + lineHeight + EditorGUIUtility.standardVerticalSpacing, position.width, EditorGUI.GetPropertyHeight(eventsToRun_prop));

                EditorGUI.PropertyField(eventsToRun_rect, eventsToRun_prop);

                var delayBeforeRun_rect = new Rect(position.x, eventsToRun_rect.y + eventsToRun_rect.height + EditorGUIUtility.standardVerticalSpacing, position.width, EditorGUI.GetPropertyHeight(delayBeforeRun_prop));

                EditorGUI.PropertyField(delayBeforeRun_rect, delayBeforeRun_prop);
                break;
            default:
                base.OnGUI(position, property, label);
                break;
        }
        
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var typeProp = property.FindPropertyRelative(nameof(HybridWaveEntry.Type));
        var eventData = property.FindPropertyRelative(nameof(HybridWaveEntry.eventData));
        var eventsToRun_prop = eventData.FindPropertyRelative(nameof(EventWaveEntry.eventsToRun));
        var delayBeforeRun_prop = eventData.FindPropertyRelative(nameof(EventWaveEntry.delayBeforeRun));

        var type = (HybridWaveEntry.HybridWaveType)typeProp.intValue;

        if (type == HybridWaveEntry.HybridWaveType.Event)
        {
            //Enum Height
            var height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            //eventsToRun Height
            height += EditorGUI.GetPropertyHeight(eventsToRun_prop) + EditorGUIUtility.standardVerticalSpacing;

            //delayBeforeRun Height
            height += EditorGUI.GetPropertyHeight(delayBeforeRun_prop) + EditorGUIUtility.standardVerticalSpacing;

            return height;
        }
        else
        {
            return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 6;
        }
    }
}

