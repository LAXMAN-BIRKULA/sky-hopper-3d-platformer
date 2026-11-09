using UnityEngine;

namespace WheatAndCo
{
    public struct PlayerRig
    {
        public PlayerController player;
        public ThirdPersonCamera camera;
        public Camera unityCamera;
    }

    /// <summary>Builds the cozy low-poly farmer and hooks up the follow camera.</summary>
    public static class PlayerFactory
    {
        public static PlayerRig Create(GameManager gm, Vector3 spawnPosition)
        {
            var root = new GameObject("Player");
            root.transform.position = spawnPosition;

            var controller = root.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.center = new Vector3(0f, 1f, 0f);
            controller.radius = 0.34f;
            controller.slopeLimit = 52f;
            controller.stepOffset = 0.4f;
            controller.skinWidth = 0.03f;

            BuildModel(root.transform, gm.Materials);
            var player = root.AddComponent<PlayerController>();

            Camera unityCamera = Camera.main;
            if (unityCamera == null) unityCamera = Object.FindFirstObjectByType<Camera>();
            if (unityCamera == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                unityCamera = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }

            unityCamera.farClipPlane = 420f;
            unityCamera.nearClipPlane = 0.15f;
            unityCamera.fieldOfView = 60f;

            var rig = unityCamera.GetComponent<ThirdPersonCamera>();
            if (rig == null) rig = unityCamera.gameObject.AddComponent<ThirdPersonCamera>();
            rig.Initialize(root.transform);
            player.SetCamera(unityCamera.transform);

            return new PlayerRig { player = player, camera = rig, unityCamera = unityCamera };
        }

        static void BuildModel(Transform root, MaterialLibrary mats)
        {
            var model = new GameObject("Model").transform;
            model.SetParent(root, false);

            Part("Body", PrimitiveType.Capsule, model, new Vector3(0f, 0.98f, 0f),
                new Vector3(0.56f, 0.52f, 0.56f), "Overalls", mats, Quaternion.identity);
            Part("Shirt", PrimitiveType.Cube, model, new Vector3(0f, 1.22f, 0f),
                new Vector3(0.6f, 0.25f, 0.44f), "Shirt", mats, Quaternion.identity);
            Part("Collar", PrimitiveType.Cube, model, new Vector3(0f, 1.35f, 0f),
                new Vector3(0.42f, 0.06f, 0.36f), "BarnTrim", mats, Quaternion.identity);

            var head = new GameObject("Head").transform;
            head.SetParent(model, false);
            head.localPosition = new Vector3(0f, 1.52f, 0f);

            Part("HeadSphere", PrimitiveType.Sphere, head, new Vector3(0f, 0.14f, 0f),
                new Vector3(0.5f, 0.52f, 0.48f), "Skin", mats, Quaternion.identity);
            Part("EyeL", PrimitiveType.Sphere, head, new Vector3(-0.085f, 0.15f, 0.215f),
                new Vector3(0.065f, 0.075f, 0.05f), "CowBlack", mats, Quaternion.identity, false);
            Part("EyeR", PrimitiveType.Sphere, head, new Vector3(0.085f, 0.15f, 0.215f),
                new Vector3(0.065f, 0.075f, 0.05f), "CowBlack", mats, Quaternion.identity, false);
            Part("Nose", PrimitiveType.Sphere, head, new Vector3(0f, 0.09f, 0.245f),
                new Vector3(0.07f, 0.07f, 0.06f), "CowPink", mats, Quaternion.identity, false);
            Part("HatBrim", PrimitiveType.Cylinder, head, new Vector3(0f, 0.33f, 0f),
                new Vector3(0.76f, 0.03f, 0.76f), "Hat", mats, Quaternion.identity);
            Part("HatCrown", PrimitiveType.Cylinder, head, new Vector3(0f, 0.42f, 0f),
                new Vector3(0.3f, 0.11f, 0.3f), "Hat", mats, Quaternion.identity);
            Part("HatBand", PrimitiveType.Cylinder, head, new Vector3(0f, 0.36f, 0f),
                new Vector3(0.31f, 0.025f, 0.31f), "BarnRed", mats, Quaternion.identity, false);

            var armL = new GameObject("ArmL").transform;
            armL.SetParent(model, false);
            armL.localPosition = new Vector3(-0.34f, 1.25f, 0f);
            Part("SleeveL", PrimitiveType.Capsule, armL, new Vector3(0f, -0.2f, 0f),
                new Vector3(0.13f, 0.22f, 0.13f), "Shirt", mats, Quaternion.Euler(0f, 0f, 8f));
            Part("HandL", PrimitiveType.Sphere, armL, new Vector3(0f, -0.45f, 0f),
                new Vector3(0.14f, 0.14f, 0.14f), "Skin", mats, Quaternion.identity, false);

            var armR = new GameObject("ArmR").transform;
            armR.SetParent(model, false);
            armR.localPosition = new Vector3(0.34f, 1.25f, 0f);
            Part("SleeveR", PrimitiveType.Capsule, armR, new Vector3(0f, -0.2f, 0f),
                new Vector3(0.13f, 0.22f, 0.13f), "Shirt", mats, Quaternion.Euler(0f, 0f, -8f));
            Part("HandR", PrimitiveType.Sphere, armR, new Vector3(0f, -0.45f, 0f),
                new Vector3(0.14f, 0.14f, 0.14f), "Skin", mats, Quaternion.identity, false);

            BuildLeg("LegL", new Vector3(-0.16f, 0.66f, 0f), model, mats);
            BuildLeg("LegR", new Vector3(0.16f, 0.66f, 0f), model, mats);
        }

        static void BuildLeg(string name, Vector3 hip, Transform parent, MaterialLibrary mats)
        {
            var leg = new GameObject(name).transform;
            leg.SetParent(parent, false);
            leg.localPosition = hip;
            Part("Trouser", PrimitiveType.Cylinder, leg, new Vector3(0f, -0.29f, 0f),
                new Vector3(0.15f, 0.29f, 0.15f), "Overalls", mats, Quaternion.identity);
            Part("Boot", PrimitiveType.Cube, leg, new Vector3(0f, -0.56f, 0.05f),
                new Vector3(0.19f, 0.13f, 0.3f), "Boots", mats, Quaternion.identity);
        }

        static GameObject Part(string name, PrimitiveType type, Transform parent, Vector3 localPos,
            Vector3 localScale, string material, MaterialLibrary mats, Quaternion rotation,
            bool castShadows = true)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = rotation;
            go.transform.localScale = localScale;

            var collider = go.GetComponent<Collider>();
            if (collider != null) Object.Destroy(collider);

            mats.Apply(go, material, castShadows, true);
            return go;
        }
    }
}
