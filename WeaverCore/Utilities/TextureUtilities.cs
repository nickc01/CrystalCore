﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using WeaverCore.Enums;
using WeaverCore.Utilities;


namespace WeaverCore.Utilities
{
    public static class TextureUtilities
	{
        /*public static bool MakeTextureReadable(this Texture2D tex)
		{

		}*/

        public static Texture2D Clone(this Texture2D tex)
		{
            var rTex = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(tex, rTex);
            var sampleTexture = rTex.ToTexture2D();
            RenderTexture.ReleaseTemporary(rTex);
            return sampleTexture;
        }

        public static Texture2D ToTexture2D(this RenderTexture rTex)
        {
            Texture2D tex = new Texture2D(rTex.width, rTex.height, TextureFormat.ARGB32, false);
            // ReadPixels looks at the active RenderTexture.
            RenderTexture.active = rTex;
            tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
            tex.Apply();
            return tex;
        }

        static Vector2 RotateVector(Vector2 vector, RotationType rotation)
        {
            return RotateVectorAroundPoint(vector, rotation, default(Vector2));
        }

        static Vector2 RotateVectorAroundPoint(Vector2 vector, RotationType rotation, Vector2 pointOfRotation)
        {
            Matrix4x4 rotationMatrix = Matrix4x4.Translate(new Vector3(pointOfRotation.x, pointOfRotation.y));
            rotationMatrix *= Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, (int)rotation));
            rotationMatrix *= Matrix4x4.Translate(new Vector3(-pointOfRotation.x, -pointOfRotation.y));

            return rotationMatrix.MultiplyPoint3x4(vector);
        }


        public static void Rotate(this Texture2D texture, RotationType rotation)
        {
            var destWidth = texture.width;
            var destHeight = texture.height;

            if (rotation == RotationType.Left || rotation == RotationType.Right)
            {
                destWidth = texture.height;
                destHeight = texture.width;
            }

            var pixels = texture.GetPixels();
            var newPixels = new Color[pixels.GetLength(0)];

            for (int x = 0; x < texture.width; x++)
            {
                for (int y = 0; y < texture.height; y++)
                {
                    

                    var newPoint = RotateVectorAroundPoint(new Vector2(x, y), rotation, new Vector2((texture.width - 1) / 2f,(texture.height - 1) / 2f));

                    newPoint.x -= ((texture.width - 1) / 2f);
                    newPoint.y -= ((texture.height - 1) / 2f);

                    newPoint.x += ((destWidth - 1) / 2f);
                    newPoint.y += ((destHeight - 1) / 2f);



                    int newX = Mathf.RoundToInt(newPoint.x);
                    int newY = Mathf.RoundToInt(newPoint.y);

                    newPixels[newX + (destWidth * newY)] = pixels[x + (texture.width * y)];
                }
            }
            texture.Resize(destWidth, destHeight);

            texture.SetPixels(newPixels);

            texture.Apply();
        }

        public static void FlipHorizontally(Texture2D texture)
        {
            var width = texture.width;
            var height = texture.height;

            Color[,] newMatrix = new Color[width, height];


            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    newMatrix[x, y] = texture.GetPixel(width - 1 - x, y);
                }
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    texture.SetPixel(x, y, newMatrix[x, y]);
                }
            }
        }

        public static void FlipVertically(Texture2D texture)
        {
            var width = texture.width;
            var height = texture.height;

            Color[,] newMatrix = new Color[width, height];


            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    newMatrix[x, y] = texture.GetPixel(x, height - 1 - y);
                }
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    texture.SetPixel(x, y, newMatrix[x, y]);
                }
            }
        }

        public static void FlipDiagonally(Texture2D texture)
        {
            var width = texture.width;
            var height = texture.height;

            Color[,] newMatrix = new Color[width, height];


            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    newMatrix[x, y] = texture.GetPixel(width - 1 - x, height - 1 - y);
                }
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    texture.SetPixel(x,y,newMatrix[x, y]);
                }
            }
        }
    }
}
