using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using WeaverCore.Attributes;
using WeaverCore.Utilities;

namespace WeaverCore.Components.Colosseum
{
    public static class ColosseumAudio
    {
        static Dictionary<string, UnboundCoroutine> audioRoutines = new Dictionary<string, UnboundCoroutine>();

        public static void TriggerAudio(AudioClip clip, float delay, Action<AudioPlayer> audioSpawnCallback = null) => TriggerAudio(Enumerable.Repeat(clip, 1), Enumerable.Repeat(delay, 1), audioSpawnCallback);

        public static void TriggerAudio(IEnumerable<AudioClip> clips, IEnumerable<float> delays, Action<AudioPlayer> audioSpawnCallback = null)
        {
            string combinedName = "";
            foreach (var clip in clips)
            {
                combinedName += clip.name + "_-_";
            }
            if (audioRoutines.ContainsKey(combinedName))
            {
                return;
            }

            UnboundCoroutine audioRoutine = null;
            
            audioRoutine = UnboundCoroutine.Start(TriggerAudioRoutine(combinedName, clips, delays, audioSpawnCallback));

            audioRoutines.Add(combinedName, audioRoutine);
        }

        static IEnumerator TriggerAudioRoutine(string combinedName, IEnumerable<AudioClip> clips, IEnumerable<float> delays, Action<AudioPlayer> audioSpawnCallback = null)
        {
            yield return null;
            var clipIter = clips.GetEnumerator();
            var delayIter = delays.GetEnumerator();

            while (clipIter.MoveNext() && delayIter.MoveNext())
            {
                var clip = clipIter.Current;
                var delay = delayIter.Current;
                if (!audioRoutines.ContainsKey(combinedName))
                {
                    yield break;
                }

                var instance = WeaverAudio.PlayAtPoint(clip, Player.Player1.transform.position);
                if (audioSpawnCallback != null)
                {
                    audioSpawnCallback.Invoke(instance);
                }
                yield return new WaitForSeconds(delay);
            }

            if (audioRoutines.ContainsKey(combinedName))
            {
                audioRoutines.Remove(combinedName);
            }
        }

        [OnRuntimeInit]
        static void OnRuntimeInit()
        {
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnSceneChange;
        }

        static void OnSceneChange(Scene from, Scene to)
        {
            audioRoutines.Clear();
        }
    }
}