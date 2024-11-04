using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using WeaverCore.Editor.TK2D;
using WeaverCore.Utilities;

namespace WeaverCore.Editor
{
	/// <summary>
	/// Used for unpacking Tk2D Sprite and animations dumped for the WeaverCore Debug Tools
	/// </summary>
	public static class TK2DUnpacker
	{
		/// <summary>
		/// A list of recently unpacked sprites
		/// </summary>
		public static System.Collections.Generic.List<Sprite> UnpackedSprites = new System.Collections.Generic.List<Sprite>();

		/// <summary>
		/// Should all sprites be flipped during import?
		/// </summary>
		public const bool FlipTK2DSpritesVertically = false;

		/// <summary>
		/// A list of valid image extensions
		/// </summary>
		public static readonly System.Collections.Generic.List<string> ImageExtensions = new System.Collections.Generic.List<string>
		{
			".png",
			".jpg",
			".jpeg"
		};

		/// <summary>
		/// Unpacks a Tk2D animation dumped from WeaverCore Debug Tools
		/// </summary>
		public static void UnpackTK2DAnimation()
		{
			UnpackedSprites.Clear();
			var openedFile = TK2dCommon.OpenAnimationMap(out var import, out var path);
			if (!openedFile)
			{
				Debug.LogError($"Unable to open file [{path}]");
			}

			UnpackTK2DAnimation(import, path);
		}

		/// <summary>
		/// Unpacks a Tk2D animation dumped from WeaverCore Debug Tools
		/// </summary>
		/// <param name="import">The loaded animmap</param>
		/// <param name="path">The path of the loaded animmap</param>
		private static void UnpackTK2DAnimation(AnimationMapImport import, string path)
		{
			var info = new FileInfo(path);
			var directory = info.Directory.AddSlash();

            Dictionary<string, System.Collections.Generic.List<Texture2D>> importedTextures = new Dictionary<string, System.Collections.Generic.List<Texture2D>>();

			bool foundAllTextures = true;

			foreach (var collection in import.collectionTextures)
			{
				var texList = new System.Collections.Generic.List<Texture2D>();
				foreach (var name in collection.TextureNames)
				{
					if (texList.Any(t => t.name == name))
					{
						continue;
					}
					bool found = false;
					foreach (var ext in ImageExtensions)
					{
						var texFilePath = directory + name + ext;
						if (File.Exists(texFilePath) && TK2dCommon.OpenImage(texFilePath, out var tex))
						{
							texList.Add(tex);
							found = true;
							break;
						}
					}
					if (!found)
					{
						Debug.LogError($"Unable to find the texture [{name}]. Make sure it's in the same folder as the spritemap file");
						foundAllTextures = false;
					}
				}
				importedTextures.Add(collection.collection.spriteCollectionName, texList);
			}

			if (!foundAllTextures)
			{
				return;
			}


			UnboundCoroutine.Start(UnpackTK2DAnimationAsync(info.Name, import, importedTextures));
		}

