using PrimeTween;
using UnityEngine;

namespace ProjectArk.Ship
{
    /// <summary>
    /// Handles short weapon fire visual feedback for the Canary ship.
    /// Driven by ShipView via CombatEvents.OnPlayerProjectileFired.
    /// Does not subscribe to events directly.
    /// </summary>
    public class ShipFireVisuals : MonoBehaviour
    {
        [Header("Sprite Layers")]
        [SerializeField] private SpriteRenderer _weaponMountRenderer;
        [SerializeField] private SpriteRenderer _coreRenderer;

        [Header("Settings")]
        [SerializeField] private ShipJuiceSettingsSO _juiceSettings;

        [Header("Enable Toggles")]
        [SerializeField] private bool _enableAll = true;
        [SerializeField] private bool _enableWeaponMountFlash = true;
        [SerializeField] private bool _enableCorePulse = true;

        private Color _weaponMountBaseColor;
        private Color _coreBaseColor;
        private Sequence _fireSequence;

        /// <summary>
        /// Called by ShipView after Awake to pass baseline colors.
        /// </summary>
        public void Initialize(Color weaponMountBase, Color coreBase)
        {
            _weaponMountBaseColor = weaponMountBase;
            _coreBaseColor = coreBase;
        }

        /// <summary>
        /// Triggers the short fire feedback at the current weapon mount.
        /// </summary>
        public void OnWeaponFired(Vector2 spawnPosition, Vector2 fireDirection)
        {
            if (!_enableAll)
            {
                return;
            }

            ResetActiveTweenOnly();

            float attackDuration = _juiceSettings != null ? _juiceSettings.FireFlashAttackDuration : 0.025f;
            float releaseDuration = _juiceSettings != null ? _juiceSettings.FireFlashReleaseDuration : 0.08f;
            Color weaponFlashColor = _juiceSettings != null ? _juiceSettings.FireWeaponMountFlashColor : new Color(0.4f, 1.4f, 1.8f, 1f);
            Color coreFlashColor = _juiceSettings != null ? _juiceSettings.FireCorePulseColor : new Color(0.5f, 1.6f, 2.2f, 1f);
            float weaponScalePeak = _juiceSettings != null ? _juiceSettings.FireWeaponMountScalePeak : 1.14f;
            float coreScalePeak = _juiceSettings != null ? _juiceSettings.FireCoreScalePeak : 1.08f;

            if (_enableWeaponMountFlash && _weaponMountRenderer != null)
            {
                _weaponMountRenderer.color = weaponFlashColor;
                _weaponMountRenderer.transform.localScale = Vector3.one * weaponScalePeak;
            }

            if (_enableCorePulse && _coreRenderer != null)
            {
                _coreRenderer.color = coreFlashColor;
                _coreRenderer.transform.localScale = Vector3.one * coreScalePeak;
            }

            _fireSequence = Sequence.Create()
                .ChainDelay(attackDuration)
                .ChainCallback(RestoreColors)
                .ChainDelay(releaseDuration)
                .ChainCallback(RestoreScales);
        }

        public void ResetState()
        {
            ResetActiveTweenOnly();
            RestoreColors();
            RestoreScales();
        }

        private void ResetActiveTweenOnly()
        {
            _fireSequence.Stop();
        }

        private void RestoreColors()
        {
            if (_weaponMountRenderer != null)
            {
                _weaponMountRenderer.color = _weaponMountBaseColor;
            }

            if (_coreRenderer != null)
            {
                _coreRenderer.color = _coreBaseColor;
            }
        }

        private void RestoreScales()
        {
            if (_weaponMountRenderer != null)
            {
                _weaponMountRenderer.transform.localScale = Vector3.one;
            }

            if (_coreRenderer != null)
            {
                _coreRenderer.transform.localScale = Vector3.one;
            }
        }
    }
}
