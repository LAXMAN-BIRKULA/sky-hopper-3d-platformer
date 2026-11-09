using System.Collections.Generic;
using UnityEngine;

namespace WheatAndCo
{
    /// <summary>
    /// Spawns and updates the cows and chickens, handles egg pickups and all the
    /// level-gated animal unlocks. Also serialises animal state for SaveSystem.
    /// </summary>
    public class AnimalSystem
    {
        readonly GameManager gm;
        readonly List<Cow> cows = new List<Cow>();
        readonly List<Chicken> chickens = new List<Chicken>();
        readonly List<EggPickup> eggs = new List<EggPickup>();

        Bounds cowBounds;
        Bounds chickenBounds;
        bool thirdCowSpawned;

        static readonly string[] CowNames = { "Buttercup", "Moe", "Clover" };
        static readonly string[] ChickenNames = { "Pip", "Nugget", "Henrietta", "Sunny", "Pepper", "Biscuit" };
        public const int MaxChickens = 6;

        public GameManager GameManager => gm;
        public int CowCount => cows.Count;
        public int ChickenCount => chickens.Count;

        public AnimalSystem(GameManager gameManager)
        {
            gm = gameManager;
        }

        public void Build()
        {
            cowBounds = gm.World.CowPen;
            chickenBounds = gm.World.ChickenCoop;

            SpawnCow(0, new Vector3(-2f, 0f, -1.5f), 100f, 0f, false);
            SpawnCow(1, new Vector3(2.2f, 0f, 2f), 100f, 0f, false);
            RefreshLevelUnlocks(false);
        }

        public void Update(float deltaTime)
        {
            if (deltaTime <= 0f) return;

            RefreshLevelUnlocks(false);

            for (int i = 0; i < cows.Count; i++)
                if (cows[i] != null) cows[i].Tick(deltaTime);

            for (int i = 0; i < chickens.Count; i++)
                if (chickens[i] != null) chickens[i].Tick(deltaTime);

            for (int i = eggs.Count - 1; i >= 0; i--)
                if (eggs[i] == null) eggs.RemoveAt(i);
        }

        public void RefreshLevelUnlocks(bool showToast = true)
        {
            if (gm.Progression.Level < 5) return;
            if (thirdCowSpawned)
            {
                if (cows.Count >= 3) return;
            }

            if (cows.Count < 3)
            {
                thirdCowSpawned = true;
                SpawnCow(2, new Vector3(-3.5f, 0f, 2.8f), 100f, 0f, false);
                if (showToast)
                {
                    gm.Audio.Play(Sfx.Level, 0.5f);
                    gm.UI?.Toast("Clover the cow has joined the farm!", UITheme.Accent, 4f);
                }
            }
            else
            {
                thirdCowSpawned = true;
            }
        }

        public void BuyChicken()
        {
            if (gm.Progression.Level < 3)
            {
                gm.UI?.Toast("Chickens unlock at level 3.", UITheme.Muted);
                gm.Audio.Play(Sfx.Error, 0.5f);
                return;
            }

            if (chickens.Count >= MaxChickens)
            {
                gm.UI?.Toast("The coop is full - " + MaxChickens + " chickens is plenty!", UITheme.Muted);
                gm.Audio.Play(Sfx.Error, 0.5f);
                return;
            }

            var data = gm.Database.Chicken;
            if (!gm.Economy.TrySpend(data.purchaseCost)) return;

            SpawnChicken();
            gm.Objectives.Notify(ObjEvent.BuyChicken, 1);
            gm.Audio.Play(Sfx.Buy);
            gm.UI?.Toast("A new chicken joined the coop!", UITheme.Accent, 3.5f);
            gm.UI?.RefreshAll();
        }

        void SpawnChicken()
        {
            int index = chickens.Count;
            Vector3 position = RandomPoint(chickenBounds, 1.2f);
            var go = new GameObject("Chicken_" + (index + 1));
            go.transform.SetParent(gm.World.AnimalRoot, false);
            go.transform.position = position;

            var chicken = go.AddComponent<Chicken>();
            chicken.Initialize(this, ChickenNames[index % ChickenNames.Length], position, index % 2 == 1, chickenBounds);
            chickens.Add(chicken);
        }

        void SpawnCow(int index, Vector3 offset, float hunger, float milkTimer, bool milkReady)
        {
            Vector3 center = cowBounds.center;
            Vector3 position = new Vector3(
                Mathf.Clamp(center.x + offset.x, cowBounds.min.x + 1f, cowBounds.max.x - 1f),
                0f,
                Mathf.Clamp(center.z + offset.z, cowBounds.min.z + 1f, cowBounds.max.z - 1f));

            var go = new GameObject("Cow_" + CowNames[index % CowNames.Length]);
            go.transform.SetParent(gm.World.AnimalRoot, false);
            go.transform.position = position;

            var cow = go.AddComponent<Cow>();
            cow.Initialize(this, CowNames[index % CowNames.Length], position, index == 1, cowBounds);
            cow.ApplySave(hunger, milkTimer, milkReady);
            cows.Add(cow);
        }

        public void SpawnEgg(Vector3 chickenPosition)
        {
            if (gm.Progression.HasUpgrade(UpgradeId.AutoCollector))
            {
                AddEggAutomatically(chickenPosition);
                return;
            }

            Vector3 position = new Vector3(
                Mathf.Clamp(chickenPosition.x + Random.Range(-0.5f, 0.5f), chickenBounds.min.x + 0.6f, chickenBounds.max.x - 0.6f),
                0.1f,
                Mathf.Clamp(chickenPosition.z + Random.Range(-0.5f, 0.5f), chickenBounds.min.z + 0.6f, chickenBounds.max.z - 0.6f));

            var go = new GameObject("Egg");
            go.transform.SetParent(gm.World.AnimalRoot, false);
            go.transform.position = position;
            var egg = go.AddComponent<EggPickup>();
            egg.Initialize(this);
            eggs.Add(egg);
        }

