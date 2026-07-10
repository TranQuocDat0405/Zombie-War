// Dissolve shader for zombie deaths (Built-in RP).
// _DissolveAmount 0 -> renders normally, 1 -> fully dissolved.
// Driven per-renderer via MaterialPropertyBlock (see DissolveEffect.cs).
Shader "ZombieWar/Dissolve"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo", 2D) = "white" {}
        _NoiseTex ("Dissolve Noise", 2D) = "white" {}
        _DissolveAmount ("Dissolve Amount", Range(0,1)) = 0
        _EdgeWidth ("Edge Width", Range(0.001,0.25)) = 0.08
        _EdgeColor ("Edge Color", Color) = (1,0.45,0.05,1)
        _EdgeIntensity ("Edge Intensity", Float) = 3.5
        _FlashAmount ("Hit Flash", Range(0,1)) = 0
        _FlashColor ("Flash Color", Color) = (1,0.25,0.2,1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        Cull Off // hollow interiors look acceptable mid-dissolve

        CGPROGRAM
        #pragma surface surf Lambert addshadow
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _NoiseTex;
        fixed4 _Color;
        float _DissolveAmount;
        float _EdgeWidth;
        fixed4 _EdgeColor;
        float _EdgeIntensity;
        float _FlashAmount;
        fixed4 _FlashColor;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_NoiseTex;
        };

        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            float noise = tex2D(_NoiseTex, IN.uv_NoiseTex).r;
            float d = noise - _DissolveAmount;
            clip(d - 0.0001);

            // Glowing edge just above the dissolve threshold.
            float edge = (1.0 - smoothstep(0.0, _EdgeWidth, d)) * step(0.001, _DissolveAmount);

            o.Albedo = lerp(c.rgb, _FlashColor.rgb, _FlashAmount);
            o.Emission = _EdgeColor.rgb * edge * _EdgeIntensity
                       + _FlashColor.rgb * _FlashAmount * 0.6;
            o.Alpha = c.a;
        }
        ENDCG
    }

    Fallback "Diffuse"
}
