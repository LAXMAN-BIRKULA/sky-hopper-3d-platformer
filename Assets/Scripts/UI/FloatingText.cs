using UnityEngine;

namespace WheatAndCo
{
    /// <summary>
    /// Small billboarded 3D label that rises and fades. Used for "+2 Wheat"
    /// feedback directly above crops and animals.
    /// </summary>
    public class FloatingText : MonoBehaviour
    {
        TextMesh mesh;
        float age;
        float lifetime = 1.35f;
        Vector3 start;
        Color color;

        public static void Spawn(string text, Vector3 position, Color color, float size = 0.09f)
        {
            var go = new GameObject("FloatingText");
            go.transform.position = position;
            var floating = go.AddComponent<FloatingText>();
            floating.Build(text, color, size);
        }

        void Build(string text, Color tint, float size)
        {
            color = tint;
            start = transform.position;

            mesh = gameObject.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.font = UITheme.Font;
            mesh.fontSize = 72;
            mesh.characterSize = size;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = tint;

            var renderer = GetComponent<MeshRenderer>();
            renderer.sharedMaterial = UITheme.Font.material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingOrder = 120;
        }

        void LateUpdate()
        {
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / lifetime);
            transform.position = start + Vector3.up * (0.55f + t * 0.65f);

            var camera = Camera.main;
            if (camera != null)
                transform.rotation = Quaternion.LookRotation(transform.position - camera.transform.position, Vector3.up);

            var tint = color;
            tint.a = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1f, t));
            if (mesh != null) mesh.color = tint;

            if (age >= lifetime) Destroy(gameObject);
        }
    }
}
