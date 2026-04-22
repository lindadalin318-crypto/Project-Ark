using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using ProjectArk.Core;
using ProjectArk.Core.Audio;
using ProjectArk.Ship;
using UnityEngine;

namespace ProjectArk.SpaceLife
{
    public class SpaceLifeManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private GameObject _spaceLifePlayerPrefab;
        [SerializeField] private Transform _spaceLifeSpawnPoint;
        [SerializeField] private Camera _spaceLifeCamera;
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private GameObject _spaceLifeSceneRoot;
        [SerializeField] private GameObject _shipRoot;
        [SerializeField] private SpaceLifeInputHandler _spaceLifeInputHandler;

        [Header("Audio")]
        [SerializeField] private AudioClip _enterSpaceLifeSFX;
        [SerializeField] private AudioClip _exitSpaceLifeSFX;

        [Header("State")]
        [SerializeField] private bool _isInSpaceLifeMode;

        private GameObject _currentPlayer;
        private InputHandler _shipInputHandler;
        private AudioManager _audioManager;
        private TransitionUI _transitionUI;
        private GameObjectPool _playerPool;
        private CancellationTokenSource _transitionCts;
        private PlayerController2D _playerController2D;
        private PlayerInteraction _playerInteraction;
        private bool _shipWasActive;
        private bool _isTransitioning;
        private int _hubInteractionLockDepth;

        public bool IsInSpaceLifeMode => _isInSpaceLifeMode;
        public bool IsTransitioning => _isTransitioning;
        public bool IsHubInteractionLocked => _hubInteractionLockDepth > 0;

