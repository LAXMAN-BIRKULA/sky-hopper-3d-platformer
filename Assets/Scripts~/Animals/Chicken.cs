using UnityEngine;

namespace WheatAndCo
{
    /// <summary>
    /// A coop chicken. Feed corn to start the egg timer; she then wanders off
    /// and leaves a collectible egg somewhere in the coop.
    /// </summary>
    public class Chicken : InteractableBase
    {
        public string ChickenName { get; private set; } = "Chicken";
        public bool HasCorn { get; private set; }
        public float EggTimer { get; private set; }

        AnimalSystem system;
        AnimalData data;
        Bounds wanderBounds;

        Transform model;
        Transform head;
        Transform wingL, wingR;
        Transform legL, legR;

        float wanderTimer;
        float walkSpeed;
        float bobPhase;
        Vector3 target;

        bool HasCornItem => system.GameManager.Inventory.Count(ItemKind.Corn) > 0;

        public void Initialize(AnimalSystem animalSystem, string chickenName, Vector3 position, bool brown, Bounds bounds)
        {
            system = animalSystem;
            data = system.GameManager.Database.Chicken;
            ChickenName = chickenName;
            wanderBounds = bounds;
            transform.position = position;

            BuildModel(brown);
            PickWanderTarget();

            HasCorn = false;
            EggTimer = 0f;
        }

        public override float HighlightRadius => 0.7f;

        public override string Prompt
        {
            get
            {
                if (HasCorn) return ChickenName + " is nesting - egg in " + Mathf.CeilToInt(EggTimer) + "s";
                if (HasCornItem) return "Feed " + ChickenName + " corn - lays an egg";
                return ChickenName + " needs corn";
            }
        }

        public override void Interact(PlayerController player)
        {
            if (HasCorn)
            {
                Pet();
                return;
            }

            if (!HasCornItem)
            {
                system.GameManager.UI.Toast("Chickens love corn. Grow some on the corn field (level 2).", UITheme.Muted, 3.5f);
                system.GameManager.Audio.Play(Sfx.Error, 0.5f);
                return;
            }

            var gm = system.GameManager;
            gm.Inventory.TryRemove(ItemKind.Corn, 1);
            HasCorn = true;
            EggTimer = data.eggSeconds;
            gm.Progression.AddXp(4, "Feed");
            gm.Audio.Play(Sfx.Feed);
            gm.Audio.Play(Sfx.Cluck, 0.7f);
            gm.UI.Toast(ChickenName + " will lay an egg in " + Mathf.CeilToInt(data.eggSeconds) + "s.", UITheme.Good, 3f);
            FloatingText.Spawn("Cluck!", transform.position + Vector3.up * 1.1f, UITheme.Accent, 0.07f);
            gm.UI.RefreshAll();
        }

        void Pet()
        {
            var gm = system.GameManager;
            gm.Audio.Play(Sfx.Cluck, 0.6f);
            FloatingText.Spawn("Cluck!", transform.position + Vector3.up * 1.1f, UITheme.Text, 0.06f);
            gm.UI.Toast(ChickenName + " is busy laying an egg.", UITheme.Muted, 2f);
        }

        public void Tick(float deltaTime)
        {
            if (HasCorn)
            {
                EggTimer -= deltaTime;
                if (EggTimer <= 0f)
                {
                    EggTimer = 0f;
                    HasCorn = false;
                    system.SpawnEgg(transform.position);
                    FloatingText.Spawn("Egg!", transform.position + Vector3.up * 1.1f, UITheme.Accent, 0.07f);
                }
            }

            Wander(deltaTime);
            Animate(deltaTime);
        }

        void PickWanderTarget()
        {
            target = new Vector3(
                Random.Range(wanderBounds.min.x, wanderBounds.max.x),
                transform.position.y,
                Random.Range(wanderBounds.min.z, wanderBounds.max.z));
            wanderTimer = Random.Range(1.5f, 4f);
            walkSpeed = Random.value < 0.2f ? 0f : Random.Range(0.7f, 1.5f);
        }

        void Wander(float deltaTime)
        {
            wanderTimer -= deltaTime;
            if (wanderTimer <= 0f) PickWanderTarget();

            Vector3 toTarget = target - transform.position;
            toTarget.y = 0f;
            if (walkSpeed > 0.05f && toTarget.sqrMagnitude > 0.09f)
            {
                Vector3 direction = toTarget.normalized;
                transform.position += direction * walkSpeed * deltaTime;
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(direction, Vector3.up), 5f * deltaTime);
            }
            else
            {
                walkSpeed = 0f;
            }

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
            bobPhase += deltaTime * (moving ? 13f : 4f);

            float bob = Mathf.Abs(Mathf.Sin(bobPhase)) * 0.045f;
            model.localPosition = new Vector3(0f, bob, 0f);

