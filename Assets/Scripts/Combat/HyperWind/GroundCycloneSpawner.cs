using UnityEngine;

namespace ProjectArk.Combat.HyperWind
{
    /// <summary>
    /// MVP arena spawner for HyperWind ground cyclones. Creates simple runtime cyclone GameObjects when no prefab is supplied.
    /// </summary>
    public sealed class GroundCycloneSpawner : MonoBehaviour
    {
        [SerializeField] private GroundCyclone _cyclonePrefab;
        [SerializeField] private Rect _spawnArea = new Rect(-5f, -5f, 10f, 10f);
        [SerializeField] [Min(0.1f)] private float _spawnInterval = 10f;
        [SerializeField] [Min(1)] private int _minSpawnCount = 1;
        [SerializeField] [Min(1)] private int _maxSpawnCount = 2;
        [SerializeField] private bool _spawnOnStart = true;

        private float _timer;

        private void Start()
        {
            _timer = _spawnInterval;
            if (_spawnOnStart)
            {
                SpawnBatch();
                _timer = 0f;
            }
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer < _spawnInterval)
            {
                return;
            }

            _timer = 0f;
            SpawnBatch();
        }

        private void SpawnBatch()
        {
            int count = Random.Range(_minSpawnCount, _maxSpawnCount + 1);
            for (int i = 0; i < count; i++)
            {
                SpawnOne();
            }
        }

        private void SpawnOne()
        {
            Vector3 position = new Vector3(
                Random.Range(_spawnArea.xMin, _spawnArea.xMax),
                Random.Range(_spawnArea.yMin, _spawnArea.yMax),
                0f);

            GroundCyclone cyclone;
            if (_cyclonePrefab != null)
            {
                cyclone = Instantiate(_cyclonePrefab, position, Quaternion.identity);
            }
            else
            {
                var go = new GameObject("GroundCyclone_Runtime");
                go.transform.position = position;
                cyclone = go.AddComponent<GroundCyclone>();
                go.AddComponent<GroundCycloneView>();

            }

            cyclone.transform.SetParent(transform, true);
            if (cyclone.GetComponent<GroundCycloneView>() == null)
            {
                cyclone.gameObject.AddComponent<GroundCycloneView>();
            }

        }

        private void OnValidate()
        {
            _spawnInterval = Mathf.Max(0.1f, _spawnInterval);
            _minSpawnCount = Mathf.Max(1, _minSpawnCount);
            _maxSpawnCount = Mathf.Max(_minSpawnCount, _maxSpawnCount);
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 center = new Vector3(_spawnArea.center.x, _spawnArea.center.y, 0f);
            Vector3 size = new Vector3(_spawnArea.width, _spawnArea.height, 0f);
            Gizmos.color = new Color(0.7f, 0.3f, 1f, 0.45f);
            Gizmos.DrawWireCube(transform.position + center, size);
        }
    }
}
