using UnityEngine;

namespace WheatAndCo
{
    /// <summary>A collectible egg in a little straw nest.</summary>
    public class EggPickup : InteractableBase
    {
        AnimalSystem system;
        Transform egg;
        float phase;

        public void Initialize(AnimalSystem animalSystem)
        {
            system = animalSystem;
            phase = Random.Range(0f, 6f);
            BuildModel();
        }

        public override float HighlightRadius => 0.55f;
        protected override float RingY => 0.04f;
        public override string Prompt => "Collect egg (+1 Egg)";

        public override void Interact(PlayerController player)
        {
            system.CollectEgg(this);
        }

        void Update()
        {
            if (egg == null) return;
            float bob = Mathf.Sin(Time.time * 2.4f + phase) * 0.035f;
            egg.localPosition = new Vector3(0f, 0.2f + bob, 0f);
            egg.localRotation = Quaternion.Euler(0f, Time.time * 55f, 0f);
        }

        void BuildModel()
        {
            var gm = system.GameManager;

            var nest = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            nest.name = "Nest";
            nest.transform.SetParent(transform, false);
            nest.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            nest.transform.localScale = new Vector3(0.5f, 0.035f, 0.5f);
            RemoveCollider(nest);
            gm.Materials.Apply(nest, "Hay", false, true);

            egg = new GameObject("Egg").transform;
            egg.SetParent(transform, false);
            egg.localPosition = new Vector3(0f, 0.2f, 0f);

            var shell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shell.name = "Shell";
            shell.transform.SetParent(egg, false);
            shell.transform.localScale = new Vector3(0.22f, 0.3f, 0.22f);
            RemoveCollider(shell);
            gm.Materials.Apply(shell, "Egg", false, true);

            var sparkle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sparkle.name = "Sparkle";
            sparkle.transform.SetParent(egg, false);
            sparkle.transform.localPosition = new Vector3(0.06f, 0.1f, 0.08f);
            sparkle.transform.localScale = new Vector3(0.04f, 0.04f, 0.04f);
            RemoveCollider(sparkle);
            gm.Materials.Apply(sparkle, "HighlightRing", false, false);
        }

        static void RemoveCollider(GameObject go)
        {
            var collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
        }
    }
}
