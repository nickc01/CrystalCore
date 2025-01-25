using UnityEngine;

namespace WeaverCore.Components.Colosseum
{
    [System.Serializable]
    public class EnemyWaveEntry
    {
        [Tooltip("Name of the enemy to spawn.")]
        public string enemyName = "Empty";

        [Tooltip("Name of the spawn location for the enemy.")]
        public string spawnLocationName = "Empty";

        [Tooltip("The delay from the start of the wave before spawning this enemy.")]
        public float delayBeforeSpawn;

        [Tooltip("Indicates if this enemy is a priority target. Priority targets must be killed before going to the next wave. ")]
        public bool isPrioritized;

        [Tooltip("Color representing this entry in the editor.")]
        public Color entryColor;
    }
}
