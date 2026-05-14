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
        {
            Move = Vector2.ClampMagnitude(move, 1f);
            BoostHeld = boostHeld;
            DodgePressed = dodgePressed;
            GrabHeld = grabHeld;
            HealHeld = healHeld;
            FireHeld = fireHeld;
        }

        public Vector2 Move { get; }
        public bool BoostHeld { get; }
        public bool DodgePressed { get; }
        public bool GrabHeld { get; }
        public bool HealHeld { get; }
        public bool FireHeld { get; }
    }
}
