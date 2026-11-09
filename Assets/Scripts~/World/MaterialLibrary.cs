using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace WheatAndCo
{
    /// <summary>
    /// Runtime material palette for the whole farm. Materials are loaded from
    /// Resources/Materials when present, otherwise generated with URP/Lit.
    /// </summary>
    public class MaterialLibrary
    {
        readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        Shader litShader;

        public void Initialize()
        {
            litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Universal Render Pipeline/Simple Lit");

            // Prefer persistent materials authored in Resources/Materials.
            foreach (var mat in Resources.LoadAll<Material>("Materials"))
                if (mat != null) materials[mat.name] = mat;

            BuildPalette();
            ApplyTextures();
            materials["ParticleSoft"] = CreateParticleMaterial();

            foreach (var mat in materials.Values)
                if (mat != null && mat.shader == null && litShader != null) mat.shader = litShader;
        }

        public Material Get(string name)
        {
            if (materials.TryGetValue(name, out var mat) && mat != null) return mat;
            if (litShader == null) litShader = Shader.Find("Universal Render Pipeline/Lit");
            mat = new Material(litShader != null ? litShader : Shader.Find("Standard")) { name = name };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            materials[name] = mat;
            return mat;
        }

        public void Apply(GameObject go, string materialName, bool castShadows = true, bool receiveShadows = true)
        {
            if (go == null) return;
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;
            renderer.sharedMaterial = Get(materialName);
            renderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            renderer.receiveShadows = receiveShadows;
        }

        public static void Tint(GameObject go, Color color)
        {
            if (go == null) return;
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            renderer.SetPropertyBlock(block);
        }

        public static void SetShadows(GameObject go, bool cast, bool receive)
        {
            if (go == null) return;
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;
            renderer.shadowCastingMode = cast ? ShadowCastingMode.On : ShadowCastingMode.Off;
            renderer.receiveShadows = receive;
        }

        void BuildPalette()
        {
            Ensure("Grass", new Color(0.39f, 0.62f, 0.30f), 0.12f);
            Ensure("GrassDark", new Color(0.27f, 0.48f, 0.23f), 0.10f);
            Ensure("GrassLight", new Color(0.49f, 0.70f, 0.35f), 0.12f);
            Ensure("Path", new Color(0.68f, 0.55f, 0.37f), 0.08f);
            Ensure("PathDark", new Color(0.52f, 0.40f, 0.26f), 0.08f);
            Ensure("Sand", new Color(0.80f, 0.71f, 0.50f), 0.05f);

            Ensure("Soil", new Color(0.37f, 0.25f, 0.16f), 0.05f);
            Ensure("SoilLocked", new Color(0.34f, 0.31f, 0.28f), 0.04f);
            Ensure("Wood", new Color(0.55f, 0.36f, 0.19f), 0.10f);
            Ensure("WoodDark", new Color(0.37f, 0.23f, 0.12f), 0.08f);
            Ensure("Fence", new Color(0.74f, 0.57f, 0.35f), 0.10f);
            Ensure("Sign", new Color(0.64f, 0.45f, 0.25f), 0.08f);

            Ensure("BarnRed", new Color(0.65f, 0.19f, 0.14f), 0.15f);
            Ensure("BarnTrim", new Color(0.96f, 0.92f, 0.83f), 0.20f);
            Ensure("Roof", new Color(0.37f, 0.20f, 0.14f), 0.10f);
            Ensure("RoofDark", new Color(0.26f, 0.14f, 0.10f), 0.08f);
            Ensure("Stone", new Color(0.60f, 0.60f, 0.57f), 0.10f);
            Ensure("StoneDark", new Color(0.44f, 0.44f, 0.42f), 0.08f);
            Ensure("Metal", new Color(0.68f, 0.70f, 0.72f), 0.55f, 0.45f);

            Ensure("Trunk", new Color(0.43f, 0.29f, 0.17f), 0.06f);
            Ensure("Leaf", new Color(0.25f, 0.52f, 0.24f), 0.10f);
            Ensure("LeafLight", new Color(0.37f, 0.65f, 0.31f), 0.12f);
            Ensure("LeafDark", new Color(0.18f, 0.42f, 0.20f), 0.08f);
            Ensure("Hay", new Color(0.91f, 0.77f, 0.39f), 0.06f);
            Ensure("Reed", new Color(0.55f, 0.64f, 0.31f), 0.06f);

            Ensure("WheatGold", new Color(0.96f, 0.80f, 0.32f), 0.10f);
            Ensure("WheatGreen", new Color(0.47f, 0.68f, 0.29f), 0.10f);
            Ensure("CornYellow", new Color(0.99f, 0.85f, 0.24f), 0.10f);
            Ensure("CornGreen", new Color(0.38f, 0.62f, 0.25f), 0.10f);
            Ensure("CarrotOrange", new Color(0.95f, 0.48f, 0.17f), 0.10f);
            Ensure("PumpkinOrange", new Color(0.96f, 0.47f, 0.12f), 0.10f);
            Ensure("Tint", Color.white, 0.08f);

            Ensure("Water", new Color(0.32f, 0.63f, 0.72f, 0.72f), 0.90f, transparent: true);
            Ensure("Lily", new Color(0.30f, 0.60f, 0.32f), 0.20f);

            Ensure("CowCream", new Color(0.95f, 0.90f, 0.80f), 0.15f);
            Ensure("CowBrown", new Color(0.55f, 0.34f, 0.20f), 0.12f);
            Ensure("CowBlack", new Color(0.18f, 0.16f, 0.15f), 0.12f);
            Ensure("CowPink", new Color(0.96f, 0.65f, 0.62f), 0.10f);
            Ensure("ChickenWhite", new Color(0.97f, 0.95f, 0.88f), 0.12f);
            Ensure("ChickenBrown", new Color(0.72f, 0.51f, 0.30f), 0.10f);
            Ensure("Beak", new Color(0.98f, 0.74f, 0.20f), 0.10f);
            Ensure("Comb", new Color(0.86f, 0.22f, 0.18f), 0.10f);
            Ensure("Egg", new Color(0.98f, 0.95f, 0.85f), 0.25f);
            Ensure("Milk", new Color(0.98f, 0.98f, 0.95f), 0.30f);

            Ensure("Skin", new Color(0.95f, 0.76f, 0.60f), 0.10f);
            Ensure("Shirt", new Color(0.83f, 0.33f, 0.24f), 0.10f);
            Ensure("Overalls", new Color(0.24f, 0.37f, 0.58f), 0.10f);
            Ensure("Hat", new Color(0.93f, 0.79f, 0.43f), 0.08f);
            Ensure("Boots", new Color(0.34f, 0.23f, 0.15f), 0.10f);
            Ensure("Hair", new Color(0.32f, 0.22f, 0.15f), 0.06f);

            Ensure("FlowerPink", new Color(0.95f, 0.55f, 0.72f), 0.10f);
            Ensure("FlowerYellow", new Color(0.98f, 0.86f, 0.30f), 0.10f);
            Ensure("FlowerWhite", new Color(0.98f, 0.96f, 0.90f), 0.10f);
            Ensure("FlowerPurple", new Color(0.66f, 0.50f, 0.88f), 0.10f);
            Ensure("MushroomRed", new Color(0.82f, 0.22f, 0.18f), 0.10f);

            Ensure("WindowGlow", new Color(1f, 0.82f, 0.45f), 0.30f,
                emission: new Color(1f, 0.68f, 0.28f), emissionStrength: 2.2f);
            Ensure("Lantern", new Color(1f, 0.86f, 0.52f), 0.30f,
                emission: new Color(1f, 0.63f, 0.22f), emissionStrength: 2.8f);

            Ensure("HighlightRing", new Color(1f, 0.78f, 0.28f, 0.42f), 0.30f,
                emission: new Color(1f, 0.65f, 0.15f), emissionStrength: 1.4f, transparent: true);
        }

        /// <summary>
        /// Swaps flat palette colours for CC0 ambientCG surface textures. The
        /// base colour becomes a tint so each variation can share one albedo
        /// map while still reading differently (lush lawn vs. dry pasture).
        /// </summary>
        void ApplyTextures()
        {
            var grass = LoadTextures("Grass004");
            var soil = LoadTextures("Ground048");
            var paving = LoadTextures("PavingStones138");
            var planks = LoadTextures("Planks037A");
            var rock = LoadTextures("Rock064");
            var roof = LoadTextures("RoofingTiles012A");
            var bark = LoadTextures("Bark012");
            var fabric = LoadTextures("Fabric086");

            AssignTexture("Grass", grass, new Color(0.82f, 0.88f, 0.76f), 18f);
            AssignTexture("GrassLight", grass, new Color(1f, 1f, 0.90f), 7f);
            AssignTexture("GrassDark", grass, new Color(0.55f, 0.62f, 0.50f), 6f);

            AssignTexture("Soil", soil, new Color(0.92f, 0.80f, 0.66f), 2f);
            AssignTexture("SoilLocked", soil, new Color(0.60f, 0.57f, 0.52f), 1.5f);

            AssignTexture("Path", paving, new Color(1f, 0.95f, 0.86f), 3f);
            AssignTexture("PathDark", paving, new Color(0.72f, 0.68f, 0.62f), 2f);

            AssignTexture("Wood", planks, new Color(0.95f, 0.80f, 0.62f), 2f);
            AssignTexture("WoodDark", planks, new Color(0.50f, 0.38f, 0.30f), 1.5f);
            AssignTexture("Fence", planks, new Color(1f, 0.90f, 0.72f), 1.5f);
            AssignTexture("Sign", planks, new Color(0.92f, 0.78f, 0.58f), 1f);
            AssignTexture("BarnRed", planks, new Color(0.62f, 0.20f, 0.16f), 2f);
            AssignTexture("BarnTrim", planks, new Color(0.98f, 0.94f, 0.86f), 1f);
            AssignTexture("Shirt", fabric, new Color(0.85f, 0.32f, 0.24f), 1.5f);

            AssignTexture("Stone", rock, new Color(0.95f, 0.93f, 0.88f), 2f);
            AssignTexture("StoneDark", rock, new Color(0.62f, 0.61f, 0.58f), 1.5f);

            AssignTexture("Roof", roof, new Color(0.62f, 0.34f, 0.26f), 2f);
            AssignTexture("RoofDark", roof, new Color(0.40f, 0.24f, 0.20f), 1.5f);
            AssignTexture("Trunk", bark, new Color(0.90f, 0.82f, 0.72f), 1f);
        }

        struct TextureSet
        {
            public Texture2D albedo;
            public Texture2D normal;
        }

        static TextureSet LoadTextures(string assetId)
        {
            return new TextureSet
            {
                albedo = Resources.Load<Texture2D>("Textures/" + assetId + "_1K-JPG_Color"),
                normal = Resources.Load<Texture2D>("Textures/" + assetId + "_1K-JPG_NormalGL")
            };
        }

        void AssignTexture(string name, TextureSet set, Color tint, float tiling)
        {
            if (set.albedo == null) return;
            if (!materials.TryGetValue(name, out var mat) || mat == null) return;

            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", set.albedo);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
            mat.SetTextureScale("_BaseMap", new Vector2(tiling, tiling));

            if (set.normal != null && mat.HasProperty("_BumpMap"))
            {
                mat.SetTexture("_BumpMap", set.normal);
                mat.SetTextureScale("_BumpMap", new Vector2(tiling, tiling));
                mat.EnableKeyword("_NORMALMAP");
            }
        }

        void Ensure(string name, Color color, float smoothness, float metallic = 0f,
            Color? emission = null, float emissionStrength = 1f, bool transparent = false)
        {
            if (materials.ContainsKey(name)) return;

            var mat = new Material(litShader) { name = name };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);

            if (emission.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emission.Value * emissionStrength);
            }

            if (transparent) MakeTransparent(mat);
            materials[name] = mat;
        }

        static void MakeTransparent(Material mat)
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_AlphaClip", 0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)RenderQueue.Transparent;
        }

        Material CreateParticleMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = litShader;
            var mat = new Material(shader) { name = "ParticleSoft" };
            MakeTransparent(mat);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetTexture("_BaseMap", CreateSoftCircleTexture());
            mat.renderQueue = (int)RenderQueue.Transparent + 10;
            return mat;
        }

        static Texture2D CreateSoftCircleTexture()
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "SoftParticle",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color[size * size];
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f - half) / half;
                    float dy = (y + 0.5f - half) / half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(1f - d);
                    alpha *= alpha;
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

#if UNITY_EDITOR
        /// <summary>Editor helper: saves the generated palette as material assets.</summary>
        public static void ExportAssets()
        {
            var library = new MaterialLibrary();
            library.Initialize();

            const string folder = "Assets/Resources/Materials";
            if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Resources"))
                UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");
            if (!UnityEditor.AssetDatabase.IsValidFolder(folder))
                UnityEditor.AssetDatabase.CreateFolder("Assets/Resources", "Materials");

            foreach (var mat in library.materials.Values)
            {
                if (mat == null) continue;
                if (string.IsNullOrEmpty(mat.name)) continue;
                string path = folder + "/" + mat.name + ".mat";
                if (UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path) != null) continue;
                UnityEditor.AssetDatabase.CreateAsset(mat, path);
            }

            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
        }
#endif
    }
}
