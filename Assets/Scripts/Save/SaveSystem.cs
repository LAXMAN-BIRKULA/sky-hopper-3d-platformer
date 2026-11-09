using System;
using System.IO;
using UnityEngine;

namespace WheatAndCo
{
    [Serializable]
    public class SaveData
    {
        public int version = 2;
        public string savedAt;

        public int coins = 60;
        public int level = 1;
        public int xp;

        public int[] items = new int[6];
        public int[] seeds = new int[4];
        public int[] upgrades = new int[0];

        public int objectiveIndex;
        public int[] objectiveProgress = new int[0];

        public int[] plotStages = new int[0];
        public float[] plotGrowth = new float[0];

        public int cowCount;
        public float[] cowHunger = new float[0];
        public float[] cowMilkTimer = new float[0];
        public bool[] cowMilkReady = new bool[0];

        public int chickenCount;
        public bool[] chickenHasCorn = new bool[0];
        public float[] chickenEggTimer = new float[0];

        public float playerX;
        public float playerY = 0.2f;
        public float playerZ = -10f;
    }

    /// <summary>JSON persistence in Application.persistentDataPath.</summary>
    public class SaveSystem
    {
        readonly GameManager gm;
        readonly string path;

        public SaveSystem(GameManager gameManager)
        {
            gm = gameManager;
            path = Path.Combine(Application.persistentDataPath, "wheatandco_save.json");
        }

        public string Path => path;
        public bool HasSave => File.Exists(path);

        public void Save()
        {
            if (gm == null) return;

            var data = new SaveData
            {
                savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                coins = gm.Economy.Coins,
                level = gm.Progression.Level,
                xp = gm.Progression.Xp,
                items = gm.Inventory.SnapshotItems(),
                seeds = gm.Inventory.SnapshotSeeds(),
                upgrades = gm.Progression.SnapshotUpgrades().ToArray(),
                objectiveIndex = gm.Objectives.Index,
                objectiveProgress = gm.Objectives.SnapshotProgress(),
                plotStages = gm.Farming.SnapshotStages(),
                plotGrowth = gm.Farming.SnapshotGrowth(),
                cowCount = gm.Animals.CowCount,
                cowHunger = gm.Animals.SnapshotCowHunger(),
                cowMilkTimer = gm.Animals.SnapshotCowMilkTimer(),
                cowMilkReady = gm.Animals.SnapshotCowMilkReady(),
                chickenCount = gm.Animals.ChickenCount,
                chickenHasCorn = gm.Animals.SnapshotChickenHasCorn(),
                chickenEggTimer = gm.Animals.SnapshotChickenEggTimer()
            };

            var position = gm.Player != null ? gm.Player.transform.position : Vector3.zero;
            data.playerX = position.x;
            data.playerY = position.y;
            data.playerZ = position.z;

            try
            {
                File.WriteAllText(path, JsonUtility.ToJson(data, true));
            }
            catch (Exception exception)
            {
                Debug.Log("[Wheat & Co.] Could not save: " + exception.Message);
            }
        }

        public bool TryLoad()
        {
            if (!HasSave) return false;

            SaveData data;
            try
            {
                data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            }
            catch (Exception exception)
            {
                Debug.Log("[Wheat & Co.] Could not read save: " + exception.Message);
                return false;
            }

            if (data == null || data.version != 2) return false;

            try
            {
                gm.Economy.SetCoins(data.coins);
                gm.Inventory.LoadCounts(data.items, data.seeds);
                gm.Progression.SetProgress(data.level, data.xp);
                gm.Progression.SetUpgrades(data.upgrades);
                gm.Objectives.ApplySave(data.objectiveIndex, data.objectiveProgress);
                gm.Farming.ApplySave(data.plotStages, data.plotGrowth);
                gm.Animals.ApplySave(
                    data.cowCount, data.cowHunger, data.cowMilkTimer, data.cowMilkReady,
                    data.chickenCount, data.chickenHasCorn, data.chickenEggTimer);
                gm.Player.Teleport(new Vector3(data.playerX, Mathf.Max(0.2f, data.playerY), data.playerZ));
                if (gm.CameraRig != null) gm.CameraRig.Snap();
                gm.UI?.RefreshAll();
                return true;
            }
            catch (Exception exception)
            {
                Debug.Log("[Wheat & Co.] Save data was incomplete: " + exception.Message);
                gm?.RestartFarm();
                return false;
            }
        }

        public void Delete()
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception exception)
            {
                Debug.Log("[Wheat & Co.] Could not delete save: " + exception.Message);
            }
        }
    }
}
