using UnityEngine;

namespace ProjectArk.Ship
{
    /// <summary>
    /// Explicit preview-only Play Mode inspector surface for BoostTrail validation.
    ///
    /// This component is not an authority tool and never auto-repairs references,
    /// never auto-restores state, and never drives the live chain from lifecycle methods.
    /// Live output changes only happen through explicit preview button calls.
    /// </summary>
    [AddComponentMenu("ProjectArk/Ship/Debug/BoostTrail Preview Only")]
    [DisallowMultipleComponent]
    public class BoostTrailDebugManager : MonoBehaviour
    {
        public enum DebugMode
        {
            ObserveRuntime,
            ForceSustainPreview
        }

        public enum SoloLayer
        {
            None,
            MainTrail,
            FlameTrail,
            FlameCore,
            EmberTrail,
            EmberSparks,
            EnergyLayer2,
            EnergyLayer3,
            Bloom
        }

        [Header("References")]
        [Tooltip("Preview target only. Must be wired explicitly by prefab/build authority; this debug component never auto-assigns it.")]
        [SerializeField] private BoostTrailView _boostTrailView;

        [Header("Inspector Debug")]
        [Tooltip("Preview-only master switch. Enabling it only changes behavior when an explicit preview/apply button is pressed.")]
        [SerializeField] private bool _enableInspectorDebug;

        [Tooltip("Observe the authored runtime sequence, or pin the sustain stack to a fixed preview intensity.")]
        [SerializeField] private DebugMode _debugMode = DebugMode.ObserveRuntime;

        [Tooltip("Fixed master intensity used only when Debug Mode = ForceSustainPreview.")]
        [Range(0f, 1f)]
        [SerializeField] private float _previewIntensity = 1f;

        [Tooltip("Optional solo focus. When set, all manual toggles below are ignored and only the selected layer remains visible.")]
        [SerializeField] private SoloLayer _soloLayer = SoloLayer.None;

        [Header("Layer Visibility")]
        [SerializeField] private bool _showMainTrail = true;
        [SerializeField] private bool _showFlameTrail = true;
        [SerializeField] private bool _showFlameCore = true;
        [SerializeField] private bool _showEmberTrail = true;
        [SerializeField] private bool _showEmberSparks = true;
        [SerializeField] private bool _showEnergyLayer2 = true;
        [SerializeField] private bool _showEnergyLayer3 = true;
        [SerializeField] private bool _showBloom = true;

        /// <summary>
        /// Applies the current Inspector mask and optional sustain preview immediately.
        /// This is an explicit preview action; nothing is applied automatically.
        /// </summary>
        public void ApplyInspectorDebugNow()
        {
            if (!Application.isPlaying || _boostTrailView == null)
                return;

            LayerVisibilityState visibility = BuildVisibilityState();

            if (_enableInspectorDebug && _debugMode == DebugMode.ForceSustainPreview)
            {
                _boostTrailView.DebugForceSustainPreview(
                    _previewIntensity,
                    visibility.ShowMainTrail,
                    visibility.ShowFlameTrail,
                    visibility.ShowEmberTrail,
                    visibility.ShowEnergyLayer2,
                    visibility.ShowEnergyLayer3);
            }

            ApplyVisibilityOnly(visibility);
        }

        /// <summary>
        /// Replays the full Boost startup chain once.
        /// Best used with DebugMode.ObserveRuntime so the authored sequence remains readable.
        /// </summary>
        public void PreviewBoostStart()
        {
            if (!CanPreview())
                return;

            _boostTrailView.ResetState();
            _boostTrailView.OnBoostStart();

            if (_enableInspectorDebug)
                ApplyVisibilityOnly(BuildVisibilityState());
        }

        /// <summary>
        /// Replays the Boost shutdown chain once.
        /// Best used with DebugMode.ObserveRuntime so the authored sequence is not pinned by sustain preview.
        /// </summary>
        public void PreviewBoostEnd()
        {
            if (!CanPreview())
                return;

            _boostTrailView.OnBoostEnd();

            if (_enableInspectorDebug)
                ApplyVisibilityOnly(BuildVisibilityState());
        }

        /// <summary>
        /// Resets the BoostTrail stack back to baseline.
        /// </summary>
        public void ResetPreview()
        {
            if (!CanPreview())
                return;

            _boostTrailView.ResetState();
        }

        /// <summary>
        /// Replays the FlameCore burst only.
        /// </summary>
        public void PreviewFlameCoreBurst()
        {
            if (!CanPreview())
                return;

            _boostTrailView.ResetState();
            _boostTrailView.DebugPreviewFlameCoreBurst();

            if (_enableInspectorDebug)
                ApplyVisibilityOnly(BuildVisibilityState());
        }

