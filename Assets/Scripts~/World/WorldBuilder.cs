using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace WheatAndCo
{
    /// <summary>Definition of one crop field: its plots, sign and unlock level.</summary>
    public class FarmField
    {
        public CropKind crop;
        public string displayName;
        public int unlockLevel;
        public Vector3 center;
        public bool unlocked;
        public TextMesh signLabel;
        public readonly List<Vector3> plotPositions = new List<Vector3>();
        public readonly List<CropPlot> plots = new List<CropPlot>();

        public void SetUnlocked(bool value)
        {
            unlocked = value;
            if (signLabel == null) return;
            signLabel.text = value ? displayName.ToUpperInvariant()
                : displayName.ToUpperInvariant() + "\nLevel " + unlockLevel;
            signLabel.color = value ? UITheme.Accent : UITheme.Muted;
        }
    }

    /// <summary>
    /// Builds the whole stylised farm from primitives: ground, fields, barn,
    /// market, pastures, pond, trees, lighting, fog and post-processing.
    /// Existing scene objects (Plane, Sphere, Cube1-10) are reused where possible.
    /// </summary>
    public class WorldBuilder
    {
        readonly GameManager gm;
        readonly System.Random rng = new System.Random(1234);

        public Vector3 PlayerSpawn = new Vector3(0f, 0.2f, -13f);
        public Vector3 MarketPosition = new Vector3(8.5f, 0f, -8.5f);
        public Bounds CowPen { get; private set; }
        public Bounds ChickenCoop { get; private set; }
        public Transform CropRoot { get; private set; }
        public Transform AnimalRoot { get; private set; }
        public Transform DecorRoot { get; private set; }
        public readonly List<FarmField> Fields = new List<FarmField>();

        readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

        public WorldBuilder(GameManager gameManager)
        {
            gm = gameManager;
        }

        public void Build()
        {
            var root = new GameObject("Wheat & Co. Farm");
            CropRoot = NewTransform("Crops", root.transform);
            AnimalRoot = NewTransform("Animals", root.transform);
            DecorRoot = NewTransform("Decor", root.transform);

            ConfigureRenderSettings();
            ResizeExistingGround();
            BuildPaths();
            BuildFields();
            BuildBarn();
            BuildMarket();
            BuildCowPen();
            BuildChickenCoop();
            BuildPond();
            BuildWindmill();
            BuildTrees();
            BuildFlowersAndRocks();
            BuildBoundary();
            BuildAtmosphere();
            BuildPostProcessing();
        }

        Material Mat(string name)
        {
            if (cache.TryGetValue(name, out var material) && material != null) return material;
            material = gm.Materials.Get(name);
            cache[name] = material;
            return material;
        }

        static Transform NewTransform(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        // ---------------------------------------------------------------
        // Lighting & atmosphere
        // ---------------------------------------------------------------

        void ConfigureRenderSettings()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0065f;
            RenderSettings.fogColor = new Color(0.78f, 0.84f, 0.78f, 1f);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.76f, 0.82f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.62f, 0.48f, 1f);
            RenderSettings.ambientGroundColor = new Color(0.28f, 0.30f, 0.22f, 1f);
            RenderSettings.reflectionIntensity = 0.5f;

            var skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                var sky = new Material(skyShader) { name = "Cozy Sky" };
                if (sky.HasProperty("_SkyTint")) sky.SetColor("_SkyTint", new Color(0.55f, 0.72f, 0.85f));
                if (sky.HasProperty("_GroundColor")) sky.SetColor("_GroundColor", new Color(0.55f, 0.58f, 0.45f));
                if (sky.HasProperty("_Exposure")) sky.SetFloat("_Exposure", 1.15f);
                if (sky.HasProperty("_AtmosphereThickness")) sky.SetFloat("_AtmosphereThickness", 1.05f);
                if (sky.HasProperty("_SunSize")) sky.SetFloat("_SunSize", 0.045f);
                if (sky.HasProperty("_SunSizeConvergence")) sky.SetFloat("_SunSizeConvergence", 6f);
                RenderSettings.skybox = sky;
            }

            var lightObject = GameObject.Find("Directional Light");
            Light sun;
            if (lightObject != null)
            {
                sun = lightObject.GetComponent<Light>();
            }
            else
            {
                var go = new GameObject("Directional Light");
                sun = go.AddComponent<Light>();
                sun.type = LightType.Directional;
            }

            if (sun != null)
            {
                sun.type = LightType.Directional;
                sun.color = new Color(1f, 0.94f, 0.82f);
                sun.intensity = 1.3f;
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = 0.82f;
                sun.transform.rotation = Quaternion.Euler(48f, -36f, 0f);
                RenderSettings.sun = sun;
            }

            AddPointLight("Barn Lantern", new Vector3(0f, 3.4f, 17.4f), new Color(1f, 0.72f, 0.38f), 2.4f, 11f);
            AddPointLight("Market Lantern", MarketPosition + new Vector3(0f, 2.4f, -1.2f),
                new Color(1f, 0.78f, 0.42f), 1.8f, 8f);
            AddPointLight("Coop Lantern", new Vector3(23f, 2.2f, 8.5f), new Color(1f, 0.76f, 0.4f), 1.4f, 7f);

            DynamicGI.UpdateEnvironment();
        }

        void AddPointLight(string name, Vector3 position, Color color, float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(DecorRoot, false);
            go.transform.position = position;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.ForcePixel;
        }

        void BuildPostProcessing()
        {
            var go = new GameObject("Post FX");
            go.transform.SetParent(DecorRoot, false);
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 5f;
            volume.weight = 1f;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.sharedProfile = profile;

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.95f);
            bloom.intensity.Override(0.42f);
            bloom.scatter.Override(0.68f);
            bloom.highQualityFiltering.Override(true);

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.26f);
            vignette.smoothness.Override(0.42f);
            vignette.color.Override(new Color(0.12f, 0.10f, 0.09f));

            var color = profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(0.12f);
            color.contrast.Override(6f);
            color.saturation.Override(10f);
        }

        // ---------------------------------------------------------------
        // Ground, paths and fields
        // ---------------------------------------------------------------

        void ResizeExistingGround()
        {
            var plane = GameObject.Find("Plane");
            if (plane != null)
            {
                plane.transform.position = new Vector3(0f, 0f, 4f);
                plane.transform.localScale = new Vector3(9f, 1f, 9f);
                plane.transform.rotation = Quaternion.identity;
                gm.Materials.Apply(plane, "Grass", false, true);
            }
            else
            {
                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "Plane";
                ground.transform.position = new Vector3(0f, 0f, 4f);
                ground.transform.localScale = new Vector3(9f, 1f, 9f);
                gm.Materials.Apply(ground, "Grass", false, true);
            }

            // Repurpose the sample Sphere as a hay bale.
            var sphere = GameObject.Find("Sphere");
            if (sphere != null)
            {
                sphere.name = "Hay Bale";
                sphere.transform.position = new Vector3(-8.6f, 0.8f, 20.4f);
                sphere.transform.localScale = new Vector3(1.7f, 1.3f, 2f);
                sphere.transform.rotation = Quaternion.Euler(0f, 26f, 0f);
                var collider = sphere.GetComponent<Collider>();
                if (collider != null) Object.Destroy(collider);
                gm.Materials.Apply(sphere, "Hay", true, true);
            }

            // Repurpose the ten cubes as barn crates.
            Vector3[] cratePositions =
            {
                new Vector3(-7.2f, 0.5f, 19.4f),
                new Vector3(-6.1f, 0.5f, 20.2f),
                new Vector3(-8.3f, 0.5f, 20.1f),
                new Vector3(-7.1f, 1.5f, 19.6f),
                new Vector3(-6.2f, 0.5f, 18.4f),
                new Vector3(-8.4f, 1.5f, 20.0f),
                new Vector3(8.9f, 0.5f, 19.8f),
                new Vector3(9.9f, 0.5f, 19.3f),
                new Vector3(9.0f, 1.5f, 19.6f),
                new Vector3(-5.2f, 0.5f, 19.3f)
            };

            for (int i = 0; i < cratePositions.Length; i++)
            {
                var cube = GameObject.Find("Cube" + (i + 1));
                if (cube == null) continue;
                cube.transform.position = cratePositions[i];
                cube.transform.localScale = new Vector3(1.15f, 1f, 1.15f);
                cube.transform.rotation = Quaternion.Euler(0f, (i * 37f) % 360f, 0f);
                gm.Materials.Apply(cube, "Wood", true, true);
            }
        }

        void BuildPaths()
        {
            Box("Main Path", new Vector3(0f, 0.045f, -1f), new Vector3(4.2f, 0.05f, 39f), "Path", false, false);
            Box("Market Path", new Vector3(4.4f, 0.05f, -8.5f), new Vector3(9.4f, 0.05f, 2.2f), "Path", false, false);
            Box("Cow Path", new Vector3(-10f, 0.05f, 6f), new Vector3(16f, 0.05f, 1.8f), "Path", false, false);
            Box("Coop Path", new Vector3(10f, 0.05f, 6f), new Vector3(16f, 0.05f, 1.8f), "Path", false, false);

            // A few worn stepping stones on the main path.
            for (int i = 0; i < 14; i++)
            {
                float z = -18f + i * 2.7f;
                var stone = Cylinder("Path Stone", new Vector3(Random(-0.7f, 0.7f), 0.06f, z),
                    new Vector3(0.8f, 0.025f, 0.8f), "StoneDark", false, false);
                stone.transform.rotation = Quaternion.Euler(0f, Random(0f, 360f), 0f);
            }
        }

        void BuildFields()
        {
            AddField(CropKind.Wheat, new Vector3(-10f, 0f, 0f));
            AddField(CropKind.Corn, new Vector3(10f, 0f, 0f));
            AddField(CropKind.Carrot, new Vector3(-10f, 0f, 12f));
            AddField(CropKind.Pumpkin, new Vector3(10f, 0f, 12f));
        }

        void AddField(CropKind crop, Vector3 center)
        {
            var cropData = gm.Database.Crop(crop);
            var field = new FarmField
            {
                crop = crop,
                displayName = cropData.displayName,
                unlockLevel = cropData.unlockLevel,
                center = center
            };

            for (int ix = -1; ix <= 1; ix++)
                for (int iz = -1; iz <= 1; iz++)
                    field.plotPositions.Add(center + new Vector3(ix * 2.95f, 0f, iz * 2.95f));

            BuildFieldFence(field);
            BuildFieldSign(field);
            Fields.Add(field);
            field.SetUnlocked(gm.Progression.Level >= field.unlockLevel);
        }

        void BuildFieldFence(FarmField field)
        {
            float halfX = 5.2f;
            float halfZ = 5.2f;
            Vector3 c = field.center;

            FenceLine(new Vector3(c.x - halfX, 0f, c.z - halfZ), new Vector3(c.x + halfX, 0f, c.z - halfZ), false);
            FenceLine(new Vector3(c.x - halfX, 0f, c.z + halfZ), new Vector3(c.x + halfX, 0f, c.z + halfZ), false);
            FenceLine(new Vector3(c.x - halfX, 0f, c.z - halfZ), new Vector3(c.x - halfX, 0f, c.z + halfZ), c.x > 0f);
            FenceLine(new Vector3(c.x + halfX, 0f, c.z - halfZ), new Vector3(c.x + halfX, 0f, c.z + halfZ), c.x < 0f);
        }

        void BuildFieldSign(FarmField field)
        {
            bool west = field.center.x < 0f;
            float x = field.center.x + (west ? 5.9f : -5.9f);
            var signRoot = new GameObject(field.displayName + " Sign");
            signRoot.transform.SetParent(DecorRoot, false);
            signRoot.transform.position = new Vector3(x, 0f, field.center.z);
            signRoot.transform.rotation = Quaternion.Euler(0f, west ? 90f : -90f, 0f);

            LocalBox(signRoot.transform, "Post", new Vector3(0f, 0.7f, 0f),
                new Vector3(0.16f, 1.4f, 0.16f), "WoodDark", false);
            LocalBox(signRoot.transform, "Board", new Vector3(0f, 1.45f, 0f),
                new Vector3(2.5f, 0.82f, 0.12f), "Sign", true);

            field.signLabel = MakeText(signRoot.transform, "Label", field.displayName.ToUpperInvariant(),
                new Vector3(0f, 1.47f, 0.085f), 0.09f, UITheme.Text, 52);
        }

        // ---------------------------------------------------------------
        // Buildings
        // ---------------------------------------------------------------

        void BuildBarn()
        {
            var barn = new GameObject("Barn");
            barn.transform.SetParent(DecorRoot, false);

            Box("Foundation", new Vector3(0f, 0.35f, 22f), new Vector3(9.2f, 0.7f, 7.2f), "Stone", true, true);
            Box("Walls", new Vector3(0f, 2.55f, 22f), new Vector3(9f, 4.4f, 7f), "BarnRed", true, true);

            var roofL = Box("Roof Left", new Vector3(-2.25f, 5.3f, 22f), new Vector3(5.5f, 0.28f, 7.9f), "Roof", false, true);
            roofL.transform.rotation = Quaternion.Euler(0f, 0f, 30f);
            var roofR = Box("Roof Right", new Vector3(2.25f, 5.3f, 22f), new Vector3(5.5f, 0.28f, 7.9f), "Roof", false, true);
            roofR.transform.rotation = Quaternion.Euler(0f, 0f, -30f);
            Box("Roof Ridge", new Vector3(0f, 6.0f, 22f), new Vector3(0.4f, 0.25f, 7.9f), "RoofDark", false, true);
            Box("Gable", new Vector3(0f, 5.1f, 18.52f), new Vector3(8.6f, 1.9f, 0.18f), "BarnRed", false, true);

            Box("Door", new Vector3(0f, 1.9f, 18.42f), new Vector3(3.4f, 3.7f, 0.18f), "WoodDark", false, true);
            Box("Door Trim L", new Vector3(-1.82f, 1.9f, 18.35f), new Vector3(0.22f, 4f, 0.16f), "BarnTrim", false, true);
            Box("Door Trim R", new Vector3(1.82f, 1.9f, 18.35f), new Vector3(0.22f, 4f, 0.16f), "BarnTrim", false, true);
            Box("Door Trim Top", new Vector3(0f, 3.85f, 18.35f), new Vector3(3.9f, 0.22f, 0.16f), "BarnTrim", false, true);
            Box("Door Cross", new Vector3(0f, 1.9f, 18.32f), new Vector3(3.1f, 0.14f, 0.06f), "BarnTrim", false, true);

            Box("Window L Trim", new Vector3(-3f, 2.9f, 18.42f), new Vector3(1.4f, 1.4f, 0.1f), "BarnTrim", false, true);
            Box("Window L", new Vector3(-3f, 2.9f, 18.33f), new Vector3(1.15f, 1.15f, 0.06f), "WindowGlow", false, false);
            Box("Window R Trim", new Vector3(3f, 2.9f, 18.42f), new Vector3(1.4f, 1.4f, 0.1f), "BarnTrim", false, true);
            Box("Window R", new Vector3(3f, 2.9f, 18.33f), new Vector3(1.15f, 1.15f, 0.06f), "WindowGlow", false, false);
            Box("Loft Trim", new Vector3(0f, 4.85f, 18.42f), new Vector3(1.7f, 1.15f, 0.1f), "BarnTrim", false, true);
            Box("Loft Window", new Vector3(0f, 4.85f, 18.33f), new Vector3(1.45f, 0.9f, 0.06f), "WindowGlow", false, false);

            Box("Name Board", new Vector3(0f, 3.9f, 18.28f), new Vector3(3.9f, 0.8f, 0.12f), "BarnTrim", false, true);
            MakeText(null, "Barn Name", "WHEAT & CO.", new Vector3(0f, 3.9f, 18.2f), 0.16f,
                UITheme.DarkText, 56, 180f);

            // Silo
            Cylinder("Silo", new Vector3(7.4f, 3.2f, 24.2f), new Vector3(2.1f, 3.2f, 2.1f), "Stone", false, true);
            Sphere("Silo Roof", new Vector3(7.4f, 6.5f, 24.2f), new Vector3(2.1f, 1.3f, 2.1f), "RoofDark", false);
            for (int i = 0; i < 3; i++)
                Cylinder("Silo Band", new Vector3(7.4f, 1.2f + i * 1.8f, 24.2f),
                    new Vector3(2.15f, 0.04f, 2.15f), "StoneDark", false, false);

            // Loose hay bales near the barn.
            var bale1 = Cylinder("Hay Bale", new Vector3(-10.2f, 0.85f, 19.2f),
                new Vector3(1.2f, 0.85f, 1.2f), "Hay", false, true);
            bale1.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            var bale2 = Cylinder("Hay Bale", new Vector3(-10.2f, 0.85f, 21f),
                new Vector3(1.05f, 0.85f, 1.05f), "Hay", false, true);
            bale2.transform.rotation = Quaternion.Euler(0f, 0f, 90f);

            Box("Bench", new Vector3(3.9f, 0.35f, 17.6f), new Vector3(2.2f, 0.18f, 0.6f), "Wood", true, true);
            Box("Bench Leg L", new Vector3(3.05f, 0.17f, 17.6f), new Vector3(0.16f, 0.35f, 0.5f), "WoodDark", false, true);
            Box("Bench Leg R", new Vector3(4.75f, 0.17f, 17.6f), new Vector3(0.16f, 0.35f, 0.5f), "WoodDark", false, true);
        }

        void BuildMarket()
        {
            var root = new GameObject("Market Stall");
            root.transform.SetParent(DecorRoot, false);
            root.transform.position = MarketPosition;

            LocalBox(root.transform, "Counter", new Vector3(0f, 0.8f, 0f),
                new Vector3(3.8f, 0.8f, 1.3f), "Wood", true);
            LocalBox(root.transform, "Counter Top", new Vector3(0f, 1.24f, 0f),
                new Vector3(4.1f, 0.12f, 1.5f), "Sign", false);
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                    LocalBox(root.transform, "Post", new Vector3(sx * 1.8f, 1.6f, sz * 0.85f),
                        new Vector3(0.16f, 3.2f, 0.16f), "WoodDark", false);

            var canopy = LocalBox(root.transform, "Canopy", new Vector3(0f, 3.25f, 0f),
                new Vector3(4.5f, 0.16f, 2.6f), "Shirt", false);
            canopy.transform.rotation = Quaternion.Euler(6f, 0f, 0f);

            LocalBox(root.transform, "Sign Board", new Vector3(0f, 2.75f, -1.06f),
                new Vector3(3.2f, 0.72f, 0.12f), "Sign", false);
            MakeText(root.transform, "Shop Sign", "FARM SHOP", new Vector3(0f, 2.75f, -1.14f),
                0.13f, UITheme.Text, 56, 180f);

            // Produce on the counter.
            for (int i = 0; i < 7; i++)
            {
                var color = i % 3 == 0 ? "CarrotOrange" : i % 3 == 1 ? "CornYellow" : "WheatGold";
                Sphere("Produce", MarketPosition + new Vector3(-1.3f + i * 0.42f, 1.42f, -0.2f),
                    new Vector3(0.22f, 0.22f, 0.22f), color, false);
            }

            var stall = new GameObject("Shop Interact");
            stall.transform.SetParent(root.transform, false);
            stall.transform.localPosition = Vector3.zero;
            var market = stall.AddComponent<MarketStall>();
            market.Initialize(gm);
        }

        void BuildCowPen()
        {
            Vector3 center = new Vector3(-23f, 0f, 6f);
            BuildPastureGround(center, 11.5f, 10.5f);
            FenceRect(center, 6f, 5.5f, true, false);

            CowPen = new Bounds(center + new Vector3(0f, 0.7f, 0f), new Vector3(10f, 1.4f, 8.8f));

            // Water trough.
            Box("Trough", new Vector3(center.x + 3.4f, 0.3f, center.z - 2.6f),
                new Vector3(2.6f, 0.6f, 1.1f), "WoodDark", false, true);
            Box("Trough Water", new Vector3(center.x + 3.4f, 0.56f, center.z - 2.6f),
                new Vector3(2.3f, 0.1f, 0.85f), "Water", false, false);

            Sphere("Hay Pile", new Vector3(center.x - 3.2f, 0.45f, center.z + 3f),
                new Vector3(1.9f, 0.9f, 1.6f), "Hay", false);
            Sphere("Hay Pile", new Vector3(center.x - 2.2f, 0.3f, center.z + 3.4f),
                new Vector3(1.2f, 0.6f, 1.1f), "Hay", false);

            var sign = new GameObject("Cow Pasture Sign");
            sign.transform.SetParent(DecorRoot, false);
            sign.transform.position = new Vector3(center.x + 6.3f, 0f, center.z);
            sign.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            LocalBox(sign.transform, "Post", new Vector3(0f, 0.7f, 0f), new Vector3(0.16f, 1.4f, 0.16f), "WoodDark", false);
            LocalBox(sign.transform, "Board", new Vector3(0f, 1.4f, 0f), new Vector3(2.2f, 0.7f, 0.12f), "Sign", false);
            MakeText(sign.transform, "Label", "COW PASTURE", new Vector3(0f, 1.42f, 0.085f), 0.075f, UITheme.Text, 44);
        }

        void BuildChickenCoop()
        {
            Vector3 center = new Vector3(23f, 0f, 6f);
            BuildPastureGround(center, 9.5f, 9.5f);
            FenceRect(center, 5f, 5f, false, true);

            ChickenCoop = new Bounds(center + new Vector3(0f, 0.7f, 0f), new Vector3(8f, 1.4f, 8.2f));

            // Coop house
            Box("Coop Base", new Vector3(center.x + 2.4f, 0.15f, center.z + 3.4f),
                new Vector3(3.6f, 0.3f, 3f), "StoneDark", false, true);
            Box("Coop Walls", new Vector3(center.x + 2.4f, 1.35f, center.z + 3.4f),
                new Vector3(3.4f, 2.4f, 2.8f), "Wood", false, true);
            var roofL = Box("Coop Roof L", new Vector3(center.x + 1.5f, 2.85f, center.z + 3.4f),
                new Vector3(2.2f, 0.16f, 3.2f), "Roof", false, true);
            roofL.transform.rotation = Quaternion.Euler(0f, 0f, 28f);
            var roofR = Box("Coop Roof R", new Vector3(center.x + 3.3f, 2.85f, center.z + 3.4f),
                new Vector3(2.2f, 0.16f, 3.2f), "Roof", false, true);
            roofR.transform.rotation = Quaternion.Euler(0f, 0f, -28f);
            Box("Coop Door", new Vector3(center.x + 2.4f, 0.9f, center.z + 2.02f),
                new Vector3(1.1f, 1.4f, 0.12f), "WoodDark", false, true);
            Box("Coop Ramp", new Vector3(center.x + 2.4f, 0.12f, center.z + 1.45f),
                new Vector3(1.2f, 0.08f, 1.2f), "Wood", false, true);
            Box("Nest Box", new Vector3(center.x + 3.7f, 0.8f, center.z + 4.2f),
                new Vector3(1.1f, 0.7f, 1f), "WoodDark", false, true);
            Sphere("Nest Straw", new Vector3(center.x + 3.7f, 1.18f, center.z + 4.2f),
                new Vector3(0.8f, 0.2f, 0.7f), "Hay", false);

            var sign = new GameObject("Chicken Coop Sign");
            sign.transform.SetParent(DecorRoot, false);
            sign.transform.position = new Vector3(center.x - 5.2f, 0f, center.z);
            sign.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            LocalBox(sign.transform, "Post", new Vector3(0f, 0.7f, 0f), new Vector3(0.16f, 1.4f, 0.16f), "WoodDark", false);
            LocalBox(sign.transform, "Board", new Vector3(0f, 1.4f, 0f), new Vector3(2.3f, 0.7f, 0.12f), "Sign", false);
            MakeText(sign.transform, "Label", "CHICKEN COOP", new Vector3(0f, 1.42f, 0.085f), 0.07f, UITheme.Text, 42);
        }

        void BuildPastureGround(Vector3 center, float width, float depth)
        {
            Cylinder("Pasture", new Vector3(center.x, 0.02f, center.z),
                new Vector3(width, 0.015f, depth), "GrassLight", false, true);
        }

        // ---------------------------------------------------------------
        // Nature
        // ---------------------------------------------------------------

        void BuildPond()
        {
            Vector3 center = new Vector3(-23f, 0f, -11f);
            Cylinder("Pond Water", new Vector3(center.x, 0.03f, center.z),
                new Vector3(11f, 0.025f, 11f), "Water", false, false);

            for (int i = 0; i < 18; i++)
            {
                float angle = i / 18f * Mathf.PI * 2f;
                float radius = 5.4f + Random(-0.18f, 0.18f);
                var rock = Sphere("Pond Rock",
                    new Vector3(center.x + Mathf.Cos(angle) * radius, 0.12f, center.z + Mathf.Sin(angle) * radius),
                    new Vector3(Random(0.5f, 1.1f), Random(0.3f, 0.6f), Random(0.5f, 1.1f)),
                    i % 2 == 0 ? "Stone" : "StoneDark", false);
                rock.transform.rotation = Quaternion.Euler(0f, Random(0f, 360f), 0f);
            }

            for (int i = 0; i < 9; i++)
            {
                float angle = Random(0f, Mathf.PI * 2f);
                float radius = Random(3.2f, 5.2f);
                var reed = Cylinder("Reed",
                    new Vector3(center.x + Mathf.Cos(angle) * radius, 0.6f, center.z + Mathf.Sin(angle) * radius),
                    new Vector3(0.06f, 0.6f, 0.06f), "Reed", false, false);
                reed.transform.rotation = Quaternion.Euler(Random(-8f, 8f), 0f, Random(-8f, 8f));
            }

            for (int i = 0; i < 6; i++)
            {
                float angle = Random(0f, Mathf.PI * 2f);
                float radius = Random(1f, 4f);
                Cylinder("Lily Pad",
                    new Vector3(center.x + Mathf.Cos(angle) * radius, 0.09f, center.z + Mathf.Sin(angle) * radius),
                    new Vector3(Random(0.5f, 1f), 0.015f, Random(0.5f, 1f)), "Lily", false, false);
            }

            // Little wooden pier.
            for (int i = 0; i < 4; i++)
                Box("Pier Plank", new Vector3(center.x + 5.8f - i * 0.55f, 0.12f + i * 0.01f, center.z),
                    new Vector3(0.55f, 0.1f, 1.7f), "Wood", false, true);

            // A duck.
            var duck = new GameObject("Duck");
            duck.transform.SetParent(DecorRoot, false);
            duck.transform.position = new Vector3(center.x + 1.5f, 0.1f, center.z + 1.2f);
            Sphere("Duck Body", duck.transform.position + new Vector3(0f, 0.18f, 0f),
                new Vector3(0.55f, 0.4f, 0.75f), "ChickenWhite", false);
            Sphere("Duck Head", duck.transform.position + new Vector3(0f, 0.48f, 0.28f),
                new Vector3(0.3f, 0.3f, 0.3f), "ChickenWhite", false);
            Box("Duck Beak", duck.transform.position + new Vector3(0f, 0.44f, 0.48f),
                new Vector3(0.18f, 0.08f, 0.2f), "Beak", false, false);
        }

        void BuildWindmill()
        {
            Vector3 basePosition = new Vector3(15.5f, 0f, 28f);
            var tower = Cylinder("Windmill Tower", basePosition + new Vector3(0f, 3.6f, 0f),
                new Vector3(1.8f, 3.6f, 1.8f), "Stone", false, true);
            tower.transform.rotation = Quaternion.identity;
            Sphere("Windmill Cap", basePosition + new Vector3(0f, 7.4f, 0f),
                new Vector3(2f, 1f, 2f), "RoofDark", false);
            Box("Windmill Door", basePosition + new Vector3(0f, 1.1f, -0.9f),
                new Vector3(0.9f, 1.6f, 0.12f), "WoodDark", false, true);

            var hub = new GameObject("Windmill Blades");
            hub.transform.SetParent(DecorRoot, false);
            hub.transform.position = basePosition + new Vector3(0f, 7f, -1.1f);
            for (int i = 0; i < 4; i++)
            {
                var blade = LocalBox(hub.transform, "Blade", Vector3.zero,
                    new Vector3(0.32f, 2.6f, 0.1f), "BarnTrim", false);
                blade.transform.localPosition = Vector3.zero;
                blade.transform.localRotation = Quaternion.Euler(0f, 0f, i * 90f);
                blade.transform.localPosition = blade.transform.localRotation * new Vector3(0f, 1.5f, 0f);
            }
            hub.AddComponent<SimpleDrift>().speed = 18f;
        }

        void BuildTrees()
        {
            Vector3[] positions =
            {
                new Vector3(-31f, 0f, -18f), new Vector3(-34f, 0f, -6f), new Vector3(-33f, 0f, 17f),
                new Vector3(-36f, 0f, 27f), new Vector3(-28f, 0f, 22f), new Vector3(-37f, 0f, 3f),
                new Vector3(31f, 0f, -18f), new Vector3(34f, 0f, -6f), new Vector3(33f, 0f, 17f),
                new Vector3(36f, 0f, 27f), new Vector3(29f, 0f, 22f), new Vector3(37f, 0f, 3f),
                new Vector3(-16f, 0f, 32f), new Vector3(16f, 0f, 32f), new Vector3(0f, 0f, 37f),
                new Vector3(-8f, 0f, 35f), new Vector3(8f, 0f, 35f), new Vector3(-18f, 0f, -21f),
                new Vector3(18f, 0f, -21f), new Vector3(-2f, 0f, -24f), new Vector3(26f, 0f, -17f),
                new Vector3(-27f, 0f, -17f), new Vector3(-40f, 0f, 12f), new Vector3(40f, 0f, 12f)
            };

            for (int i = 0; i < positions.Length; i++)
                BuildTree(positions[i], Random(0.85f, 1.35f), i % 3);
        }

        void BuildTree(Vector3 position, float scale, int variant)
        {
            var root = new GameObject("Tree");
            root.transform.SetParent(DecorRoot, false);
            root.transform.position = position;

            var trunk = Cylinder("Trunk", position + new Vector3(0f, 1.3f * scale, 0f),
                new Vector3(0.3f * scale, 1.3f * scale, 0.3f), "Trunk", true, true);
            trunk.transform.rotation = Quaternion.Euler(0f, Random(0f, 360f), 0f);

            string[] foliage = { "Leaf", "LeafLight", "LeafDark" };
            Sphere("Foliage", position + new Vector3(0f, 3.1f * scale, 0f),
                new Vector3(2.1f * scale, 1.7f * scale, 2.1f * scale), foliage[variant % 3], true);
            Sphere("Foliage", position + new Vector3(0.45f * scale, 3.8f * scale, 0.2f * scale),
                new Vector3(1.5f * scale, 1.3f * scale, 1.5f * scale), foliage[(variant + 1) % 3], true);
            Sphere("Foliage", position + new Vector3(-0.5f * scale, 3.6f * scale, -0.35f * scale),
                new Vector3(1.4f * scale, 1.2f * scale, 1.4f * scale), foliage[(variant + 2) % 3], true);
        }

        void BuildFlowersAndRocks()
        {
            string[] colors = { "FlowerPink", "FlowerYellow", "FlowerWhite", "FlowerPurple" };
            for (int i = 0; i < 42; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float x = side * Random(3.1f, 4.5f);
                float z = Random(-15f, 17f);
                var stem = Cylinder("Flower Stem", new Vector3(x, 0.14f, z),
                    new Vector3(0.025f, 0.14f, 0.025f), "CornGreen", false, false);
                Sphere("Flower", new Vector3(x, 0.3f, z), new Vector3(0.16f, 0.12f, 0.16f),
                    colors[i % colors.Length], false);
            }

            for (int i = 0; i < 12; i++)
            {
                Vector3 position = i < 6
                    ? new Vector3(Random(-34f, -18f), 0f, Random(-18f, -2f))
                    : new Vector3(Random(18f, 34f), 0f, Random(-18f, 2f));
                var rock = Sphere("Rock", position + new Vector3(0f, 0.2f, 0f),
                    new Vector3(Random(0.6f, 1.5f), Random(0.35f, 0.8f), Random(0.6f, 1.5f)),
                    i % 2 == 0 ? "Stone" : "StoneDark", false);
                rock.transform.rotation = Quaternion.Euler(0f, Random(0f, 360f), 0f);
            }

            for (int i = 0; i < 10; i++)
            {
                var bush = Sphere("Bush",
                    new Vector3(Random(-16f, 16f), 0.35f, Random(-14f, 16f)),
                    new Vector3(Random(1.2f, 2.2f), Random(0.8f, 1.3f), Random(1.2f, 2.2f)),
                    i % 2 == 0 ? "LeafDark" : "Leaf", false);
                bush.transform.rotation = Quaternion.Euler(0f, Random(0f, 360f), 0f);
            }

            for (int i = 0; i < 8; i++)
            {
                Vector3 basePosition = new Vector3(Random(-26f, -16f), 0f, Random(-18f, 12f));
                Cylinder("Mushroom Stem", basePosition + new Vector3(0f, 0.12f, 0f),
                    new Vector3(0.08f, 0.12f, 0.08f), "BarnTrim", false, false);
                Sphere("Mushroom Cap", basePosition + new Vector3(0f, 0.28f, 0f),
                    new Vector3(0.28f, 0.16f, 0.28f), "MushroomRed", false);
            }

            // Low hills behind the farm.
            Sphere("Hill", new Vector3(-34f, 1f, 52f), new Vector3(36f, 7f, 18f), "GrassDark", false, false);
            Sphere("Hill", new Vector3(34f, 1f, 52f), new Vector3(38f, 8f, 20f), "GrassDark", false, false);
            Sphere("Hill", new Vector3(0f, 0f, 62f), new Vector3(50f, 9f, 20f), "GrassLight", false, false);
            Sphere("Hill", new Vector3(-48f, 0f, 18f), new Vector3(20f, 6f, 32f), "GrassDark", false, false);
            Sphere("Hill", new Vector3(48f, 0f, 18f), new Vector3(20f, 6f, 32f), "GrassDark", false, false);
        }

        void BuildBoundary()
        {
            AddInvisibleWall("Boundary South", new Vector3(0f, 2f, -30f), new Vector3(80f, 5f, 1f));
            AddInvisibleWall("Boundary North", new Vector3(0f, 2f, 39f), new Vector3(80f, 5f, 1f));
            AddInvisibleWall("Boundary West", new Vector3(-39f, 2f, 4f), new Vector3(1f, 5f, 72f));
            AddInvisibleWall("Boundary East", new Vector3(39f, 2f, 4f), new Vector3(1f, 5f, 72f));
        }

        void AddInvisibleWall(string name, Vector3 position, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(DecorRoot, false);
            go.transform.position = position;
            var collider = go.AddComponent<BoxCollider>();
            collider.size = size;
        }

        void BuildAtmosphere()
        {
            BuildPollen(new Vector3(0f, 2.1f, 4f), new Vector3(34f, 2.4f, 34f), 5.5f, 0.07f,
                new Color(1f, 0.92f, 0.62f, 0.75f));
            BuildPollen(new Vector3(-22f, 1.4f, -10f), new Vector3(16f, 1.5f, 16f), 1.6f, 0.06f,
                new Color(0.75f, 0.95f, 0.72f, 0.6f));

            // Soft clouds.
            for (int i = 0; i < 6; i++)
            {
                var cloud = Sphere("Cloud",
                    new Vector3(Random(-34f, 34f), Random(20f, 26f), Random(12f, 46f)),
                    new Vector3(Random(9f, 16f), Random(1.5f, 2.4f), Random(5f, 8f)),
                    "BarnTrim", false, false);
                cloud.transform.rotation = Quaternion.Euler(0f, Random(0f, 360f), 0f);
            }
        }

        void BuildPollen(Vector3 position, Vector3 size, float rate, float particleSize, Color color)
        {
            var go = new GameObject("Pollen");
            go.transform.SetParent(DecorRoot, false);
            go.transform.position = position;
            var particles = go.AddComponent<ParticleSystem>();

            var main = particles.main;
            main.loop = true;
            main.startLifetime = 10f;
            main.startSpeed = 0.18f;
            main.startSize = particleSize;
            main.startColor = color;
            main.maxParticles = 120;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.004f;

            var emission = particles.emission;
            emission.rateOverTime = rate;

            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = size;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = gm.Materials.Get("ParticleSoft");
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;

            particles.Play();
        }

        // ---------------------------------------------------------------
        // Primitive helpers
        // ---------------------------------------------------------------

        GameObject Box(string name, Vector3 position, Vector3 scale, string material, bool collider, bool shadows)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(DecorRoot, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            if (!collider) RemoveCollider(go);
            gm.Materials.Apply(go, material, shadows, true);
            return go;
        }

        GameObject LocalBox(Transform parent, string name, Vector3 localPosition, Vector3 localScale,
            string material, bool collider)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            if (!collider) RemoveCollider(go);
            gm.Materials.Apply(go, material, true, true);
            return go;
        }

        GameObject Cylinder(string name, Vector3 position, Vector3 scale, string material, bool collider, bool shadows)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(DecorRoot, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            if (!collider) RemoveCollider(go);
            gm.Materials.Apply(go, material, shadows, true);
            return go;
        }

        GameObject Sphere(string name, Vector3 position, Vector3 scale, string material, bool collider)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(DecorRoot, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            if (!collider) RemoveCollider(go);
            gm.Materials.Apply(go, material, true, true);
            return go;
        }

        TextMesh MakeText(Transform parent, string name, string text, Vector3 position, float characterSize,
            Color color, int fontSize, float yaw = 0f)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = parent != null ? position : position;
            go.transform.localPosition = parent != null ? position : position;
            go.transform.rotation = parent != null ? Quaternion.Euler(0f, yaw, 0f) : Quaternion.Euler(0f, yaw, 0f);

            var mesh = go.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.font = UITheme.Font;
            mesh.fontSize = fontSize;
            mesh.characterSize = characterSize;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = color;

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = UITheme.Font.material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return mesh;
        }

        void FenceRect(Vector3 center, float halfX, float halfZ, bool gapEast, bool gapWest)
        {
            FenceLine(new Vector3(center.x - halfX, 0f, center.z - halfZ),
                new Vector3(center.x + halfX, 0f, center.z - halfZ), false);
            FenceLine(new Vector3(center.x - halfX, 0f, center.z + halfZ),
                new Vector3(center.x + halfX, 0f, center.z + halfZ), false);
            FenceLine(new Vector3(center.x - halfX, 0f, center.z - halfZ),
                new Vector3(center.x - halfX, 0f, center.z + halfZ), gapWest);
            FenceLine(new Vector3(center.x + halfX, 0f, center.z - halfZ),
                new Vector3(center.x + halfX, 0f, center.z + halfZ), gapEast);
        }

        void FenceLine(Vector3 a, Vector3 b, bool gap)
        {
            Vector3 delta = b - a;
            float length = delta.magnitude;
            if (length < 0.01f) return;
            Vector3 direction = delta / length;
            float postSpacing = 1.8f;

            for (float d = 0f; d <= length + 0.01f; d += postSpacing)
            {
                if (gap && Mathf.Abs(d - length * 0.5f) < 1.35f) continue;
                var post = Box("Fence Post", a + direction * d + new Vector3(0f, 0.48f, 0f),
                    new Vector3(0.13f, 0.96f, 0.13f), "Fence", false, true);
                post.transform.rotation = Quaternion.identity;
            }

            if (gap)
            {
                float gapStart = length * 0.5f - 1.2f;
                float gapEnd = length * 0.5f + 1.2f;
                BuildRail(a, a + direction * Mathf.Max(0f, gapStart));
                BuildRail(a + direction * Mathf.Min(length, gapEnd), b);
            }
            else
            {
                BuildRail(a, b);
            }
        }

        void BuildRail(Vector3 a, Vector3 b)
        {
            float length = Vector3.Distance(a, b);
            if (length < 0.2f) return;

            for (int i = 0; i < 2; i++)
            {
                float height = 0.42f + i * 0.36f;
                var rail = Box("Fence Rail", (a + b) * 0.5f + new Vector3(0f, height, 0f),
                    new Vector3(0.08f, 0.09f, length), "Fence", false, true);
                rail.transform.rotation = Quaternion.LookRotation(b - a, Vector3.up);
            }
        }

        static void RemoveCollider(GameObject go)
        {
            var collider = go.GetComponent<Collider>();
            if (collider != null) Object.Destroy(collider);
        }

        float Random(float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }
    }
}
