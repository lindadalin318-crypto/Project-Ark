using UnityEngine;

namespace ProjectArk.Level
{
    /// <summary>
    /// Lightweight room-level camera policy.
    /// FollowPlayer is the default exploration behavior; HardConfine is an opt-in special case.
    /// </summary>
    public enum RoomCameraPolicy
    {
        FollowPlayer = 0,
        HardConfine = 1
    }
}
