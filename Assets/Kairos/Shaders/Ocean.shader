Shader "Kairos/Ocean"
{
    Properties
    {
        _Deep("Deep", Color) = (0.043, 0.102, 0.141, 1)
        _Shallow("Crest tint", Color) = (0.07, 0.2, 0.25, 1)
        _SkyTint("Sky", Color) = (0.18, 0.227, 0.267, 1)
        _FoamColor("Foam", Color) = (0.6, 0.7, 0.75, 1)
        [Normal] _WaterNormal("Water normal", 2D) = "bump" {}
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

            TEXTURE2D(_WaterNormal); SAMPLER(sampler_WaterNormal);
            CBUFFER_START(UnityPerMaterial)
                float4 _Deep, _Shallow, _SkyTint, _FoamColor, _WaterNormal_ST;
            CBUFFER_END
            float4 _OceanWaves[4];
            float _OceanAmp, _OceanT;
            float4 _BoatPos, _BoatDir; // xyz posición y w velocidad; xz rumbo

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
            // Distancia aproximada al casco del Hai Feng 77, que siempre está en el origen.
            float ShipDist(float2 p)
            {
                float x = abs(p.x), z = p.y;
                float k = (z - 15.0) / 30.0;
                float beam = z < 15.0 ? 7.0 : 7.0 * sqrt(max(0.0, 1.0 - k * k));
                return max(x - beam, max(-45.0 - z, z - 45.0));
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
                float t = _OceanT;
                float2 p = i.ws.xz;
                // Dos capas de pequeñas olas a distinta escala que se cruzan: rompen las bandas regulares.
                float3 n1 = UnpackNormal(SAMPLE_TEXTURE2D(_WaterNormal, sampler_WaterNormal, p * .05 + float2(.013, .021) * t));
                float3 n2 = UnpackNormal(SAMPLE_TEXTURE2D(_WaterNormal, sampler_WaterNormal, p * .17 + float2(-.031, .017) * t));
                float2 g = WaveGrad(p) + (n1.xy + n2.xy * .7) * (.06 + _OceanAmp * .1);
                float3 n = normalize(float3(-g.x, 1, -g.y));
                float3 v = normalize(_WorldSpaceCameraPos - i.ws);
                Light l = GetMainLight();
                float fres = pow(1 - saturate(dot(n, v)), 4);
                float h01 = saturate(WaveH(p) / max(_OceanAmp, .01) * .5 + .5);
                float3 c = lerp(_Deep.rgb, _Shallow.rgb, h01 * .7);
                c = lerp(c, _SkyTint.rgb, fres * .85);
                float3 hv = normalize(l.direction + v);
                float nh = saturate(dot(n, hv));
                c += l.color * (pow(nh, 320) * .45 + pow(nh, 60) * .02);

                float detail = saturate(1 - dist / 260.0);
                float grain = SAMPLE_TEXTURE2D(_WaterNormal, sampler_WaterNormal, p * .31 + float2(.02, -.03) * t).r;
                grain = saturate(grain * 1.6 - .3);
                float foam = smoothstep(.72, .98, h01 + (n1.x * .2)) * saturate(_OceanAmp - .15) * detail;

                // Estela de la lancha: una V de espuma que se abre y se apaga.
                if (_BoatPos.w > .2)
                {
                    float2 rel = p - _BoatPos.xz;
                    float along = -dot(rel, _BoatDir.xz);
                    float side = dot(rel, float2(_BoatDir.z, -_BoatDir.x));
                    if (along > -1.5)
                    {
                        float len = 6.0 + _BoatPos.w * 5.0;
                        float spread = 1.2 + saturate(along / len) * len * .22;
                        float edge = abs(abs(side) - spread * .75);
                        float wake = max(smoothstep(.9, 0, edge), smoothstep(spread * .4, 0, abs(side)) * .8);
                        foam = max(foam, wake * saturate(1 - along / len) * saturate((along + 1.5) * .7) * saturate(_BoatPos.w * .25));
                    }
                }

                // El casco del barco rompe el agua y sus focos la tiñen.
                float d = max(ShipDist(p), 0.0);
                foam = max(foam, smoothstep(1.8, .1, d) * (.15 + saturate(_OceanAmp) * .35));
                c += float3(.95, .62, .22) * exp(-d / 7.0) * (.012 + fres * .1);

                c = lerp(c, _FoamColor.rgb * (.3 + l.color * .35), saturate(foam * grain * 1.2));
                c = MixFog(c, i.fog);
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
