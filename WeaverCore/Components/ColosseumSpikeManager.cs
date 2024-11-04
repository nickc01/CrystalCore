using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace WeaverCore.Components
{
    public class ColosseumSpikeManager : MonoBehaviour, ISerializationCallbackReceiver
    {
        [Serializable]
        public class SpikeGroup {
            [SerializeField]
            Transform _groupParent;

            public Transform GroupParent
            {
                get => _groupParent;
                set
                {
                    if (_groupParent != value)
                    {
                        _groupParent = value;
                        _spikeCache = null;
                    }
                }
            }

            public float PreDelay = 0f;
            public float AnticDuration = -1;
            public SpikeMode Mode = SpikeMode.AllAtOnce;
            public float StaggeredDelay = 0.15f;

            [NonSerialized]
            List<ColosseumSpike> _spikeCache;

            public List<ColosseumSpike> Spikes 
            {
                get 
                {
                    if (GroupParent != null && _spikeCache == null)
                    {
                        _spikeCache = new List<ColosseumSpike>();
                        GroupParent.GetComponentsInChildren(_spikeCache);
                    }

                    return _spikeCache;
                }
            }
        }

        public enum SpikeMode
        {
            AllAtOnce,
            StaggeredDynamic,
            StaggeredLeftToRight,
            StaggeredRightToLeft,
            StaggeredSidesToCenter,
            StaggeredCenterToSides
        }

        [SerializeField]
        List<SpikeGroup> spikeGroups = new List<SpikeGroup>();

        [SerializeField]
        [HideInInspector]
        string spikeGroups_json;

        [SerializeField]
        [HideInInspector]
        List<UnityEngine.Object> spikeGroups_references = new List<UnityEngine.Object>();

        [SerializeField]
        UnityEvent testEvent;

        private void Awake() {
            testEvent?.Invoke();
        }

        public IEnumerable<string> GetSpikeGroups()
        {
            return spikeGroups.Where(g => g.GroupParent != null).Select(g => g.GroupParent.name);
        }

        public SpikeGroup GetSpikeGroup(string groupName)
        {
            return spikeGroups.FirstOrDefault(g => g.GroupParent != null && g.GroupParent.name == groupName);
        }

        public void RaiseSpikeGroup(string spikeGroup) => RaiseSpikes(GetSpikeGroup(spikeGroup));

        public static Func<bool> RaiseSpikes(SpikeGroup group)
        {
            if (group == null)
            {
                return () => true;
            }

            List<Func<bool>> completedSpikes = new List<Func<bool>>();

            if (group.Mode == SpikeMode.AllAtOnce)
            {
                WeaverLog.Log("Raising All At Once");
                WeaverLog.Log("SPIKES COUNT = " + group.Spikes?.Count);
                foreach (var spike in group.Spikes)
                {
                    completedSpikes.Add(spike.ExpandAndWait(group.PreDelay, group.AnticDuration, spike == group.Spikes[0] ? 1f : 0f));
                }
            }
            else if (group.Mode == SpikeMode.StaggeredDynamic)
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
            else if (group.Mode == SpikeMode.StaggeredLeftToRight)
            {
                List<ColosseumSpike> orderedSpikes = group.Spikes.OrderBy(s => s.transform.position.x).ToList();
                for (int i = 0; i < orderedSpikes.Count; i++)
                {
                    completedSpikes.Add(orderedSpikes[i].ExpandAndWait(group.PreDelay + (group.StaggeredDelay * i), group.AnticDuration, 0.5f));
                }
            }
            else if (group.Mode == SpikeMode.StaggeredRightToLeft)
            {
                List<ColosseumSpike> orderedSpikes = group.Spikes.OrderByDescending(s => s.transform.position.x).ToList();
                for (int i = 0; i < orderedSpikes.Count; i++)
                {
                    completedSpikes.Add(orderedSpikes[i].ExpandAndWait(group.PreDelay + (group.StaggeredDelay * i), group.AnticDuration, 0.5f));
                }
            }
            else if (group.Mode == SpikeMode.StaggeredSidesToCenter)
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
                /*List<ColosseumSpike> leftToRightSpikes = group.Spikes.OrderBy(s => s.transform.position.x).ToList();
                var left = group.Spikes.Min(s => s.transform.position.x);
                var right = group.Spikes.Min(s => s.transform.position.x);

                var centerX = Mathf.Lerp(left, right, 0.5f);

                var orderedSpikes = group.Spikes.OrderBy(s => Mathf.Abs(s.transform.position.x - centerX)).ToList();

                var nearestIndex = leftToRightSpikes.IndexOf(orderedSpikes[0]);
                //List<ColosseumSpike> orderedSpikes = group.Spikes.OrderBy(s => Mathf.Abs()).ToList();
                for (int i = 0; i < orderedSpikes.Count; i++)
                {
                    var index = leftToRightSpikes.IndexOf(orderedSpikes[i]);
                    var diff = Mathf.Abs(index - nearestIndex);
                    completedSpikes.Add(orderedSpikes[i].ExpandAndWait(group.PreDelay + (group.StaggeredDelay * diff), group.AnticDuration));
                }*/
            }
            else if (group.Mode == SpikeMode.StaggeredCenterToSides)
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
                /*List<ColosseumSpike> leftToRightSpikes = group.Spikes.OrderBy(s => s.transform.position.x).ToList();
                var left = group.Spikes.Min(s => s.transform.position.x);
                var right = group.Spikes.Min(s => s.transform.position.x);

                var centerX = Mathf.Lerp(left, right, 0.5f);

                var orderedSpikes = group.Spikes.OrderBy(s => Mathf.Abs(s.transform.position.x - centerX)).ToList();

                var nearestIndex = leftToRightSpikes.IndexOf(orderedSpikes[0]);
                //List<ColosseumSpike> orderedSpikes = group.Spikes.OrderBy(s => Mathf.Abs()).ToList();
                for (int i = 0; i < orderedSpikes.Count; i++)
                {
                    var index = leftToRightSpikes.IndexOf(orderedSpikes[i]);
                    var diff = Mathf.Abs(index - nearestIndex);
                    completedSpikes.Add(orderedSpikes[i].ExpandAndWait(group.PreDelay + (group.StaggeredDelay * diff), group.AnticDuration));
                }*/
            }

            return () => completedSpikes.All(c => c());
        }

        #if UNITY_EDITOR
        private void OnValidate() 
        {
            WeaverSerializer.Serialize(spikeGroups,out spikeGroups_json, out spikeGroups_references);
        }
        #endif

        public void OnBeforeSerialize()
        {
            
        }

        public void OnAfterDeserialize()
        {
            #if !UNITY_EDITOR
            spikeGroups = WeaverSerializer.Deserialize<List<SpikeGroup>>(spikeGroups_json, spikeGroups_references);
            #endif
        }
    }
}