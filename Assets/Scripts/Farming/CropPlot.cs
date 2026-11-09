using System.Collections.Generic;
using UnityEngine;

namespace WheatAndCo
{
    public enum PlotStage
    {
        Empty = 0,
        Seeded = 1,
        Growing = 2,
        Ripe = 3
    }

    /// <summary>
    /// One tilled square. Grows a single crop, shows stage visuals and handles
    /// planting and harvesting when the player interacts.
    /// </summary>
    public class CropPlot : InteractableBase
    {
        public CropKind Crop { get; private set; }
        public PlotStage Stage { get; private set; } = PlotStage.Empty;
        public bool Unlocked { get; private set; }
        public float Growth01 => growth01;

        FarmingSystem farm;
        CropData data;
        GameObject plant;
        GameObject soilPad;
        Renderer soilRenderer;
        readonly List<GameObject> fruits = new List<GameObject>();
        float growth01;
        float swayPhase;

        public void Initialize(FarmingSystem farming, CropKind crop, int unlockLevel, bool unlocked)
        {
            farm = farming;
            Crop = crop;
            data = farm.GameManager.Database.Crop(crop);
            Unlocked = unlocked;
            swayPhase = Random.Range(0f, 10f);

            BuildSoil();
            BuildPlant();
            SetUnlocked(unlocked);
            Restore(PlotStage.Empty, 0f);
        }

        public override string Prompt
        {
            get
            {
                if (!Unlocked) return "Locked field";
                switch (Stage)
                {
                    case PlotStage.Empty:
                        if (farm.GameManager.Inventory.SeedCount(Crop) > 0)
                            return "Plant " + data.displayName + " (1 seed)";
                        return "Plant " + data.displayName + " (" + data.seedCost + " coins)";
                    case PlotStage.Seeded:
                    case PlotStage.Growing:
                        return data.displayName + " growing - " + SecondsLeft() + "s";
                    case PlotStage.Ripe:
                        return "Harvest " + data.displayName + " (+" + YieldAmount() + ")";
                }
                return data.displayName;
            }
        }

        public override bool CanInteract
        {
            get
            {
                if (!Unlocked) return false;
                return Stage == PlotStage.Empty || Stage == PlotStage.Ripe;
            }
        }

        public override float HighlightRadius => 1.25f;
        protected override float RingY => 0.2f;

        public override void Interact(PlayerController player)
        {
            if (!Unlocked) return;
            if (Stage == PlotStage.Empty) Plant();
            else if (Stage == PlotStage.Ripe) Harvest();
        }

        void Plant()
        {
            var gm = farm.GameManager;

            if (gm.Inventory.TryConsumeSeed(Crop))
            {
                // Used a seed pack bought from the shop.
            }
            else if (gm.Economy.TrySpend(data.seedCost))
            {
                // Paid coins directly.
            }
            else
            {
                return;
            }

            Stage = PlotStage.Seeded;
            growth01 = 0.02f;
            ApplyVisual();
            gm.Audio.Play(Sfx.Plant);
            gm.Progression.AddXp(2, "Planting");
            gm.UI.Toast("Planted " + data.displayName + ".", UITheme.Good, 2f);
            FloatingText.Spawn("Planted", transform.position + Vector3.up * 0.9f, UITheme.Good, 0.07f);

            if (Crop == CropKind.Corn)
                gm.Objectives.Notify(ObjEvent.PlantCorn, 1);

            gm.UI.RefreshAll();
        }

        void Harvest()
        {
            var gm = farm.GameManager;
            int amount = YieldAmount();

            gm.Inventory.Add(GameDatabase.ItemForCrop(Crop), amount);
            gm.Progression.AddXp(data.xpPerHarvest, "Harvest");
            gm.Audio.Play(Sfx.Harvest);
            SpawnHarvestBurst();
            FloatingText.Spawn("+" + amount + " " + data.displayName,
                transform.position + Vector3.up * 0.95f, UITheme.Accent, 0.085f);

            switch (Crop)
            {
                case CropKind.Wheat: gm.Objectives.Notify(ObjEvent.HarvestWheat, 1); break;
                case CropKind.Carrot: gm.Objectives.Notify(ObjEvent.HarvestCarrot, 1); break;
                case CropKind.Pumpkin: gm.Objectives.Notify(ObjEvent.HarvestPumpkin, 1); break;
            }

            Stage = PlotStage.Empty;
            growth01 = 0f;
            ApplyVisual();
            gm.UI.RefreshAll();
        }

        int YieldAmount()
        {
            int bonus = farm.GameManager.Progression.HasUpgrade(UpgradeId.BetterTools) ? 1 : 0;
            return Mathf.Max(1, data.yieldAmount + bonus);
        }

        int SecondsLeft()
        {
            float remaining = (1f - growth01) * data.growSeconds / Mathf.Max(0.1f, farm.GrowthMultiplier);
            return Mathf.Max(0, Mathf.CeilToInt(remaining));
        }

