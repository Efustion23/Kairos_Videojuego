Shader "Kairos/Pruebas/InspectableOutlineGlow"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.75, 0.72, 0.68, 1.0)
        _GlowColor("Glow Highlight Color", Color) = (0.18, 0.96, 1.0, 1.0)
        _GlowPower("Rim Fresnel Power", Range(0.5, 8.0)) = 2.8
        _HighlightStrength("Highlight Strength", Range(0.0, 5.0)) = 1.6
        _PulseSpeed("Pulse Speed", Float) = 2.8
        _PulseIntensity("Pulse Intensity", Range(0.0, 1.0)) = 0.35
        _ActiveGlow("Active Glow (0 or 1)", Float) = 1.0
    }
    SubShader
    {
        Tags 
        { 
            "RenderPipeline"="UniversalPipeline" 
            "Queue"="Geometry" 
            "RenderType"="Opaque" 
        }

        Pass
        {
            Name "ForwardLitGlow"
            Tags { "LightMode"="UniversalForward" }
            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _GlowColor;
                float _GlowPower;
                float _HighlightStrength;
                float _PulseSpeed;
                float _PulseIntensity;
                float _ActiveGlow;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(GetCameraPositionWS() - input.positionWS);

                // Cálculo Fresnel para efecto de silueta y borde iluminado
                float NdotV = saturate(dot(normalWS, viewDirWS));
                float fresnel = pow(1.0 - NdotV, max(_GlowPower, 0.001));

                // Pulsación senoidal temporal suave para guiar la atención del jugador
                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseIntensity;
                float3 rimGlow = _GlowColor.rgb * (fresnel * _HighlightStrength * pulse * _ActiveGlow);

                // Color final combinando albedo base y resplandor de evidencia
                float3 finalRgb = _BaseColor.rgb + rimGlow;
                return half4(finalRgb, _BaseColor.a);
            }
            ENDHLSL
        }

        // Pase complementario de fallback para rendering unlit
        Pass
        {
            Name "SRPDefaultUnlit"
            Tags { "LightMode"="SRPDefaultUnlit" }
            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _GlowColor;
                float _GlowPower;
                float _HighlightStrength;
                float _PulseSpeed;
                float _PulseIntensity;
                float _ActiveGlow;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(GetCameraPositionWS() - input.positionWS);
                float NdotV = saturate(dot(normalWS, viewDirWS));
                float fresnel = pow(1.0 - NdotV, max(_GlowPower, 0.001));
                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseIntensity;
                float3 rimGlow = _GlowColor.rgb * (fresnel * _HighlightStrength * pulse * _ActiveGlow);

                return half4(_BaseColor.rgb + rimGlow, _BaseColor.a);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
