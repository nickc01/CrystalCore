using UnityEngine;

namespace WeaverCore.Utilities
{
    /// <summary>
    /// Contains utility functions for working with GameObjects
    /// </summary>
    public static class GameObjectUtilities
    {
        /// <summary>
        /// Activates a GameObject, as well as all children recursively
        /// </summary>
        /// <param name="gm">The gameObject to activate</param>
        /// <param name="active">Should the gameobject and children be active?</param>
        public static void ActivateAllChildren(this GameObject gm, bool active)
        {
            gm.SetActive(active);
            for (int i = 0; i < gm.transform.childCount; i++)
            {
                ActivateAllChildren(gm.transform.GetChild(i).gameObject, active);
            }
        }

        /// <summary>
        /// Gets the full path of the GameObject in the hierarchy.
        /// </summary>
        /// <param name="gameObject">The GameObject to get the path for.</param>
        /// <returns>The full path of the GameObject in the hierarchy.</returns>
        public static string GetFullPath(this GameObject gameObject)
        {
            if (gameObject == null)
            {
                return null;
            }

            string path = gameObject.name;
            Transform current = gameObject.transform;

            while (current.parent != null)
            {
                current = current.parent;
                path = $"{current.name}/{path}";
            }

            return path;
        }
    }
}