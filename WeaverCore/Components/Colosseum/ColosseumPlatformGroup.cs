using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace WeaverCore.Components.Colosseum
{
    public class ColosseumPlatformGroup : MonoBehaviour, IColosseumIdentifier
    {
        [Tooltip("Delay before activating platforms in the group.")]
        public float PreDelay = 0f;

        [SerializeField, Tooltip("Show labels for platforms in the editor.")]
        private bool showLabels = true;

        [NonSerialized]
        private List<ColosseumPlatform> _platformCache;

        public List<ColosseumPlatform> Platforms
        {
            get
            {
                if (_platformCache == null)
                {
                    _platformCache = new List<ColosseumPlatform>();
                    GetComponentsInChildren(_platformCache);
                }
                return _platformCache;
            }
        }

        string IColosseumIdentifier.Identifier => "Platform Groups";

        Color IColosseumIdentifier.Color => new Color(0.0f, 0.5f, 0.5f);

        bool IColosseumIdentifier.ShowShortcut => true;

        public void RaisePlatforms() => RaisePlatformsRoutine(this);

        public void LowerPlatforms() => LowerPlatformsRoutine(this);

        public static Func<bool> RaisePlatformsRoutine(ColosseumPlatformGroup group)
        {
            if (group == null)
            {
                return () => true;
            }

            group.gameObject.SetActive(true);

            List<Func<bool>> completedPlatforms = new List<Func<bool>>();

            foreach (var platform in group.Platforms)
            {
                platform.ExpandWithDelay(group.PreDelay);
                completedPlatforms.Add(() => !platform.Changing);
            }

            return () => completedPlatforms.All(c => c());
        }

        public static Func<bool> LowerPlatformsRoutine(ColosseumPlatformGroup group)
        {
            if (group == null)
            {
                return () => true;
            }

            List<Func<bool>> completedPlatforms = new List<Func<bool>>();

            foreach (var platform in group.Platforms)
            {
                platform.RetractWithDelay(group.PreDelay);
                completedPlatforms.Add(() => !platform.Changing);
            }

            return () => completedPlatforms.All(c => c());
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!showLabels || Application.isPlaying)
            {
                return;
            }

            if (Platforms == null || Platforms.Count == 0)
                return;

            // Calculate the bounding box for the group
            Bounds groupBounds = new Bounds(Platforms[0].transform.position, Vector3.zero);
            foreach (var platform in Platforms)
            {
                Renderer renderer = platform.GetComponent<Renderer>();
                if (renderer != null)
                {
                    groupBounds.Encapsulate(renderer.bounds);
                }
                else
                {
                    groupBounds.Encapsulate(platform.transform.position);
                }
            }

            Color color = GetColorFromString(gameObject.name);

            // Set the color for Handles
            Handles.color = color;

            // Calculate the corners of the bounding box
            Vector3 center = groupBounds.center;
            Vector3 extents = groupBounds.extents;

            Vector3[] corners = new Vector3[8];
            corners[0] = center + new Vector3(-extents.x, -extents.y, -extents.z);
            corners[1] = center + new Vector3(extents.x, -extents.y, -extents.z);
            corners[2] = center + new Vector3(extents.x, -extents.y, extents.z);
            corners[3] = center + new Vector3(-extents.x, -extents.y, extents.z);
            corners[4] = center + new Vector3(-extents.x, extents.y, -extents.z);
            corners[5] = center + new Vector3(extents.x, extents.y, -extents.z);
            corners[6] = center + new Vector3(extents.x, extents.y, extents.z);
            corners[7] = center + new Vector3(-extents.x, extents.y, extents.z);

            // Line thickness
            float lineThickness = 4f;

            // Draw bottom face
            Handles.DrawAAPolyLine(lineThickness, new Vector3[] { corners[0], corners[1], corners[2], corners[3], corners[0] });
            // Draw top face
            Handles.DrawAAPolyLine(lineThickness, new Vector3[] { corners[4], corners[5], corners[6], corners[7], corners[4] });
            // Draw vertical edges
            Handles.DrawAAPolyLine(lineThickness, corners[0], corners[4]);
            Handles.DrawAAPolyLine(lineThickness, corners[1], corners[5]);
            Handles.DrawAAPolyLine(lineThickness, corners[2], corners[6]);
            Handles.DrawAAPolyLine(lineThickness, corners[3], corners[7]);

            // Display group name
            DrawLabelWithOutline(groupBounds.center, gameObject.name, Color.white, Color.black);
        }

        private void DrawLabelWithOutline(Vector3 position, string text, Color textColor, Color outlineColor)
        {
            GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = outlineColor }
            };

            // Calculate a small offset based on handle size for consistent appearance
            float offset = HandleUtility.GetHandleSize(position) * 0.04f;

            // Offsets for the outline (8 directions)
            Vector3[] offsets = new Vector3[]
            {
                new Vector3(-offset, -offset, 0),
                new Vector3(-offset, offset, 0),
                new Vector3(offset, -offset, 0),
                new Vector3(offset, offset, 0),
                new Vector3(0, -offset, 0),
                new Vector3(-offset, 0, 0),
                new Vector3(offset, 0, 0),
                new Vector3(0, offset, 0)
            };

            // Draw the outline by drawing the text in black at each offset position
            foreach (var ofs in offsets)
            {
                Handles.Label(position + ofs, text, style);
            }

            // Draw the main text in the specified color
            style.normal.textColor = textColor;
            Handles.Label(position, text, style);
        }
#endif

        private Color GetColorFromString(string str)
        {
            int hash = str.GetHashCode();
            float h = Mathf.Abs(((hash % 1000) / 1000f) % 1f);
            return Color.HSVToRGB(h, 0.7f, 1f);
        }
    }
}
