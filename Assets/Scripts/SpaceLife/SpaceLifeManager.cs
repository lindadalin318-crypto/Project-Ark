
using System;
using UnityEngine;

namespace ProjectArk.SpaceLife
{
    public class SpaceLifeManager : MonoBehaviour
    {
        public static SpaceLifeManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private GameObject _spaceLifePlayerPrefab;
        [SerializeField] private Transform _spaceLifeSpawnPoint;
        [SerializeField] private Camera _spaceLifeCamera;

        [Header("State")]
        [SerializeField] private bool _isInSpaceLifeMode;

        private GameObject _currentPlayer;

        public bool IsInSpaceLifeMode => _isInSpaceLifeMode;

        public event Action OnEnterSpaceLife;
        public event Action OnExitSpaceLife;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (_spaceLifeCamera != null)
                _spaceLifeCamera.gameObject.SetActive(false);
        }

        public void EnterSpaceLife()
        {
            if (_isInSpaceLifeMode) return;

            _isInSpaceLifeMode = true;

            SpawnPlayer();

            if (_spaceLifeCamera != null)
                _spaceLifeCamera.gameObject.SetActive(true);

            OnEnterSpaceLife?.Invoke();

            Debug.Log("[SpaceLifeManager] Entered Space Life mode!");
        }

        public void ExitSpaceLife()
        {
            if (!_isInSpaceLifeMode) return;

            _isInSpaceLifeMode = false;

            DestroyPlayer();

            if (_spaceLifeCamera != null)
                _spaceLifeCamera.gameObject.SetActive(false);

            OnExitSpaceLife?.Invoke();

            Debug.Log("[SpaceLifeManager] Exited Space Life mode!");
        }

        public void ToggleSpaceLife()
        {
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
                Destroy(_currentPlayer);

            Transform spawnPoint = _spaceLifeSpawnPoint != null ? _spaceLifeSpawnPoint : transform;
            _currentPlayer = Instantiate(_spaceLifePlayerPrefab, spawnPoint.position, spawnPoint.rotation);
        }

        private void DestroyPlayer()
        {
            if (_currentPlayer != null)
            {
                Destroy(_currentPlayer);
                _currentPlayer = null;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}

