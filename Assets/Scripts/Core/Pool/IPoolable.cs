namespace ProjectArk.Core
{
    /// <summary>
    /// Contract for objects managed by <see cref="GameObjectPool"/>.
    /// Implement on any MonoBehaviour that needs setup/cleanup when pooled.
    /// </summary>
    public interface IPoolable
    {
        /// <summary> Called when retrieved from pool. Reset state here. </summary>
        void OnGetFromPool();

        /// <summary> Called when returned to pool. Cleanup here. </summary>
        void OnReturnToPool();
    }
}
