using UnityEngine;

namespace ProjectArk.Level
{
    /// <summary>
    /// Configuration data for a checkpoint. Defines what happens on activation
    /// (restore HP/heat, play SFX) and stores display metadata.
    /// </summary>
    [CreateAssetMenu(fileName = "New Checkpoint", menuName = "Project Ark/Level/Checkpoint")]
    public class CheckpointSO : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique identifier for this checkpoint. Used by save system.")]
        [SerializeField] private string _checkpointID;

        [Tooltip("Display name shown in UI / map.")]
        [SerializeField] private string _displayName;

        [Header("Restoration")]
        [Tooltip("If true, fully restore player HP on activation.")]
        [SerializeField] private bool _restoreHP = true;

        [Tooltip("If true, fully reset heat to 0 on activation.")]
        [SerializeField] private bool _restoreHeat = true;

        [Header("Feedback")]
        [Tooltip("Sound effect played when the checkpoint is activated.")]
        [SerializeField] private AudioClip _activationSFX;

        // ──────────────────── Public Properties ────────────────────

        /// <summary> Unique checkpoint identifier (save key). </summary>
        public string CheckpointID => _checkpointID;

        /// <summary> Display name for UI. </summary>
        public string DisplayName => _displayName;

        /// <summary> Whether activating this checkpoint restores HP. </summary>
        public bool RestoreHP => _restoreHP;

        /// <summary> Whether activating this checkpoint resets heat. </summary>
        public bool RestoreHeat => _restoreHeat;

        /// <summary> Activation sound effect (nullable). </summary>
        public AudioClip ActivationSFX => _activationSFX;
    }
}
