#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ProjectArk.Ship.Editor
{
    /// <summary>
    /// Play Mode only helpers for deterministic Boost VFX validation.
    /// </summary>
    public static class ShipBoostDebugMenu
    {
        [MenuItem("ProjectArk/Ship/Debug/Trigger Boost Once")]
        public static void TriggerBoostOnce()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[ShipBoostDebugMenu] Enter Play Mode before triggering Boost.");
                return;
            }

            var boost = Object.FindFirstObjectByType<ShipBoost>();
            if (boost == null)
            {
                Debug.LogWarning("[ShipBoostDebugMenu] No ShipBoost found in scene.");
                return;
            }

            boost.ForceActivate();
            Debug.Log("[ShipBoostDebugMenu] Triggered ShipBoost.ForceActivate().");
        }

        [MenuItem("ProjectArk/Ship/Debug/Show Activation Halo")]
        public static void ShowActivationHalo()
        {
            var halo = FindNamedSpriteRenderer("BoostActivationHalo");
            if (halo == null)
            {
                Debug.LogWarning("[ShipBoostDebugMenu] BoostActivationHalo not found.");
                return;
            }

            halo.enabled = true;
            halo.transform.localScale = new Vector3(1.45f, 1.45f, 1f);
            halo.color = new Color(3.2f, 2.2f, 1.4f, 1.15f);
            Debug.Log("[ShipBoostDebugMenu] Forced BoostActivationHalo visible.");
        }

        [MenuItem("ProjectArk/Ship/Debug/Hide Activation Halo")]
        public static void HideActivationHalo()
        {
            var halo = FindNamedSpriteRenderer("BoostActivationHalo");
            if (halo == null)
            {
                Debug.LogWarning("[ShipBoostDebugMenu] BoostActivationHalo not found.");
                return;
            }

            halo.enabled = false;
            halo.transform.localScale = Vector3.one;
            halo.color = new Color(3.2f, 2.2f, 1.4f, 0f);
            Debug.Log("[ShipBoostDebugMenu] Reset BoostActivationHalo.");
        }

        private static SpriteRenderer FindNamedSpriteRenderer(string objectName)
        {
            var renderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var renderer in renderers)
            {
                if (renderer != null && renderer.gameObject.name == objectName)
                    return renderer;
            }

            return null;
        }
    }
}
#endif
