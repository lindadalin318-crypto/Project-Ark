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
            AresTrail,
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
        [SerializeField] private bool _showAresTrail = true;
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
                    visibility.ShowAresTrail);
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

        private void ApplyVisibilityOnly(LayerVisibilityState visibility)
        {
            _boostTrailView.DebugApplyVisibilityMask(
                visibility.ShowAresTrail,
                visibility.ShowBloom);
        }

        private bool CanPreview()
        {
            return Application.isPlaying && _boostTrailView != null;
        }

        private LayerVisibilityState BuildVisibilityState()
        {
            return new LayerVisibilityState(
                ResolveVisibility(SoloLayer.AresTrail, _showAresTrail),
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
            public readonly bool ShowAresTrail;
            public readonly bool ShowBloom;

            public LayerVisibilityState(bool showAresTrail, bool showBloom)
            {
                ShowAresTrail = showAresTrail;
                ShowBloom = showBloom;
            }
        }
    }
}
