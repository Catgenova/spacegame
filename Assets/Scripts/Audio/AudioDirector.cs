using UnityEngine;

namespace SpaceGame
{
    /// <summary>
    /// Owns the AudioSources: a round-robin pool for one-shots (so each can
    /// carry its own pitch) and looping channels for engine, mining laser,
    /// and ambient drone, with volumes driven by game state each frame.
    /// F9 toggles mute; master volume persists via PlayerPrefs.
    /// </summary>
    public class AudioDirector : MonoBehaviour
    {
        public static AudioDirector I { get; private set; }

        const string VolumeKey = "spacegame_volume";

        float _master;
        bool _muted;
        AudioSource[] _oneshots;
        int _oneshotIdx;
        AudioSource _engine, _mining, _drone;

        float Master => _muted ? 0f : _master;

        void Awake()
        {
            I = this;
            _master = PlayerPrefs.GetFloat(VolumeKey, 0.7f);
            Sfx.Init();

            // The bootstrap's camera always carries an AudioListener; make sure.
            var cam = Camera.main;
            if (cam != null && cam.GetComponent<AudioListener>() == null)
                cam.gameObject.AddComponent<AudioListener>();

            _oneshots = new AudioSource[4];
            for (int i = 0; i < _oneshots.Length; i++)
                _oneshots[i] = MakeSource(false, null);
            _engine = MakeSource(true, Sfx.EngineLoop);
            _mining = MakeSource(true, Sfx.MiningLoop);
            _drone = MakeSource(true, Sfx.DroneLoop);
        }

        AudioSource MakeSource(bool loop, AudioClip clip)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f; // 2D — the whole soundscape is the pilot's cockpit
            src.loop = loop;
            src.volume = 0f;
            if (clip != null)
            {
                src.clip = clip;
                src.Play();
            }
            return src;
        }

        public void PlayOneShot(AudioClip clip, float vol, float pitch)
        {
            if (Master <= 0.001f) return;
            var src = _oneshots[_oneshotIdx];
            _oneshotIdx = (_oneshotIdx + 1) % _oneshots.Length;
            src.pitch = pitch;
            src.PlayOneShot(clip, vol * Master);
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || !gm.Ready) return;

            if (Input.GetKeyDown(KeyCode.F9))
            {
                _muted = !_muted;
                gm.Log(_muted ? "Audio muted (F9 to unmute)." : "Audio unmuted.");
                PlayerPrefs.SetFloat(VolumeKey, _master);
            }

            float dt = Time.deltaTime;

            // Ambient drone: always faintly present in space, silent docked.
            SetVol(_drone, gm.Docked ? 0f : 0.07f * Master, dt * 2f);

            // Engine: volume from actual speed, pitch rises with velocity + AB.
            float engineVol = 0f, enginePitch = 0.8f;
            if (!gm.Docked && !gm.Ship.InWarp)
            {
                var st = gm.Player.ComputeStats();
                float frac = Mathf.Clamp01(gm.Ship.Vel.magnitude / Mathf.Max(st.Speed, 0.01f));
                engineVol = (0.05f + frac * 0.3f) * Master;
                enginePitch = 0.8f + frac * 0.55f;
                foreach (var r in gm.Ship.Rack)
                    if (r.Active && r.Def.Kind == ModuleKind.Afterburner) { enginePitch += 0.25f; break; }
            }
            else if (!gm.Docked && gm.Ship.InWarp)
            {
                engineVol = 0.3f * Master;
                enginePitch = 1.7f;
            }
            SetVol(_engine, engineVol, dt * 4f);
            _engine.pitch = Mathf.Lerp(_engine.pitch, enginePitch, dt * 4f);

            // Mining hum while any miner cycles.
            bool miningActive = false;
            if (!gm.Docked)
                foreach (var r in gm.Ship.Rack)
                    if (r.Active && r.Def.Kind == ModuleKind.Miner) { miningActive = true; break; }
            SetVol(_mining, miningActive ? 0.22f * Master : 0f, dt * 5f);
        }

        static void SetVol(AudioSource src, float target, float k)
        {
            src.volume = Mathf.Lerp(src.volume, target, Mathf.Clamp01(k));
        }
    }
}
