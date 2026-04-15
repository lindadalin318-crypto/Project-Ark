using UnityEngine;

namespace ProjectArk.Level
{
    /// <summary>
    /// Marker component for a Room's static geometry authoring root.
    /// Lives on `Navigation/Geometry` and provides a stable validator/tooling anchor.
    /// Does not own runtime wall state.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoomGeometryRoot : MonoBehaviour
    {
    }
}