        public void CollectEgg(EggPickup pickup)
        {
            if (pickup == null) return;
            eggs.Remove(pickup);
            Vector3 position = pickup.transform.position;

            AddEgg(position, false);
            Object.Destroy(pickup.gameObject);
        }

        void AddEggAutomatically(Vector3 position)
        {
            AddEgg(position, true);
        }

        void AddEgg(Vector3 position, bool automatic)
        {
            var chickenData = gm.Database.Chicken;
            gm.Inventory.Add(ItemKind.Egg, 1);
            gm.Progression.AddXp(chickenData.eggXp, "Egg");
            gm.Objectives.Notify(ObjEvent.CollectEgg, 1);
            gm.Audio.Play(Sfx.Egg, 0.8f);
            FloatingText.Spawn(automatic ? "+1 Egg (auto)" : "+1 Egg",
                position + Vector3.up * 1.1f, UITheme.Accent, 0.075f);
            if (!automatic)
                gm.UI?.Toast("Collected a fresh egg!", UITheme.Accent, 2.2f);
            gm.UI?.RefreshAll();
        }

        Vector3 RandomPoint(Bounds bounds, float margin)
        {
            return new Vector3(
                Random.Range(bounds.min.x + margin, bounds.max.x - margin),
                0f,
                Random.Range(bounds.min.z + margin, bounds.max.z - margin));
        }

        public void ResetAll()
        {
            DestroyAllAnimals();
            thirdCowSpawned = false;
            SpawnCow(0, new Vector3(-2f, 0f, -1.5f), 100f, 0f, false);
            SpawnCow(1, new Vector3(2.2f, 0f, 2f), 100f, 0f, false);
        }

        void DestroyAllAnimals()
        {
            for (int i = cows.Count - 1; i >= 0; i--)
                if (cows[i] != null) Object.DestroyImmediate(cows[i].gameObject);
            cows.Clear();

            for (int i = chickens.Count - 1; i >= 0; i--)
                if (chickens[i] != null) Object.DestroyImmediate(chickens[i].gameObject);
            chickens.Clear();

            for (int i = eggs.Count - 1; i >= 0; i--)
                if (eggs[i] != null) Object.DestroyImmediate(eggs[i].gameObject);
            eggs.Clear();
        }

        // ---- Save / load ----

        public float[] SnapshotCowHunger()
        {
            var result = new float[cows.Count];
            for (int i = 0; i < cows.Count; i++) result[i] = cows[i] != null ? cows[i].Hunger : 100f;
            return result;
        }

        public float[] SnapshotCowMilkTimer()
        {
            var result = new float[cows.Count];
            for (int i = 0; i < cows.Count; i++) result[i] = cows[i] != null ? cows[i].MilkTimer : 0f;
            return result;
        }

        public bool[] SnapshotCowMilkReady()
        {
            var result = new bool[cows.Count];
            for (int i = 0; i < cows.Count; i++) result[i] = cows[i] != null && cows[i].MilkReady;
            return result;
        }

        public bool[] SnapshotChickenHasCorn()
        {
            var result = new bool[chickens.Count];
            for (int i = 0; i < chickens.Count; i++) result[i] = chickens[i] != null && chickens[i].HasCorn;
            return result;
        }

        public float[] SnapshotChickenEggTimer()
        {
            var result = new float[chickens.Count];
            for (int i = 0; i < chickens.Count; i++) result[i] = chickens[i] != null ? chickens[i].EggTimer : 0f;
            return result;
        }

        public void ApplySave(int cowCount, float[] hunger, float[] milkTimer, bool[] milkReady,
            int chickenCount, bool[] chickenHasCorn, float[] chickenEggTimer)
        {
            DestroyAllAnimals();

            int cowsToSpawn = Mathf.Clamp(cowCount <= 0 ? 2 : cowCount, 2, 3);
            for (int i = 0; i < cowsToSpawn; i++)
            {
                Vector3 offset = i == 0 ? new Vector3(-2f, 0f, -1.5f)
                    : i == 1 ? new Vector3(2.2f, 0f, 2f)
                    : new Vector3(-3.5f, 0f, 2.8f);
                float cowHunger = hunger != null && i < hunger.Length ? hunger[i] : 100f;
                float timer = milkTimer != null && i < milkTimer.Length ? milkTimer[i] : 0f;
                bool ready = milkReady != null && i < milkReady.Length && milkReady[i];
                SpawnCow(i, offset, cowHunger, timer, ready);
            }
            thirdCowSpawned = cowsToSpawn >= 3;

            int chickensToSpawn = Mathf.Clamp(chickenCount, 0, MaxChickens);
            for (int i = 0; i < chickensToSpawn; i++)
            {
                int index = chickens.Count;
                Vector3 position = RandomPoint(chickenBounds, 1.2f);
                var go = new GameObject("Chicken_" + (index + 1));
                go.transform.SetParent(gm.World.AnimalRoot, false);
                go.transform.position = position;
                var chicken = go.AddComponent<Chicken>();
                chicken.Initialize(this, ChickenNames[index % ChickenNames.Length], position, index % 2 == 1, chickenBounds);
                chicken.ApplySave(
                    chickenHasCorn != null && i < chickenHasCorn.Length && chickenHasCorn[i],
                    chickenEggTimer != null && i < chickenEggTimer.Length ? chickenEggTimer[i] : 0f);
                chickens.Add(chicken);
            }
        }
    }
}
