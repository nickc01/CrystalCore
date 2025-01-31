﻿using System;
using System.Collections;
using UnityEngine;
using WeaverCore.Utilities;

namespace WeaverCore.Enums
{
    /// <summary>
    /// Used on a variety of different scripts to determine what they do when they finish
    /// </summary>
    public enum OnDoneBehaviour
	{
		/// <summary>
		/// The script does nothing when finished
		/// </summary>
		Nothing,
		/// <summary>
		/// The script's object will be disabled when finished
		/// </summary>
		Disable,
		/// <summary>
		/// The script's object will be destroyed when finished
		/// </summary>
		Destroy,
		/// <summary>
		/// The script's object will be sent back into a pool (or destroyed of it's not part of a pool)
		/// </summary>
		DestroyOrPool
	}

	public static class OnDoneBehaviour_Extensions
	{
		public static void DoneWithObject(this OnDoneBehaviour behaviour, GameObject gameObject)
		{
			switch (behaviour)
			{
				case OnDoneBehaviour.Nothing:
					break;
				case OnDoneBehaviour.Disable:
					gameObject.SetActive(false);
					break;
				case OnDoneBehaviour.Destroy:
					GameObject.Destroy(gameObject);
					break;
				case OnDoneBehaviour.DestroyOrPool:
					var poolable = gameObject.GetComponent<PoolableObject>();
					if (poolable != null)
					{
						poolable.ReturnToPool();
					}
					else
					{
						GameObject.Destroy(gameObject);
					}
					break;
				default:
					break;
			}
		}

        public static void DoneWithObject(this OnDoneBehaviour behaviour, GameObject gameObject, float time, Action onDone = null)
		{
			IEnumerator Waiter()
			{
				for (float t = 0; t < time; t += Time.deltaTime)
				{
					if (gameObject == null)
					{
                        onDone?.Invoke();
                        yield break;
					}
					if (behaviour == OnDoneBehaviour.Disable && !gameObject.activeSelf)
					{
						onDone?.Invoke();
						yield break;
					}
					yield return null;
				}

                onDone?.Invoke();
                behaviour.DoneWithObject(gameObject);
            }


			UnboundCoroutine.Start(Waiter());
		}


        public static void DoneWithObject(this OnDoneBehaviour behaviour, Component component)
		{
			DoneWithObject(behaviour, component.gameObject);
		}

        public static void DoneWithObject(this OnDoneBehaviour behaviour, Component component, float time, Action onDone = null)
        {
			DoneWithObject(behaviour, component.gameObject, time, onDone);
        }
    }

}
