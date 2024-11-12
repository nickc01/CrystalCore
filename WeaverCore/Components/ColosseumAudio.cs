using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using WeaverCore.Attributes;
using WeaverCore.Utilities;

namespace WeaverCore.Components
{
    public static class ColosseumAudio
    {
        static Dictionary<string, UnboundCoroutine> audioRoutines = new Dictionary<string, UnboundCoroutine>();

        public static void TriggerAudio(AudioClip clip, float delay) => TriggerAudio(Enumerable.Repeat(clip, 1), Enumerable.Repeat(delay, 1));

        public static void TriggerAudio(IEnumerable<AudioClip> clips, IEnumerable<float> delays)
        {
            string combinedName = "";
            //int count = 0;
            foreach (var clip in clips)
            {
                combinedName += clip.name + "_-_";
                //count++;
            }
            /*for (int i = 0; i < clips.Count - 1; i++)
            {
                combinedName += clips[i].name + "_-_";
            }*/
            //combinedName += clips[clips.Count - 1].name;
            if (audioRoutines.ContainsKey(combinedName))
            {
                //WeaverLog.Log("NOT TRIGGERING Clip = " + combinedName);
                return;
            }

            //WeaverLog.Log("TRIGGERING Clip = " + combinedName);

            UnboundCoroutine audioRoutine = null;
            
            audioRoutine = UnboundCoroutine.Start(TriggerAudioRoutine(combinedName, clips, delays, () => audioRoutine));

            audioRoutines.Add(combinedName, audioRoutine);
        }

        static IEnumerator TriggerAudioRoutine(string combinedName, IEnumerable<AudioClip> clips, IEnumerable<float> delays, Func<UnboundCoroutine> routine)
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
                    //WeaverLog.Log("NOT Playing Clip = " + combinedName);
                    yield break;
                }

                //WeaverLog.Log("Playing Clip = " + combinedName);

                WeaverAudio.PlayAtPoint(clip, Player.Player1.transform.position);
                yield return new WaitForSeconds(delay);
            }


            /*for (int i = 0; i < clips.Count; i++)
            {
                if (!audioRoutines.ContainsKey(combinedName))
                {
                    //WeaverLog.Log("NOT Playing Clip = " + combinedName);
                    yield break;
                }

                //WeaverLog.Log("Playing Clip = " + combinedName);

                WeaverAudio.PlayAtPoint(clips[i], Player.Player1.transform.position);
                yield return new WaitForSeconds(delays[i]);
            }*/

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