        /// <summary>Called by FarmingSystem every frame while the game is not paused.</summary>
        public void Tick(float deltaTime)
        {
            if (!Unlocked) return;
            if (Stage != PlotStage.Seeded && Stage != PlotStage.Growing) return;

            growth01 += deltaTime * farm.GrowthMultiplier / Mathf.Max(1f, data.growSeconds);

            if (growth01 >= 0.45f && Stage == PlotStage.Seeded)
                Stage = PlotStage.Growing;

            if (growth01 >= 1f)
            {
                growth01 = 1f;
                Stage = PlotStage.Ripe;
                farm.GameManager.Audio.Play(Sfx.Objective, 0.25f);
                SpawnSparkle();
            }

            ApplyVisual();

            if (plant != null)
            {
                float sway = Mathf.Sin(Time.time * 1.25f + swayPhase) * 3.2f;
                plant.transform.localRotation = Quaternion.Euler(0f, sway, 0f);
            }
        }

        public void SetUnlocked(bool unlocked)
        {
            Unlocked = unlocked;
            if (soilRenderer != null)
                soilRenderer.sharedMaterial = farm.GameManager.Materials.Get(unlocked ? "Soil" : "SoilLocked");
            if (!unlocked)
            {
                Stage = PlotStage.Empty;
                growth01 = 0f;
                ApplyVisual();
            }
        }

        public void Restore(PlotStage stage, float growth)
        {
            Stage = stage;
            growth01 = Mathf.Clamp01(growth);
            if (Stage == PlotStage.Ripe) growth01 = 1f;
            ApplyVisual();
        }

        void ApplyVisual()
        {
            if (plant == null) return;
            bool visible = Stage != PlotStage.Empty;
            plant.SetActive(visible);
            if (!visible) return;

            float progress = Stage == PlotStage.Ripe ? 1f : Mathf.Clamp01(0.24f + growth01 * 0.76f);
            plant.transform.localScale = Vector3.one * progress;

            for (int i = 0; i < fruits.Count; i++)
                if (fruits[i] != null) fruits[i].SetActive(Stage == PlotStage.Ripe);
        }

        void BuildSoil()
        {
            var gm = farm.GameManager;
            soilPad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            soilPad.name = "Soil";
            soilPad.transform.SetParent(transform, false);
            soilPad.transform.localPosition = new Vector3(0f, 0.09f, 0f);
            soilPad.transform.localScale = new Vector3(2.35f, 0.18f, 2.35f);
            RemoveCollider(soilPad);
            soilRenderer = soilPad.GetComponent<Renderer>();
            gm.Materials.Apply(soilPad, "Soil", false, true);

            // Three tilled rows for a hand-made look.
            for (int i = -1; i <= 1; i++)
            {
                var ridge = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ridge.name = "Ridge";
                ridge.transform.SetParent(transform, false);
                ridge.transform.localPosition = new Vector3(i * 0.62f, 0.19f, 0f);
                ridge.transform.localScale = new Vector3(0.42f, 0.06f, 2.05f);
                RemoveCollider(ridge);
                gm.Materials.Apply(ridge, "PathDark", false, true);
            }
        }

        void BuildPlant()
        {
            var gm = farm.GameManager;
            plant = new GameObject("Plant");
            plant.transform.SetParent(transform, false);
            plant.transform.localPosition = new Vector3(0f, 0.18f, 0f);

            switch (data.visualStyle)
            {
                case CropVisualStyle.Wheat:
                    BuildWheat(gm);
                    break;
                case CropVisualStyle.Corn:
                    BuildCorn(gm);
                    break;
                case CropVisualStyle.Carrot:
                    BuildCarrot(gm);
                    break;
                case CropVisualStyle.Pumpkin:
                    BuildPumpkin(gm);
                    break;
            }
        }

        void BuildWheat(GameManager gm)
        {
            for (int i = 0; i < 6; i++)
            {
                float angle = i / 6f * Mathf.PI * 2f;
                var pos = new Vector3(Mathf.Cos(angle) * 0.55f, 0.42f, Mathf.Sin(angle) * 0.55f);
                Part(PrimitiveType.Cylinder, "Stem", plant.transform, pos,
                    new Vector3(0.035f, 0.42f, 0.035f), "WheatGreen", gm);
                var head = Part(PrimitiveType.Cube, "Grain", plant.transform, pos + Vector3.up * 0.48f,
                    new Vector3(0.11f, 0.22f, 0.11f), "WheatGold", gm);
                fruits.Add(head);
            }
        }

