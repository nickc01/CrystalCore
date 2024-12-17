using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using WeaverCore.Utilities;
using System.Reflection;
using System;

namespace WeaverCore.Components.Colosseum
{
    public class EventWave : Wave, ISerializationCallbackReceiver, IColosseumIdentifier
    {
        static Dictionary<Type, FieldCopierBuilder<object>.ShallowCopyDelegate> copiers;

        [Tooltip("Minimum duration the wave can last.")]
        public float minimumDuration = 10f;

        [Tooltip("List of event wave entries defining events to run during the wave.")]
        public List<EventWaveEntry> entries = new List<EventWaveEntry>();

        [SerializeField]
        [HideInInspector]
        List<UnityEvent> entries_eventsToRun = new List<UnityEvent>();

        [SerializeField]
        [HideInInspector]
        List<float> entries_delayBeforeRun = new List<float>();

        string IColosseumIdentifier.Identifier => "Event Waves";

        Color IColosseumIdentifier.Color => new Color(0.0f, 1.0f, 1.0f);

        public bool ShowShortcut => true;

        public override IEnumerator RunWave(ColosseumRoomManager challenge)
        {
            float waveStartTime = Time.time;

            foreach (var entry in entries.OrderBy(e => e.delayBeforeRun))
            {
                yield return new WaitForSeconds(entry.delayBeforeRun - (Time.time - waveStartTime));

                entry.eventsToRun?.Invoke();
            }

            while (Time.time - waveStartTime < minimumDuration)
            {
                yield return null;
            }
        }

        static UnityEvent Clone(UnityEvent source)
        {
            return JsonUtility.FromJson<UnityEvent>(JsonUtility.ToJson(source));
            /*var copy = new UnityEvent();
            foreach (var pair in copiers)
            {
                pair.Value(copy, source);
            }
            return copy;*/
        }

        #if UNITY_EDITOR
        private void OnValidate() 
        {
            if (copiers == null)
            {
                copiers = new Dictionary<Type, FieldCopierBuilder<object>.ShallowCopyDelegate>();
                var currentType = typeof(UnityEvent);

                while (currentType != null && currentType != typeof(object))
                {
                    var copier = new FieldCopierBuilder<object>(currentType);
                    foreach (FieldInfo field in currentType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                    {
                        if (!field.IsInitOnly && !field.IsLiteral)
                        {
                            if (field.FieldType.IsValueType || field.FieldType.IsEnum)
                            {
                                copier.AddField(field);
                            }
                            else if (field.FieldType.IsClass && (field.IsPublic || field.IsDefined(typeof(SerializeField), true)))
                            {
                                if (!typeof(Component).IsAssignableFrom(field.FieldType) && !typeof(GameObject).IsAssignableFrom(field.FieldType))
                                {
                                    copier.AddField(field);
                                }
                            }
                        }
                    }
                    
                    copiers.Add(currentType, copier.Finish());
                    //cData.Copiers.Add(CreateFieldCopier(currentType));

                    currentType = currentType.BaseType;
                }
            }

            entries_eventsToRun.Clear();
            entries_eventsToRun.AddRange(entries.Select(e => Clone(e.eventsToRun)));

            entries_delayBeforeRun.Clear();
            entries_delayBeforeRun.AddRange(entries.Select(e => e.delayBeforeRun));

            //WeaverSerializer.Serialize(entries, out entries_json, out entries_references);
            /*if (entries_eventsToRun.Count != entries.Count)
            {
                entries_eventsToRun.Clear();
                entries_eventsToRun.AddRange(entries.Select(e => e.eventsToRun));
            }*/
            /*else
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    entries_eventsToRun[i] = entries[i].eventsToRun;
                }
            }*/

            //entries_eventsToRun = entries.Select(e => e.eventsToRun).ToList();

            /*if (entries_delayBeforeRun.Count != entries.Count)
            {
                entries_delayBeforeRun.Clear();
                entries_delayBeforeRun.AddRange(entries.Select(e => e.delayBeforeRun));
            }*/
            /*else
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    entries_delayBeforeRun[i] = entries[i].delayBeforeRun;
                }
            }*/
            //entries_delayBeforeRun = entries.Select(e => e.delayBeforeRun).ToList();
        }
        #endif

        public void OnBeforeSerialize()
        {
            //WeaverLog.Log("BEFORE = " + Newtonsoft.Json.JsonConvert.SerializeObject(entries));
            //entries_eventsToRun.Clear();
            //WeaverLog.Log("AFTER = " + Newtonsoft.Json.JsonConvert.SerializeObject(entries));
            //entries_eventsToRun.AddRange(entries.Select(e => e.eventsToRun));
            //WeaverLog.Log("AFTER 2 = " + Newtonsoft.Json.JsonConvert.SerializeObject(entries));


            //entries_delayBeforeRun.Clear();
            //entries_delayBeforeRun.AddRange(entries.Select(e => e.delayBeforeRun));
            /*entries_eventsToRun.Clear();
            entries_eventsToRun.AddRange(entries.Select(e => e.eventsToRun));

            entries_delayBeforeRun.Clear();
            entries_delayBeforeRun.AddRange(entries.Select(e => e.delayBeforeRun));*/
        }

        public void OnAfterDeserialize()
        {
            #if !UNITY_EDITOR
            //entries = WeaverSerializer.Deserialize<List<EnemyWaveEntry>>(entries_json, entries_references);
            entries.Clear();

            for (int i = 0; i < entries_eventsToRun.Count; i++)
            {
                entries.Add(new EventWaveEntry {
                    eventsToRun = entries_eventsToRun[i],
                    delayBeforeRun = entries_delayBeforeRun[i]
                });
            }
            #endif
        }
    }
}
