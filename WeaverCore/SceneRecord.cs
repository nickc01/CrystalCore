﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using WeaverCore.Attributes;
using WeaverCore.Interfaces;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WeaverCore
{
#if UNITY_EDITOR
	[Serializable]
	struct SceneReplacement
	{
		[Tooltip("The name of the scene to replace in-game")]
		public string SceneToReplace;
		[Tooltip("The scene to replace it with")]
		public UnityEditor.SceneAsset Replacement;
	}

	[Serializable]
	struct SceneUnion
	{
		[Tooltip("The name of the scene to combine with in-game")]
		public string SceneToUniteWith;
		[Tooltip("The scene to combine to. This scene will be loaded when \"Scene To Unite With\" is also loaded, combining the contents of the two scenes together")]
		public UnityEditor.SceneAsset UnionScene;
	}
#endif

	/// <summary>
	/// Contains a list of scenes a mod is adding to the game
	/// </summary>
	[ShowFeature]
	[CreateAssetMenu(fileName = "Scene Record", menuName = "WeaverCore/Scene Record")]
	public class SceneRecord : ScriptableObject
	{
#if UNITY_EDITOR
		[SerializeField]
		[Tooltip("A list of scenes to add to the game")]
		List<UnityEditor.SceneAsset> sceneAdditions = new List<SceneAsset>();

		[SerializeField]
		[Tooltip("A list of scenes to replace in the game")]
		List<SceneReplacement> sceneReplacements = new List<SceneReplacement>();

		[SerializeField]
		[Tooltip("A list of scenes to combine with in game")]
		List<SceneUnion> sceneUnions = new List<SceneUnion>();
#endif

		[Space(40f)]
		[HideInInspector]
		[SerializeField]
		List<string> sceneAdditionPaths = new List<string>();


		[HideInInspector]
		[SerializeField]
		List<string> sceneToReplacePaths = new List<string>();
		[HideInInspector]
		[SerializeField]
		List<string> sceneReplacementPaths = new List<string>();

		[HideInInspector]
		[SerializeField]
		List<string> sceneToUnionizePaths = new List<string>();
		[HideInInspector]
		[SerializeField]
		List<string> sceneUnionPaths = new List<string>();


		public IEnumerable<string> SceneAdditions => sceneAdditionPaths;
		public IEnumerable<(string SceneToReplace,string Replacement)> SceneReplacements
		{
			get
			{
				for (int i = 0; i < sceneToReplacePaths.Count; i++)
				{
					yield return (sceneToReplacePaths[i],sceneReplacementPaths[i]);
				}
			}
		}

		public IEnumerable<(string SceneToUnionize, string SceneUnion)> SceneUnions
		{
			get
			{
				for (int i = 0; i < sceneToUnionizePaths.Count; i++)
				{
					yield return (sceneToUnionizePaths[i], sceneUnionPaths[i]);
				}
			}
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			sceneAdditionPaths = sceneAdditions.Select(s => AssetDatabase.GetAssetPath(s)).ToList();

			sceneToReplacePaths = sceneReplacements.Select(s => s.SceneToReplace).ToList();
			sceneReplacementPaths = sceneReplacements.Select(s => AssetDatabase.GetAssetPath(s.Replacement)).ToList();

			sceneToUnionizePaths = sceneUnions.Select(s => s.SceneToUniteWith).ToList();
			sceneUnionPaths = sceneUnions.Select(s => AssetDatabase.GetAssetPath(s.UnionScene)).ToList();
			/*if (scenePaths.Count != sceneAssets.Count)
			{
				scenePaths = new List<string>();
				foreach (var asset in sceneAssets)
				{
					scenePaths.Add(AssetDatabase.GetAssetPath(asset));
				}
			}
			else
			{
				for (int i = 0; i < sceneAssets.Count; i++)
				{
					var path = AssetDatabase.GetAssetPath(sceneAssets[i]);
					if (scenePaths[i] != path)
					{
						scenePaths[i] = path;
					}
				}
			}*/
		}

		[BeforeBuild]
		static void SetBundles()
		{
			Debug.Log("Before Build");
			var scenes = AssetDatabase.FindAssets($"t:{nameof(SceneRecord)}");
			foreach (var guid in scenes)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var record = AssetDatabase.LoadAssetAtPath<SceneRecord>(path);
				if (record != null)
				{
					record.OnValidate();
					var oldBundleName = GetAssetBundleName(record);
					var match = Regex.Match(oldBundleName, @"([\d\w]+?)_bundle");
					if (match.Success)
					{
						var newBundleName = $"{match.Groups[1].Value}_scenes_bundle";
						foreach (var sceneAsset in record.sceneAdditions)
						{
							SetAssetBundleName(newBundleName, sceneAsset);
						}

						foreach (var sceneAsset in record.sceneReplacements)
						{
							SetAssetBundleName(newBundleName, sceneAsset.Replacement);
						}

						foreach (var sceneAsset in record.sceneUnions)
						{
							SetAssetBundleName(newBundleName, sceneAsset.UnionScene);
						}
					}
				}
			}
		}

		static void SetAssetBundleName(string bundleName, UnityEngine.Object obj)
		{
			var path = AssetDatabase.GetAssetPath(obj);
			if (path != null && path != "")
			{
				var import = AssetImporter.GetAtPath(path);
				import.SetAssetBundleNameAndVariant(bundleName, import.assetBundleVariant);
			}
		}

		static string GetAssetBundleName(UnityEngine.Object obj)
		{
			var path = AssetDatabase.GetAssetPath(obj);
			if (path != null && path != "")
			{
				var import = AssetImporter.GetAtPath(path);
				return import.assetBundleName;
			}
			return "";
		}

#endif
	}
	/// <summary>
	/// Contains a list of scenes a mod is adding to the game
	/// </summary>
	//[ShowFeature]
	//[CreateAssetMenu(fileName = "Scene Record", menuName = "WeaverCore/Scene Record")]
	/*public class SceneRecord : ScriptableObject
	{
#if UNITY_EDITOR
		[SerializeField]
		List<UnityEditor.SceneAsset> sceneAssets = new List<SceneAsset>();
#endif

		[SerializeField]
#if UNITY_EDITOR
		[HideInInspector]
#endif
		List<string> scenePaths = new List<string>();


		public IEnumerable<string> Scenes
		{
			get
			{
				return scenePaths;
			}
		}



#if UNITY_EDITOR
		private void OnValidate()
		{
			if (scenePaths.Count != sceneAssets.Count)
			{
				scenePaths = new List<string>();
				foreach (var asset in sceneAssets)
				{
					scenePaths.Add(AssetDatabase.GetAssetPath(asset));
				}
			}
			else
			{
				for (int i = 0; i < sceneAssets.Count; i++)
				{
					var path = AssetDatabase.GetAssetPath(sceneAssets[i]);
					if (scenePaths[i] != path)
					{
						scenePaths[i] = path;
					}
				}
			}
		}

		[BeforeBuild]
		static void SetBundles()
		{
			Debug.Log("Before Build");
			var scenes = AssetDatabase.FindAssets($"t:{nameof(SceneRecord)}");
			foreach (var guid in scenes)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var record = AssetDatabase.LoadAssetAtPath<SceneRecord>(path);
				if (record != null)
				{
					var oldBundleName = GetAssetBundleName(record);
					var match = Regex.Match(oldBundleName, @"([\d\w]+?)_bundle");
					if (match.Success)
					{
						var newBundleName = $"{match.Groups[1].Value}_scenes_bundle";
						foreach (var sceneAsset in record.sceneAssets)
						{
							SetAssetBundleName(newBundleName, sceneAsset);
						}
					}
				}
			}
		}

		static void SetAssetBundleName(string bundleName, UnityEngine.Object obj)
		{
			var path = AssetDatabase.GetAssetPath(obj);
			if (path != null && path != "")
			{
				var import = AssetImporter.GetAtPath(path);
				import.SetAssetBundleNameAndVariant(bundleName, import.assetBundleVariant);
			}
		}

		static string GetAssetBundleName(UnityEngine.Object obj)
		{
			var path = AssetDatabase.GetAssetPath(obj);
			if (path != null && path != "")
			{
				var import = AssetImporter.GetAtPath(path);
				return import.assetBundleName;
			}
			return "";
		}

#endif
	}*/
}
