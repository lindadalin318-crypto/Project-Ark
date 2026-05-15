using System;
using UnityEngine;

namespace ProjectArk.Ship
{
    public enum GGReplicaVisualState
    {
        Normal,
        Boost,
        Dodge,
        Fire,
        FireBoost
    }

    [Serializable]
    public class GGReplicaSpritePack
    {
        public GGReplicaVisualState State;
        [Min(0f)] public float FadeDuration = 0.2f;
        public Sprite SolidSprite;
        public Sprite LiquidSprite;
        public Sprite HighlightSprite;
        public Vector3 SpritesOffset;
    }

    [CreateAssetMenu(fileName = "GGReplicaShipVisualProfile", menuName = "ProjectArk/Ship/GG Replica/Visual Profile")]
    public class GGReplicaShipVisualProfileSO : ScriptableObject
    {
        [Header("Sprite Packs")]
        [SerializeField] private GGReplicaSpritePack[] _spritePacks = Array.Empty<GGReplicaSpritePack>();

        [Header("Persistent Layers")]
        [SerializeField] private Sprite _backSprite;
        [SerializeField] private Sprite _coreSprite;
        [SerializeField] private Sprite _dodgeGhostSprite;

        [Header("Audio")]
        [SerializeField] private AudioClip _boostIgniteClip;
        [SerializeField] private AudioClip _boostLoopClip;
        [SerializeField] private AudioClip _dodgeClip;
        [SerializeField] private AudioClip _fireClip;
        [SerializeField] private AudioClip _healClip;

        public Sprite BackSprite => _backSprite;
        public Sprite CoreSprite => _coreSprite;
        public Sprite DodgeGhostSprite => _dodgeGhostSprite;
        public AudioClip BoostIgniteClip => _boostIgniteClip;
        public AudioClip BoostLoopClip => _boostLoopClip;
        public AudioClip DodgeClip => _dodgeClip;
        public AudioClip FireClip => _fireClip;
        public AudioClip HealClip => _healClip;

        public bool TryGetPack(GGReplicaVisualState state, out GGReplicaSpritePack pack)
        {
            foreach (var candidate in _spritePacks)
            {
                if (candidate != null && candidate.State == state)
                {
                    pack = candidate;
                    return true;
                }
            }

            pack = null;
            return false;
        }
    }
}
