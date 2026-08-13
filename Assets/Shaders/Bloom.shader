// Bloom for the Built-in Render Pipeline.
//
// This is the one non-code asset in the project, and it is here under protest:
// Unity removed runtime compilation of ShaderLab from strings, so a post-process
// shader cannot be generated the way every mesh and material here is. It is
// still plain source text — no serialized asset, nothing to wire up in a scene.
//
// Four passes: prefilter the bright pixels, blur across, blur down, add back.
Shader "Hidden/SpaceGame/Bloom"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    CGINCLUDE
    #include "UnityCG.cginc"

    sampler2D _MainTex;
    float4 _MainTex_TexelSize;
    sampler2D _BloomTex;
    float _Threshold;
    float _Knee;
    float _Intensity;

    struct v2f
    {
        float4 pos : SV_POSITION;
        float2 uv  : TEXCOORD0;
    };

    v2f vert(appdata_img v)
    {
        v2f o;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.uv = v.texcoord;
        return o;
    }
    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // 0 — prefilter: keep only what is brighter than the threshold, with a
        // soft knee so the transition does not band.
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                float b = max(c.r, max(c.g, c.b));
                float soft = clamp(b - _Threshold + _Knee, 0.0, 2.0 * _Knee);
                soft = soft * soft / (4.0 * _Knee + 0.0001);
                float contrib = max(soft, b - _Threshold) / max(b, 0.0001);
                return c * contrib;
            }
            ENDCG
        }

        // 1 — horizontal blur.
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            fixed4 frag(v2f i) : SV_Target
            {
                float2 o = float2(_MainTex_TexelSize.x, 0.0);
                fixed4 s = tex2D(_MainTex, i.uv) * 0.2270270;
                s += (tex2D(_MainTex, i.uv + o * 1.3846) + tex2D(_MainTex, i.uv - o * 1.3846)) * 0.3162162;
                s += (tex2D(_MainTex, i.uv + o * 3.2307) + tex2D(_MainTex, i.uv - o * 3.2307)) * 0.0702702;
                return s;
            }
            ENDCG
        }

        // 2 — vertical blur.
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            fixed4 frag(v2f i) : SV_Target
            {
                float2 o = float2(0.0, _MainTex_TexelSize.y);
                fixed4 s = tex2D(_MainTex, i.uv) * 0.2270270;
                s += (tex2D(_MainTex, i.uv + o * 1.3846) + tex2D(_MainTex, i.uv - o * 1.3846)) * 0.3162162;
                s += (tex2D(_MainTex, i.uv + o * 3.2307) + tex2D(_MainTex, i.uv - o * 3.2307)) * 0.0702702;
                return s;
            }
            ENDCG
        }

        // 3 — composite: the original scene plus the blurred highlights.
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 scene = tex2D(_MainTex, i.uv);
                fixed4 glow = tex2D(_BloomTex, i.uv);
                return scene + glow * _Intensity;
            }
            ENDCG
        }
    }
    Fallback Off
}
