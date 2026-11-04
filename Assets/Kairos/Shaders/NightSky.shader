Shader "Kairos/NightSky"
{
    Properties
    {
        _Zenith("Zenith", Color) = (0.02, 0.04, 0.06, 1)
        _Horizon("Horizon", Color) = (0.18, 0.227, 0.267, 1)
        _MoonDir("Moon direction", Vector) = (0.4, 0.5, 0.6, 0)
        _Clouds("Clouds", Range(0, 1)) = 0.6
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float4 _Zenith, _Horizon, _MoonDir;
            float _Clouds;
            float _OceanT, _RainDim;
            struct A { float4 pos : POSITION; };
            struct V { float4 cs : SV_POSITION; float3 dir : TEXCOORD0; };
            V vert(A a)
            {
                V o; o.dir = a.pos.xyz;
                o.cs = TransformObjectToHClip(a.pos.xyz);
                return o;
            }
            float Hash(float3 p) { p = frac(p * .3183099 + .1); p *= 17.0; return frac(p.x * p.y * p.z * (p.x + p.y + p.z)); }
            float Noise(float3 x)
            {
                float3 i = floor(x), f = frac(x); f = f * f * (3 - 2 * f);
                return lerp(lerp(lerp(Hash(i), Hash(i + float3(1, 0, 0)), f.x), lerp(Hash(i + float3(0, 1, 0)), Hash(i + float3(1, 1, 0)), f.x), f.y),
                            lerp(lerp(Hash(i + float3(0, 0, 1)), Hash(i + float3(1, 0, 1)), f.x), lerp(Hash(i + float3(0, 1, 1)), Hash(i + float3(1, 1, 1)), f.x), f.y), f.z);
            }
            float Fbm(float3 p) { float a = .5, s = 0; for (int i = 0; i < 4; i++) { s += a * Noise(p); p *= 2.1; a *= .5; } return s; }
            float4 frag(V i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float up = saturate(d.y);
                float3 c = lerp(_Horizon.rgb, _Zenith.rgb, pow(up, .45));
                float star = step(.9988, Hash(floor(d * 620)));
                c += star * .55 * smoothstep(.05, .4, up) * (1 - _RainDim * .9);
                float3 m = normalize(_MoonDir.xyz);
                float md = dot(d, m);
                c += smoothstep(.9992, .9996, md) * float3(.9, .95, 1) + pow(saturate(md), 90) * float3(.12, .16, .2);
                float3 p = d / max(d.y + .25, .05);
                float cl = smoothstep(1 - _Clouds, 1, Fbm(p * 1.1 + float3(_OceanT * .012, 0, _OceanT * .006)));
                c = lerp(c, _Horizon.rgb * .55 + pow(saturate(md), 12) * .1, cl * step(-.05, d.y) * .92);
                return float4(c, 1);
            }
            ENDHLSL
        }
    }
}
