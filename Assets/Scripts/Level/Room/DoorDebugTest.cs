using UnityEngine;

namespace ProjectArk.Level
{
    /// <summary>
    /// Temporary debug script to test if OnTriggerEnter2D fires on Door.
    /// Attach alongside Door component. Remove after debugging.
    /// </summary>
    public class DoorDebugTest : MonoBehaviour
    {
        private void Awake()
        {
            Debug.Log($"[DoorDebugTest] Awake on '{gameObject.name}', activeInHierarchy={gameObject.activeInHierarchy}, activeSelf={gameObject.activeSelf}");
            Debug.Log($"[DoorDebugTest] Layer={LayerMask.LayerToName(gameObject.layer)}, parent='{(transform.parent != null ? transform.parent.name : "none")}'");

            var col = GetComponent<Collider2D>();
            if (col != null)
                Debug.Log($"[DoorDebugTest] Collider: type={col.GetType().Name}, isTrigger={col.isTrigger}, enabled={col.enabled}, bounds={col.bounds}");
            else
                Debug.LogError($"[DoorDebugTest] NO Collider2D found on '{gameObject.name}'!");

            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
                Debug.Log($"[DoorDebugTest] Rigidbody2D: bodyType={rb.bodyType}, simulated={rb.simulated}");
            else
                Debug.LogWarning($"[DoorDebugTest] No Rigidbody2D on '{gameObject.name}'");
        }

        private void OnEnable()
        {
            Debug.Log($"[DoorDebugTest] OnEnable on '{gameObject.name}'");
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Debug.Log($"[DoorDebugTest] >>> OnTriggerEnter2D on '{gameObject.name}' by '{other.gameObject.name}' (Layer: {LayerMask.LayerToName(other.gameObject.layer)})");
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            // Only log once per second to avoid spam
            if (Time.frameCount % 60 == 0)
                Debug.Log($"[DoorDebugTest] OnTriggerStay2D on '{gameObject.name}' by '{other.gameObject.name}'");
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            Debug.Log($"[DoorDebugTest] >>> OnCollisionEnter2D on '{gameObject.name}' by '{collision.gameObject.name}'");
        }
    }
}
