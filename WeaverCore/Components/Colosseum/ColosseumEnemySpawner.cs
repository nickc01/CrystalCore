using System;
using UnityEngine;
using WeaverCore.Attributes;

namespace WeaverCore.Components.Colosseum
{
    public class ColosseumEnemySpawner : MonoBehaviour, IColosseumIdentifier, IColosseumIdentifierExtra
    {
        [NonSerialized]
        ColosseumRoomManager _roomManager = null;
        public ColosseumRoomManager RoomManager => _roomManager ??= GetComponentInParent<ColosseumRoomManager>();

        [field: SerializeField, Tooltip("Color used for visualizing the enemy spawner in the editor.")]
        public Color EditorColor { get; private set; } = Color.blue;

        string IColosseumIdentifier.Identifier => "Enemy Spawners";

        Color IColosseumIdentifier.Color => Color.red;

        Color IColosseumIdentifierExtra.UnderlineColor => EditorColor;

        bool IColosseumIdentifier.ShowShortcut => true;

        // This function can be overridden to define custom spawn logic
        public virtual void SpawnEnemy(GameObject enemyPrefab, Action<GameObject> onSummon)
        {
            // Default behavior: instantiate the enemy at the given location
            GameObject spawnedEnemy = Instantiate(enemyPrefab, transform.position, transform.rotation);

            spawnedEnemy.gameObject.SetActive(true);

            onSummon?.Invoke(spawnedEnemy);
        }

        protected virtual void OnDrawGizmosSelected() 
        {
            if (RoomManager != null && RoomManager.EnemySpawnerLabels)
            {
                Gizmos.color = EditorColor;
                Gizmos.DrawSphere(transform.position, 0.5f);
                Gizmos.DrawWireSphere(transform.position, 0.5f);
                Gizmos.DrawLine(transform.position, transform.position);
                Gizmos.color = Color.cyan;
                Gizmos.DrawCube(transform.position, new Vector3(0.5f, 0.5f, 0.5f));
                //RoomManager.OnDrawGizmosSelected();
            }
        }
    }
}
