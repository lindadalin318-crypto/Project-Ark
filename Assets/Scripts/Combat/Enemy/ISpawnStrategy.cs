namespace ProjectArk.Combat.Enemy
{
    /// <summary>
    /// Strategy interface for enemy spawning behavior.
    /// Implementations include LoopSpawnStrategy (legacy) and WaveSpawnStrategy (encounter-driven).
    /// </summary>
    public interface ISpawnStrategy
    {
        /// <summary> Initialize the strategy with a reference to the spawner context. </summary>
        void Initialize(EnemySpawner spawner);

        /// <summary> Begin spawning. Called once when the encounter starts. </summary>
        void Start();

        /// <summary> Notify the strategy that an enemy has died. </summary>
        void OnEnemyDied(UnityEngine.GameObject enemy);

        /// <summary> Whether all waves/spawns have been completed and all enemies are dead. </summary>
        bool IsEncounterComplete { get; }

        /// <summary> Reset the strategy to its initial state (for room respawn). </summary>
        void Reset();
    }
}
