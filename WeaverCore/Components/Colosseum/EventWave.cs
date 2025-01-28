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

        [Tooltip("How many times to loop the event wave.")]
        public int loopCount = 0;

        [Tooltip("The delay between each loop. This doesn't nothing if the Loop Count is set to 0")]
        public float loopDelay = 0;

        [Tooltip("Delay before the wave ends.")]
        public float endingDelay = 0f;

        [Tooltip("If set to true, then this wave can only be run if manually triggered. Be sure to check this box to prevent the wave from being automatically triggered")]
        public bool ManuallyTriggered = false;

        [Tooltip("List of event wave entries defining events to run during the wave.")]
        public List<EventWaveEntry> entries = new List<EventWaveEntry>();

        public override bool AutoRun => base.AutoRun && !ManuallyTriggered;

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
            yield return RunWaveInternal(challenge, () => ManualStopType.None);
        }

        public IEnumerator RunWaveInternal(ColosseumRoomManager challenge, Func<ManualStopType> doStop)
        {
            for (int l = 0; l <= loopCount; l++)
            {
                float waveStartTime = Time.time;
                foreach (var entry in entries.OrderBy(e => e.delayBeforeRun))
                {
                    yield return CoroutineUtilities.WaitForTimeOrPredicate(entry.delayBeforeRun - (Time.time - waveStartTime), () => doStop() == ManualStopType.Forcefully);

                    if (doStop() == ManualStopType.Forcefully)
                    {
                        yield break;
                    }
                    //yield return new WaitForSeconds(entry.delayBeforeRun - (Time.time - waveStartTime));

                    entry.eventsToRun?.Invoke();
                }

                while (Time.time - waveStartTime < minimumDuration)
                {
                    if (doStop() == ManualStopType.Forcefully)
                    {
                        yield break;
                    }
                    yield return null;
                }

                if (l != loopCount)
                {
                    yield return CoroutineUtilities.WaitForTimeOrPredicate(loopDelay, () => doStop() == ManualStopType.Forcefully);
                    if (doStop() == ManualStopType.Forcefully)
                    {
                        yield break;
                    }
                    //yield return new WaitForSeconds(loopDelay);
                }

                if (doStop() == ManualStopType.Gracefully || doStop() == ManualStopType.Forcefully)
                {
                    yield break;
                }
            }

            if (endingDelay > 0)
            {
                yield return new WaitForSeconds(endingDelay);
            }
        }

        static UnityEvent Clone(UnityEvent source)
        {
            return JsonUtility.FromJson<UnityEvent>(JsonUtility.ToJson(source));
        }

        #if UNITY_EDITOR
        private void OnValidate() 
        {
            entries_eventsToRun.Clear();
            entries_eventsToRun.AddRange(entries.Select(e => Clone(e.eventsToRun)));

            entries_delayBeforeRun.Clear();
            entries_delayBeforeRun.AddRange(entries.Select(e => e.delayBeforeRun));
        }
        #endif

        public void OnBeforeSerialize()
        {
            
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

        protected override IEnumerator ManuallyRunRoutine(ColosseumRoomManager challenge, Func<ManualStopType> doStop)
        {
            yield return RunWaveInternal(challenge, doStop);
        }
    }
}