            float swing = moving ? Mathf.Sin(bobPhase) * 30f : 0f;
            if (legL != null) legL.localRotation = Quaternion.Euler(swing, 0f, 0f);
            if (legR != null) legR.localRotation = Quaternion.Euler(-swing, 0f, 0f);

            float flap = moving ? Mathf.Sin(bobPhase * 2f) * 14f : Mathf.Sin(bobPhase * 0.7f) * 4f;
            if (wingL != null) wingL.localRotation = Quaternion.Euler(0f, 0f, -18f - flap);
            if (wingR != null) wingR.localRotation = Quaternion.Euler(0f, 0f, 18f + flap);
            if (head != null) head.localRotation = Quaternion.Euler(Mathf.Sin(bobPhase * 0.8f) * 5f, 0f, 0f);
        }

        void BuildModel(bool brown)
        {
            string bodyMaterial = brown ? "ChickenBrown" : "ChickenWhite";
            var gm = system.GameManager;

            model = new GameObject("Model").transform;
            model.SetParent(transform, false);

            Part(PrimitiveType.Sphere, "Body", model, new Vector3(0f, 0.42f, 0f),
                new Vector3(0.62f, 0.56f, 0.7f), bodyMaterial, gm);

            head = new GameObject("Head").transform;
            head.SetParent(model, false);
            head.localPosition = new Vector3(0f, 0.72f, 0.28f);

            Part(PrimitiveType.Sphere, "HeadSphere", head, Vector3.zero,
                new Vector3(0.38f, 0.36f, 0.38f), bodyMaterial, gm);
            var beak = Part(PrimitiveType.Cube, "Beak", head, new Vector3(0f, -0.02f, 0.2f),
                new Vector3(0.1f, 0.09f, 0.18f), "Beak", gm);
            beak.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
            Part(PrimitiveType.Sphere, "Comb", head, new Vector3(0f, 0.2f, 0.02f),
                new Vector3(0.12f, 0.12f, 0.22f), "Comb", gm);
            Part(PrimitiveType.Sphere, "Wattle", head, new Vector3(0f, -0.14f, 0.18f),
                new Vector3(0.08f, 0.12f, 0.07f), "Comb", gm);
            Part(PrimitiveType.Sphere, "EyeL", head, new Vector3(-0.13f, 0.05f, 0.14f),
                new Vector3(0.05f, 0.05f, 0.04f), "CowBlack", gm);
            Part(PrimitiveType.Sphere, "EyeR", head, new Vector3(0.13f, 0.05f, 0.14f),
                new Vector3(0.05f, 0.05f, 0.04f), "CowBlack", gm);

            wingL = new GameObject("WingL").transform;
            wingL.SetParent(model, false);
            wingL.localPosition = new Vector3(-0.3f, 0.5f, 0f);
            Part(PrimitiveType.Sphere, "Wing", wingL, Vector3.zero,
                new Vector3(0.12f, 0.3f, 0.4f), bodyMaterial, gm);
            wingR = new GameObject("WingR").transform;
            wingR.SetParent(model, false);
            wingR.localPosition = new Vector3(0.3f, 0.5f, 0f);
            Part(PrimitiveType.Sphere, "Wing", wingR, Vector3.zero,
                new Vector3(0.12f, 0.3f, 0.4f), bodyMaterial, gm);

            var tail = Part(PrimitiveType.Cube, "Tail", model, new Vector3(0f, 0.62f, -0.4f),
                new Vector3(0.28f, 0.3f, 0.18f), bodyMaterial, gm);
            tail.transform.localRotation = Quaternion.Euler(32f, 0f, 0f);

            legL = MakeLeg("LegL", new Vector3(-0.12f, 0.22f, 0f), "Beak", gm);
            legR = MakeLeg("LegR", new Vector3(0.12f, 0.22f, 0f), "Beak", gm);
        }

        Transform MakeLeg(string name, Vector3 hip, string material, GameManager gm)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(model, false);
            pivot.localPosition = hip;
            Part(PrimitiveType.Cylinder, "Leg", pivot, new Vector3(0f, -0.13f, 0f),
                new Vector3(0.035f, 0.13f, 0.035f), material, gm);
            Part(PrimitiveType.Cube, "Foot", pivot, new Vector3(0f, -0.27f, 0.05f),
                new Vector3(0.13f, 0.035f, 0.18f), material, gm);
            return pivot;
        }

        public void ApplySave(bool hasCorn, float eggTimer)
        {
            HasCorn = hasCorn;
            EggTimer = Mathf.Max(0f, eggTimer);
        }

        static GameObject Part(PrimitiveType type, string name, Transform parent, Vector3 localPosition,
            Vector3 localScale, string material, GameManager gm)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            var collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            gm.Materials.Apply(go, material, false, true);
            return go;
        }
    }
}
