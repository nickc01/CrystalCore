using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using WeaverCore.Editor.Utilities;
using WeaverCore.Utilities;
using System.Linq;
using System.Collections;

namespace WeaverCore.Editor.Menu_Items
{
	/// <summary>
	/// Contains all menu items for inserting objects into a scene
	/// </summary>
    public static class InsertMenu
	{
        public static GameObject InsertObject(string prefabName)
        {
            var prefab = WeaverAssets.LoadWeaverAsset<GameObject>(prefabName);
            if (prefab == null)
            {
                prefab = EditorAssets.LoadEditorAsset<GameObject>(prefabName);
            }

            if (prefab == null)
            {
                return null;
            }

            var instance = GameObject.Instantiate(prefab, null);

            instance.name = prefab.name;

            Vector3 oldPos = instance.transform.position;

            var sceneView = ((IEnumerable)SceneView.sceneViews).OfType<SceneView>().FirstOrDefault();

            if (sceneView != null)
            {
                var pivot = sceneView.pivot;

                oldPos.x = pivot.x;
                oldPos.y = pivot.y;
            }
            else
            {
                oldPos.x = 0;
                oldPos.y = 0;
            }

            instance.transform.position = oldPos;

            if (!Application.isPlaying)
            {
                Undo.RegisterCreatedObjectUndo(instance.gameObject, $"Create {prefabName}");
                Selection.activeGameObject = instance.gameObject;
                EditorSceneManager.MarkSceneDirty(instance.scene);
            }

            return instance;
        }

		public static GameObject InsertObject(string prefabName, Vector3 position, Quaternion rotation)
        {
			var prefab = WeaverAssets.LoadWeaverAsset<GameObject>(prefabName);
			if (prefab == null)
			{
				prefab = EditorAssets.LoadEditorAsset<GameObject>(prefabName);
			}

			if (prefab == null)
			{
				return null;
			}

			var instance = GameObject.Instantiate(prefab, null);

			instance.name = prefab.name;

			instance.transform.position = position;
			instance.transform.rotation = rotation;

			if (!Application.isPlaying)
			{
				Undo.RegisterCreatedObjectUndo(instance.gameObject, $"Create {prefabName}");
				Selection.activeGameObject = instance.gameObject;
				EditorSceneManager.MarkSceneDirty(instance.scene);
			}

			return instance;
		}

		[MenuItem("WeaverCore/Insert/Demo Player")]
		public static void InsertDemoPlayer()
		{
			InsertObject("Demo Player");
		}

		[MenuItem("WeaverCore/Insert/Blur Plane")]
		public static void InsertBlurPlane()
		{
			InsertObject("BlurPlane");
		}

		[MenuItem("WeaverCore/Insert/Game Cameras")]
		public static void InsertGameCameras()
		{
			InsertObject("Game Cameras");
		}

		[MenuItem("WeaverCore/Insert/Game Manager")]
		public static void InsertGameManager()
		{
			InsertObject("GameManager");
		}

		[MenuItem("WeaverCore/Insert/Weaver NPC")]
		public static void InsertWeaverNPC()
		{
			InsertObject("Weaver NPC");
		}

		[MenuItem("WeaverCore/Insert/Weaver Bench")]
		public static void InsertWeaverBench()
		{
			InsertObject("Weaver Bench");
		}

		[MenuItem("WeaverCore/Insert/Weaver Scene Manager")]
		public static void InsertWeaverSceneManager()
		{
			InsertObject("Weaver Scene Manager");
		}

		[MenuItem("WeaverCore/Insert/Transition Point")]
		public static void InsertWeaverTransitionPoint()
		{
			InsertObject("Transition Point Template");
		}

		[MenuItem("WeaverCore/Insert/Dreamnail Warp Object")]
		public static void InsertDreamWarpObject()
		{
			InsertObject("Dream Warp Object");
		}

		[MenuItem("WeaverCore/Insert/Dream Fall Catcher")]
		public static void InsertDreamFallCatcher()
		{
			InsertObject("Dream Fall Catcher");
		}

		[MenuItem("WeaverCore/Insert/Template Enemy")]
		public static void InsertTemplateEnemy()
		{
			InsertObject("Template Enemy");
		}

        [MenuItem("WeaverCore/Insert/Camera Lock Region")]
        public static void InsertCameraLockRegion()
        {
            InsertObject("Camera Lock Region");
        }

