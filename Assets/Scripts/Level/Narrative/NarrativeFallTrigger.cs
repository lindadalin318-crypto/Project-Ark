using UnityEngine;
using UnityEngine.Playables;

namespace ProjectArk.Level
{
    /// <summary>
    /// Placeholder stub for narrative fall events (e.g., floor collapse, 
    /// scripted descent to a lower level). Not functional — provides serialized
    /// fields and TODO comments describing the full implementation for future work.
    /// 
    /// Intended usage: Place on a trigger collider in the scene. When the player
    /// enters the trigger, a cinematic fall sequence plays (disable confiner,
    /// animate camera, PrimeTween vertical scroll, re-enable confiner at target floor).
    /// 
    /// This is a PLACEHOLDER — functionality will be implemented during the
    /// narrative/cinematic pass.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class NarrativeFallTrigger : MonoBehaviour
    {
        // ──────────────────── Configuration ────────────────────

        [Header("Target")]
        [Tooltip("The Room to fall into (on a lower floor).")]
        [SerializeField] private Room _targetRoom;

        [Tooltip("Spawn point in the target room where the player lands.")]
        [SerializeField] private Transform _landingPoint;

        [Header("Visual Effects")]
        [Tooltip("Particle system prefab to spawn during the fall (e.g., debris, wind).")]
        [SerializeField] private ParticleSystem _fallParticlePrefab;

        [Tooltip("Optional Timeline asset for the fall cinematic sequence.")]
        [SerializeField] private PlayableAsset _fallTimeline;

        [Header("Audio")]
        [Tooltip("Sound effect for the fall sequence (e.g., rumble, wind).")]
        [SerializeField] private AudioClip _fallSFX;

        [Header("Camera")]
        [Tooltip("Duration of the camera vertical scroll (seconds).")]
        [SerializeField] private float _cameraScrollDuration = 1.5f;

        [Tooltip("How far the camera scrolls vertically during the fall (world units).")]
        [SerializeField] private float _cameraScrollDistance = 30f;

        [Header("Player Detection")]
        [Tooltip("Layer mask for the player ship.")]
        [SerializeField] private LayerMask _playerLayer;

        // ──────────────────── Runtime State ────────────────────

        private bool _hasTriggered;

        // ──────────────────── Lifecycle ────────────────────

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (!col.isTrigger)
            {
                col.isTrigger = true;
                Debug.LogWarning($"[NarrativeFallTrigger] {gameObject.name}: Collider was not set as trigger. Auto-fixed.");
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_hasTriggered) return;
            if ((_playerLayer.value & (1 << other.gameObject.layer)) == 0) return;

            _hasTriggered = true;
            TriggerFall();
        }

        // ──────────────────── Public API ────────────────────

        /// <summary>
        /// Initiate the narrative fall sequence.
        /// 
        /// PLACEHOLDER — Full implementation steps (TODO):
        /// 
        /// 1. Disable player input (InputHandler.enabled = false)
        /// 2. Disable camera confiner (RoomCameraConfiner or CinemachineConfiner2D)
        /// 3. If _fallTimeline is assigned, play it via PlayableDirector
        ///    Otherwise, use manual scripted sequence:
        ///    a. Play _fallSFX via AudioManager.PlaySFX2D()
        ///    b. Spawn _fallParticlePrefab at player position
        ///    c. PrimeTween: animate camera Y position downward over _cameraScrollDuration
        ///       (scroll by _cameraScrollDistance world units)
        ///    d. Simultaneously PrimeTween: fade screen alpha slightly for dramatic effect
        /// 4. Teleport player to _landingPoint.position
        /// 5. RoomManager.EnterRoom(_targetRoom)
        /// 6. Re-enable camera confiner for the target room
        /// 7. Re-enable player input
        /// 8. Fire LevelEvents.RaiseFloorChanged() with target room's floor level
        /// 9. Optional: brief screen shake on landing
        /// </summary>
        public void TriggerFall()
        {
            // ── Placeholder implementation: just log and teleport ──
            Debug.Log($"[NarrativeFallTrigger] PLACEHOLDER — Fall sequence triggered on {gameObject.name}. " +
                      $"Target: {(_targetRoom != null ? _targetRoom.RoomID : "null")}. " +
                      "Full cinematic implementation pending.");

            // Minimal functionality: teleport player to target (no cinematic)
            if (_targetRoom == null || _landingPoint == null)
            {
                Debug.LogWarning("[NarrativeFallTrigger] TargetRoom or LandingPoint not assigned!");
                return;
            }

            var inputHandler = Core.ServiceLocator.Get<Ship.InputHandler>();
            if (inputHandler != null)
            {
                inputHandler.transform.position = _landingPoint.position;
            }

            var roomManager = Core.ServiceLocator.Get<RoomManager>();
            if (roomManager != null)
            {
                roomManager.EnterRoom(_targetRoom);
            }
        }

        /// <summary>
        /// Reset the trigger so it can fire again (e.g., after respawn).
        /// </summary>
        public void ResetTrigger()
        {
            _hasTriggered = false;
        }
    }
}
