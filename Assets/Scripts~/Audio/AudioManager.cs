using System;
using System.Collections.Generic;
using UnityEngine;

namespace WheatAndCo
{
    public enum Sfx
    {
        Step,
        Harvest,
        Plant,
        Coin,
        Error,
        Level,
        Objective,
        Milk,
        Egg,
        Click,
        Feed,
        Buy,
        Moo,
        Cluck,
        Chirp
    }

    /// <summary>
    /// Generates every sound procedurally so the project needs no audio assets.
    /// A small pool of sources plays overlapping one-shots, plus a soft wind loop.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();
        readonly List<AudioSource> sources = new List<AudioSource>();
        AudioSource ambient;
        AudioSource birdSource;
        int nextSource;
        float birdTimer = 9f;
        System.Random random = new System.Random(7);

        const int SampleRate = 44100;

        public void Initialize()
        {
            for (int i = 0; i < 8; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.volume = 1f;
                sources.Add(source);
            }

            clips[Sfx.Step] = Make("Step", 0.09f, t => Noise(t) * Envelope(t, 0.09f, 0.003f, 0.05f), 0.28f);
            clips[Sfx.Harvest] = Make("Harvest", 0.28f, t =>
            {
                float sweep = Mathf.Sin(2f * Mathf.PI * (520f * t + 420f * t * t));
                return sweep * Envelope(t, 0.28f, 0.01f, 0.2f);
            }, 0.45f);
            clips[Sfx.Plant] = Make("Plant", 0.24f, t =>
            {
                float body = Mathf.Sin(2f * Mathf.PI * 150f * t) * Envelope(t, 0.24f, 0.005f, 0.2f);
                return body + Noise(t) * 0.25f * Envelope(t, 0.12f, 0.002f, 0.09f);
            }, 0.45f);
            clips[Sfx.Coin] = Make("Coin", 0.25f, t =>
            {
                float a = Mathf.Sin(2f * Mathf.PI * 1046f * t) * Envelope(t, 0.25f, 0.005f, 0.22f);
                float b = Mathf.Sin(2f * Mathf.PI * 1568f * t) * Envelope(t, 0.18f, 0.01f, 0.15f) * 0.5f;
                return a + b;
            }, 0.4f);
            clips[Sfx.Error] = Make("Error", 0.28f, t =>
            {
                float freq = t < 0.14f ? 235f : 185f;
                return Mathf.Sin(2f * Mathf.PI * freq * t) * Envelope(t, 0.28f, 0.01f, 0.24f) * 0.7f;
            }, 0.4f);
            clips[Sfx.Level] = Make("Level", 0.85f, t => Arpeggio(t, 0.2f, 523f, 659f, 784f, 1046f), 0.5f);
            clips[Sfx.Objective] = Make("Objective", 0.5f, t => Arpeggio(t, 0.2f, 659f, 880f, 1046f), 0.45f);
            clips[Sfx.Milk] = Make("Milk", 0.35f, t =>
            {
                float wobble = 1f + Mathf.Sin(2f * Mathf.PI * 12f * t) * 0.08f;
                return Mathf.Sin(2f * Mathf.PI * 330f * t * wobble) * Envelope(t, 0.35f, 0.02f, 0.3f) * 0.6f;
            }, 0.4f);
            clips[Sfx.Egg] = Make("Egg", 0.22f, t =>
            {
                float sweep = Mathf.Sin(2f * Mathf.PI * (1200f * t + 900f * t * t));
                return sweep * Envelope(t, 0.22f, 0.005f, 0.18f) * 0.5f;
            }, 0.35f);
            clips[Sfx.Click] = Make("Click", 0.06f, t =>
                Mathf.Sin(2f * Mathf.PI * 1400f * t) * Envelope(t, 0.06f, 0.002f, 0.05f), 0.28f);
            clips[Sfx.Feed] = Make("Feed", 0.22f, t =>
                Noise(t) * Envelope(t, 0.22f, 0.01f, 0.18f) * 0.6f, 0.35f);
            clips[Sfx.Buy] = Make("Buy", 0.32f, t =>
            {
                float a = Mathf.Sin(2f * Mathf.PI * 784f * t) * Envelope(t, 0.32f, 0.005f, 0.28f);
                float b = Mathf.Sin(2f * Mathf.PI * 1176f * t) * Envelope(t, 0.22f, 0.01f, 0.2f) * 0.4f;
                return a + b;
            }, 0.4f);
            clips[Sfx.Moo] = Make("Moo", 0.75f, t =>
            {
                float vibrato = 1f + Mathf.Sin(2f * Mathf.PI * 5f * t) * 0.03f;
                float baseTone = Mathf.Sin(2f * Mathf.PI * 155f * t * vibrato);
                float harmonic = Mathf.Sin(2f * Mathf.PI * 310f * t * vibrato) * 0.3f;
                return (baseTone + harmonic) * Envelope(t, 0.75f, 0.08f, 0.6f) * 0.55f;
            }, 0.35f);
            clips[Sfx.Cluck] = Make("Cluck", 0.3f, t =>
            {
                float gate = Mathf.Sin(2f * Mathf.PI * 9f * t) > 0f ? 1f : 0.15f;
                return Mathf.Sin(2f * Mathf.PI * 900f * t) * gate * Envelope(t, 0.3f, 0.01f, 0.26f) * 0.4f;
            }, 0.3f);
            clips[Sfx.Chirp] = Make("Chirp", 0.4f, t =>
            {
                float sweep = Mathf.Sin(2f * Mathf.PI * (2100f * t - 900f * t * t));
                return sweep * Envelope(t, 0.4f, 0.01f, 0.34f) * 0.25f;
            }, 0.25f);

            ambient = gameObject.AddComponent<AudioSource>();
            ambient.clip = MakeAmbient();
            ambient.loop = true;
            ambient.volume = 0.09f;
            ambient.playOnAwake = false;
            ambient.Play();

            birdSource = gameObject.AddComponent<AudioSource>();
            birdSource.playOnAwake = false;
            birdSource.spatialBlend = 0f;
            birdSource.volume = 0.5f;
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.Initialized || gm.IsPaused) return;