        [MenuItem("WeaverCore/Insert/Health Cocoon")]
        public static void InsertHealthCocoon()
        {
            InsertObject("Health Cocoon Pack");
        }

        [MenuItem("WeaverCore/Insert/Dream Return Warp")]
        public static void InsertDreamReturnWarp()
        {
            InsertObject("Dream Return Warp");
        }

        [MenuItem("WeaverCore/Insert/Water")]
        public static void InsertWater()
        {
            InsertObject("Water Pack");
        }

        [MenuItem("WeaverCore/Insert/Acid")]
        public static void InsertAcid()
        {
            InsertObject("Acid Pack");
        }

        [MenuItem("WeaverCore/Insert/Chest")]
        public static void InsertChest()
        {
            InsertObject("Chest");
        }

        [MenuItem("WeaverCore/Insert/Geo Rock")]
        public static void InsertGeoRock()
        {
            InsertObject("Geo Rock");
        }

        [MenuItem("WeaverCore/Insert/Shade Gate")]
        public static void InsertShadeGate()
        {
            InsertObject("Shade Gate");
        }

        [MenuItem("WeaverCore/Insert/Soul Totem")]
        public static void InsertSoulTotem()
        {
            InsertObject("Soul Totem");
        }

		[MenuItem("WeaverCore/Insert/Cinematic Player")]
        public static void InsertCinematicPlayer()
        {
            InsertObject("Weaver Cinematic Player");
        }

        [MenuItem("WeaverCore/Insert/In-Game Cinematic Player")]
        public static void InsertInGameCinematicPlayer()
        {
            InsertObject("Weaver In-Game Cinematic Player");
        }

        //Godhome Statue Template

        [MenuItem("WeaverCore/Insert/Godhome Statue Template")]
        public static void InsertGodhomeStatueTemplate()
        {
            InsertObject("Godhome Statue Template");
        }

        [MenuItem("WeaverCore/Insert/Boss Scene Controller")]
        public static void InsertBossSceneController()
        {
            InsertObject("Boss Scene Controller", default, default);
        }

        [MenuItem("WeaverCore/Insert/Colosseum/Cage Small")]
        public static void InsertColosseumCageSmall()
        {
            InsertObject("Colosseum Cage Small");
        }

        [MenuItem("WeaverCore/Insert/Colosseum/Cage Large")]
        public static void InsertColosseumCageLarge()
        {
            InsertObject("Colosseum Cage Large");
        }

        [MenuItem("WeaverCore/Insert/Colosseum/Platform")]
        public static void InsertColosseumPlatform()
        {
            InsertObject("Colosseum Platform");
        }

        [MenuItem("WeaverCore/Insert/Colosseum/Simple Gate")]
        public static void InsertSimpleColosseumGate()
        {
            InsertObject("Simple Colosseum Gate");
        }

        [MenuItem("WeaverCore/Insert/Colosseum/Room Manager Template")]
        public static void InsertColosseumRoomManagerTemplate()
        {
            InsertObject("Colosseum Room Manager Template");
        }

        [MenuItem("WeaverCore/Insert/Breakable Wall")]
        public static void InsertBreakableWall()
        {
            InsertObject("Breakable Wall");
        }

        [MenuItem("WeaverCore/Insert/Colosseum/Wall Left")]
        public static void InsertColosseumWallL()
        {
            InsertObject("Colosseum Wall L");
        }

        [MenuItem("WeaverCore/Insert/Colosseum/Wall Right")]
        public static void InsertColosseumWallR()
        {
            InsertObject("Colosseum Wall R");
        }

        [MenuItem("WeaverCore/Insert/Colosseum/Wall Ceiling")]
        public static void InsertColosseumWallC()
        {
            InsertObject("Colosseum Wall C");
        }


        [MenuItem("WeaverCore/Insert/Weaver Canvas")]
		public static void InsertWeaverCanvas()
		{
			if (GameObject.FindObjectOfType<EventSystem>() == null)
			{
				var eventObject = new GameObject("Event System");
				if (Application.isPlaying)
				{
					GameObject.DontDestroyOnLoad(eventObject);
				}
				eventObject.AddComponent<EventSystem>();
				eventObject.AddComponent<StandaloneInputModule>();
			}

			InsertObject("Weaver Canvas");
		}
	}
}
