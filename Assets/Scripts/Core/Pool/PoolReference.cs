using UnityEngine;

namespace ProjectArk.Core
{
    /// <summary>
    /// Stamped on pooled instances by <see cref="GameObjectPool"/>.
    /// Provides a self-return mechanism so objects can return themselves to their owner pool.
    /// </summary>
    public class PoolReference : MonoBehaviour
    {
        /// <summary> The pool this instance belongs to. Set by GameObjectPool on creation. </summary>
        public GameObjectPool OwnerPool { get; set; }

        /// <summary> Returns this GameObject to its owner pool. </summary>
        public void ReturnToPool()
        {
            if (OwnerPool != null)
                OwnerPool.Return(gameObject);
            else
                Debug.LogWarning($"[PoolReference] {gameObject.name} has no OwnerPool set, destroying instead.");
        }
    }
}
