using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Audio;
using WeaverCore.Attributes;
using WeaverCore.Utilities;

namespace WeaverCore
{
    /// <summary>
    /// Used for playing music in WeaverCore
    /// </summary>
	[CreateAssetMenu(fileName = "WeaverMusicCue", menuName = "WeaverCore/Music Cue")]
    public class WeaverMusicCue : MusicCue, ISerializationCallbackReceiver
	{
        static Func<MusicCue, MusicChannelInfo[]> channelInfosGetter;
        static Func<MusicCue, Alternative[]> alternativesGetter;

        static Action<MusicCue, MusicChannelInfo[]> channelInfosSetter;
        static Action<MusicCue, Alternative[]> alternativesSetter;

        static Func<MusicChannelInfo, MusicChannelSync> syncGetter;
        static Action<MusicChannelInfo, MusicChannelSync> syncSetter;

        static Func<MusicChannelInfo, AudioClip> clipGetter;
        static Action<MusicChannelInfo, AudioClip> clipSetter;


        /*[Serializable]
        struct DataContainer
        {
            public MusicChannelInfo[] channelInfos;
            public Alternative[] alternatives;
        }

		[SerializeField]
        [HideInInspector]
        string cueData;*/

        [SerializeField]
        [Tooltip("If set to true, then the audio snapshot specified below will be used when the music cue is applied")]
        bool useSnapshot = true;

        [SerializeField]
        Music.SnapshotType m_snapshot;

        public Music.SnapshotType SnapshotType => m_snapshot;


        [SerializeField]
        [HideInInspector]
        System.Collections.Generic.List<AudioClip> channelInfos_clip;

        [SerializeField]
        [HideInInspector]
        System.Collections.Generic.List<MusicChannelSync> channelInfos_sync;

        [SerializeField]
        [HideInInspector]
        System.Collections.Generic.List<string> alternatives_PlayerDataBoolKey;

        [SerializeField]
        [HideInInspector]
        System.Collections.Generic.List<MusicCue> alternatives_Cue;

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
#if UNITY_EDITOR
            //cueData = "";

            if (channelInfosGetter(this) == null)
            {
                var newInfo = new MusicChannelInfo[6];

                for (int i = 0; i < newInfo.Length; i++)
                {
                    newInfo[i] = new MusicChannelInfo();
                }

                channelInfosSetter(this, newInfo);
            }

            if (alternativesGetter(this) == null)
            {
                alternativesSetter(this, new Alternative[0]);
            }

            if (channelInfos_clip == null)
            {
                channelInfos_clip = new System.Collections.Generic.List<AudioClip>();
            }

            if (channelInfos_sync == null)
            {
                channelInfos_sync = new System.Collections.Generic.List<MusicChannelSync>();
            }

            if (alternatives_PlayerDataBoolKey == null)
            {
                alternatives_PlayerDataBoolKey = new System.Collections.Generic.List<string>();
            }

            if (alternatives_Cue == null)
            {
                alternatives_Cue = new System.Collections.Generic.List<MusicCue>();
            }

            channelInfos_clip.Clear();
            channelInfos_clip.AddRange(channelInfosGetter(this).Select(C => C.Clip));

            channelInfos_sync.Clear();
            channelInfos_sync.AddRange(channelInfosGetter(this).Select(C => syncGetter(C)));

            alternatives_PlayerDataBoolKey.Clear();
            alternatives_PlayerDataBoolKey.AddRange(alternativesGetter(this).Select(a => a.PlayerDataBoolKey));

            alternatives_Cue.Clear();
            alternatives_Cue.AddRange(alternativesGetter(this).Select(a => a.Cue));

            /*cueData = JsonUtility.ToJson(new DataContainer
            {
                channelInfos = channelInfosGetter(this),
                alternatives = alternativesGetter(this)
            }, false);*/
#endif
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
#if !UNITY_EDITOR
            //var container = JsonUtility.FromJson<DataContainer>(cueData);

            //channelInfosSetter(this, container.channelInfos);
            //alternativesSetter(this, container.alternatives);

