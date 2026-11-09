using UnityEngine;

namespace WheatAndCo
{
    /// <summary>
    /// A friendly cow. Feed wheat to start milk production, then milk her when
    /// the bottle icon appears. Hunger slowly falls and pauses production.
    /// </summary>
    public class Cow : InteractableBase
    {
        public string CowName { get; private set; } = "Cow";
        public float Hunger { get; private set; } = 100f;
        public float MilkTimer { get; private set; }
        public bool MilkReady { get; private set; }
        public bool Producing { get; private set; }

        AnimalSystem system;
        AnimalData data;
        Bounds wanderBounds;

        Transform model;
        Transform head;
        Transform legFL, legFR, legBL, legBR;
        Transform tail;
        Transform barRoot;
        Transform barFill;
        Renderer barFillRenderer;
        GameObject milkIcon;

        float wanderTimer;
        float walkSpeed;
        float bobPhase;
        Vector3 target;
        bool autoCollecting;
        float autoTimer;

        bool HasWheat => system.GameManager.Inventory.Count(ItemKind.Wheat) > 0;

        public void Initialize(AnimalSystem animalSystem, string cowName, Vector3 position, bool brown, Bounds bounds)
        {
            system = animalSystem;
            data = system.GameManager.Database.Cow;
            CowName = cowName;
            wanderBounds = bounds;
            transform.position = new Vector3(position.x, position.y, position.z);

            BuildModel(brown);
            BuildStatusBar();
            PickWanderTarget();

            Hunger = 100f;
            MilkTimer = 0f;
            MilkReady = false;
            Producing = false;
        }

        public override float HighlightRadius => 1.15f;

        public override string Prompt
        {
            get
            {
                if (MilkReady) return "Milk " + CowName + " (+1 Milk)";
                if (Producing)
                    return CowName + " is grazing - milk in " + Mathf.CeilToInt(MilkTimer) + "s";
                if (HasWheat)
                    return "Feed " + CowName + " wheat (+" + data.feedXp + " XP)";
                return CowName + " is waiting for wheat";
            }
        }

        public override void Interact(PlayerController player)
        {
            if (MilkReady)
            {
                CollectMilk(false);
                return;
            }

            if (!Producing && HasWheat)
            {
                Feed();
                return;
            }

            if (!Producing)
            {
                system.GameManager.UI.Toast("You need 1 Wheat to feed " + CowName + ".", UITheme.Muted);
                system.GameManager.Audio.Play(Sfx.Error, 0.5f);
                return;
            }

            Pet();
        }

        void Feed()
        {
            var gm = system.GameManager;
            gm.Inventory.TryRemove(ItemKind.Wheat, 1);
            Hunger = Mathf.Min(100f, Hunger + 40f);
            Producing = true;
            MilkReady = false;
            MilkTimer = data.milkSeconds;

            gm.Progression.AddXp(data.feedXp, "Feed");
            gm.Objectives.Notify(ObjEvent.FeedCow, 1);
            gm.Audio.Play(Sfx.Feed);
            gm.Audio.Play(Sfx.Moo, 0.7f);
            gm.UI.Toast(CowName + " enjoyed the wheat! Milk in " + Mathf.CeilToInt(data.milkSeconds) + "s.", UITheme.Good, 3f);
            FloatingText.Spawn("Munch!", transform.position + Vector3.up * 1.8f, UITheme.Good, 0.08f);
            gm.UI.RefreshAll();
        }

        void CollectMilk(bool automatic)
        {
            var gm = system.GameManager;
            MilkReady = false;
            Producing = false;
            MilkTimer = 0f;
            autoCollecting = false;
            Hunger = Mathf.Max(0f, Hunger - 12f);

            gm.Inventory.Add(ItemKind.Milk, 1);
            gm.Progression.AddXp(data.milkXp, "Milk");
            gm.Objectives.Notify(ObjEvent.CollectMilk, 1);
            gm.Audio.Play(Sfx.Milk);
            FloatingText.Spawn("+1 Milk", transform.position + Vector3.up * 1.9f,
                new Color(0.98f, 0.98f, 0.94f), 0.085f);

            if (!automatic)
                gm.UI.Toast("Fresh milk from " + CowName + "!", UITheme.Accent, 2.5f);

            gm.UI.RefreshAll();
        }

