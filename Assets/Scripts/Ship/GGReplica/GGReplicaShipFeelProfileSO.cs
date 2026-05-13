using UnityEngine;

namespace ProjectArk.Ship
{
    [CreateAssetMenu(fileName = "GGReplicaShipFeelProfile", menuName = "ProjectArk/Ship/GG Replica/Feel Profile")]
    public class GGReplicaShipFeelProfileSO : ScriptableObject
    {
        [Header("Dodge")]
        [Min(0f)] [SerializeField] private float _dodgeForce = 13f;
        [Min(0f)] [SerializeField] private float _dodgeForceAfterDodge = 6f;
        [Min(0f)] [SerializeField] private float _dodgeInvulnerabilityTime = 0.15f;
        [Min(0f)] [SerializeField] private float _dodgeCacheTime = 0.12f;
        [Min(0f)] [SerializeField] private float _dodgeRechargeTime = 0.5f;
        [Min(1)] [SerializeField] private int _maxDodgeCharges = 1;
        [SerializeField] private float _speedModAfterDodge = 1.15f;
        [Min(0f)] [SerializeField] private float _speedModAfterDodgeTime = 0.2f;
        [Min(0f)] [SerializeField] private float _timeForActionAfterEndDodge = 0.12f;

        [Header("Boost")]
        [Min(0f)] [SerializeField] private float _boostSpeedMultiplier = 1.2f;
        [Min(0f)] [SerializeField] private float _afterBoostDrag = 2.5f;
        [Min(0f)] [SerializeField] private float _dragChangeTime = 0.15f;
        [Min(0f)] [SerializeField] private float _boostStartImpulse = 4f;
        [Min(0f)] [SerializeField] private float _boostDecayDuration = 0.2f;
        [Min(0f)] [SerializeField] private float _boostIgniteDuration = 0.08f;

        public float DodgeForce => _dodgeForce;
        public float DodgeForceAfterDodge => _dodgeForceAfterDodge;
        public float DodgeInvulnerabilityTime => _dodgeInvulnerabilityTime;
        public float DodgeCacheTime => _dodgeCacheTime;
        public float DodgeRechargeTime => _dodgeRechargeTime;
        public int MaxDodgeCharges => _maxDodgeCharges;
        public float SpeedModAfterDodge => _speedModAfterDodge;
        public float SpeedModAfterDodgeTime => _speedModAfterDodgeTime;
        public float TimeForActionAfterEndDodge => _timeForActionAfterEndDodge;
        public float BoostSpeedMultiplier => _boostSpeedMultiplier;
        public float AfterBoostDrag => _afterBoostDrag;
        public float DragChangeTime => _dragChangeTime;
        public float BoostStartImpulse => _boostStartImpulse;
        public float BoostDecayDuration => _boostDecayDuration;
        public float BoostIgniteDuration => _boostIgniteDuration;
    }
}
