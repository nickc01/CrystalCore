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
    public class ColosseumSpikeGroup : MonoBehaviour, IColosseumIdentifier
    {
        [NonSerialized]
        ColosseumRoomManager _roomManager = null;
        public ColosseumRoomManager RoomManager => _roomManager ??= GetComponentInParent<ColosseumRoomManager>();

        public enum SpikeMode
        {
            AllAtOnce,
            StaggeredDynamic,
            StaggeredLeftToRight,
            StaggeredRightToLeft,
            StaggeredSidesToCenter,
            StaggeredCenterToSides
        }

        [Tooltip("Delay before activating spikes in the group.")]
        public float PreDelay = 0f;

        [Tooltip("Duration of the antic phase before spikes expand.")]
        public float AnticDuration = -1;

        [Tooltip("Mode of spike activation (e.g., all at once, staggered).")]
        public SpikeMode Mode = SpikeMode.AllAtOnce;

        [Tooltip("Delay between staggered spike activations.")]
        public float StaggeredDelay = 0.15f;

        [SerializeField]
        [Tooltip("If set to true, the spike group will repeatedly enable and disable itself")]
        bool debugTesting = false;

        [NonSerialized]
        private List<ColosseumSpike> _spikeCache;

        public List<ColosseumSpike> Spikes
        {
            get
            {
                if (_spikeCache == null)
                {
                    _spikeCache = new List<ColosseumSpike>();
                    GetComponentsInChildren(true, _spikeCache);
                    _spikeCache.RemoveAll(s => !s.gameObject.activeSelf || !s.enabled);
                    //_spikeCache = new List<ColosseumSpike>();
                    //GetComponentsInChildren(_spikeCache, true);
                }
                return _spikeCache;
            }
        }

        string IColosseumIdentifier.Identifier => "Spike Groups";

        Color IColosseumIdentifier.Color => new Color(0.5f, 0.0f, 1.0f);

        bool IColosseumIdentifier.ShowShortcut => true;

        public void RaiseSpikes() => RaiseSpikesRoutine(this);

        public void RetractSpikes() => RetractSpikesRoutine(this);

        public void RaiseSpikesByType(int spikeMode) 
        {
            if (spikeMode < 0 || spikeMode > 5)
            {
                throw new Exception($"Error: {spikeMode} isn't a valid spike mode");
            }

            RaiseSpikesRoutine(this, (SpikeMode)spikeMode);
            /*var values = Enum.GetValues(typeof(SpikeMode));

            foreach (var val in values)
            {
                if ((int)val == spikeMode)
                {
                    RaiseSpikesRoutine(this, (SpikeMode)spikeMode);
                }
            }

            throw new Exception($"Error: {spikeMode} isn't a valid spike mode");*/
        }

        public void RetractSpikesByType(int spikeMode)
        {
            if (spikeMode < 0 || spikeMode > 5)
            {
                throw new Exception($"Error: {spikeMode} isn't a valid spike mode");
            }

            RetractSpikesRoutine(this, (SpikeMode)spikeMode);
            /*var values = Enum.GetValues(typeof(SpikeMode));

            foreach (var val in values)
            {
                if ((int)val == spikeMode)
                {
                    RetractSpikesRoutine(this, (SpikeMode)spikeMode);
                }
            }

            throw new Exception($"Error: {spikeMode} isn't a valid spike mode");*/
        }

        private void Awake() 
        {
            if (debugTesting)
            {
                StartCoroutine(DebugRoutine());
            }
        }

        IEnumerator DebugRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);
                var extendFunc = RaiseSpikesRoutine(this);
                yield return new WaitUntil(extendFunc);

                yield return new WaitForSeconds(1f);
                var retractFunc = RetractSpikesRoutine(this);
                yield return new WaitUntil(retractFunc);
            }
        }

        public static Func<bool> RetractSpikesRoutine(ColosseumSpikeGroup group, SpikeMode? spikeModeOverride = null)
        {
            if (group == null)
            {
                return () => true;
            }

            SpikeMode spikeMode = spikeModeOverride ?? group.Mode;

            List<Func<bool>> completedSpikes = new List<Func<bool>>();

            if (spikeMode == SpikeMode.AllAtOnce)
            {
                foreach (var spike in group.Spikes)
                {
                    completedSpikes.Add(spike.RetractAndWait(group.PreDelay, spike == group.Spikes[0] ? 1f : 0f));
                }
            }
            else if (spikeMode == SpikeMode.StaggeredDynamic)
            {
                List<ColosseumSpike> leftToRightSpikes = group.Spikes.OrderBy(s => s.transform.position.x).ToList();
                List<ColosseumSpike> orderedSpikes = group.Spikes.OrderBy(s => Mathf.Abs(Player.Player1.transform.position.x - s.transform.position.x)).ToList();

                var farthestIndex = leftToRightSpikes.IndexOf(orderedSpikes[orderedSpikes.Count - 1]);
                var nearestIndex = leftToRightSpikes.IndexOf(orderedSpikes[0]);

                var max = Mathf.Abs(farthestIndex - nearestIndex);

                for (int i = orderedSpikes.Count - 1; i >= 0 ; i--)
                {
                    var index = leftToRightSpikes.IndexOf(orderedSpikes[i]);

                    var diff = Mathf.Abs(index - nearestIndex);

                    completedSpikes.Add(orderedSpikes[i].RetractAndWait(group.PreDelay + (group.StaggeredDelay * max) - (group.StaggeredDelay * diff), 0.5f));
                }
            }
            else if (spikeMode == SpikeMode.StaggeredLeftToRight)
            {
                List<ColosseumSpike> orderedSpikes = group.Spikes.OrderBy(s => s.transform.position.x).ToList();
                for (int i = 0; i < orderedSpikes.Count; i++)
                {
                    completedSpikes.Add(orderedSpikes[i].RetractAndWait(group.PreDelay + (group.StaggeredDelay * i), 0.5f));
                }
            }
            else if (spikeMode == SpikeMode.StaggeredRightToLeft)
            {
                List<ColosseumSpike> orderedSpikes = group.Spikes.OrderByDescending(s => s.transform.position.x).ToList();
                for (int i = 0; i < orderedSpikes.Count; i++)
                {
                    completedSpikes.Add(orderedSpikes[i].RetractAndWait(group.PreDelay + (group.StaggeredDelay * i), 0.5f));
                }
            }
            else if (spikeMode == SpikeMode.StaggeredSidesToCenter)
            {
                List<ColosseumSpike> leftToRightSpikes = group.Spikes.OrderBy(s => s.transform.position.x).ToList();

                var left = leftToRightSpikes.Min(s => s.transform.position.x);
                var right = leftToRightSpikes.Max(s => s.transform.position.x);

                var centerX = Mathf.Lerp(left, right, 0.5f);

                List<ColosseumSpike> orderedSpikes = group.Spikes.OrderBy(s => Mathf.Abs(centerX - s.transform.position.x)).ToList();

                var farthestIndex = leftToRightSpikes.IndexOf(orderedSpikes[orderedSpikes.Count - 1]);
                var nearestIndex = leftToRightSpikes.IndexOf(orderedSpikes[0]);

                var max = Mathf.Abs(farthestIndex - nearestIndex);

                for (int i = orderedSpikes.Count - 1; i >= 0 ; i--)
                {
                    var index = leftToRightSpikes.IndexOf(orderedSpikes[i]);

                    var diff = Mathf.Abs(index - nearestIndex);

                    completedSpikes.Add(orderedSpikes[i].RetractAndWait(group.PreDelay + (group.StaggeredDelay * max) - (group.StaggeredDelay * diff), 0.5f));
                }
            }
            else if (spikeMode == SpikeMode.StaggeredCenterToSides)
            {
                List<ColosseumSpike> leftToRightSpikes = group.Spikes.OrderBy(s => s.transform.position.x).ToList();

                var left = leftToRightSpikes.Min(s => s.transform.position.x);
                var right = leftToRightSpikes.Max(s => s.transform.position.x);

                var centerX = Mathf.Lerp(left, right, 0.5f);

                List<ColosseumSpike> orderedSpikes = group.Spikes.OrderBy(s => Mathf.Abs(centerX - s.transform.position.x)).ToList();

                var farthestIndex = leftToRightSpikes.IndexOf(orderedSpikes[orderedSpikes.Count - 1]);
                var nearestIndex = leftToRightSpikes.IndexOf(orderedSpikes[0]);

                var max = Mathf.Abs(farthestIndex - nearestIndex);

                for (int i = orderedSpikes.Count - 1; i >= 0 ; i--)
                {
                    var index = leftToRightSpikes.IndexOf(orderedSpikes[i]);

                    var diff = Mathf.Abs(index - nearestIndex);

                    completedSpikes.Add(orderedSpikes[i].RetractAndWait(group.PreDelay + (group.StaggeredDelay * diff), 0.5f));
                }
            }

            return () => completedSpikes.All(c => c());
        }

        public static Func<bool> RaiseSpikesRoutine(ColosseumSpikeGroup group, SpikeMode? spikeModeOverride = null)
        {
            if (group == null)
            {
                return () => true;
            }

            SpikeMode spikeMode = spikeModeOverride ?? group.Mode;

            group.gameObject.SetActive(true);

            List<Func<bool>> completedSpikes = new List<Func<bool>>();

            if (spikeMode == SpikeMode.AllAtOnce)
            {
                foreach (var spike in group.Spikes)
                {
                    completedSpikes.Add(spike.ExpandAndWait(group.PreDelay, group.AnticDuration, spike == group.Spikes[0] ? 1f : 0f));
                }
            }
            else if (spikeMode == SpikeMode.StaggeredDynamic)
            {
                List<ColosseumSpike> leftToRightSpikes = group.Spikes.OrderBy(s => s.transform.position.x).ToList();
                List<ColosseumSpike> orderedSpikes = group.Spikes.OrderBy(s => Mathf.Abs(Player.Player1.transform.position.x - s.transform.position.x)).ToList();

                var farthestIndex = leftToRightSpikes.IndexOf(orderedSpikes[orderedSpikes.Count - 1]);
                var nearestIndex = leftToRightSpikes.IndexOf(orderedSpikes[0]);

                var max = Mathf.Abs(farthestIndex - nearestIndex);

                for (int i = orderedSpikes.Count - 1; i >= 0 ; i--)
                {
                    var index = leftToRightSpikes.IndexOf(orderedSpikes[i]);

                    var diff = Mathf.Abs(index - nearestIndex);

                    completedSpikes.Add(orderedSpikes[i].ExpandAndWait(group.PreDelay + (group.StaggeredDelay * max) - (group.StaggeredDelay * diff), group.AnticDuration, 0.5f));
                }
            }
            else if (spikeMode == SpikeMode.StaggeredLeftToRight)
            {
                List<ColosseumSpike> orderedSpikes = group.Spikes.OrderBy(s => s.transform.position.x).ToList();
                for (int i = 0; i < orderedSpikes.Count; i++)
                {
                    completedSpikes.Add(orderedSpikes[i].ExpandAndWait(group.PreDelay + (group.StaggeredDelay * i), group.AnticDuration, 0.5f));
                }
            }
            else if (spikeMode == SpikeMode.StaggeredRightToLeft)
            {
                List<ColosseumSpike> orderedSpikes = group.Spikes.OrderByDescending(s => s.transform.position.x).ToList();
                for (int i = 0; i < orderedSpikes.Count; i++)
                {
                    completedSpikes.Add(orderedSpikes[i].ExpandAndWait(group.PreDelay + (group.StaggeredDelay * i), group.AnticDuration, 0.5f));
                }
            }
            else if (spikeMode == SpikeMode.StaggeredSidesToCenter)
            {
                List<ColosseumSpike> leftToRightSpikes = group.Spikes.OrderBy(s => s.transform.position.x).ToList();

                var left = leftToRightSpikes.Min(s => s.transform.position.x);
                var right = leftToRightSpikes.Max(s => s.transform.position.x);

                var centerX = Mathf.Lerp(left, right, 0.5f);

                List<ColosseumSpike> orderedSpikes = group.Spikes.OrderBy(s => Mathf.Abs(centerX - s.transform.position.x)).ToList();

                var farthestIndex = leftToRightSpikes.IndexOf(orderedSpikes[orderedSpikes.Count - 1]);
                var nearestIndex = leftToRightSpikes.IndexOf(orderedSpikes[0]);

                var max = Mathf.Abs(farthestIndex - nearestIndex);

                for (int i = orderedSpikes.Count - 1; i >= 0 ; i--)
                {
                    var index = leftToRightSpikes.IndexOf(orderedSpikes[i]);

                    var diff = Mathf.Abs(index - nearestIndex);

                    completedSpikes.Add(orderedSpikes[i].ExpandAndWait(group.PreDelay + (group.StaggeredDelay * max) - (group.StaggeredDelay * diff), group.AnticDuration, 0.5f));
                }
            }
            else if (spikeMode == SpikeMode.StaggeredCenterToSides)
            {
                List<ColosseumSpike> leftToRightSpikes = group.Spikes.OrderBy(s => s.transform.position.x).ToList();

                var left = leftToRightSpikes.Min(s => s.transform.position.x);
                var right = leftToRightSpikes.Max(s => s.transform.position.x);

                var centerX = Mathf.Lerp(left, right, 0.5f);

                List<ColosseumSpike> orderedSpikes = group.Spikes.OrderBy(s => Mathf.Abs(centerX - s.transform.position.x)).ToList();

                var farthestIndex = leftToRightSpikes.IndexOf(orderedSpikes[orderedSpikes.Count - 1]);
                var nearestIndex = leftToRightSpikes.IndexOf(orderedSpikes[0]);

                var max = Mathf.Abs(farthestIndex - nearestIndex);

                for (int i = orderedSpikes.Count - 1; i >= 0 ; i--)
                {
                    var index = leftToRightSpikes.IndexOf(orderedSpikes[i]);

                    var diff = Mathf.Abs(index - nearestIndex);

                    completedSpikes.Add(orderedSpikes[i].ExpandAndWait(group.PreDelay + (group.StaggeredDelay * diff), group.AnticDuration, 0.5f));
                }
            }

            return () => completedSpikes.All(c => c());
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (RoomManager == null || !RoomManager.SpikeLabels || Application.isPlaying)
                return;

            if (Spikes == null || Spikes.Count == 0)
                return;

            // Calculate the bounding box for the group
            Bounds groupBounds = new Bounds(Spikes[0].transform.position, Vector3.zero);
            foreach (var spike in Spikes)
            {
                Renderer renderer = spike.GetComponent<Renderer>();
                if (renderer != null)
                {
                    groupBounds.Encapsulate(renderer.bounds);
                }
                else
                {
                    groupBounds.Encapsulate(spike.transform.position);
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
            float offset = HandleUtility.GetHandleSize(position) * 0.02f;

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
