using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using WeaverCore.Utilities;

namespace WeaverCore.Components.Colosseum
{
    public class ColosseumSpawnPoint : MonoBehaviour, IColosseumIdentifier
    {
		[SerializeField, Tooltip("Hazard respawn marker associated with this spawn point.")]
		HazardRespawnMarker respawnMarker;

        public bool ShowShortcut => true;

        string IColosseumIdentifier.Identifier => "Player Respawn Points";

        Color IColosseumIdentifier.Color => new Color(1.0f, 0.5f, 0.0f);

        public void SetRespawnPoint()
		{
			WeaverLog.Log("SETTING RESPAWN POINT to = " + gameObject.name);
			PlayerData.instance.SetHazardRespawn(respawnMarker);
		}

		public void Reset()
		{
			respawnMarker = GetComponent<HazardRespawnMarker>();
		}
	}
}
