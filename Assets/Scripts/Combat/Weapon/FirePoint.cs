using UnityEngine;

namespace ProjectArk.Combat
{
    /// <summary>
    /// Marks a Transform as the weapon muzzle position.
    /// Attach to a child GameObject of the Ship (e.g., at the tip of the hull).
    /// </summary>
    public class FirePoint : MonoBehaviour
    {
        /// <summary> World-space muzzle position. </summary>
        public Vector3 Position => transform.position;

        /// <summary> World-space forward direction (inherits ship rotation). </summary>
        public Vector2 Direction => transform.up;
    }
}