		static IEnumerator UnpackTK2DAnimationAsync(string name, AnimationMapImport import, Dictionary<string, System.Collections.Generic.List<Texture2D>> importedTextures)
		{
			var fileName = name.Split('.')[0];
			var totalUnpackedSprites = new System.Collections.Generic.List<Sprite>();

            System.Collections.Generic.List<DirectoryInfo> directories = new System.Collections.Generic.List<DirectoryInfo>();
            Dictionary<string, System.Collections.Generic.List<Sprite>> ExtractedSprites = new Dictionary<string, System.Collections.Generic.List<Sprite>>();
			foreach (var texCollection in import.collectionTextures)
			{
				var dir = new DirectoryInfo($"Assets{Path.DirectorySeparatorChar}{fileName} Animations{Path.DirectorySeparatorChar}{texCollection.collection.spriteCollectionName} Unpacked");
				directories.Add(dir);
				yield return UnpackSprites(texCollection.collection.spriteCollectionName, new SpriteMapImport
				{
					collection = texCollection.collection,
					TextureNames = texCollection.TextureNames
				}, importedTextures[texCollection.collection.spriteCollectionName], dir);
				ExtractedSprites.Add(texCollection.collection.spriteCollectionName, UnpackedSprites);
				totalUnpackedSprites.AddRange(UnpackedSprites);
                UnpackedSprites = new System.Collections.Generic.List<Sprite>();
			}

			UnpackedSprites = totalUnpackedSprites;


			WeaverAnimationData data = ScriptableObject.CreateInstance<WeaverAnimationData>();

			var animationData = import.animation;

			foreach (var clipRaw in animationData.clips)
			{
				if (string.IsNullOrEmpty(clipRaw.name))
				{
					continue;
				}
				var clip = new WeaverAnimationData.Clip
				{
					FPS = clipRaw.fps,
					LoopStart = clipRaw.loopStart,
					Name = clipRaw.name,
				};
				switch (clipRaw.wrapMode)
				{
					case tk2dSpriteAnimationClip.WrapMode.Loop:
						clip.WrapMode = WeaverAnimationData.WrapMode.Loop;
						break;
					case tk2dSpriteAnimationClip.WrapMode.LoopSection:
						clip.WrapMode = WeaverAnimationData.WrapMode.LoopSection;
						break;
					case tk2dSpriteAnimationClip.WrapMode.Once:
						clip.WrapMode = WeaverAnimationData.WrapMode.Once;
						break;
					case tk2dSpriteAnimationClip.WrapMode.PingPong:
						clip.WrapMode = WeaverAnimationData.WrapMode.PingPong;
						break;
					case tk2dSpriteAnimationClip.WrapMode.RandomFrame:
						clip.WrapMode = WeaverAnimationData.WrapMode.RandomFrame;
						break;
					case tk2dSpriteAnimationClip.WrapMode.RandomLoop:
						clip.WrapMode = WeaverAnimationData.WrapMode.RandomLoop;
						break;
					case tk2dSpriteAnimationClip.WrapMode.Single:
						clip.WrapMode = WeaverAnimationData.WrapMode.SingleFrame;
						break;
				}

				foreach (var frameRaw in clipRaw.frames)
				{
					clip.AddFrame(ExtractedSprites[frameRaw.spriteCollection.spriteCollectionName][frameRaw.spriteId]);
				}

				data.AddClip(clip);
			}

			AssetDatabase.CreateAsset(data, $"Assets{Path.DirectorySeparatorChar}{fileName} Animations{Path.DirectorySeparatorChar}{fileName}.asset");
		}

		/// <summary>
		/// Unpacks a Tk2D sprite dumped from WeaverCore Debug Tools
		/// </summary>
		public static void UnpackTK2DSprites()
		{
			UnpackedSprites.Clear();
			var openedFile = TK2dCommon.OpenSpriteMap(out var import, out var path);
			if (!openedFile)
			{
				Debug.LogError($"Unable to open file [{path}]");
			}

			UnpackTK2DSprites(import, path);
		}

		/// <summary>
		/// Unpacks a Tk2D sprite dumped from WeaverCore Debug Tools
		/// </summary>
		/// <param name="import">The imported spritemap</param>
		/// <param name="path">The path fo the imported spritemap</param>
		private static void UnpackTK2DSprites(SpriteMapImport import, string path)
		{
			var info = new FileInfo(path);
			var directory = info.Directory.AddSlash();

            System.Collections.Generic.List<Texture2D> importedTextures = new System.Collections.Generic.List<Texture2D>();

			bool foundAllTextures = true;

			foreach (var name in import.TextureNames)
			{
				bool found = false;
				foreach (var ext in ImageExtensions)
				{
					var texFilePath = directory + name + ext;
					if (File.Exists(texFilePath) && TK2dCommon.OpenImage(texFilePath, out var tex))
					{
						importedTextures.Add(tex);
						found = true;
						break;
					}
				}
				if (!found)
				{
					Debug.LogError($"Unable to find the texture [{name}]. Make sure it's in the same folder as the spritemap file");
					foundAllTextures = false;
				}
			}

			if (!foundAllTextures)
			{
				return;
			}


			UnboundCoroutine.Start(UnpackSprites(info.GetFileName(), import, importedTextures, new DirectoryInfo($"Assets{Path.DirectorySeparatorChar}{info.GetFileName()} Unpacked")));
		}

		/// <summary>
		/// Takes the UV coordinates of an image and uses it to tell of the image is rotated or not
		/// </summary>
		static bool AreUVsRotated(Vector2[] uvs)
		{
			return uvs[0].x == uvs[1].x && uvs[1].y == uvs[3].y;
		}

		/// <summary>
		/// Gets the color at the specified pixel coordinates of a texture
		/// </summary>
		static Color GetTexColor(int x, int y, Texture2D source)
		{
			if (x < 0 || x >= source.width || y < 0 || y >= source.height)
			{
				return default;
			}
			else
			{
				return source.GetPixel(x, y);
			}
		}

		/// <summary>
		/// Converts an array of texture UVs into a rect
		/// </summary>
		/// <param name="uvs">The UVs of the texture</param>
		static Rect UVsToRegion(IEnumerable<Vector2> uvs)
		{
			bool ranOnce = false;
			Vector2 min = default;
			Vector2 max = default;

			foreach (var uv in uvs)
			{
				if (!ranOnce)
				{
					ranOnce = true;
					min = uv;
					max = uv;
				}
				else
				{
					if (uv.x < min.x)
					{
						min.x = uv.x;
					}
					if (uv.x > max.x)
					{
						max.x = uv.x;
					}
					if (uv.y < min.y)
					{
						min.y = uv.y;
					}
					if (uv.y > max.y)
					{
						max.y = uv.y;
					}
				}
			}
			return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
		}

