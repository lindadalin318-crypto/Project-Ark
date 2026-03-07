namespace ProjectArk.Combat
{
    /// <summary>
    /// Encapsulates a single loadout configuration:
    /// two weapon tracks (Primary/Secondary) and a Light Sail.
    /// Each WeaponTrack owns its own satellite list (Per-Track).
    /// Pure C# class — not a MonoBehaviour.
    /// </summary>
    public class LoadoutSlot
    {
        /// <summary> Primary weapon track for this loadout. </summary>
        public readonly WeaponTrack PrimaryTrack;

        /// <summary> Secondary weapon track for this loadout. </summary>
        public readonly WeaponTrack SecondaryTrack;

        /// <summary> Currently equipped Light Sail SO, or null. </summary>
        public LightSailSO EquippedLightSailSO;

        /// <summary>
        /// Creates a new LoadoutSlot with two fresh, independent WeaponTrack instances.
        /// </summary>
        public LoadoutSlot()
        {
            PrimaryTrack   = new WeaponTrack(WeaponTrack.TrackId.Primary);
            SecondaryTrack = new WeaponTrack(WeaponTrack.TrackId.Secondary);
        }

        /// <summary>
        /// Clears all equipped items from both tracks and removes LightSail.
        /// Satellite lists are cleared inside WeaponTrack.ClearAll().
        /// Does NOT dispose Runners — caller is responsible for that.
        /// </summary>
        public void Clear()
        {
            PrimaryTrack.ClearAll();
            SecondaryTrack.ClearAll();
            EquippedLightSailSO = null;
        }
    }
}
