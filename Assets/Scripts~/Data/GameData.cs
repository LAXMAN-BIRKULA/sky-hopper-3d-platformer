using System;
using System.Collections.Generic;
using UnityEngine;

namespace WheatAndCo
{
    public enum CropKind { Wheat, Corn, Carrot, Pumpkin }
    public enum CropVisualStyle { Wheat, Corn, Carrot, Pumpkin }

    /// <summary>All tunable values for one crop. Created as an asset in Resources/Data.</summary>
    [CreateAssetMenu(fileName = "Crop", menuName = "Wheat & Co/Crop Data")]
    public class CropData : ScriptableObject
    {
        public CropKind kind = CropKind.Wheat;
        public string displayName = "Wheat";
        [TextArea(2, 4)] public string description = "";

        [Header("Unlock & Economy")]
        public int unlockLevel = 1;
        public int seedCost = 6;
        public float growSeconds = 30f;
        public int sellPrice = 10;
        public int xpPerHarvest = 10;
        public int yieldAmount = 2;
        public int seedPackSize = 5;
        public int seedPackPrice = 24;

        [Header("Visuals")]
        public CropVisualStyle visualStyle = CropVisualStyle.Wheat;
        public Color stemColor = new Color(0.45f, 0.68f, 0.28f, 1f);
        public Color ripeColor = new Color(0.95f, 0.78f, 0.30f, 1f);
        public Color fruitColor = new Color(0.95f, 0.78f, 0.30f, 1f);
    }

    /// <summary>Shared tuning for cows and chickens.</summary>
    [CreateAssetMenu(fileName = "Animal", menuName = "Wheat & Co/Animal Data")]
    public class AnimalData : ScriptableObject
    {
        public string displayName = "Cow";
        public int purchaseCost = 150;
        public float hungerDecayPerSecond = 0.35f;
        public float milkSeconds = 22f;
        public float eggSeconds = 30f;
        public int feedXp = 12;
        public int milkXp = 15;
        public int eggXp = 10;
        public int milkSellPrice = 25;
        public int eggSellPrice = 15;
    }

    public enum UpgradeId { BetterTools, BarnStorage, Sprinkler, AutoCollector }

    [CreateAssetMenu(fileName = "Upgrade", menuName = "Wheat & Co/Upgrade Data")]
    public class UpgradeData : ScriptableObject
    {
        public UpgradeId id = UpgradeId.BetterTools;
        public string displayName = "Better Tools";
        [TextArea(2, 4)] public string description = "";
        public int cost = 200;
        public int unlockLevel = 5;
        public Color color = new Color(0.85f, 0.65f, 0.30f, 1f);
    }

    [Serializable]
    public class LevelInfo
    {
        public int level;
        public int xpToNext;
        public int rewardCoins;
        public string unlockTitle;
        [TextArea(1, 3)] public string unlockDescription;
    }

    /// <summary>XP curve and the content unlocked at each level.</summary>
    [CreateAssetMenu(fileName = "LevelData", menuName = "Wheat & Co/Level Data")]
    public class LevelData : ScriptableObject
    {
        public List<LevelInfo> levels = new List<LevelInfo>();

        public LevelInfo Get(int level)
        {
            for (int i = 0; i < levels.Count; i++)
                if (levels[i].level == level) return levels[i];
            return null;
        }

        public int GetXpToNext(int level)
        {
            var info = Get(level);
            if (info != null && info.xpToNext > 0) return info.xpToNext;
            return 100 + Mathf.Max(0, level - 1) * 80;
        }
    }

    /// <summary>
    /// Central, read-only catalogue of gameplay data. It loads ScriptableObject
    /// assets from Resources/Data and falls back to sensible built-in defaults.
    /// </summary>
    public class GameDatabase
    {
        public readonly Dictionary<CropKind, CropData> Crops = new Dictionary<CropKind, CropData>();
        public readonly Dictionary<UpgradeId, UpgradeData> Upgrades = new Dictionary<UpgradeId, UpgradeData>();
        public AnimalData Cow;
        public AnimalData Chicken;
        public LevelData Levels;

        public static GameDatabase LoadOrCreate()
        {
            var db = new GameDatabase();
            db.BuildDefaults();

            // Override defaults with authored assets when they exist.
            var cropAssets = Resources.LoadAll<CropData>("Data");
            foreach (var crop in cropAssets)
                if (crop != null) db.Crops[crop.kind] = crop;

            var animalAssets = Resources.LoadAll<AnimalData>("Data");
            foreach (var animal in animalAssets)
            {
                if (animal == null) continue;
                if (animal.displayName.ToLowerInvariant().Contains("chick")) db.Chicken = animal;
                else db.Cow = animal;
            }

            var upgradeAssets = Resources.LoadAll<UpgradeData>("Data");
            foreach (var up in upgradeAssets)
                if (up != null) db.Upgrades[up.id] = up;

            var levelAsset = Resources.Load<LevelData>("Data/LevelData");
            if (levelAsset != null && levelAsset.levels != null && levelAsset.levels.Count > 0)
                db.Levels = levelAsset;

            return db;
        }

