
using ProjectArk.SpaceLife.Data;
using UnityEngine;

namespace ProjectArk.SpaceLife
{
    public class SpaceLifeQuickSetup : MonoBehaviour
    {
        [Header("Managers")]
        [SerializeField] private SpaceLifeManager _spaceLifeManager;
        [SerializeField] private RelationshipManager _relationshipManager;
        [SerializeField] private RoomManager _roomManager;
        [SerializeField] private GiftInventory _giftInventory;

        [Header("Test Gifts")]
        [SerializeField] private ItemSO[] _testGifts;

        private void Start()
        {
            SetupTestGifts();
            LogSetupInfo();
        }

        private void SetupTestGifts()
        {
            if (_giftInventory == null || _testGifts == null || _testGifts.Length == 0)
                return;

            foreach (var gift in _testGifts)
            {
                if (gift != null)
                {
                    _giftInventory.AddItem(gift);
                }
            }
        }

        private void LogSetupInfo()
        {
            Debug.Log("=== Space Life Setup Complete ===");
            Debug.Log($"SpaceLifeManager: {(_spaceLifeManager != null ? "✓" : "✗")}");
            Debug.Log($"RelationshipManager: {(_relationshipManager != null ? "✓" : "✗")}");
            Debug.Log($"RoomManager: {(_roomManager != null ? "✓" : "✗")}");
            Debug.Log($"GiftInventory: {(_giftInventory != null ? "✓" : "✗")}");
            Debug.Log("Press TAB to toggle Space Life mode!");
            Debug.Log("==================================");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if (_spaceLifeManager != null)
                {
                    _spaceLifeManager.ToggleSpaceLife();
                }
            }
        }
    }
}

