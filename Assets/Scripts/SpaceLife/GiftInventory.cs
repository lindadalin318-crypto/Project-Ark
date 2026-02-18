
using System.Collections.Generic;
using ProjectArk.Core;
using ProjectArk.SpaceLife.Data;
using UnityEngine;

namespace ProjectArk.SpaceLife
{
    public class GiftInventory : MonoBehaviour
    {
        [Header("Inventory")]
        [SerializeField] private List<ItemSO> _items = new List<ItemSO>();

        public List<ItemSO> Items => _items;

        public event System.Action OnInventoryChanged;

        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        public void AddItem(ItemSO item)
        {
            if (item == null) return;

            _items.Add(item);
            OnInventoryChanged?.Invoke();
            Debug.Log($"[GiftInventory] Added: {item.ItemName}");
        }

        public bool RemoveItem(ItemSO item)
        {
            if (item == null) return false;

            if (_items.Remove(item))
            {
                OnInventoryChanged?.Invoke();
                Debug.Log($"[GiftInventory] Removed: {item.ItemName}");
                return true;
            }

            return false;
        }

        public bool HasItem(ItemSO item)
        {
            return _items.Contains(item);
        }

        public int GetItemCount()
        {
            return _items.Count;
        }

        private void OnDestroy()
        {
            OnInventoryChanged = null;
            ServiceLocator.Unregister(this);
        }
    }
}

