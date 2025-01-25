using UnityEngine;

namespace WeaverCore.Components.Colosseum
{
    [System.Serializable]
    public class HybridWaveEntry
    {
        public enum HybridWaveType
        {
            Enemy,
            Event
        }

        [Tooltip("The Type of the Hybrid Wave. Either EnemyWave or EventWave")]
        public HybridWaveType Type = HybridWaveType.Enemy;

        public EventWaveEntry eventData;

        public EnemyWaveEntry enemyData;

        public float DelayBeforeSpawn
        {
            get
            {
                switch (Type)
                {
                    case HybridWaveType.Enemy:
                        return enemyData.delayBeforeSpawn;
                    case HybridWaveType.Event:
                        return eventData.delayBeforeRun;
                    default:
                        return -1;
                }
                
            }
        }
    }
}
