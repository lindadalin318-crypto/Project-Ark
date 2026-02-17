using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using ProjectArk.Core;
using ProjectArk.Core.Audio;
using ProjectArk.Ship;

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

        [Header("Transition")]
        [SerializeField] private string _enterText = "进入飞船...";
        [SerializeField] private string _exitText = "准备出击";

        [Header("State")]
        [SerializeField] private bool _isInSpaceLifeMode;

        private GameObject _currentPlayer;
        private InputHandler _shipInputHandler;
        private AudioManager _audioManager;
        private TransitionUI _transitionUI;
        private GameObjectPool _playerPool;
        private CancellationTokenSource _transitionCts;
        private bool _shipWasActive;
        private bool _isTransitioning;

        public bool IsInSpaceLifeMode => _isInSpaceLifeMode;
        public bool IsTransitioning => _isTransitioning;

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
                _spaceLifeCamera.gameObject.SetActive(false);

            if (_spaceLifeSceneRoot != null)
                _spaceLifeSceneRoot.SetActive(false);

            // Auto-find _spaceLifeInputHandler via fallback
            if (_spaceLifeInputHandler == null)
            {
                _spaceLifeInputHandler = FindFirstObjectByType<SpaceLifeInputHandler>();
                if (_spaceLifeInputHandler != null)
                    Debug.LogWarning("[SpaceLifeManager] WARNING: _spaceLifeInputHandler auto-found via fallback. Please assign in Inspector.");
                else
                    Debug.LogError("[SpaceLifeManager] CRITICAL: SpaceLifeInputHandler not found in scene. Ensure a GameObject with SpaceLifeInputHandler component exists.");
            }

            if (_spaceLifeInputHandler != null)
            {
                _spaceLifeInputHandler.enabled = false;
                Debug.Log("[SpaceLifeManager] Disabled SpaceLifeInputHandler");
            }

            // Auto-find _mainCamera via fallback
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera != null)
                    Debug.LogWarning("[SpaceLifeManager] WARNING: _mainCamera auto-found via Camera.main fallback. Please assign in Inspector.");
                else
                    Debug.LogError("[SpaceLifeManager] CRITICAL: Main Camera not found. Ensure a Camera tagged 'MainCamera' exists in scene.");
            }

            // Get Ship InputHandler from ServiceLocator
            _shipInputHandler = ServiceLocator.Get<InputHandler>();
            
            if (_shipInputHandler == null)
            {
                // Fallback: try FindFirstObjectByType
                _shipInputHandler = FindFirstObjectByType<InputHandler>();
                if (_shipInputHandler != null)
                    Debug.LogWarning("[SpaceLifeManager] WARNING: _shipInputHandler auto-found via FindFirstObjectByType fallback. Ensure Ship Prefab registers to ServiceLocator.");
                else
                    Debug.LogError("[SpaceLifeManager] CRITICAL: InputHandler not found via ServiceLocator or FindFirstObjectByType. Ensure Ship Prefab is in scene and has InputHandler component.");
            }
            
            Debug.Log($"[SpaceLifeManager] ShipInputHandler found: {_shipInputHandler != null}");
            
            if (_shipInputHandler != null)
            {
                // Auto-find _shipRoot via fallback
                if (_shipRoot == null)
                {
                    _shipRoot = _shipInputHandler.gameObject;
                    Debug.LogWarning("[SpaceLifeManager] WARNING: _shipRoot auto-found via ServiceLocator InputHandler. Please assign in Inspector.");
                }
                _shipInputHandler.OnToggleSpaceLifePerformed += ToggleSpaceLife;
                Debug.Log("[SpaceLifeManager] Subscribed to Ship InputHandler ToggleSpaceLife");
            }

            // Validate _spaceLifePlayerPrefab
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
            if (_isInSpaceLifeMode || _isTransitioning) return;

            // Pre-condition checks
            if (_spaceLifePlayerPrefab == null)
            {
                Debug.LogError("[SpaceLifeManager] Cannot enter SpaceLife: _spaceLifePlayerPrefab is null! Please assign Player2D_Prefab.prefab in Inspector.");
                return;
            }

            EnterSpaceLifeAsync().Forget();
        }

        private async UniTaskVoid EnterSpaceLifeAsync()
        {
            _isTransitioning = true;
            CancelTransition();
            _transitionCts = new CancellationTokenSource();

            try
            {
                if (_transitionUI != null)
                {
                    await _transitionUI.PlayEnterTransitionAsync(_enterText);
                }

                _isInSpaceLifeMode = true;

                if (_mainCamera != null)
                    _mainCamera.gameObject.SetActive(false);

                if (_spaceLifeCamera != null)
                    _spaceLifeCamera.gameObject.SetActive(true);

                if (_spaceLifeSceneRoot != null)
                    _spaceLifeSceneRoot.SetActive(true);

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

                PlaySFX(_enterSpaceLifeSFX);

                OnEnterSpaceLife?.Invoke();

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

        public void ExitSpaceLife()
        {
            if (!_isInSpaceLifeMode || _isTransitioning) return;

            ExitSpaceLifeAsync().Forget();
        }

        private async UniTaskVoid ExitSpaceLifeAsync()
        {
            _isTransitioning = true;
            CancelTransition();
            _transitionCts = new CancellationTokenSource();

            try
            {
                if (_transitionUI != null)
                {
                    await _transitionUI.PlayExitTransitionAsync(_exitText);
                }

                _isInSpaceLifeMode = false;

                PlaySFX(_exitSpaceLifeSFX);

                DestroyPlayer();

                if (_spaceLifeCamera != null)
                    _spaceLifeCamera.gameObject.SetActive(false);

                if (_spaceLifeSceneRoot != null)
                    _spaceLifeSceneRoot.SetActive(false);

                if (_mainCamera != null)
                    _mainCamera.gameObject.SetActive(true);

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

        public void ToggleSpaceLife()
        {
            Debug.Log($"[SpaceLifeManager] ToggleSpaceLife called! isInSpaceLifeMode={_isInSpaceLifeMode}, isTransitioning={_isTransitioning}");
            
            if (_isTransitioning) return;

            // Pre-condition checks for entering SpaceLife
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
                ExitSpaceLife();
            else
                EnterSpaceLife();
        }

        private void SpawnPlayer()
        {
            if (_spaceLifePlayerPrefab == null)
            {
                Debug.LogWarning("[SpaceLifeManager] No player prefab assigned!");
                return;
            }

            if (_currentPlayer != null)
                ReturnPlayerToPool();

            Transform spawnPoint = _spaceLifeSpawnPoint != null ? _spaceLifeSpawnPoint : transform;

            if (_playerPool != null)
            {
                _currentPlayer = _playerPool.Get(spawnPoint.position, spawnPoint.rotation);
            }
            else
            {
                _currentPlayer = Instantiate(_spaceLifePlayerPrefab, spawnPoint.position, spawnPoint.rotation);
            }
        }

        private void ReturnPlayerToPool()
        {
            if (_currentPlayer == null) return;

            if (_playerPool != null)
            {
                _playerPool.Return(_currentPlayer);
            }
            else
            {
                Destroy(_currentPlayer);
            }

            _currentPlayer = null;
        }

        private void DestroyPlayer()
        {
            ReturnPlayerToPool();
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
            
            if (_shipInputHandler != null)
            {
                _shipInputHandler.OnToggleSpaceLifePerformed -= ToggleSpaceLife;
            }
            
            ServiceLocator.Unregister(this);
        }
    }
}
