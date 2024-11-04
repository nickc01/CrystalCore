using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using WeaverCore.Components;

namespace WeaverCore.Utilities
{
    /// <summary>
    /// Contains many utility functions related to the Hero (The player character)
    /// </summary>
    public static class HeroUtilities
    {
		static AudioClip cutsceneBeginSound = null;

		static Component heroAnimator = null;
		static Type heroAnimatorType;
		static PropertyInfo CurrentClipP;
		static Type ClipType;
		static PropertyInfo ClipDurationP;

		static void InitHeroAnim()
		{
            if (heroAnimator == null)
            {
                heroAnimator = HeroController.instance.GetComponent("tk2dSpriteAnimator");
                heroAnimatorType = heroAnimator.GetType();
                CurrentClipP = heroAnimatorType.GetProperty("CurrentClip");
                ClipType = CurrentClipP.PropertyType;
                ClipDurationP = ClipType.GetProperty("Duration");
            }
        }

		/// <summary>
		/// Plays an animation clip on the player, and waits until the clip is done
		/// </summary>
		/// <param name="clip">The clip to play</param>
		public static IEnumerator PlayPlayerClipTillDone(string clip)
		{
			if (Initialization.Environment == Enums.RunningState.Game)
			{
				InitHeroAnim();
                heroAnimator.SendMessage("Play", clip);
				yield return new WaitForSeconds((float)ClipDurationP.GetValue(CurrentClipP.GetValue(heroAnimator)));
			}

			var weaverAnim = HeroController.instance.GetComponent<WeaverAnimationPlayer>();
			if (weaverAnim != null)
			{
				yield return weaverAnim.PlayAnimationTillDone(clip);
			}
		}

		/// <summary>
		/// Plays an animation clip on the player
		/// </summary>
		/// <param name="clip">The clip to play</param>
		public static void PlayPlayerClip(string clip)
		{
			if (Initialization.Environment == Enums.RunningState.Game)
			{
                InitHeroAnim();
                heroAnimator.SendMessage("Play", clip);
			}

			var weaverAnim = HeroController.instance.GetComponent<WeaverAnimationPlayer>();
			weaverAnim?.PlayAnimation(clip);
		}

		public static void PauseCurrentAnimation(bool pause)
		{
            InitHeroAnim();

			if (pause)
			{
				//heroAnimator.SendMessage("Pause");
				heroAnimator.ReflectCallMethod("Pause");
			}
			else
			{
                //heroAnimator.SendMessage("Resume");
                heroAnimator.ReflectCallMethod("Resume");
            }
		}

		/// <summary>
		/// Begins an in-game cutscene that freezes the player
		/// </summary>
		/// <param name="playSound">If true, a cutscene sound effect is played</param>
		public static void BeginInGameCutscene(bool playSound = true)
		{
			EventManager.SendEventToGameObject("FSM CANCEL", Player.Player1.gameObject);

			var hudCanvas = CameraUtilities.GetHudCanvas();

			if (hudCanvas != null)
			{
				EventManager.SendEventToGameObject("OUT", hudCanvas, null);
			}

			HeroController.instance.RelinquishControl();
			HeroController.instance.StartAnimationControl();
			PlayerData.instance.SetBool("disablePause", true);
			HeroController.instance.GetComponent<Rigidbody2D>().velocity = default;
			HeroController.instance.AffectedByGravity(true);

			if (playSound)
			{
                /*if (cutsceneBeginSound == null)
                {
                    cutsceneBeginSound = WeaverAssets.LoadWeaverAsset<AudioClip>("dream_ghost_appear");
                }*/

				WeaverAudio.PlayAtPoint(WeaverAssets.LoadWeaverAsset<AudioClip>("dream_ghost_appear"), Player.Player1.transform.position);
            }
        }

		/// <summary>
		/// Ends an in-game cutscene and unfreezes the player
		/// </summary>
		public static void EndInGameCutscene()
		{
			HeroController.instance.RegainControl();
			HeroController.instance.StartAnimationControl();

            var hudCanvas = CameraUtilities.GetHudCanvas();

            if (hudCanvas != null)
            {
                EventManager.SendEventToGameObject("IN", hudCanvas, null);
            }
            PlayerData.instance.SetBool("disablePause", false);
        }
	}

}
