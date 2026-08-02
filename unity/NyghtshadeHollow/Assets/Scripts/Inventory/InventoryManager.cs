using UnityEngine;
using System.Collections.Generic;

namespace NyghtshadeHollow.Inventory
{
    public enum ItemCategory { Contraband, Tool, Food, Currency, Document, Weapon, Medical }
    public enum ItemRarity { Common, Uncommon, Rare, Unique }

    [System.Serializable]
    public class NHItem
    {
        public string ID;
        public string DisplayName;
        public string Description;
        public ItemCategory Category;
        public ItemRarity Rarity;
        public int Quantity;
        public bool IsStackable;
        public Sprite Icon;
        public bool IsContraband;
        public int ContrabandSeverity; // 1-5
    }

    public class InventoryManager : MonoBehaviour
    {
        public const int GridWidth  = 6;
        public const int GridHeight = 5;

        private NHItem[] _grid = new NHItem[GridWidth * GridHeight];

        public static event System.Action OnInventoryChanged;

        public bool AddItem(NHItem item)
        {
            // Stack if possible
            if (item.IsStackable)
            {
                for (int i = 0; i < _grid.Length; i++)
                {
                    if (_grid[i] != null && _grid[i].ID == item.ID)
                    {
                        _grid[i].Quantity += item.Quantity;
                        OnInventoryChanged?.Invoke();
                        return true;
                    }
                }
            }

            // Find empty slot
            for (int i = 0; i < _grid.Length; i++)
            {
                if (_grid[i] == null)
                {
                    _grid[i] = item;
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }
            return false; // Full
        }

        public bool RemoveItem(string itemId, int qty = 1)
        {
            for (int i = 0; i < _grid.Length; i++)
            {
                if (_grid[i]?.ID == itemId)
                {
                    _grid[i].Quantity -= qty;
                    if (_grid[i].Quantity <= 0) _grid[i] = null;
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }
            return false;
        }

        public NHItem GetSlot(int index) => _grid[index];
        public NHItem[] GetAllItems() => (NHItem[])_grid.Clone();

        public bool HasItem(string id) => System.Array.Exists(_grid, i => i?.ID == id);

        public bool HasContraband() => System.Array.Exists(_grid, i => i?.IsContraband == true);

        public int GetContrabandSeverity()
        {
            int max = 0;
            foreach (var item in _grid)
                if (item?.IsContraband == true && item.ContrabandSeverity > max)
                    max = item.ContrabandSeverity;
            return max;
        }

        public bool SwapSlots(int a, int b)
        {
            (_grid[a], _grid[b]) = (_grid[b], _grid[a]);
            OnInventoryChanged?.Invoke();
            return true;
        }
    }
}
