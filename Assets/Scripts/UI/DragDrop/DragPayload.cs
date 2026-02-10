using ProjectArk.Combat;

namespace ProjectArk.UI
{
    /// <summary> Where a drag operation originated from. </summary>
    public enum DragSource
    {
        Inventory,
        Slot
    }

    /// <summary>
    /// Lightweight data carrier for an in-progress drag operation.
    /// Holds the dragged item, its source, and (when dragged from a slot)
    /// the originating <see cref="WeaponTrack"/>.
    /// </summary>
    public class DragPayload
    {
        /// <summary> The item being dragged. </summary>
        public StarChartItemSO Item { get; }

        /// <summary> Where the drag started. </summary>
        public DragSource Source { get; }

        /// <summary> Non-null when dragged from an equipped slot. </summary>
        public WeaponTrack SourceTrack { get; }

        public DragPayload(StarChartItemSO item, DragSource source, WeaponTrack sourceTrack = null)
        {
            Item = item;
            Source = source;
            SourceTrack = sourceTrack;
        }
    }
}
