using UnityEngine;
using Unity.Cinemachine;

namespace ProjectArk.Ship
{
    /// <summary>
    /// Registers a <see cref="CinemachineImpulseSource"/> to <see cref="HitFeedbackService"/>
    /// on startup. Attach to the same GameObject as the CinemachineCamera.
    /// </summary>
    [RequireComponent(typeof(CinemachineImpulseSource))]
    public class ImpulseSourceRegistrar : MonoBehaviour
    {
        private void Start()
        {
            var source = GetComponent<CinemachineImpulseSource>();
            HitFeedbackService.RegisterImpulseSource(source);
        }
    }
}