		/// <summary>
		/// Extracts a new texture from the specified region of an old texture
		/// </summary>
		/// <param name="region">The region to extract from the old texture</param>
		/// <param name="source">The texture to extract from</param>
		static Texture2D ExtractRegion(Rect region, Texture2D source)
		{
			Texture2D result = new Texture2D(Mathf.RoundToInt(region.width), Mathf.RoundToInt(region.height), source.format,false);
			for (int x = Mathf.RoundToInt(region.xMin); x < Mathf.RoundToInt(region.xMax); x++)
			{
				for (int y = Mathf.RoundToInt(region.yMin); y < Mathf.RoundToInt(region.yMax); y++)
				{
					int relX = x - Mathf.RoundToInt(region.xMin);
					int relY = y - Mathf.RoundToInt(region.yMin);
					result.SetPixel(relX, relY, GetTexColor(x, y, source));
				}
			}
			result.Apply();
			return result;
		}

		/// <summary>
		/// Given a sprite definition, uses it to extract it out of an old texture
		/// </summary>
		/// <param name="source">The old texture to extract from</param>
		/// <param name="sprite">The sprite definition that used to define where the sprite is located on the old texture</param>
		static Texture2D ExtractSpriteFromTex(Texture2D source, tk2dSpriteDefinition sprite)
		{
			Vector2 vector2;
			Vector2 vector3;

			bool rotated = AreUVsRotated(sprite.uvs);

			if (rotated)
			{
				vector2 = new Vector2(sprite.uvs[0].x,sprite.uvs[1].y);
				vector3 = new Vector2(sprite.uvs[2].x,sprite.uvs[2].y);
			}
			else
			{
				vector2 = new Vector2(sprite.uvs[0].x, sprite.uvs[0].y);
				vector3 = new Vector2(sprite.uvs[3].x, sprite.uvs[3].y);
			}

			float x = source.width;
			float y = source.height;

			var multipliedUVs = sprite.uvs.Select(u => new Vector2(Mathf.RoundToInt(u.x * x), Mathf.RoundToInt(u.y * y)));

			var spriteTexture = ExtractRegion(UVsToRegion(multipliedUVs), source);
			spriteTexture.name = sprite.name;

			if (rotated)
			{
				TextureUtilities.Rotate(spriteTexture, WeaverCore.Enums.RotationType.Right);
			}

			if (sprite.flipped == FlipMode.Tk2d || sprite.flipped == FlipMode.TPackerCW)
			{
				TextureUtilities.FlipHorizontally(spriteTexture);
			}

			return spriteTexture;
		}

		/// <summary>
		/// Unpacks all sprites from a spritemap
		/// </summary>
		/// <param name="name">The name of the spritemap</param>
		/// <param name="spriteMap">The spritemap itself</param>
		/// <param name="importedTextures">A list of imported textures to extract the sprites from</param>
		/// <param name="outputDir">The output directory to put the sprites in</param>
		static IEnumerator UnpackSprites(string name, SpriteMapImport spriteMap, System.Collections.Generic.List<Texture2D> importedTextures, DirectoryInfo outputDir)
		{
			outputDir.Create();
			var relativeDir = PathUtilities.ConvertToProjectPath(outputDir.FullName);

			if (FlipTK2DSpritesVertically)
			{
				foreach (var tex in importedTextures)
				{
					TextureUtilities.FlipVertically(tex);
				}
			}

			EditorUtility.DisplayProgressBar("Unpacking Sprites", "", 0f);
            System.Collections.Generic.List<string> spritePaths = new System.Collections.Generic.List<string>();
            System.Collections.Generic.List<Texture2D> spriteTextures = new System.Collections.Generic.List<Texture2D>();
			try
			{
				AssetDatabase.StartAssetEditing();
				for (int i = 0; i < spriteMap.collection.spriteDefinitions.GetLength(0); i++)
				{
					var map = spriteMap.collection.spriteDefinitions[i];
					if (map.name == "")
					{
						spritePaths.Add(null);
						spriteTextures.Add(null);
						continue;
					}
					EditorUtility.DisplayProgressBar("Unpacking Sprites", map.name, (float)i / spriteMap.collection.spriteDefinitions.GetLength(0));

					var texture = importedTextures[map.materialId];
					var spriteTexture = ExtractSpriteFromTex(texture, map);
					var spritePath = $"{relativeDir}{Path.DirectorySeparatorChar}{map.name}.png";
					spritePaths.Add(spritePath);
					spriteTextures.Add(spriteTexture);
					File.WriteAllBytes(spritePath, spriteTexture.EncodeToPNG());
					AssetDatabase.ImportAsset(spritePath);
				}
			}
			finally
			{
				AssetDatabase.StopAssetEditing();
				EditorUtility.ClearProgressBar();
			}
			yield return PostUnpackSprites(name, spriteMap, importedTextures, spritePaths, spriteTextures, outputDir);
		}