        public CropData Crop(CropKind kind)
        {
            return Crops.TryGetValue(kind, out var data) ? data : null;
        }

        public UpgradeData Upgrade(UpgradeId id)
        {
            return Upgrades.TryGetValue(id, out var data) ? data : null;
        }

        public int SellPrice(ItemKind item)
        {
            switch (item)
            {
                case ItemKind.Wheat: return Crops[CropKind.Wheat].sellPrice;
                case ItemKind.Corn: return Crops[CropKind.Corn].sellPrice;
                case ItemKind.Carrot: return Crops[CropKind.Carrot].sellPrice;
                case ItemKind.Pumpkin: return Crops[CropKind.Pumpkin].sellPrice;
                case ItemKind.Milk: return Cow.milkSellPrice;
                case ItemKind.Egg: return Chicken.eggSellPrice;
            }
            return 0;
        }

        public static ItemKind ItemForCrop(CropKind crop)
        {
            switch (crop)
            {
                case CropKind.Wheat: return ItemKind.Wheat;
                case CropKind.Corn: return ItemKind.Corn;
                case CropKind.Carrot: return ItemKind.Carrot;
                case CropKind.Pumpkin: return ItemKind.Pumpkin;
            }
            return ItemKind.Wheat;
        }

        public static string ItemName(ItemKind item) => item.ToString();

        public static Color ItemColor(ItemKind item)
        {
            switch (item)
            {
                case ItemKind.Wheat: return new Color(0.96f, 0.80f, 0.34f);
                case ItemKind.Corn: return new Color(0.99f, 0.86f, 0.24f);
                case ItemKind.Carrot: return new Color(0.95f, 0.48f, 0.17f);
                case ItemKind.Pumpkin: return new Color(0.96f, 0.47f, 0.12f);
                case ItemKind.Milk: return new Color(0.97f, 0.97f, 0.94f);
                case ItemKind.Egg: return new Color(0.98f, 0.94f, 0.80f);
            }
            return Color.white;
        }

        void BuildDefaults()
        {
            Crops[CropKind.Wheat] = MakeCrop(CropKind.Wheat, "Wheat", 1, 6, 26f, 10, 10, 2,
                5, 20, CropVisualStyle.Wheat,
                new Color(0.47f, 0.68f, 0.29f), new Color(0.96f, 0.80f, 0.32f), new Color(0.96f, 0.80f, 0.32f),
                "The heart of the farm. Feed cows, sell at the market, or grow your fortune.");

            Crops[CropKind.Corn] = MakeCrop(CropKind.Corn, "Corn", 2, 12, 48f, 26, 16, 2,
                5, 45, CropVisualStyle.Corn,
                new Color(0.40f, 0.64f, 0.26f), new Color(0.60f, 0.78f, 0.30f), new Color(0.99f, 0.85f, 0.24f),
                "Sweet golden corn. A little slow to grow, but the chickens adore it.");

            Crops[CropKind.Carrot] = MakeCrop(CropKind.Carrot, "Carrot", 4, 20, 62f, 44, 20, 2,
                5, 70, CropVisualStyle.Carrot,
                new Color(0.37f, 0.64f, 0.27f), new Color(0.46f, 0.72f, 0.30f), new Color(0.95f, 0.48f, 0.17f),
                "Crunchy and colourful. A favourite at the market.");

            Crops[CropKind.Pumpkin] = MakeCrop(CropKind.Pumpkin, "Pumpkin", 6, 30, 85f, 78, 26, 2,
                3, 105, CropVisualStyle.Pumpkin,
                new Color(0.36f, 0.56f, 0.25f), new Color(0.48f, 0.66f, 0.28f), new Color(0.96f, 0.47f, 0.12f),
                "A prize pumpkin. Takes patience, but sells for a small fortune.");

            Cow = ScriptableObject.CreateInstance<AnimalData>();
            Cow.name = "Cow";
            Cow.displayName = "Cow";
            Cow.hungerDecayPerSecond = 0.35f;
            Cow.milkSeconds = 22f;
            Cow.feedXp = 12;
            Cow.milkXp = 15;
            Cow.milkSellPrice = 25;

            Chicken = ScriptableObject.CreateInstance<AnimalData>();
            Chicken.name = "Chicken";
            Chicken.displayName = "Chicken";
            Chicken.purchaseCost = 150;
            Chicken.eggSeconds = 32f;
            Chicken.eggXp = 10;
            Chicken.eggSellPrice = 15;

            Upgrades[UpgradeId.BetterTools] = MakeUpgrade(UpgradeId.BetterTools, "Better Tools",
                "+1 extra crop from every harvest.", 200, 5, new Color(0.82f, 0.62f, 0.30f));
            Upgrades[UpgradeId.BarnStorage] = MakeUpgrade(UpgradeId.BarnStorage, "Barn Storage",
                "+15% coins from everything you sell.", 250, 6, new Color(0.65f, 0.46f, 0.28f));
            Upgrades[UpgradeId.Sprinkler] = MakeUpgrade(UpgradeId.Sprinkler, "Sprinkler System",
                "Crops grow 25% faster in every field.", 300, 7, new Color(0.35f, 0.65f, 0.78f));
            Upgrades[UpgradeId.AutoCollector] = MakeUpgrade(UpgradeId.AutoCollector, "Auto-Collector",
                "Milk and eggs are collected for you automatically.", 450, 8, new Color(0.55f, 0.75f, 0.45f));

            Levels = ScriptableObject.CreateInstance<LevelData>();
            Levels.name = "LevelData";
            Levels.levels = new List<LevelInfo>
            {
                Level(1, 100, 0, "A New Farmer", "Welcome to Wheat & Co. Plant wheat, feed cows and make the farm yours."),
                Level(2, 170, 50, "Corn Field", "A corn field has opened on the east side of the path."),
                Level(3, 260, 75, "Chicken Coop", "The coop is open! Buy chickens from the Farm Shop (Tab)."),
                Level(4, 370, 100, "Carrot Field", "The west back field is ready for carrots."),
                Level(5, 500, 125, "Third Cow", "Clover the cow joins your pen. More milk, more friends."),
                Level(6, 650, 150, "Pumpkin Field", "The east back field is ready for prize pumpkins."),
                Level(7, 820, 200, "Sprinkler System", "Buy the sprinkler upgrade in the shop for faster growth."),
                Level(8, 1000, 250, "Auto-Collector", "Buy the auto-collector and never chase an egg again."),
                Level(9, 1200, 300, "Farm Legend", "Your farm is the talk of the valley. Keep growing!"),
                Level(10, 1500, 400, "Master Farmer", "A true master of the harvest."),
            };
        }

