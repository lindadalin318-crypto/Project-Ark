using System.Collections.Generic;

namespace ProjectArk.SpaceLife.Dialogue
{
    /// <summary>
    /// Executes the limited MVP effect set for dialogue choices and nodes.
    /// </summary>
    public sealed class DialogueEffectExecutor
    {
        private readonly DialogueFlagStore _flagStore;

        public DialogueEffectExecutor(DialogueFlagStore flagStore)
        {
            _flagStore = flagStore;
        }

        public DialogueServiceExit Execute(IReadOnlyList<DialogueEffectData> effects, DialogueContext context)
        {
            if (effects == null || effects.Count == 0 || context == null)
            {
                return DialogueServiceExit.None;
            }

            DialogueServiceExitType exitType = DialogueServiceExitType.None;
            string payload = string.Empty;
            bool shouldEndDialogue = false;

            for (int i = 0; i < effects.Count; i++)
            {
                DialogueEffectData effect = effects[i];
                if (effect == null)
                {
                    continue;
                }

                switch (effect.EffectType)
                {
                    case DialogueEffectType.SetFlag:
                        context.SetFlag(effect.FlagKey);
                        _flagStore?.Set(effect.FlagKey);
                        break;

                    case DialogueEffectType.ClearFlag:
                        context.ClearFlag(effect.FlagKey);
                        _flagStore?.Clear(effect.FlagKey);
                        break;

                    case DialogueEffectType.AddRelationship:
                        context.AddRelationship(effect.IntValue);
                        break;

                    case DialogueEffectType.EmitServiceExit:
                        exitType = effect.ServiceExitType;
                        payload = effect.Payload;
                        break;

                    case DialogueEffectType.EndDialogue:
                        shouldEndDialogue = true;
                        break;
                }
            }

            if (exitType == DialogueServiceExitType.None && !shouldEndDialogue)
            {
                return DialogueServiceExit.None;
            }

            return new DialogueServiceExit(exitType, payload, shouldEndDialogue);
        }
    }
}
