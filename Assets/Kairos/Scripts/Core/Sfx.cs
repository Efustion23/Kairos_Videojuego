using System;
using UnityEngine;

namespace Kairos
{
    /// Audio sintetizado: el sonido da información de juego (GDD) y no hay assets aún.
    public static class Sfx
    {
        const int Rate = 44100;
        static System.Random rng = new System.Random(77);
        static float N() => (float)rng.NextDouble() * 2f - 1f;

        static AudioClip Make(string name, float seconds, Func<float, float, float> f, bool seamlessLoop = false)
        {
            int n = (int)(seconds * Rate);
            var d = new float[n];
            for (int i = 0; i < n; i++) { float t = i / (float)Rate; d[i] = Mathf.Clamp(f(t, t / seconds), -1, 1); }
            if (seamlessLoop)
            {
                int fade = Rate / 4;
                for (int i = 0; i < fade; i++) { float a = i / (float)fade; d[i] = d[i] * a + d[n - fade + i] * (1 - a); }
                Array.Resize(ref d, n - fade);
                n -= fade;
            }
            var c = AudioClip.Create(name, n, 1, Rate, false);
            c.SetData(d, 0);
            return c;
        }

        static float Sin(float hz, float t) => Mathf.Sin(2 * Mathf.PI * hz * t);

        static AudioClip click, shutter, step, creak, grapple, plasma, alarm, sealedTone, knock, slip, stun, pingDense, pingHollow, hum, wind, voices, breath, drone, rain, engine, seal;

        public static AudioClip Click => click ??= Make("click", .05f, (t, u) => Sin(1400, t) * (1 - u) * .5f);
        public static AudioClip Shutter => shutter ??= Make("shutter", .14f, (t, u) => N() * Mathf.Exp(-u * 9) * (u < .4f || u > .6f ? 1 : .1f) * .6f);
        public static AudioClip Step => step ??= Make("step", .12f, (t, u) => (Sin(90, t) + N() * .4f) * Mathf.Exp(-u * 8) * .7f);
        public static AudioClip Slip => slip ??= Make("slip", .4f, (t, u) => (Sin(70, t) + N() * .7f) * Mathf.Exp(-u * 5));
        public static AudioClip Creak => creak ??= Make("creak", 1.8f, (t, u) => Mathf.Sin(2 * Mathf.PI * (60 + 30 * Mathf.Sin(t * 5)) * t + 3 * Sin(23, t)) * Mathf.Sin(Mathf.PI * u) * .7f);
        public static AudioClip Grapple => grapple ??= Make("grapple", .5f, (t, u) => (Sin(900 - 700 * u, t) + N() * .3f) * Mathf.Exp(-u * 5) * .7f);
        public static AudioClip Plasma => plasma ??= Make("plasma", .6f, (t, u) => (N() * .6f + Sin(180, t) * .4f) * (.6f + .4f * Sin(47, t)) * .6f);
        public static AudioClip Alarm => alarm ??= Make("alarm", .9f, (t, u) => Sin(u < .5f ? 620 : 470, t) * .5f);
        public static AudioClip Seal => seal ??= Make("seal", .35f, (t, u) => (Sin(880, t) + Sin(1320, t)) * .3f * Mathf.Exp(-u * 4));
        public static AudioClip Knock => knock ??= Make("knock", .45f, (t, u) => (Sin(120, t) * .9f) * (Mathf.Exp(-((u * 3) % 1f) * 9)));
        public static AudioClip Stun => stun ??= Make("stun", .4f, (t, u) => N() * Sin(60, t) * Mathf.Exp(-u * 4));
        /// Tono grave: masa densa. Tono hueco: espacio vacío o habitable.
        public static AudioClip PingDense => pingDense ??= Make("pingDense", 1.1f, (t, u) => Sin(320, t) * Mathf.Exp(-u * 6) * .8f);
        public static AudioClip PingHollow => pingHollow ??= Make("pingHollow", 1.3f, (t, u) => (Sin(980, t) * .7f + Sin(1960, t) * .25f) * Mathf.Exp(-u * 3.5f) * .8f);

        public static AudioClip Hum => hum ??= Make("hum", 4f, (t, u) => (Sin(50, t) * .5f + Sin(100, t) * .3f + Sin(150, t) * .15f + N() * .05f), true);
        public static AudioClip Engine => engine ??= Make("engine", 3f, (t, u) => Sin(62, t) * .4f + Sin(124, t) * .2f + N() * .15f, true);
        public static AudioClip Wind
        {
            get
            {
                if (wind != null) return wind;
                float lp = 0;
                return wind = Make("wind", 7f, (t, u) => { lp += (N() - lp) * (.03f + .02f * Mathf.Sin(t * 1.4f)); return lp * 3f; }, true);
            }
        }
        public static AudioClip Rain
        {
            get
            {
                if (rain != null) return rain;
                float hp = 0;
                return rain = Make("rain", 4f, (t, u) => { float n = N(); float v = n - hp; hp = n * .7f; return v * .25f; }, true);
            }
        }
        /// Voces apagadas tras un mamparo: formantes sin palabras.
        public static AudioClip Voices => voices ??= Make("voices", 6f, (t, u) =>
        {
            float gate = Mathf.Clamp01(Mathf.Sin(t * 2.3f) + Mathf.Sin(t * 3.7f + 1) * .7f);
            float f0 = 130 + 25 * Mathf.Sin(t * 1.9f);
            return (Sin(f0, t) + Sin(f0 * 2, t) * .6f + Sin(f0 * 5, t) * .15f) * gate * .35f;
        }, true);
        public static AudioClip Breath => breath ??= Make("breath", 2.4f, (t, u) => N() * Mathf.Pow(Mathf.Sin(Mathf.PI * ((u * 2) % 1f)), 2) * .25f);
        public static AudioClip Drone => drone ??= Make("drone", 12f, (t, u) =>
            (Sin(55, t) * .4f + Sin(82.4f, t) * .3f * (.5f + .5f * Sin(.1f, t)) + Sin(110.2f, t) * .2f * (.5f + .5f * Sin(.17f, t)) + Sin(164.8f, t) * .08f), true);

        static AudioSource oneShot;
        static AudioSource Src
        {
            get
            {
                if (oneShot == null)
                {
                    var go = new GameObject("Sfx");
                    UnityEngine.Object.DontDestroyOnLoad(go);
                    oneShot = go.AddComponent<AudioSource>();
                    oneShot.playOnAwake = false;
                    oneShot.ignoreListenerPause = true;
                }
                return oneShot;
            }
        }

        public static void Play(AudioClip clip, float volume = 1f) => Src.PlayOneShot(clip, volume);

        public static void PlayAt(AudioClip clip, Vector3 pos, float volume = 1f, float maxDist = 30f)
        {
            var go = new GameObject("sfx3d");
            go.transform.position = pos;
            var a = go.AddComponent<AudioSource>();
            a.clip = clip; a.volume = volume; a.spatialBlend = 1; a.maxDistance = maxDist; a.rolloffMode = AudioRolloffMode.Linear;
            a.Play();
            UnityEngine.Object.Destroy(go, clip.length + .1f);
        }

        public static AudioSource Loop(GameObject host, AudioClip clip, float volume, float spatial = 0, float maxDist = 25f)
        {
            var a = host.AddComponent<AudioSource>();
            a.clip = clip; a.loop = true; a.volume = volume; a.spatialBlend = spatial; a.maxDistance = maxDist;
            a.rolloffMode = AudioRolloffMode.Linear;
            a.Play();
            return a;
        }
    }
}