        /// <summary>
        /// Replays the EmberSparks burst only.
        /// </summary>
        public void PreviewEmberSparksBurst()
        {
            if (!CanPreview())
                return;

            _boostTrailView.ResetState();
            _boostTrailView.DebugPreviewEmberSparksBurst();

            if (_enableInspectorDebug)
                ApplyVisibilityOnly(BuildVisibilityState());
        }

        /// <summary>
        /// Replays the Bloom burst only.
        /// </summary>
        public void PreviewBloomBurst()
        {
            if (!CanPreview())
                return;

            _boostTrailView.ResetState();
            _boostTrailView.DebugPreviewBloomBurst();

            if (_enableInspectorDebug)
                ApplyVisibilityOnly(BuildVisibilityState());
        }

        /// <summary>
        /// Forces FlameTrail_R only into sustained preview, isolating it from all other layers.
        /// </summary>
        public void PreviewFlameTrailR()
        {
            if (!CanPreview())
                return;

            _boostTrailView.ResetState();
            _boostTrailView.DebugPreviewFlameTrailR();
        }

        /// <summary>
        /// Forces FlameTrail_B only into sustained preview, isolating it from all other layers.
        /// </summary>
        public void PreviewFlameTrailB()
        {
            if (!CanPreview())
                return;

            _boostTrailView.ResetState();
            _boostTrailView.DebugPreviewFlameTrailB();
        }

        /// <summary>
        /// Forces FlameTrail_R + FlameTrail_B into sustained preview at full intensity,
        /// isolating them from all other layers for quick visual check.
        /// </summary>
        public void PreviewFlameTrailBoth()
        {
            if (!CanPreview())
                return;

            _boostTrailView.ResetState();
            _boostTrailView.DebugPreviewFlameTrailBoth();
        }

        /// <summary>
        /// Forces EmberTrail into sustained preview at full intensity,
        /// isolating it from all other layers for quick visual check.
        /// </summary>
        public void PreviewEmberTrailSustain()
        {
            if (!CanPreview())
                return;

            _boostTrailView.ResetState();
            _boostTrailView.DebugPreviewEmberTrailSustain();
        }

        private void ApplyVisibilityOnly(LayerVisibilityState visibility)
        {
            _boostTrailView.DebugApplyVisibilityMask(
                visibility.ShowMainTrail,
                visibility.ShowFlameTrail,
                visibility.ShowFlameCore,
                visibility.ShowEmberTrail,
                visibility.ShowEmberSparks,
                visibility.ShowEnergyLayer2,
                visibility.ShowEnergyLayer3,
                visibility.ShowBloom);
        }

        private bool CanPreview()
        {
            return Application.isPlaying && _boostTrailView != null;
        }

        private LayerVisibilityState BuildVisibilityState()
        {
            return new LayerVisibilityState(
                ResolveVisibility(SoloLayer.MainTrail, _showMainTrail),
                ResolveVisibility(SoloLayer.FlameTrail, _showFlameTrail),
                ResolveVisibility(SoloLayer.FlameCore, _showFlameCore),
                ResolveVisibility(SoloLayer.EmberTrail, _showEmberTrail),
                ResolveVisibility(SoloLayer.EmberSparks, _showEmberSparks),
                ResolveVisibility(SoloLayer.EnergyLayer2, _showEnergyLayer2),
                ResolveVisibility(SoloLayer.EnergyLayer3, _showEnergyLayer3),
                ResolveVisibility(SoloLayer.Bloom, _showBloom));
        }

        private bool ResolveVisibility(SoloLayer targetLayer, bool manualToggleValue)
        {
            return _soloLayer == SoloLayer.None
                ? manualToggleValue
                : _soloLayer == targetLayer;
        }

        private readonly struct LayerVisibilityState
        {
            public readonly bool ShowMainTrail;
            public readonly bool ShowFlameTrail;
            public readonly bool ShowFlameCore;
            public readonly bool ShowEmberTrail;
            public readonly bool ShowEmberSparks;
            public readonly bool ShowEnergyLayer2;
            public readonly bool ShowEnergyLayer3;
            public readonly bool ShowBloom;

            public LayerVisibilityState(
                bool showMainTrail,
                bool showFlameTrail,
                bool showFlameCore,
                bool showEmberTrail,
                bool showEmberSparks,
                bool showEnergyLayer2,
                bool showEnergyLayer3,
                bool showBloom)
            {
                ShowMainTrail = showMainTrail;
                ShowFlameTrail = showFlameTrail;
                ShowFlameCore = showFlameCore;
                ShowEmberTrail = showEmberTrail;
                ShowEmberSparks = showEmberSparks;
                ShowEnergyLayer2 = showEnergyLayer2;
                ShowEnergyLayer3 = showEnergyLayer3;
                ShowBloom = showBloom;
            }
        }
    }
}
