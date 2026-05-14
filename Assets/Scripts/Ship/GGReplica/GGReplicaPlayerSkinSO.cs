using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectArk.Ship
{
    public enum GGReplicaViewState
    {
        Idle = 0,
        Boost = 1,
        Dodge = 2,
        Aim = 3,
        Fire = 4,
        HeavyFire = 5,
        HeavyAim = 6,
        Grab = 7,
        WeaponUseMoment = 8,
        Heal = 9,
        Undefined = 15
    }

    [Serializable]
    public sealed class GGReplicaViewSpritePack
    {
        public GGReplicaViewState State;
        [Min(0f)] public float FadeDuration = 0.2f;
        public Sprite SolidSprite;
        public Sprite LiquidSprite;
        public Sprite HighlightSprite;
        public Vector3 SpritesOffset;
    }

    [CreateAssetMenu(fileName = "GGReplicaPlayerSkin", menuName = "ProjectArk/Ship/GG Replica/Player Skin")]
    public sealed class GGReplicaPlayerSkinSO : ScriptableObject
    {
        [SerializeField] private GGReplicaViewSpritePack[] _stateToSpritesTable = Array.Empty<GGReplicaViewSpritePack>();

        [Header("Fixed Skin Fields")]
        [SerializeField] private Sprite _shipSpriteSolidGrabR;
        [SerializeField] private Sprite _shipSpriteSolidGrabL;
        [SerializeField] private Sprite _shipSpriteBack;
        [SerializeField] private Sprite _reactorSprite;
        [SerializeField] private Sprite _eyeSprite;
        [SerializeField] private Sprite _viewSilhouetteSprite;
        [SerializeField] private Sprite _dodgeSprite;
        [SerializeField] private Sprite _dodgeHalfSprite;

        [Header("Colors")]
        [SerializeField] private Color _shipHighlightColor = new Color(0.545f, 0.09f, 1f, 1f);
        [SerializeField] private Color _transitionColor = new Color(0.671f, 0f, 1f, 1f);

        public Sprite ShipSpriteSolidGrabR => _shipSpriteSolidGrabR;
        public Sprite ShipSpriteSolidGrabL => _shipSpriteSolidGrabL;
        public Sprite ShipSpriteBack => _shipSpriteBack;
        public Sprite ReactorSprite => _reactorSprite;
        public Sprite EyeSprite => _eyeSprite;
        public Sprite ViewSilhouetteSprite => _viewSilhouetteSprite;
        public Sprite DodgeSprite => _dodgeSprite;
        public Sprite DodgeHalfSprite => _dodgeHalfSprite;
        public Color ShipHighlightColor => _shipHighlightColor;
        public Color TransitionColor => _transitionColor;
        public IReadOnlyList<GGReplicaViewSpritePack> StateToSpritesTable => _stateToSpritesTable;

        public bool TryGetPack(GGReplicaViewState state, out GGReplicaViewSpritePack pack)
        {
            foreach (var candidate in _stateToSpritesTable)
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