            if (channelInfos_clip != null)
            {
                MusicChannelInfo[] channels = new MusicChannelInfo[channelInfos_clip.Count];

                for (int i = 0; i < channels.Length; i++)
                {
                    channels[i] = new MusicChannelInfo();
                    clipSetter(channels[i], channelInfos_clip[i]);
                    syncSetter(channels[i], channelInfos_sync[i]);
                }

                channelInfosSetter(this, channels);
            }

            if (alternatives_Cue != null)
            {
                Alternative[] alternatives = new Alternative[alternatives_Cue.Count];

                for (int i = 0; i < alternatives.Length; i++)
                {
                    alternatives[i] = new Alternative()
                    {
                        PlayerDataBoolKey = alternatives_PlayerDataBoolKey[i],
                        Cue = alternatives_Cue[i]
                    };
                }

                alternativesSetter(this, alternatives);
            }

#endif
        }

        /*void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
#if UNITY_EDITOR
            //cueData = "";

            cueData = JsonUtility.ToJson(new DataContainer
            {
                channelInfos = channelInfosGetter(this),
                alternatives = alternativesGetter(this)
            }, false);
#endif
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
#if !UNITY_EDITOR
            var container = JsonUtility.FromJson<DataContainer>(cueData);

            channelInfosSetter(this, container.channelInfos);
            alternativesSetter(this, container.alternatives);
#endif
        }*/

        private void Reset()
        {
            var newInfo = new MusicChannelInfo[6];

            for (int i = 0; i < newInfo.Length; i++)
            {
                newInfo[i] = new MusicChannelInfo();
            }

            channelInfosSetter(this, newInfo);

            if (alternativesGetter(this) == null)
            {
                alternativesSetter(this, new Alternative[0]);
            }
        }

        [OnInit]
        static void Init()
        {
            if (channelInfosGetter == null)
            {
                channelInfosGetter = ReflectionUtilities.CreateFieldGetter<MusicCue, MusicChannelInfo[]>("channelInfos");
            }
            if (alternativesGetter == null)
            {
                alternativesGetter = ReflectionUtilities.CreateFieldGetter<MusicCue, Alternative[]>("alternatives");
            }

            if (channelInfosSetter == null)
            {
                channelInfosSetter = ReflectionUtilities.CreateFieldSetter<MusicCue, MusicChannelInfo[]>("channelInfos");
            }

            if (alternativesSetter == null)
            {
                alternativesSetter = ReflectionUtilities.CreateFieldSetter<MusicCue, Alternative[]>("alternatives");
            }

            if (syncGetter == null)
            {
                syncGetter = ReflectionUtilities.CreateFieldGetter<MusicChannelInfo, MusicChannelSync>("sync");
            }

            if (syncSetter == null)
            {
                syncSetter = ReflectionUtilities.CreateFieldSetter<MusicChannelInfo, MusicChannelSync>("sync");
            }

            if (clipGetter == null)
            {
                clipGetter = ReflectionUtilities.CreateFieldGetter<MusicChannelInfo, AudioClip>("clip");
            }

            if (clipSetter == null)
            {
                clipSetter = ReflectionUtilities.CreateFieldSetter<MusicChannelInfo, AudioClip>("clip");
            }
        }

        [OnHarmonyPatch]
        static void Patch(HarmonyPatcher patcher)
        {
            var snapshotGetter = typeof(MusicCue).GetProperty(nameof(Snapshot)).GetGetMethod();

            var prefix = typeof(WeaverMusicCue).GetMethod(nameof(SnapshotGetterPrefix), BindingFlags.NonPublic | BindingFlags.Static);

            patcher.Patch(snapshotGetter, prefix, null);
        }

        static bool SnapshotGetterPrefix(MusicCue __instance, ref AudioMixerSnapshot __result)
        {
            if (__instance is WeaverMusicCue wmc)
            {
                if (wmc.useSnapshot)
                {
                    __result = Music.GetSnapshot(wmc.SnapshotType);
                }
                else
                {
                    __result = null;
                }
                return false;
            }
            return true;
        }
    }
}