        void Pet()
        {
            var gm = system.GameManager;
            gm.Audio.Play(Sfx.Moo, 0.6f);
            FloatingText.Spawn("Moo!", transform.position + Vector3.up * 1.8f, UITheme.Text, 0.08f);
            gm.UI.Toast(CowName + " nudges you affectionately.", UITheme.Muted, 2f);
        }

        public void Tick(float deltaTime)
        {
            var gm = system.GameManager;

            if (Hunger > 0f)
                Hunger = Mathf.Max(0f, Hunger - data.hungerDecayPerSecond * deltaTime);

            if (Producing && !MilkReady && Hunger > 8f)
            {
                MilkTimer -= deltaTime;
                if (MilkTimer <= 0f)
                {
                    MilkTimer = 0f;
                    MilkReady = true;
                    if (gm.Progression.HasUpgrade(UpgradeId.AutoCollector))
                    {
                        autoCollecting = true;
                        autoTimer = 1.2f;
                    }
                }
            }

            if (MilkReady && !autoCollecting && gm.Progression.HasUpgrade(UpgradeId.AutoCollector))
            {
                autoCollecting = true;
                autoTimer = 1.2f;
            }

            if (autoCollecting)
            {
                autoTimer -= deltaTime;
                if (autoTimer <= 0f) CollectMilk(true);
            }

            Wander(deltaTime);
            Animate(deltaTime);
            UpdateStatusBar();
        }

        void PickWanderTarget()
        {
            target = new Vector3(
                Random.Range(wanderBounds.min.x, wanderBounds.max.x),
                transform.position.y,
                Random.Range(wanderBounds.min.z, wanderBounds.max.z));
            wanderTimer = Random.Range(3.5f, 7f);
            walkSpeed = Random.value < 0.25f ? 0f : Random.Range(0.65f, 1.15f);
        }

        void Wander(float deltaTime)
        {
            wanderTimer -= deltaTime;
            if (wanderTimer <= 0f) PickWanderTarget();

            Vector3 toTarget = target - transform.position;
            toTarget.y = 0f;
            if (walkSpeed > 0.05f && toTarget.sqrMagnitude > 0.25f)
            {
                Vector3 direction = toTarget.normalized;
                transform.position += direction * walkSpeed * deltaTime;
                Quaternion desired = Quaternion.LookRotation(direction, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, desired, 3.2f * deltaTime);
            }
            else
            {
                walkSpeed = 0f;
            }

            // Keep cows inside the pen regardless of steering.
            Vector3 position = transform.position;
            position.x = Mathf.Clamp(position.x, wanderBounds.min.x, wanderBounds.max.x);
            position.z = Mathf.Clamp(position.z, wanderBounds.min.z, wanderBounds.max.z);
            position.y = wanderBounds.center.y - wanderBounds.extents.y * 0.5f;
            transform.position = position;
        }

        void Animate(float deltaTime)
        {
            if (model == null) return;
            bool moving = walkSpeed > 0.05f;
            bobPhase += deltaTime * (moving ? 5.5f : 1.4f);

            float bob = moving ? Mathf.Abs(Mathf.Sin(bobPhase)) * 0.045f : Mathf.Sin(bobPhase) * 0.012f;
            model.localPosition = new Vector3(0f, bob, 0f);

            float swing = moving ? Mathf.Sin(bobPhase) * 22f : 0f;
            SetLeg(legFL, swing);
            SetLeg(legBR, swing);
            SetLeg(legFR, -swing);
            SetLeg(legBL, -swing);

            if (head != null)
                head.localRotation = Quaternion.Euler(Mathf.Sin(bobPhase * 0.4f) * 3f, Mathf.Sin(bobPhase * 0.22f) * 5f, 0f);
            if (tail != null)
                tail.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(bobPhase * 0.7f) * 12f);
        }

        static void SetLeg(Transform leg, float angle)
        {
            if (leg != null) leg.localRotation = Quaternion.Euler(angle, 0f, 0f);
        }

