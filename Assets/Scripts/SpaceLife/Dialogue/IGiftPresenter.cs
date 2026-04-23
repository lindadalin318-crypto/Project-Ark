using System;

namespace ProjectArk.SpaceLife.Dialogue
{
    /// <summary>
    /// Presentation contract for the gift-giving UI.
    /// Kept in the Domain layer so Coordinator can open/close it without referencing a concrete MonoBehaviour.
    /// </summary>
    /// <remarks>
    /// Phase 1 scope (Master Plan §5.2 P1): interface only, no implementations yet.
    /// Phase 2: <c>GiftUIPresenter</c> implements this and registers via <c>ServiceLocator</c>.
    /// Phase 3: Coordinator consumes it through <c>ServiceLocator.Get&lt;IGiftPresenter&gt;()</c>.
    ///
    /// Parameter convention: <paramref name="ShowGiftUI"/> takes <c>npcId</c> as a stable string
    /// (matching <c>NPCDataSO.NpcId</c> and <c>PlayerSaveData.RelationshipValues</c>), not an index or concrete SO reference.
    /// This keeps the Presenter Domain-only and avoids taking a hard dependency on Data-layer SOs.
    /// </remarks>
    public interface IGiftPresenter
    {
        /// <summary>
        /// Raised when the gift interaction finishes (gift given, cancelled, or otherwise closed).
        /// Subscribers (Coordinator) should release the hub interaction lock and re-enable movement.
        /// </summary>
        event Action OnGiftFinished;

        /// <summary>
        /// Opens the gift UI for the NPC identified by <paramref name="npcId"/>.
        /// </summary>
        /// <param name="npcId">Stable NPC id as stored in save data; must not be null/empty.</param>
        void ShowGiftUI(string npcId);

        /// <summary>
        /// Hides the gift UI (typically by collapsing <c>CanvasGroup.alpha</c> to 0).
        /// Must not <c>SetActive(false)</c> the panel — see Implement_rules.md §12.6 R5.
        /// </summary>
        void HideGiftUI();
    }
}
