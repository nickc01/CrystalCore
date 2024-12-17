using System;
using UnityEngine;
using WeaverCore.Components;

namespace WeaverCore.Components.Colosseum
{
    public class CageColosseumSpawner : ColosseumEnemySpawner
    {
        [Tooltip("Prefab of the colosseum cage used to spawn entities.")]
        public ColosseumCage Prefab;

        private void Awake() 
        {
            var renderer = GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }

        public override void SpawnEnemy(GameObject enemyPrefab, Action<GameObject> onSummon)
        {
            if (enemyPrefab == null)
            {
                onSummon?.Invoke(enemyPrefab);
            }
            else
            {
                ColosseumCage.Summon(transform.position, enemyPrefab, onSummon, Prefab);
            }
        }

        private void Reset() 
        {
            Prefab = ColosseumCage.GetDefaultPrefab(ColosseumCage.CageType.Small);
        }

        public void OnValidate() 
        {
            var renderer = GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                if (RoomManager == null || RoomManager.SpawnLocationSpriteLabels == false || Prefab == null)
                {
                    renderer.sprite = null;
                }
                else
                {
                    var otherRenderer = Prefab.GetComponent<SpriteRenderer>();
                    if (otherRenderer != null)
                    {
                        renderer.sprite = otherRenderer.sprite;
                    }
                }
            }
        }
    }
}
