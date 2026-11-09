using System;
using UnityEngine;

namespace WheatAndCo
{
    /// <summary>Coins, purchases and selling produce.</summary>
    public class EconomySystem
    {
        readonly GameManager gm;

        public int Coins { get; private set; } = 60;
        public event Action Changed;

        public EconomySystem(GameManager gameManager)
        {
            gm = gameManager;
        }

        public bool CanAfford(int amount) => Coins >= amount;

        public bool TrySpend(int amount, bool complain = true)
        {
            if (amount <= 0) return true;
            if (Coins < amount)
            {
                if (complain)
                {
                    gm.UI?.Toast($"Not enough coins — you need {amount - Coins} more.", UITheme.Bad);
                    gm.Audio?.Play(Sfx.Error);
                }
                return false;
            }

            Coins -= amount;
            Changed?.Invoke();
            return true;
        }

        public void Add(int amount, bool toast = false)
        {
            if (amount == 0) return;
            Coins = Math.Max(0, Coins + amount);
            Changed?.Invoke();
            if (toast) gm.UI?.Toast($"+{amount} coins", UITheme.Accent);
        }

        public void SetCoins(int amount)
        {
            Coins = Math.Max(0, amount);
            Changed?.Invoke();
        }

        /// <summary>Sells every produce item in the bag. Returns the coins earned.</summary>
        public int SellAllProduce(bool showToast = true)
        {
            var sold = gm.Inventory.TakeAllProduce(gm.Database);
            if (sold.itemCount == 0)
            {
                if (showToast) gm.UI?.Toast("Nothing to sell yet — harvest some crops first.", UITheme.Muted);
                return 0;
            }

            float multiplier = gm.Progression.HasUpgrade(UpgradeId.BarnStorage) ? 1.15f : 1f;
            int earned = Mathf.RoundToInt(sold.baseValue * multiplier);
            Coins += earned;
            Changed?.Invoke();

            gm.Progression.AddXp(sold.itemCount * 2, "Sale");
            gm.Objectives.Notify(ObjEvent.SellProduce, sold.itemCount);
            gm.Audio?.Play(Sfx.Coin);
            if (showToast)
                gm.UI?.Toast($"Sold {sold.itemCount} items for {earned} coins!", UITheme.Accent, 3.5f);
            return earned;
        }
    }
}
