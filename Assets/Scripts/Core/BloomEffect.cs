using UnityEngine;

namespace SpaceGame
{
    /// <summary>
    /// Bloom on the main camera, for the Built-in Render Pipeline.
    ///
    /// Everything that is meant to be a light in this game is an emissive
    /// material — engine nozzles, glow ports, the pirates' eye pinpoints, anomaly
    /// cores, station seams. Without a post-process those are just brighter
    /// polygons: the colour never leaves the surface, so nothing reads as
    /// glowing. This bright-passes the frame, blurs it, and adds it back.
    ///
    /// Fails safe. If the shader is missing or unsupported on the machine, the
    /// component disables itself and the game renders exactly as it did before —
    /// a broken effect must never cost you the frame.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class BloomEffect : MonoBehaviour
    {
        /// <summary>Brightness a pixel must exceed to glow. Emissive materials are
        /// authored at 2x their colour, so anything above 1 is deliberate.</summary>
        public float Threshold = 1.0f;
        public float Knee = 0.45f;
        public float Intensity = 0.85f;
        /// <summary>Blur passes. Each halves the resolution again, so more means a
        /// wider, softer glow for very little cost.</summary>
        public int Iterations = 4;

        Material _mat;
        bool _dead;

        void OnEnable()
        {
            var shader = Shader.Find("Hidden/SpaceGame/Bloom");
            if (shader == null || !shader.isSupported)
            {
                _dead = true;
                enabled = false;
                return;
            }
            _mat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }

        void OnDisable()
        {
            if (_mat != null) DestroyImmediate(_mat);
            _mat = null;
        }

        void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            if (_dead || _mat == null) { Graphics.Blit(src, dst); return; }

            int w = Mathf.Max(1, src.width / 2), h = Mathf.Max(1, src.height / 2);
            var fmt = src.format;

            _mat.SetFloat("_Threshold", Threshold);
            _mat.SetFloat("_Knee", Mathf.Max(0.0001f, Knee));
            _mat.SetFloat("_Intensity", Intensity);

            // Bright-pass into a half-size buffer.
            var current = RenderTexture.GetTemporary(w, h, 0, fmt);
            Graphics.Blit(src, current, _mat, 0);

            // Blur, halving each time: cheap and progressively wider.
            int steps = Mathf.Clamp(Iterations, 1, 8);
            for (int i = 0; i < steps; i++)
            {
                w = Mathf.Max(1, w / 2);
                h = Mathf.Max(1, h / 2);
                if (w < 2 || h < 2) break;

                var hBlur = RenderTexture.GetTemporary(w, h, 0, fmt);
                Graphics.Blit(current, hBlur, _mat, 1);
                RenderTexture.ReleaseTemporary(current);

                var vBlur = RenderTexture.GetTemporary(w, h, 0, fmt);
                Graphics.Blit(hBlur, vBlur, _mat, 2);
                RenderTexture.ReleaseTemporary(hBlur);

                current = vBlur;
            }

            _mat.SetTexture("_BloomTex", current);
            Graphics.Blit(src, dst, _mat, 3);
            RenderTexture.ReleaseTemporary(current);
        }
    }
}