            birdTimer -= Time.unscaledDeltaTime;
            if (birdTimer <= 0f)
            {
                birdTimer = 9f + (float)random.NextDouble() * 14f;
                birdSource.panStereo = Mathf.Lerp(-0.7f, 0.7f, (float)random.NextDouble());
                birdSource.pitch = Mathf.Lerp(0.9f, 1.15f, (float)random.NextDouble());
                if (clips.TryGetValue(Sfx.Chirp, out var chirp)) birdSource.PlayOneShot(chirp, 0.22f);
            }
        }

        public void Play(Sfx sfx, float volume = 1f)
        {
            if (sources.Count == 0 || !clips.TryGetValue(sfx, out var clip) || clip == null) return;
            var source = sources[nextSource];
            nextSource = (nextSource + 1) % sources.Count;
            source.pitch = sfx == Sfx.Step || sfx == Sfx.Chirp ? UnityEngine.Random.Range(0.94f, 1.06f) : 1f;
            source.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        static AudioClip Make(string name, float seconds, Func<float, float> sample, float volume)
        {
            int count = Mathf.Max(1, Mathf.CeilToInt(seconds * SampleRate));
            var data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                data[i] = Mathf.Clamp(sample(t) * volume, -1f, 1f);
            }
            var clip = AudioClip.Create(name, count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        AudioClip MakeAmbient()
        {
            const float seconds = 7f;
            int count = Mathf.CeilToInt(seconds * SampleRate);
            var data = new float[count];
            float low = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float n = (float)(random.NextDouble() * 2.0 - 1.0);
                low = Mathf.Lerp(low, n, 0.0035f);
                float breeze = Mathf.Sin(2f * Mathf.PI * 0.11f * t) * 0.25f;
                float rustle = low * (0.35f + 0.25f * Mathf.Sin(2f * Mathf.PI * 0.07f * t));
                data[i] = Mathf.Clamp((rustle + breeze * 0.2f) * 0.35f, -1f, 1f);
            }
            // Soften the loop seam.
            int fade = SampleRate / 2;
            for (int i = 0; i < fade; i++)
            {
                float f = i / (float)fade;
                data[i] *= f;
                data[count - 1 - i] *= f;
            }
            var clip = AudioClip.Create("FarmAmbient", count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Envelope(float t, float duration, float attack, float release)
        {
            if (t < attack) return t / Mathf.Max(0.0001f, attack);
            float fromEnd = duration - t;
            if (fromEnd < release) return Mathf.Clamp01(fromEnd / Mathf.Max(0.0001f, release));
            return 1f;
        }

        static float Noise(float t)
        {
            return Mathf.PerlinNoise(t * 900f, 0.37f) * 2f - 1f;
        }

        static float Arpeggio(float t, float noteLength, params float[] notes)
        {
            int index = Mathf.FloorToInt(t / noteLength);
            if (index >= notes.Length) return 0f;
            float local = t - index * noteLength;
            return Mathf.Sin(2f * Mathf.PI * notes[index] * t) * Envelope(local, noteLength, 0.01f, noteLength * 0.6f);
        }
    }
}
