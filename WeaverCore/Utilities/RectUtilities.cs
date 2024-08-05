﻿using UnityEngine;

namespace WeaverCore.Utilities
{
    /// <summary>
    /// Contains some utility functions related to rects
    /// </summary>
    public static class RectUtilities
	{
		/// <summary>
		/// Clamps a point within a rect
		/// </summary>
		/// <param name="rect">The rect to clamp the point within</param>
		/// <param name="point">The point to clamp</param>
		/// <returns>Returns the point clamped within the rect</returns>
		public static Vector2 ClampWithin(this Rect rect, Vector2 point)
		{
			return new Vector2(Mathf.Clamp(point.x, rect.xMin, rect.xMax), Mathf.Clamp(point.y, rect.yMin, rect.yMax));
		}

		/// <summary>
		/// Clamps a point within a rect
		/// </summary>
		/// <param name="rect">The rect to clamp the point within</param>
		/// <param name="point">The point to clamp</param>
		/// <returns>Returns the point clamped within the rect</returns>
		public static Vector3 ClampWithin(this Rect rect, Vector3 point)
		{
			return new Vector3(Mathf.Clamp(point.x, rect.xMin, rect.xMax), Mathf.Clamp(point.y, rect.yMin, rect.yMax), point.z);
		}

		/// <summary>
		/// Clamps a rect within a rect
		/// </summary>
		/// <param name="rect">The rect to clamp the point within</param>
		/// <param name="rectToClamp">The rect to clamp</param>
		/// <returns>Returns the rect clamped within the rect</returns>
		public static Rect ClampWithin(this Rect rect, Rect rectToClamp)
		{
			var min = rectToClamp.min;
			var max = rectToClamp.max;

			if (min.x < rect.min.x)
			{
				min.x = rect.min.x;
			}

			if (min.y < rect.min.y)
			{
				min.y = rect.min.y;
			}

			if (max.x > rect.max.x)
			{
				max.x = rect.max.x;
			}

			if (max.y > rect.max.y)
			{
				max.y = rect.max.y;
			}

			return new Rect{
				min = min,
				max = max
			};
		}

		/// <summary>
		/// Gets a random point within a rect
		/// </summary>
		/// <param name="rect">The rect to get a random point within</param>
		/// <returns>Returns a random point within the rect</returns>
		public static Vector2 RandomPointWithin(this Rect rect)
		{
			return new Vector2(UnityEngine.Random.Range(rect.xMin, rect.xMax), UnityEngine.Random.Range(rect.yMin, rect.yMax));
		}
	}
}