        void BuildCorn(GameManager gm)
        {
            for (int i = -1; i <= 1; i += 2)
            {
                Part(PrimitiveType.Cylinder, "Stalk", plant.transform, new Vector3(i * 0.32f, 0.75f, 0f),
                    new Vector3(0.07f, 0.75f, 0.07f), "CornGreen", gm);
                for (int leaf = 0; leaf < 3; leaf++)
                {
                    var blade = Part(PrimitiveType.Cube, "Leaf", plant.transform,
                        new Vector3(i * (0.32f + 0.18f), 0.55f + leaf * 0.28f, 0f),
                        new Vector3(0.5f, 0.035f, 0.2f), "CornGreen", gm);
                    blade.transform.localRotation = Quaternion.Euler(0f, 0f, i * (28f + leaf * 6f));
                }

                var cob = Part(PrimitiveType.Sphere, "Cob", plant.transform,
                    new Vector3(i * 0.32f, 0.92f, 0.19f),
                    new Vector3(0.18f, 0.32f, 0.18f), "CornYellow", gm);
                cob.transform.localRotation = Quaternion.Euler(14f, 0f, i * 10f);
                fruits.Add(cob);
            }
        }

        void BuildCarrot(GameManager gm)
        {
            for (int i = 0; i < 5; i++)
            {
                float angle = i / 5f * Mathf.PI * 2f;
                var leaf = Part(PrimitiveType.Cylinder, "Leaf", plant.transform,
                    new Vector3(Mathf.Cos(angle) * 0.18f, 0.32f, Mathf.Sin(angle) * 0.18f),
                    new Vector3(0.045f, 0.32f, 0.045f), "CornGreen", gm);
                leaf.transform.localRotation = Quaternion.Euler(Mathf.Sin(angle) * 22f, 0f, Mathf.Cos(angle) * 22f);
            }

            var carrot = Part(PrimitiveType.Cube, "Carrot", plant.transform, new Vector3(0f, 0.18f, 0f),
                new Vector3(0.2f, 0.34f, 0.2f), "CarrotOrange", gm);
            carrot.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);
            fruits.Add(carrot);
        }

        void BuildPumpkin(GameManager gm)
        {
            for (int i = 0; i < 4; i++)
            {
                float angle = i / 4f * Mathf.PI * 2f;
                var leaf = Part(PrimitiveType.Cube, "VineLeaf", plant.transform,
                    new Vector3(Mathf.Cos(angle) * 0.55f, 0.12f, Mathf.Sin(angle) * 0.55f),
                    new Vector3(0.55f, 0.05f, 0.38f), "LeafDark", gm);
                leaf.transform.localRotation = Quaternion.Euler(0f, angle * Mathf.Rad2Deg, 0f);
            }

            for (int i = 0; i < 2; i++)
            {
                var pumpkin = Part(PrimitiveType.Sphere, "Pumpkin", plant.transform,
                    new Vector3(i == 0 ? -0.35f : 0.42f, 0.3f, i == 0 ? 0.2f : -0.25f),
                    new Vector3(i == 0 ? 0.7f : 0.5f, i == 0 ? 0.55f : 0.4f, i == 0 ? 0.7f : 0.5f),
                    "PumpkinOrange", gm);
                pumpkin.transform.localRotation = Quaternion.Euler(0f, i * 35f, 0f);
                fruits.Add(pumpkin);
            }
        }

        static GameObject Part(PrimitiveType type, string name, Transform parent, Vector3 localPosition,
            Vector3 localScale, string material, GameManager gm)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            RemoveCollider(go);
            gm.Materials.Apply(go, material, false, true);
            return go;
        }

        static void RemoveCollider(GameObject go)
        {
            var collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
        }

        void SpawnHarvestBurst()
        {
            var go = new GameObject("HarvestBurst");
            go.transform.position = transform.position + Vector3.up * 0.7f;
            var particles = go.AddComponent<ParticleSystem>();

            var main = particles.main;
            main.duration = 0.25f;
            main.loop = false;
            main.startLifetime = 0.85f;
            main.startSpeed = 2.4f;
            main.startSize = 0.16f;
            main.startColor = data.ripeColor;
            main.gravityModifier = 1.1f;
            main.playOnAwake = false;

            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)16) });

            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = farm.GameManager.Materials.Get("ParticleSoft");
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            particles.Play();
            Destroy(go, 2.2f);
        }

        void SpawnSparkle()
        {
            var go = new GameObject("RipeSparkle");
            go.transform.position = transform.position + Vector3.up * 0.8f;
            var particles = go.AddComponent<ParticleSystem>();

            var main = particles.main;
            main.duration = 0.4f;
            main.loop = false;
            main.startLifetime = 1.1f;
            main.startSpeed = 0.7f;
            main.startSize = 0.1f;
            main.startColor = new Color(1f, 0.92f, 0.55f, 0.9f);
            main.playOnAwake = false;

            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)10) });

            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35f;
            shape.radius = 0.35f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = farm.GameManager.Materials.Get("ParticleSoft");
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            particles.Play();
            Destroy(go, 2.5f);
        }
    }
}
