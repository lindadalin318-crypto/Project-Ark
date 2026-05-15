using UnityEngine;

namespace ProjectArk.Ship
{
    public enum GGReplicaGlitchState
    {
        Idle,
        Move,
        BoostHold,
        DodgeBurst,
        GrabHold,
        Heal,
        FireAim
    }

    public readonly struct GGReplicaGlitchInputFrame
    {
        public GGReplicaGlitchInputFrame(Vector2 move, bool boostHeld, bool dodgePressed, bool grabHeld, bool healHeld, bool fireHeld)
            : this(move, boostHeld, dodgePressed, grabHeld, healHeld, fireHeld, Vector2.zero)
        {
        }

        public GGReplicaGlitchInputFrame(Vector2 move, bool boostHeld, bool dodgePressed, bool grabHeld, bool healHeld, bool fireHeld, Vector2 aimDirection)
        {
            Move = Vector2.ClampMagnitude(move, 1f);
            AimDirection = aimDirection.sqrMagnitude > 0.001f ? aimDirection.normalized : Vector2.zero;
            BoostHeld = boostHeld;
            DodgePressed = dodgePressed;
            GrabHeld = grabHeld;
            HealHeld = healHeld;
            FireHeld = fireHeld;
        }

        public Vector2 Move { get; }
        public Vector2 AimDirection { get; }
        public bool BoostHeld { get; }
        public bool DodgePressed { get; }
        public bool GrabHeld { get; }
        public bool HealHeld { get; }
        public bool FireHeld { get; }
    }
}
