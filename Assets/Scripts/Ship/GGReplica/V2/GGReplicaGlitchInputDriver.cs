using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectArk.Ship
{
    [RequireComponent(typeof(GGReplicaGlitchMotor))]
    public sealed class GGReplicaGlitchInputDriver : MonoBehaviour
    {
        [SerializeField] private GGReplicaGlitchMotor _motor;

        private void Awake()
        {
            if (_motor == null) _motor = GetComponent<GGReplicaGlitchMotor>();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || _motor == null) return;

            Vector2 move = Vector2.zero;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) move.x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) move.x += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) move.y -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) move.y += 1f;

            var frame = new GGReplicaGlitchInputFrame(
                move,
                keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed,
                keyboard.spaceKey.wasPressedThisFrame,
                keyboard.eKey.isPressed,
                keyboard.qKey.isPressed,
                Mouse.current != null && Mouse.current.leftButton.isPressed);

            _motor.ApplyInput(frame, Time.deltaTime);
        }
    }
}
