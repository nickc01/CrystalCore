using GlobalEnums;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using WeaverCore.Components;

[CustomEditor(typeof(PlayerDamager))]
public class PlayerDamagerEditor : Editor
{
	public override void OnInspectorGUI()
	{
		EditorGUILayout.PropertyField(serializedObject.FindProperty("damageDealt"));

		var hazardType = serializedObject.FindProperty("hazardType");

		//EditorGUILayout.BeginHorizontal();

		//EditorGUILayout.LabelField("Hazard Type");
		hazardType.intValue = (int)(HazardType)EditorGUILayout.EnumPopup("Hazard Type", (HazardType)hazardType.intValue);

		//EditorGUILayout.EndHorizontal();

		EditorGUILayout.PropertyField(serializedObject.FindProperty("shadowDashHazard"));
		EditorGUILayout.PropertyField(serializedObject.FindProperty("resetOnEnable"));
		serializedObject.ApplyModifiedProperties();
	}
}
