using System;
using UnityEngine;

namespace SkyHopper
{
    /// <summary>
    /// Tiny procedural sound bank. The Quaternius pack ships no audio, so the
    /// pickup / jump / death cues are synthesized at runtime (no extra assets).
    /// </summary>
    public static class Sfx
    {
        const int SampleRate = 44100;

        static AudioClip coinClip;
        static AudioClip jumpClip;
        static AudioClip deathClip;

        public static AudioClip CoinClip => coinClip != null
            ? coinClip
            : coinClip = Make("Coin", 0.28f, t =>
            {
                float env = Mathf.Exp(-t * 12f);
                float f = 1250f + 900f * t;
                return (Mathf.Sin(2f * Mathf.PI * f * t) * 0.55f +
                        Mathf.Sin(2f * Mathf.PI * f * 1.5f * t) * 0.2f) * env;
            });

        public static AudioClip JumpClip => jumpClip != null
            ? jumpClip
            : jumpClip = Make("Jump", 0.16f, t =>
            {
                float env = Mathf.Exp(-t * 24f);
                float f = 300f + 500f * (t / 0.16f);
                return Mathf.Sin(2f * Mathf.PI * f * t) * 0.5f * env;
            });

        public static AudioClip DeathClip => deathClip != null
            ? deathClip
            : deathClip = Make("Death", 0.85f, t =>
            {
                float env = Mathf.Exp(-t * 3.2f);
                float f = 420f * Mathf.Exp(-t * 2.2f) + 55f;
                return (Mathf.Sin(2f * Mathf.PI * f * t) * 0.5f +
                        Mathf.Sin(2f * Mathf.PI * f * 0.5f * t) * 0.25f) * env;
            });

        public static void Play(AudioClip clip, Vector3 position, float volume = 0.6f)
        {
            if (clip == null) return;

            GameObject go = new GameObject("Sfx OneShot");
            go.transform.position = position;
            AudioSource source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = volume;
            source.spatialBlend = 0f;
            source.playOnAwake = false;
            source.Play();
            UnityEngine.Object.Destroy(go, clip.length + 0.15f);
        }

        static AudioClip Make(string name, float duration, Func<float, float> sample)
        {
            int count = Mathf.Max(1, Mathf.CeilToInt(SampleRate * duration));
            float[] data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / SampleRate;
                float fadeOut = Mathf.Clamp01((duration - t) * 40f);
                data[i] = Mathf.Clamp(sample(t) * fadeOut, -1f, 1f) * 0.8f;
            }

            AudioClip clip = AudioClip.Create(name, count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
