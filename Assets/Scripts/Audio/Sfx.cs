using UnityEngine;

namespace SpaceGame
{
    /// <summary>
    /// Every sound in the game, synthesized at runtime — sine stacks, noise
    /// bursts, and sweeps rendered into AudioClips. No audio files, same
    /// philosophy as the code-built ships and UI. Playback goes through
    /// AudioDirector; every call here is safe when audio failed to set up.
    /// </summary>
    public static class Sfx
    {
        const int Rate = 44100;

        public static AudioClip EngineLoop, MiningLoop, DroneLoop;
        static AudioClip _click, _deny, _locked, _blaster, _rail, _chunk, _explosion,
            _shieldHit, _armorHit, _warpEnter, _warpExit, _dock, _jump, _payout, _levelUp;
        static bool _ready;

        // ---------- synthesis ----------

        static AudioClip Make(string name, float dur, System.Func<float, float> f)
        {
            int n = (int)(Rate * dur);
            var data = new float[n];
            for (int i = 0; i < n; i++)
                data[i] = Mathf.Clamp(f(i / (float)Rate), -1f, 1f);
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Sin(float freq, float t) => Mathf.Sin(2f * Mathf.PI * freq * t);
        static float Square(float freq, float t) => Mathf.Sin(2f * Mathf.PI * freq * t) > 0f ? 0.6f : -0.6f;

        public static void Init()
        {
            if (_ready) return;
            _ready = true;
            var rng = new System.Random(1337);
            System.Func<float> noise = () => (float)rng.NextDouble() * 2f - 1f;

            _click = Make("click", 0.06f, t => Sin(1800f, t) * 0.5f * Mathf.Exp(-t * 90f));
            _deny = Make("deny", 0.18f, t => Square(150f, t) * 0.35f * Mathf.Exp(-t * 12f));
            _locked = Make("locked", 0.22f, t => t < 0.1f
                ? Sin(880f, t) * 0.5f * Mathf.Exp(-t * 20f)
                : Sin(1320f, t) * 0.5f * Mathf.Exp(-(t - 0.1f) * 20f));

            _blaster = Make("blaster", 0.16f, t =>
                (noise() * 0.5f + Sin(420f - 1400f * t, t)) * 0.7f * Mathf.Exp(-t * 26f));
            _rail = Make("rail", 0.32f, t =>
                (Sin(110f, t) * 0.9f + noise() * 0.35f * Mathf.Exp(-t * 55f)) * Mathf.Exp(-t * 11f));
            _chunk = Make("chunk", 0.1f, t =>
                (noise() * 0.5f + Sin(320f, t) * 0.6f) * Mathf.Exp(-t * 32f));

            float brown = 0f;
            _explosion = Make("explosion", 0.85f, t =>
            {
                brown += noise() * 0.16f;
                brown *= 0.985f;
                return brown * 3.2f * Mathf.Exp(-t * 4.2f);
            });

            _shieldHit = Make("shieldhit", 0.28f, t =>
                Sin(720f, t) * (1f + 0.3f * Sin(30f, t)) * 0.45f * Mathf.Exp(-t * 17f));
            _armorHit = Make("armorhit", 0.3f, t =>
                (Sin(140f, t) * 0.8f + noise() * 0.2f * Mathf.Exp(-t * 42f)) * Mathf.Exp(-t * 13f));

            _warpEnter = Make("warpenter", 1.3f, t =>
            {
                float p = t / 1.3f;
                float freq = 90f + 620f * p * p;
                return Sin(freq, t) * 0.4f * Mathf.Min(t * 3f, 1f);
            });
            _warpExit = Make("warpexit", 0.5f, t =>
                Sin(500f - 840f * t, t) * 0.45f * Mathf.Exp(-t * 6f));

            _dock = Make("dock", 0.5f, t =>
                Sin(85f, t) * 0.7f * Mathf.Exp(-t * 8f) + Sin(520f, t) * 0.15f * Mathf.Exp(-t * 5f));
            _jump = Make("jump", 0.9f, t =>
            {
                float lp = Sin(240f - 180f * t, t) * 0.4f;
                return (lp + noise() * 0.25f * Mathf.Sin(Mathf.PI * t / 0.9f)) * Mathf.Exp(-t * 2.2f);
            });

            _payout = Make("payout", 0.34f, t =>
            {
                float f = t < 0.11f ? 660f : t < 0.22f ? 880f : 1100f;
                float lt = t % 0.11f;
                return Sin(f, t) * 0.4f * Mathf.Exp(-lt * 22f);
            });
            _levelUp = Make("levelup", 0.42f, t =>
            {
                float f = t < 0.1f ? 523f : t < 0.2f ? 659f : t < 0.3f ? 784f : 1046f;
                float lt = t % 0.1f;
                return Sin(f, t) * 0.4f * Mathf.Exp(-lt * 18f);
            });

            // Loops: frequencies chosen for an integer cycle count -> seamless.
            EngineLoop = Make("engine", 1f, t =>
                (Sin(60f, t) + Sin(120f, t) * 0.5f + Sin(181f, t) * 0.25f) * 0.28f);
            MiningLoop = Make("mining", 1f, t =>
                Sin(220f, t) * (1f + 0.4f * Sin(4f, t)) * 0.3f);
            DroneLoop = Make("drone", 2f, t =>
                (Sin(40f, t) + Sin(55.5f, t) * 0.8f) * 0.4f);
        }

        // ---------- event API (all null-safe) ----------

        static void Play(AudioClip clip, float vol, float pitch = 1f)
        {
            if (AudioDirector.I != null && clip != null)
                AudioDirector.I.PlayOneShot(clip, vol, pitch);
        }

        static float Jit() => 0.94f + Random.value * 0.12f;

        public static void Click() => Play(_click, 0.5f, Jit());
        public static void Deny() => Play(_deny, 0.6f, 1f);
        public static void Locked() => Play(_locked, 0.7f, 1f);
        public static void MinerChunk() => Play(_chunk, 0.5f, Jit());
        public static void Payout() => Play(_payout, 0.8f, 1f);
        public static void LevelUp() => Play(_levelUp, 0.8f, 1f);
        public static void Dock() => Play(_dock, 0.8f, 1f);
        public static void Undock() => Play(_dock, 0.7f, 1.35f);
        public static void JumpGate() => Play(_jump, 0.9f, 1f);
        public static void WarpEnter() => Play(_warpEnter, 0.7f, 1f);
        public static void WarpExit() => Play(_warpExit, 0.7f, 1f);

        public static void WeaponFire(ModuleDef m, bool hit)
        {
            var clip = m.Id.Contains("rail") ? _rail : _blaster;
            Play(clip, hit ? 0.7f : 0.3f, hit ? Jit() : 0.8f);
        }

        public static void Explosion(float size = 1f)
            => Play(_explosion, 0.9f, Mathf.Clamp(1.15f - size * 0.15f, 0.6f, 1.2f));

        public static void PlayerHit(bool onShield)
            => Play(onShield ? _shieldHit : _armorHit, 0.7f, Jit());
    }
}
