using System;
using UnityEngine;

namespace ProjectArk.Level
{
    /// <summary>
    /// Data-driven encounter definition: defines enemy waves for a combat room.
    /// Referenced by RoomSO. Used by EnemySpawner's WaveSpawnStrategy (Phase 3).
    /// </summary>
    [CreateAssetMenu(fileName = "New Encounter", menuName = "ProjectArk/Level/Encounter")]
    public class EncounterSO : ScriptableObject
    {
        [Tooltip("Ordered list of enemy waves. Waves spawn sequentially after the previous wave is cleared.")]
        [SerializeField] private EnemyWave[] _waves;

        /// <summary> All waves in this encounter. </summary>
        public EnemyWave[] Waves => _waves;

        /// <summary> Total number of waves. </summary>
        public int WaveCount => _waves != null ? _waves.Length : 0;
    }

    /// <summary>
    /// A single wave within an encounter. Contains enemy entries and a delay before spawning.
    /// </summary>
    [Serializable]
    public class EnemyWave
    {
        [Tooltip("Delay in seconds before this wave spawns (after previous wave is cleared).")]
        [Min(0f)]
        public float DelayBeforeWave;

        [Tooltip("Enemies to spawn in this wave.")]
        public EnemySpawnEntry[] Entries;

        /// <summary> Total enemy count in this wave. </summary>
        public int TotalEnemyCount
        {
            get
            {
                if (Entries == null) return 0;
                int total = 0;
                for (int i = 0; i < Entries.Length; i++)
                    total += Entries[i].Count;
                return total;
            }
        }
    }

    /// <summary>
    /// A single enemy type + count entry within a wave.
    /// </summary>
    [Serializable]
    public class EnemySpawnEntry
    {
        [Tooltip("Enemy prefab to spawn (must have EnemyEntity + EnemyBrain).")]
        public GameObject EnemyPrefab;

        [Tooltip("Number of this enemy type to spawn in this wave.")]
        [Min(1)]
        public int Count = 1;
    }
}
