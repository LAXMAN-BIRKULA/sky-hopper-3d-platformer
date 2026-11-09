using System;
using System.Collections.Generic;
using UnityEngine;

namespace WheatAndCo
{
    /// <summary>Tracks level, XP and purchased upgrades.</summary>
    public class ProgressionSystem
    {
        readonly GameManager gm;
        readonly HashSet<UpgradeId> upgrades = new HashSet<UpgradeId>();

        public int Level { get; private set; } = 1;
        public int Xp { get; private set; }

        public int XpToNext => gm.Database.Levels.GetXpToNext(Level);
        public float Xp01 => Mathf.Clamp01(XpToNext <= 0 ? 0f : (float)Xp / XpToNext);

        public event Action Changed;
        public event Action<int> LeveledUp;

        public ProgressionSystem(GameManager gameManager)
        {
            gm = gameManager;
        }

        /// <summary>Adds XP and levels up as many times as the new total allows.</summary>
        public void AddXp(int amount, string source = "")
        {
            if (amount <= 0) return;
            Xp += amount;

            bool leveled = false;
            while (Xp >= XpToNext && Level < 30)
            {
                Xp -= XpToNext;
                Level++;
                leveled = true;
                LeveledUp?.Invoke(Level);
            }

            Changed?.Invoke();
            if (leveled) gm?.UI?.RefreshAll();
        }

        public bool HasUpgrade(UpgradeId id) => upgrades.Contains(id);

        public bool CanBuyUpgrade(UpgradeData upgrade)
        {
            if (upgrade == null) return false;
            return Level >= upgrade.unlockLevel && !HasUpgrade(upgrade.id);
        }

        public void AddUpgrade(UpgradeId id)
        {
            if (upgrades.Add(id)) Changed?.Invoke();
        }

        public void SetUpgrades(IEnumerable<int> ids)
        {
            upgrades.Clear();
            if (ids != null)
                foreach (int id in ids)
                    if (Enum.IsDefined(typeof(UpgradeId), id)) upgrades.Add((UpgradeId)id);
            Changed?.Invoke();
        }

        public List<int> SnapshotUpgrades()
        {
            var result = new List<int>();
            foreach (var id in upgrades) result.Add((int)id);
            return result;
        }

        public bool IsCropUnlocked(CropKind kind)
        {
            var crop = gm.Database.Crop(kind);
            return crop != null && Level >= crop.unlockLevel;
        }

        public bool IsChickenUnlocked => Level >= 3;
        public bool IsThirdCowUnlocked => Level >= 5;

        public void SetProgress(int level, int xp)
        {
            Level = Mathf.Clamp(level, 1, 30);
            Xp = Mathf.Max(0, xp);
            Changed?.Invoke();
        }
    }
}