        static CropData MakeCrop(CropKind kind, string name, int unlock, int seedCost, float grow, int sell,
            int xp, int yield, int packSize, int packPrice, CropVisualStyle style,
            Color stem, Color ripe, Color fruit, string description)
        {
            var c = ScriptableObject.CreateInstance<CropData>();
            c.name = "Crop_" + kind;
            c.kind = kind;
            c.displayName = name;
            c.unlockLevel = unlock;
            c.seedCost = seedCost;
            c.growSeconds = grow;
            c.sellPrice = sell;
            c.xpPerHarvest = xp;
            c.yieldAmount = yield;
            c.seedPackSize = packSize;
            c.seedPackPrice = packPrice;
            c.visualStyle = style;
            c.stemColor = stem;
            c.ripeColor = ripe;
            c.fruitColor = fruit;
            c.description = description;
            return c;
        }

        static UpgradeData MakeUpgrade(UpgradeId id, string name, string desc, int cost, int level, Color color)
        {
            var u = ScriptableObject.CreateInstance<UpgradeData>();
            u.name = "Upgrade_" + id;
            u.id = id;
            u.displayName = name;
            u.description = desc;
            u.cost = cost;
            u.unlockLevel = level;
            u.color = color;
            return u;
        }

        static LevelInfo Level(int level, int xp, int coins, string title, string desc)
        {
            return new LevelInfo
            {
                level = level,
                xpToNext = xp,
                rewardCoins = coins,
                unlockTitle = title,
                unlockDescription = desc
            };
        }

#if UNITY_EDITOR
        /// <summary>Editor helper: writes the default catalogue to Resources/Data once.</summary>
        public static void ExportAssets()
        {
            var db = new GameDatabase();
            db.BuildDefaults();

            const string folder = "Assets/Resources/Data";
            if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Resources"))
                UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");
            if (!UnityEditor.AssetDatabase.IsValidFolder(folder))
                UnityEditor.AssetDatabase.CreateFolder("Assets/Resources", "Data");

            foreach (var crop in db.Crops.Values)
                ExportAsset(crop, folder + "/Crop_" + crop.kind + ".asset");
            ExportAsset(db.Cow, folder + "/Animal_Cow.asset");
            ExportAsset(db.Chicken, folder + "/Animal_Chicken.asset");
            foreach (var upgrade in db.Upgrades.Values)
                ExportAsset(upgrade, folder + "/Upgrade_" + upgrade.id + ".asset");
            ExportAsset(db.Levels, folder + "/LevelData.asset");

            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
        }

        static void ExportAsset(UnityEngine.Object asset, string path)
        {
            if (UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null) return;
            UnityEditor.AssetDatabase.CreateAsset(asset, path);
        }
#endif
    }
}
