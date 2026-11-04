Shader "Kairos/Ocean"
{
    Properties
    {
        _Deep("Deep", Color) = (0.043, 0.102, 0.141, 1)
        _Shallow("Crest tint", Color) = (0.07, 0.2, 0.25, 1)
        _SkyTint("Sky", Color) = (0.18, 0.227, 0.267, 1)
        _FoamColor("Foam", Color) = (0.6, 0.7, 0.75, 1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "OceanForward"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Deep, _Shallow, _SkyTint, _FoamColor;
            CBUFFER_END
            float4 _OceanWaves[4];
            float _OceanAmp, _OceanT;

            struct A { float4 pos : POSITION; };
            struct V { float4 cs : SV_POSITION; float3 ws : TEXCOORD0; float fog : TEXCOORD1; };

            // Misma fórmula que Ocean.Height en C#.
            float WaveH(float2 p)
            {
                float h = 0;
                for (int i = 0; i < 4; i++)
                    h += _OceanWaves[i].w * sin(dot(_OceanWaves[i].xy, p) + _OceanWaves[i].z * _OceanT);
                return h * _OceanAmp;
            }
            float2 WaveGrad(float2 p)
            {
                float2 g = 0;
                for (int i = 0; i < 4; i++)
                    g += _OceanWaves[i].w * _OceanWaves[i].xy * cos(dot(_OceanWaves[i].xy, p) + _OceanWaves[i].z * _OceanT);
                return g * _OceanAmp;
            }
            float Ripple(float2 p, float t)
            {
                // Dominio deformado y direcciones no alineadas: evita rayas regulares en el brillo de la luna.
                p += .8 * float2(sin(p.y * .37 + t * .2), sin(p.x * .41 - t * .17));
                return .5 * sin(dot(p, float2(1.3, .4)) + t * 1.7) + .5 * sin(dot(p, float2(-.7, 1.1)) - t * 1.3)
                     + .3 * sin(dot(p, float2(2.9, -2.1)) + t * 2.3) + .2 * sin(dot(p, float2(-3.7, 4.3)) - t * 3.1)
                     + .12 * sin(dot(p, float2(6.1, 5.3)) + t * 3.7);
            }

            V vert(A a)
            {
                V o;
                float3 ws = TransformObjectToWorld(a.pos.xyz);
                ws.y += WaveH(ws.xz) * saturate(1.2 - length(ws.xz - _WorldSpaceCameraPos.xz) / 1800.0);
                o.ws = ws;
                o.cs = TransformWorldToHClip(ws);
                o.fog = ComputeFogFactor(o.cs.z);
                return o;
            }

            half4 frag(V i) : SV_Target
            {
                float dist = length(i.ws - _WorldSpaceCameraPos);
                float detail = saturate(1 - dist / 220.0);
                float2 g = WaveGrad(i.ws.xz);
                float2 p = i.ws.xz * .35;
                float e = .08;
                float r0 = Ripple(p, _OceanT);
                float2 rg = float2(Ripple(p + float2(e, 0), _OceanT) - r0, Ripple(p + float2(0, e), _OceanT) - r0) / e;
                g += rg * .03 * detail * (0.4 + _OceanAmp);
                float3 n = normalize(float3(-g.x, 1, -g.y));
                float3 v = normalize(_WorldSpaceCameraPos - i.ws);
                Light l = GetMainLight();
                float fres = pow(1 - saturate(dot(n, v)), 4);
                float h01 = saturate(WaveH(i.ws.xz) / max(_OceanAmp, .01) * .5 + .5);
                float3 c = lerp(_Deep.rgb, _Shallow.rgb, h01 * .7);
                c = lerp(c, _SkyTint.rgb, fres * .85);
                float3 hv = normalize(l.direction + v);
                float spec = pow(saturate(dot(n, hv)), 220) * .9 + pow(saturate(dot(n, hv)), 24) * .03;
                c += l.color * spec;
                float foam = smoothstep(.72, .98, h01 + r0 * .07) * saturate(_OceanAmp - .15) * detail;
                c = lerp(c, _FoamColor.rgb * (.25 + l.color * .4), foam * .55);
                c = MixFog(c, i.fog);
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