        public event Action OnEnterSpaceLife;
        public event Action OnExitSpaceLife;

        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        private void Start()
        {
            Debug.Log("[SpaceLifeManager] Start called");

            _audioManager = ServiceLocator.Get<AudioManager>();
            _transitionUI = ServiceLocator.Get<TransitionUI>();

            if (_spaceLifeCamera != null)
            {
                _spaceLifeCamera.gameObject.SetActive(false);
            }

            if (_spaceLifeSceneRoot != null)
            {
                _spaceLifeSceneRoot.SetActive(false);
            }

            if (_spaceLifeInputHandler == null)
            {
                _spaceLifeInputHandler = ServiceLocator.Get<SpaceLifeInputHandler>();
                if (_spaceLifeInputHandler == null)
                {
                    Debug.LogError("[SpaceLifeManager] CRITICAL: SpaceLifeInputHandler not found via ServiceLocator or Inspector. Ensure it is registered.");
                }
            }

            if (_spaceLifeInputHandler != null)
            {
                _spaceLifeInputHandler.enabled = false;
                Debug.Log("[SpaceLifeManager] Disabled SpaceLifeInputHandler");
            }

            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera != null)
                {
                    Debug.LogWarning("[SpaceLifeManager] WARNING: _mainCamera auto-found via Camera.main fallback. Please assign in Inspector.");
                }
                else
                {
                    Debug.LogError("[SpaceLifeManager] CRITICAL: Main Camera not found. Ensure a Camera tagged 'MainCamera' exists in scene.");
                }
            }

            _shipInputHandler = ServiceLocator.Get<InputHandler>();
            if (_shipInputHandler == null)
            {
                Debug.LogError("[SpaceLifeManager] CRITICAL: InputHandler not found via ServiceLocator. Ensure Ship Prefab is in scene and registers InputHandler.");
            }

            Debug.Log($"[SpaceLifeManager] ShipInputHandler found: {_shipInputHandler != null}");

            if (_shipInputHandler != null)
            {
                if (_shipRoot == null)
                {
                    _shipRoot = _shipInputHandler.gameObject;
                    Debug.LogWarning("[SpaceLifeManager] WARNING: _shipRoot auto-found via ServiceLocator InputHandler. Please assign in Inspector.");
                }

                _shipInputHandler.OnToggleSpaceLifePerformed += ToggleSpaceLife;
                Debug.Log("[SpaceLifeManager] Subscribed to Ship InputHandler ToggleSpaceLife");
            }

            if (_spaceLifePlayerPrefab == null)
            {
                Debug.LogError("[SpaceLifeManager] CRITICAL: _spaceLifePlayerPrefab is NOT assigned! SpaceLife mode will not function. Please assign Player2D_Prefab in Inspector.");
            }

            if (_spaceLifePlayerPrefab != null && PoolManager.Instance != null)
            {
                _playerPool = PoolManager.Instance.GetPool(_spaceLifePlayerPrefab, initialSize: 1, maxSize: 5);
            }

            Debug.Log($"[SpaceLifeManager] Setup complete - PlayerPrefab: {_spaceLifePlayerPrefab != null}, ShipInputHandler: {_shipInputHandler != null}, SpaceLifeInputHandler: {_spaceLifeInputHandler != null}");
        }

        public void EnterSpaceLife()
        {
            if (_isInSpaceLifeMode || _isTransitioning)
            {
                return;
            }

            if (_spaceLifePlayerPrefab == null)
            {
                Debug.LogError("[SpaceLifeManager] Cannot enter SpaceLife: _spaceLifePlayerPrefab is null! Please assign Player2D_Prefab.prefab in Inspector.");
                return;
            }

            EnterSpaceLifeAsync().Forget();
        }

        public void ExitSpaceLife()
        {
            if (!_isInSpaceLifeMode || _isTransitioning)
            {
                return;
            }

            ExitSpaceLifeAsync().Forget();
        }

        public void ToggleSpaceLife()
        {
            Debug.Log($"[SpaceLifeManager] ToggleSpaceLife called! isInSpaceLifeMode={_isInSpaceLifeMode}, isTransitioning={_isTransitioning}");

            if (_isTransitioning)
            {
                return;
            }

            if (!_isInSpaceLifeMode)
            {
                if (_spaceLifePlayerPrefab == null)
                {
                    Debug.LogError("[SpaceLifeManager] Cannot toggle to SpaceLife: _spaceLifePlayerPrefab is null. Assign Player2D_Prefab.prefab in Inspector.");
                    return;
                }

                if (_spaceLifeCamera == null)
                {
                    Debug.LogError("[SpaceLifeManager] Cannot toggle to SpaceLife: _spaceLifeCamera is null. Ensure SpaceLife Camera exists in scene.");
                    return;
                }

                if (_spaceLifeSceneRoot == null)
                {
                    Debug.LogError("[SpaceLifeManager] Cannot toggle to SpaceLife: _spaceLifeSceneRoot is null. Ensure SpaceLife Scene Root exists in scene.");
                    return;
                }
            }

            if (_isInSpaceLifeMode)
            {
                ExitSpaceLife();
            }
            else
            {
                EnterSpaceLife();
            }
        }

        public void SetHubInteractionLocked(bool locked)
        {
            if (locked)
            {
                _hubInteractionLockDepth++;
            }
            else if (_hubInteractionLockDepth > 0)
            {
                _hubInteractionLockDepth--;
            }

            ApplyHubInteractionLockState();
        }

        private async UniTaskVoid EnterSpaceLifeAsync()
        {
            _isTransitioning = true;
            CancelTransition();
            _transitionCts = new CancellationTokenSource();

            try
            {
                CancellationToken ct = _transitionCts.Token;

                if (_transitionUI != null)
                {
                    await _transitionUI.FadeOutAsync(ct);
                }

                _isInSpaceLifeMode = true;

                if (_mainCamera != null)
                {
                    _mainCamera.gameObject.SetActive(false);
                }

                if (_spaceLifeCamera != null)
                {
                    _spaceLifeCamera.gameObject.SetActive(true);
                }

                if (_spaceLifeSceneRoot != null)
                {
                    _spaceLifeSceneRoot.SetActive(true);
                }

                if (_shipRoot != null)
                {
                    _shipWasActive = _shipRoot.activeSelf;
                    _shipRoot.SetActive(false);
                }

                if (_shipInputHandler != null)
                {
                    _shipInputHandler.enabled = false;
                }

                if (_spaceLifeInputHandler != null)
                {
                    _spaceLifeInputHandler.enabled = true;
                }

                SpawnPlayer();
                ApplyHubInteractionLockState();
                PlaySFX(_enterSpaceLifeSFX);
                OnEnterSpaceLife?.Invoke();

                if (_transitionUI != null)
                {
                    await _transitionUI.FadeInAsync(ct);
                }

                Debug.Log("[SpaceLifeManager] Entered Space Life mode!");
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _isTransitioning = false;
            }
        }

        private async UniTaskVoid ExitSpaceLifeAsync()
        {
            _isTransitioning = true;
            CancelTransition();
            _transitionCts = new CancellationTokenSource();

            try
            {
                CancellationToken ct = _transitionCts.Token;

                if (_transitionUI != null)
                {
                    await _transitionUI.FadeOutAsync(ct);
                }

                _isInSpaceLifeMode = false;
                _hubInteractionLockDepth = 0;

                PlaySFX(_exitSpaceLifeSFX);
                DestroyPlayer();

                if (_spaceLifeCamera != null)
                {
                    _spaceLifeCamera.gameObject.SetActive(false);
                }

                if (_spaceLifeSceneRoot != null)
                {
                    _spaceLifeSceneRoot.SetActive(false);
                }

                if (_mainCamera != null)
                {
                    _mainCamera.gameObject.SetActive(true);
                }

                if (_shipRoot != null)
                {
                    _shipRoot.SetActive(_shipWasActive);
                }

                if (_shipInputHandler != null)
                {
                    _shipInputHandler.enabled = true;
                }

                if (_spaceLifeInputHandler != null)
                {
                    _spaceLifeInputHandler.enabled = false;
                }

                OnExitSpaceLife?.Invoke();

                if (_transitionUI != null)
                {
                    await _transitionUI.FadeInAsync(ct);
                }

                Debug.Log("[SpaceLifeManager] Exited Space Life mode!");
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _isTransitioning = false;
            }
        }

        private void SpawnPlayer()
        {
            if (_spaceLifePlayerPrefab == null)
            {
                Debug.LogWarning("[SpaceLifeManager] No player prefab assigned!");
                return;
            }

            if (_currentPlayer != null)
            {
                ReturnPlayerToPool();
            }

            Transform spawnPoint = _spaceLifeSpawnPoint != null ? _spaceLifeSpawnPoint : transform;

            if (_playerPool != null)
            {
                _currentPlayer = _playerPool.Get(spawnPoint.position, spawnPoint.rotation);
            }
            else
            {
                _currentPlayer = Instantiate(_spaceLifePlayerPrefab, spawnPoint.position, spawnPoint.rotation);
            }

            CacheHubPlayerControllers();
        }

        private void ReturnPlayerToPool()
        {
            if (_currentPlayer == null)
            {
                return;
            }

            if (_playerPool != null)
            {
                if (_currentPlayer != null)
                {
                    _playerPool.Return(_currentPlayer);
                }
            }
            else if (_currentPlayer != null)
            {
                Destroy(_currentPlayer);
            }

            _currentPlayer = null;
            _playerController2D = null;
            _playerInteraction = null;
        }

        private void DestroyPlayer()
        {
            ReturnPlayerToPool();
            _playerController2D = null;
            _playerInteraction = null;
        }

        private void CacheHubPlayerControllers()
        {
            if (_currentPlayer == null)
            {
                _playerController2D = null;
                _playerInteraction = null;
                return;
            }

            _playerController2D = _currentPlayer.GetComponent<PlayerController2D>();
            _playerInteraction = _currentPlayer.GetComponent<PlayerInteraction>();
        }

        private void ApplyHubInteractionLockState()
        {
            CacheHubPlayerControllers();
            bool shouldEnableGameplay = _isInSpaceLifeMode && !IsHubInteractionLocked;

            if (_playerController2D != null)
            {
                _playerController2D.enabled = shouldEnableGameplay;
            }

            if (_playerInteraction != null)
            {
                _playerInteraction.enabled = shouldEnableGameplay;
            }
        }

        private void PlaySFX(AudioClip clip)
        {
            if (_audioManager != null && clip != null)
            {
                _audioManager.PlaySFX2D(clip);
            }
        }

        private void CancelTransition()
        {
            if (_transitionCts != null)
            {
                _transitionCts.Cancel();
                _transitionCts.Dispose();
                _transitionCts = null;
            }
        }

        private void OnDestroy()
        {
            CancelTransition();
            OnEnterSpaceLife = null;
            OnExitSpaceLife = null;

            if (_shipInputHandler != null)
            {
                _shipInputHandler.OnToggleSpaceLifePerformed -= ToggleSpaceLife;
            }

            ServiceLocator.Unregister(this);
        }
    }
}
