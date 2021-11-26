﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using WeaverCore.Attributes;
using WeaverCore.Editor.Utilities;

namespace WeaverCore.Editor
{
	public static class PlayModeInit
	{
		[OnRuntimeInit]
		static void OnGameStart()
		{
			if (GameObject.FindObjectOfType<GameManager>() == null)
			{
				GameObject.Instantiate(EditorAssets.LoadEditorAsset<GameObject>("GameManager"));
			}
		}
	}
}
