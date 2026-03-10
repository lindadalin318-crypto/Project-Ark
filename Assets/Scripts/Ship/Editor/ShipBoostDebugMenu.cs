#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

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

        [MenuItem("ProjectArk/Ship/Debug/Show Flash Overlay")]
        public static void ShowFlashOverlay()
        {
            var image = Object.FindFirstObjectByType<Image>(FindObjectsInactive.Include);
            image = FindNamedImage("BoostTrailFlashOverlay") ?? image;
            if (image == null)
            {
                Debug.LogWarning("[ShipBoostDebugMenu] BoostTrailFlashOverlay not found.");
                return;
            }

            var color = image.color;
            color.a = 0.7f;
            image.color = color;
            Debug.Log("[ShipBoostDebugMenu] Set BoostTrailFlashOverlay alpha to 0.7.");
        }

        [MenuItem("ProjectArk/Ship/Debug/Hide Flash Overlay")]
        public static void HideFlashOverlay()
        {
            var image = FindNamedImage("BoostTrailFlashOverlay");
            if (image == null)
            {
                Debug.LogWarning("[ShipBoostDebugMenu] BoostTrailFlashOverlay not found.");
                return;
            }

            var color = image.color;
            color.a = 0f;
            image.color = color;
            Debug.Log("[ShipBoostDebugMenu] Reset BoostTrailFlashOverlay alpha to 0.");
        }

        private static Image FindNamedImage(string objectName)
        {
            var images = Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var image in images)
            {
                if (image != null && image.gameObject.name == objectName)
                    return image;
            }

            return null;
        }
    }
}
#endif
