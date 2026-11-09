using System;
using System.Collections.Generic;

namespace WheatAndCo
{
    public enum ItemKind { Wheat, Corn, Carrot, Pumpkin, Milk, Egg }

    /// <summary>Bag of produce and seed counts. Raises Changed whenever it is modified.</summary>
    public class InventorySystem
    {
        readonly GameManager gm;
        readonly Dictionary<ItemKind, int> items = new Dictionary<ItemKind, int>();
        readonly Dictionary<CropKind, int> seeds = new Dictionary<CropKind, int>();

        public event Action Changed;

        public InventorySystem(GameManager gameManager)
        {
            gm = gameManager;
            foreach (ItemKind kind in Enum.GetValues(typeof(ItemKind))) items[kind] = 0;
            foreach (CropKind kind in Enum.GetValues(typeof(CropKind))) seeds[kind] = 0;
        }

        public int Count(ItemKind kind) => items.TryGetValue(kind, out var n) ? n : 0;
        public int SeedCount(CropKind kind) => seeds.TryGetValue(kind, out var n) ? n : 0;

        public void Add(ItemKind kind, int amount)
        {
            if (amount == 0) return;
            items[kind] = Math.Max(0, Count(kind) + amount);
            Changed?.Invoke();
        }

        public bool TryRemove(ItemKind kind, int amount)
        {
            if (amount <= 0) return true;
            if (Count(kind) < amount) return false;
            items[kind] = Count(kind) - amount;
            Changed?.Invoke();
            return true;
        }

        public void AddSeed(CropKind kind, int amount)
        {
            if (amount == 0) return;
            seeds[kind] = Math.Max(0, SeedCount(kind) + amount);
            Changed?.Invoke();
        }

        public bool TryConsumeSeed(CropKind kind)
        {
            if (SeedCount(kind) <= 0) return false;
            seeds[kind] = SeedCount(kind) - 1;
            Changed?.Invoke();
            return true;
        }

        public int TotalItems()
        {
            int total = 0;
            foreach (var pair in items) total += pair.Value;
            return total;
        }

        public int TotalSellValue(GameDatabase db)
        {
            int total = 0;
            foreach (var pair in items)
                if (pair.Value > 0) total += db.SellPrice(pair.Key) * pair.Value;
            return total;
        }

        /// <summary>Removes every sellable item and returns what was sold.</summary>
        public SaleResult TakeAllProduce(GameDatabase db)
        {
            var result = new SaleResult();
            foreach (ItemKind kind in Enum.GetValues(typeof(ItemKind)))
            {
                int amount = Count(kind);
                if (amount <= 0) continue;
                result.itemCount += amount;
                result.baseValue += db.SellPrice(kind) * amount;
                items[kind] = 0;
            }
            Changed?.Invoke();
            return result;
        }

        public void ResetToNewGame()
        {
            foreach (ItemKind kind in Enum.GetValues(typeof(ItemKind))) items[kind] = 0;
            foreach (CropKind kind in Enum.GetValues(typeof(CropKind))) seeds[kind] = 0;
            seeds[CropKind.Wheat] = 5; // a friendly starter pouch
            Changed?.Invoke();
        }

        public void LoadCounts(int[] itemCounts, int[] seedCounts)
        {
            foreach (ItemKind kind in Enum.GetValues(typeof(ItemKind))) items[kind] = 0;
            foreach (CropKind kind in Enum.GetValues(typeof(CropKind))) seeds[kind] = 0;

            if (itemCounts != null)
                for (int i = 0; i < itemCounts.Length && i < 6; i++)
                    items[(ItemKind)i] = Math.Max(0, itemCounts[i]);

            if (seedCounts != null)
                for (int i = 0; i < seedCounts.Length && i < 4; i++)
                    seeds[(CropKind)i] = Math.Max(0, seedCounts[i]);

            Changed?.Invoke();
        }

        public int[] SnapshotItems()
        {
            var result = new int[6];
            for (int i = 0; i < 6; i++) result[i] = Count((ItemKind)i);
            return result;
        }

        public int[] SnapshotSeeds()
        {
            var result = new int[4];
            for (int i = 0; i < 4; i++) result[i] = SeedCount((CropKind)i);
            return result;
        }
    }

    public struct SaleResult
    {
        public int itemCount;
        public int baseValue;
    }
}