        void BuildModel(bool brown)
        {
            string bodyMaterial = brown ? "CowBrown" : "CowCream";
            string spotMaterial = brown ? "CowCream" : "CowBlack";
            var gm = system.GameManager;

            model = new GameObject("Model").transform;
            model.SetParent(transform, false);

            Part(PrimitiveType.Sphere, "Body", model, new Vector3(0f, 1.02f, 0f),
                new Vector3(1.45f, 0.98f, 1.95f), bodyMaterial, gm);
            Part(PrimitiveType.Sphere, "Chest", model, new Vector3(0f, 1.06f, 0.8f),
                new Vector3(1.2f, 0.88f, 0.9f), bodyMaterial, gm);

            for (int i = 0; i < 4; i++)
            {
                var spot = Part(PrimitiveType.Sphere, "Spot", model,
                    new Vector3(i % 2 == 0 ? -0.52f : 0.52f, 1.2f, -0.55f + (i / 2) * 1.05f),
                    new Vector3(0.62f, 0.48f, 0.62f), spotMaterial, gm);
                spot.transform.localScale = new Vector3(0.55f, 0.42f, 0.7f);
            }

            head = new GameObject("Head").transform;
            head.SetParent(model, false);
            head.localPosition = new Vector3(0f, 1.24f, 1.02f);

            Part(PrimitiveType.Sphere, "HeadSphere", head, Vector3.zero,
                new Vector3(0.78f, 0.72f, 0.88f), bodyMaterial, gm);
            Part(PrimitiveType.Cube, "Snout", head, new Vector3(0f, -0.16f, 0.42f),
                new Vector3(0.44f, 0.28f, 0.28f), "CowPink", gm);
            Part(PrimitiveType.Sphere, "NoseL", head, new Vector3(-0.1f, -0.13f, 0.57f),
                new Vector3(0.06f, 0.06f, 0.04f), "CowBlack", gm);
            Part(PrimitiveType.Sphere, "NoseR", head, new Vector3(0.1f, -0.13f, 0.57f),
                new Vector3(0.06f, 0.06f, 0.04f), "CowBlack", gm);
            Part(PrimitiveType.Sphere, "EyeL", head, new Vector3(-0.23f, 0.1f, 0.4f),
                new Vector3(0.09f, 0.1f, 0.07f), "CowBlack", gm);
            Part(PrimitiveType.Sphere, "EyeR", head, new Vector3(0.23f, 0.1f, 0.4f),
                new Vector3(0.09f, 0.1f, 0.07f), "CowBlack", gm);

            var earL = Part(PrimitiveType.Sphere, "EarL", head, new Vector3(-0.44f, 0.16f, 0f),
                new Vector3(0.24f, 0.14f, 0.34f), bodyMaterial, gm);
            earL.transform.localRotation = Quaternion.Euler(0f, 0f, 18f);
            var earR = Part(PrimitiveType.Sphere, "EarR", head, new Vector3(0.44f, 0.16f, 0f),
                new Vector3(0.24f, 0.14f, 0.34f), bodyMaterial, gm);
            earR.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);

            var hornL = Part(PrimitiveType.Cylinder, "HornL", head, new Vector3(-0.24f, 0.38f, -0.05f),
                new Vector3(0.06f, 0.14f, 0.06f), "Hat", gm);
            hornL.transform.localRotation = Quaternion.Euler(0f, 0f, -22f);
            var hornR = Part(PrimitiveType.Cylinder, "HornR", head, new Vector3(0.24f, 0.38f, -0.05f),
                new Vector3(0.06f, 0.14f, 0.06f), "Hat", gm);
            hornR.transform.localRotation = Quaternion.Euler(0f, 0f, 22f);

            Part(PrimitiveType.Sphere, "Tuft", head, new Vector3(0f, 0.4f, 0f),
                new Vector3(0.22f, 0.14f, 0.22f), spotMaterial, gm);

            legFL = MakeLeg("LegFL", new Vector3(-0.48f, 0.78f, 0.66f), bodyMaterial, gm);
            legFR = MakeLeg("LegFR", new Vector3(0.48f, 0.78f, 0.66f), bodyMaterial, gm);
            legBL = MakeLeg("LegBL", new Vector3(-0.48f, 0.78f, -0.66f), bodyMaterial, gm);
            legBR = MakeLeg("LegBR", new Vector3(0.48f, 0.78f, -0.66f), bodyMaterial, gm);

            Part(PrimitiveType.Sphere, "Udder", model, new Vector3(0f, 0.58f, -0.28f),
                new Vector3(0.42f, 0.34f, 0.48f), "CowPink", gm);
            for (int i = 0; i < 3; i++)
            {
                Part(PrimitiveType.Cylinder, "Teat", model,
                    new Vector3((i - 1) * 0.12f, 0.4f, -0.28f),
                    new Vector3(0.045f, 0.1f, 0.045f), "CowPink", gm);
            }

