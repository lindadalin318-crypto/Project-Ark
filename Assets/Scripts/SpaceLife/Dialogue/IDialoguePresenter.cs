using System;
using UnityEngine;

namespace ProjectArk.SpaceLife.Dialogue
{
    /// <summary>
    /// Presentation contract for dialogue UI.
    /// Defined in the Domain layer so Coordinator / Runner can consume any concrete UI implementation
    /// (e.g. <c>DialogueUIPresenter</c> built in Phase 2) without a direct MonoBehaviour reference.
    /// </summary>
    /// <remarks>
    /// Phase 1 scope (Master Plan §5.2 P1): interface only, no implementations yet.
    /// Phase 2: <c>DialogueUIPresenter</c> implements this and registers via
    /// <c>ServiceLocator.Register&lt;IDialoguePresenter&gt;(this)</c>.
    /// Phase 3: <c>SpaceLifeDialogueCoordinator</c> resolves it through
    /// <c>ServiceLocator.Get&lt;IDialoguePresenter&gt;()</c>, never by concrete type.
    /// </remarks>
    public interface IDialoguePresenter
    {
        /// <summary>
        /// Raised when the player selects a visible choice on the current node.
        /// The payload is the <c>choiceId</c> produced by the Runner's <c>DialogueNodeViewModel.ChoiceViewModel</c>.
        /// Subscribers (Coordinator) should forward it to <c>DialogueRunner.Choose(choiceId)</c>.
        /// </summary>
        event Action<string> OnChoiceSelected;

        /// <summary>
        /// Renders the given dialogue node view model. Must be idempotent when called multiple times
        /// with different view models (transition from node to node during an active session).
        /// </summary>
        void ShowNode(DialogueNodeViewModel viewModel);

        /// <summary>
        /// Hides the dialogue UI (typically by collapsing <c>CanvasGroup.alpha</c> to 0).
        /// Must not <c>SetActive(false)</c> the panel — see Implement_rules.md §12.6 R5.
        /// </summary>
        void HideDialogue();

        /// <summary>
        /// Assigns the speaker avatar sprite for the currently active dialogue. Passing <c>null</c>
        /// hides the avatar image (Terminal / AI sessions without a portrait).
        /// </summary>
        /// <remarks>
        /// Phase 3 §7.1: promoted from a concrete-type-only method on <c>DialogueUIPresenter</c>
        /// into the interface so <see cref="SpaceLifeDialogueCoordinator"/> can depend on
        /// <see cref="IDialoguePresenter"/> without casting back to the MonoBehaviour subtype.
        /// </remarks>
        void SetSpeakerAvatar(Sprite avatar);
    }
}
