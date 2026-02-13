using UnityEngine;

namespace ProjectArk.Level
{
    /// <summary>
    /// Data-driven world progress stage definition.
    /// Each stage represents an irreversible milestone (e.g., Boss A defeated, core area unlocked).
    /// WorldProgressManager checks stage requirements and advances automatically.
    /// </summary>
    [CreateAssetMenu(fileName = "New World Stage", menuName = "ProjectArk/Level/World Progress Stage")]
    public class WorldProgressStageSO : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stage index (0 = initial). Must be unique and sequential.")]
        [SerializeField] private int _stageIndex;

        [Tooltip("Human-readable stage name (e.g., 'Post-Guardian', 'Core Unlocked').")]
        [SerializeField] private string _stageName;

        [Header("Requirements")]
        [Tooltip("Boss IDs that must be defeated to reach this stage. ALL must be satisfied.")]
        [SerializeField] private string[] _requiredBossIDs;

        [Header("Effects")]
        [Tooltip("Door IDs that should be unlocked when this stage is reached.")]
        [SerializeField] private string[] _unlockDoorIDs;

        // ──────────────────── Public Properties ────────────────────

        /// <summary> Stage index (0-based sequential). </summary>
        public int StageIndex => _stageIndex;

        /// <summary> Human-readable stage name. </summary>
        public string StageName => _stageName;

        /// <summary> Boss IDs required to reach this stage. </summary>
        public string[] RequiredBossIDs => _requiredBossIDs;

        /// <summary> Door IDs unlocked by reaching this stage. </summary>
        public string[] UnlockDoorIDs => _unlockDoorIDs;
    }
}