            tail = new GameObject("Tail").transform;
            tail.SetParent(model, false);
            tail.localPosition = new Vector3(0f, 1.2f, -0.92f);
            var tailStem = Part(PrimitiveType.Cylinder, "TailStem", tail, new Vector3(0f, -0.32f, 0f),
                new Vector3(0.055f, 0.32f, 0.055f), bodyMaterial, gm);
            tailStem.transform.localRotation = Quaternion.identity;
            Part(PrimitiveType.Sphere, "TailTuft", tail, new Vector3(0f, -0.66f, 0f),
                new Vector3(0.14f, 0.2f, 0.14f), spotMaterial, gm);
        }

        Transform MakeLeg(string name, Vector3 hip, string material, GameManager gm)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(model, false);
            pivot.localPosition = hip;
            Part(PrimitiveType.Cylinder, "Leg", pivot, new Vector3(0f, -0.3f, 0f),
                new Vector3(0.16f, 0.3f, 0.16f), material, gm);
            Part(PrimitiveType.Cube, "Hoof", pivot, new Vector3(0f, -0.62f, 0.03f),
                new Vector3(0.2f, 0.12f, 0.26f), "CowBlack", gm);
            return pivot;
        }

        void BuildStatusBar()
        {
            var gm = system.GameManager;
            barRoot = new GameObject("Status").transform;
            barRoot.SetParent(transform, false);
            barRoot.localPosition = new Vector3(0f, 2.3f, 0f);

            var background = GameObject.CreatePrimitive(PrimitiveType.Cube);
            background.name = "HungerBG";
            background.transform.SetParent(barRoot, false);
            background.transform.localScale = new Vector3(1.12f, 0.14f, 0.045f);
            RemoveCollider(background);
            gm.Materials.Apply(background, "RoofDark", false, false);

            var fill = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fill.name = "HungerFill";
            fill.transform.SetParent(barRoot, false);
            fill.transform.localScale = new Vector3(1f, 0.09f, 0.055f);
            fill.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            RemoveCollider(fill);
            gm.Materials.Apply(fill, "Tint", false, false);
            barFill = fill.transform;
            barFillRenderer = fill.GetComponent<Renderer>();

            milkIcon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            milkIcon.name = "MilkReady";
            milkIcon.transform.SetParent(barRoot, false);
            milkIcon.transform.localPosition = new Vector3(0f, 0.32f, 0f);
            milkIcon.transform.localScale = new Vector3(0.13f, 0.17f, 0.13f);
            RemoveCollider(milkIcon);
            gm.Materials.Apply(milkIcon, "Milk", false, false);
            milkIcon.SetActive(false);
        }

        void UpdateStatusBar()
        {
            if (barFill == null) return;

            float hunger01 = Mathf.Clamp01(Hunger / 100f);
            barFill.localScale = new Vector3(Mathf.Max(0.001f, hunger01), 0.09f, 0.055f);
            barFill.localPosition = new Vector3(-(1f - hunger01) * 0.5f, 0f, -0.01f);

            Color color = hunger01 > 0.6f ? new Color(0.55f, 0.85f, 0.48f)
                : hunger01 > 0.3f ? new Color(0.96f, 0.78f, 0.30f)
                : new Color(0.95f, 0.42f, 0.36f);
            if (barFillRenderer != null) MaterialLibrary.Tint(barFillRenderer.gameObject, color);

            if (milkIcon != null) milkIcon.SetActive(MilkReady);

            var camera = system.GameManager.MainCamera;
            if (camera != null)
                barRoot.rotation = Quaternion.LookRotation(barRoot.position - camera.transform.position, Vector3.up);
        }

        public void ApplySave(float hunger, float milkTimer, bool milkReady)
        {
            Hunger = Mathf.Clamp(hunger, 0f, 100f);
            MilkReady = milkReady;
            MilkTimer = Mathf.Max(0f, milkTimer);
            Producing = milkReady || MilkTimer > 0.01f;
            if (milkReady) MilkTimer = 0f;
            autoCollecting = false;

            if (barFill != null) UpdateStatusBar();
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
    }
}