		static float UnclampedLerp(float A, float B, float T)
		{
			return A + ((B - A) * T);
		}

		static float UnclampedInverseLerp(float A, float B, float C)
		{
			return (C - A) / (B - A);
		}

		/// <summary>
		/// Called when the sprites have all been unpacked. Used to do some extra post processing
		/// </summary>
		/// <param name="name">The name of the spritemap</param>
		/// <param name="spriteMap">The spritemap itself</param>
		/// <param name="importedTextures">A list of all imported textures to extract the sprites from</param>
		/// <param name="spritePaths">The paths of all the sprites extracted from the spritemap</param>
		/// <param name="spriteTextures">A list of textures extracted from the spritemap</param>
		/// <param name="outputDir">The output location where all the extracted sprites are located</param>
		/// <returns></returns>
		static IEnumerator PostUnpackSprites(string name, SpriteMapImport spriteMap, System.Collections.Generic.List<Texture2D> importedTextures, System.Collections.Generic.List<string> spritePaths, System.Collections.Generic.List<Texture2D> spriteTextures, DirectoryInfo outputDir)
		{
			var relativeDir = PathUtilities.ConvertToProjectPath(outputDir.FullName);
			yield return null;

			AssetDatabase.StartAssetEditing();

			try
			{
				for (int i = 0; i < spritePaths.Count; i++)
				{
					var path = spritePaths[i];
					if (path == null)
					{
						continue;
					}
					var info = new FileInfo(path);
					var texture = spriteTextures[i];

					var definition = spriteMap.collection.spriteDefinitions.First(d => $"{d.name}{info.Extension}" == info.Name);
					var relativePath = path;
					var importer = (TextureImporter)AssetImporter.GetAtPath(relativePath);
					var settings = new TextureImporterSettings();
					importer.ReadTextureSettings(settings);

					var blPos = definition.positions[0];
					var trPos = definition.positions[3];

					var horizontalppu = texture.width / (((double)trPos.x) - blPos.x);
					var verticalppu = texture.height / ((double)trPos.y - blPos.y);

					var averagePPU = (horizontalppu + verticalppu) / 2.0;
					settings.spriteAlignment = (int)SpriteAlignment.Custom;
					settings.spritePixelsPerUnit = (float)averagePPU;
					settings.filterMode = FilterMode.Bilinear;
					settings.spritePivot = new Vector2(UnclampedInverseLerp(blPos.x,trPos.x,0f),UnclampedInverseLerp(blPos.y,trPos.y,0f));

					if (UnpackTK2DWindow.Settings.UnpackMode == UnpackTK2DWindow.UnpackMode.ToSprite)
					{
						settings.readable = true;
						importer.isReadable = true;
					}

					importer.SetTextureSettings(settings);
					importer.SaveAndReimport();
				}
			}
			finally
			{
				AssetDatabase.StopAssetEditing();
			}

			yield return null;

			if (UnpackTK2DWindow.Settings.UnpackMode == UnpackTK2DWindow.UnpackMode.ToTextures)
			{
				foreach (var spritePath in spritePaths)
				{
					if (spritePath == null)
					{
						UnpackedSprites.Add(null);
					}
					else
					{
						UnpackedSprites.AddRange(AssetDatabase.LoadAllAssetsAtPath(spritePath).OfType<Sprite>());
					}
				}
			}
			else if (UnpackTK2DWindow.Settings.UnpackMode == UnpackTK2DWindow.UnpackMode.ToSprite)
			{
				var outputPath = $"{relativeDir}{Path.DirectorySeparatorChar}{name}.png";
				var sprites = spritePaths.Select(s => s == null ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(s)).ToList();

				yield return TexturePacker.PackTextures(new FileInfo(outputPath), sprites, deleteOld: true);

				var finalSprites = AssetDatabase.LoadAllAssetsAtPath(outputPath).OfType<Sprite>().ToList();

				var orderedSprites = new Sprite[spritePaths.Count];
				for (int i = 0; i < finalSprites.Count; i++)
				{
					var index = spriteTextures.FindIndex(t => t != null && t.name == finalSprites[i].name);
					orderedSprites[index] = finalSprites[i];
				}

				UnpackedSprites.AddRange(orderedSprites);
			}
		}
	}
}
