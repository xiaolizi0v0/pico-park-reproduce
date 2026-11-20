using UnityEngine;

namespace PicoPark
{
    /// <summary>Every sound in the game is synthesised at boot - no audio assets needed.</summary>
    public static class PicoAudio
    {
        private static AudioSource src;
        private static AudioClip jump, land, pickup, doorOpen, death, click, gate, clear, fail, tick;

        public static void Init()
        {
            if (src != null) return;
            GameObject go = new GameObject("PicoAudio");
            Object.DontDestroyOnLoad(go);
            src = go.AddComponent<AudioSource>();
            src.spatialBlend = 0f;
            src.volume = 0.55f;

            jump = Sweep(440f, 880f, 0.12f, Wave.Square, 0.35f);
            land = Sweep(220f, 90f, 0.10f, Wave.Sine, 0.45f);
            pickup = Arp(new float[] { 660f, 990f }, 0.07f, Wave.Square, 0.4f);
            doorOpen = Arp(new float[] { 523f, 659f, 784f, 1046f }, 0.09f, Wave.Tri, 0.4f);
            death = Sweep(400f, 70f, 0.35f, Wave.Saw, 0.4f);
            click = Sweep(900f, 500f, 0.05f, Wave.Square, 0.3f);
            gate = Noise(0.3f, 0.18f);
            clear = Arp(new float[] { 523f, 659f, 784f, 1046f, 1318f }, 0.11f, Wave.Tri, 0.45f);
            fail = Arp(new float[] { 392f, 330f, 262f }, 0.16f, Wave.Saw, 0.35f);
            tick = Sweep(1200f, 1200f, 0.04f, Wave.Square, 0.25f);
        }

        /// <summary>全局音量（0-1），设置界面可调。</summary>
        public static float Volume
        {
            get { return src != null ? src.volume : 0.55f; }
            set { if (src != null) src.volume = Mathf.Clamp01(value); }
        }

        public static void Play(string id, float pitchJitter)
        {
            if (src == null) return;
            AudioClip c = null;
            switch (id)
            {
                case "jump": c = jump; break;
                case "land": c = land; break;
                case "pickup": c = pickup; break;
                case "door": c = doorOpen; break;
                case "death": c = death; break;
                case "click": c = click; break;
                case "gate": c = gate; break;
                case "clear": c = clear; break;
                case "fail": c = fail; break;
                case "tick": c = tick; break;
            }
            if (c == null) return;
            src.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            src.PlayOneShot(c, 1f);
        }

        private enum Wave { Sine, Square, Saw, Tri }

        private static float Sample(Wave w, float phase)
        {
            float x = phase % 1f;
            switch (w)
            {
                case Wave.Sine: return Mathf.Sin(x * Mathf.PI * 2f);
                case Wave.Square: return x < 0.5f ? 1f : -1f;
                case Wave.Saw: return x * 2f - 1f;
                default: return x < 0.5f ? (x * 4f - 1f) : (3f - x * 4f);
            }
        }

        private static AudioClip Sweep(float f0, float f1, float dur, Wave w, float amp)
        {
            int rate = 44100;
            int n = Mathf.CeilToInt(dur * rate);
            AudioClip clip = AudioClip.Create("snd", n, 1, rate, false);
            float[] data = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float f = Mathf.Lerp(f0, f1, t);
                phase += f / rate;
                float env = Mathf.Clamp01(1f - t) * Mathf.Clamp01(t * 12f);
                data[i] = Sample(w, phase) * amp * env;
            }
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip Arp(float[] notes, float noteDur, Wave w, float amp)
        {
            int rate = 44100;
            int n = Mathf.CeilToInt(noteDur * notes.Length * rate);
            AudioClip clip = AudioClip.Create("snd", n, 1, rate, false);
            float[] data = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                int idx = Mathf.Min(notes.Length - 1, Mathf.FloorToInt(t / noteDur));
                float f = notes[idx];
                phase += f / rate;
                float lt = (t - idx * noteDur) / noteDur;
                float env = Mathf.Clamp01(1f - lt) * Mathf.Clamp01(lt * 20f);
                data[i] = Sample(w, phase) * amp * env;
            }
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip Noise(float dur, float amp)
        {
            int rate = 44100;
            int n = Mathf.CeilToInt(dur * rate);
            AudioClip clip = AudioClip.Create("snd", n, 1, rate, false);
            float[] data = new float[n];
            float last = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float white = Random.Range(-1f, 1f);
                last = last * 0.82f + white * 0.18f;
                data[i] = last * amp * Mathf.Clamp01(1f - t);
            }
            clip.SetData(data, 0);
            return clip;
        }
    }
}
