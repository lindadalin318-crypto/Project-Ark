using System;
using UnityEngine;

namespace ProjectArk.SpaceLife.Dialogue
{
    /// <summary>
    /// Authored effect descriptor executed by the dialogue runtime.
    /// </summary>
    [Serializable]
    public class DialogueEffectData
    {
        [SerializeField] private DialogueEffectType _effectType;
        [SerializeField] private int _intValue;
        [SerializeField] private string _flagKey;
        [SerializeField] private DialogueServiceExitType _serviceExitType;
        [SerializeField] private string _payload;

        public DialogueEffectType EffectType => _effectType;
        public int IntValue => _intValue;
        public string FlagKey => _flagKey;
        public DialogueServiceExitType ServiceExitType => _serviceExitType;
        public string Payload => _payload;
    }
}
