Shader "Kairos/Pruebas/BoatWakeFoam"
{
    Properties
    {
        _FoamColor("Foam Color", Color) = (0.85, 0.95, 1.0, 0.8)
        _DeepColor("Deep Water Color", Color) = (0.05, 0.15, 0.25, 0.3)
        _Speed("Wake Speed", Float) = 1.2
        _WaveFreq("Wave Frequency", Float) = 8.0
        _FoamTurbulence("Turbulence", Float) = 3.5
        _EdgeFade("Edge Fade Power", Float) = 2.0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+60" "RenderType"="Transparent" }
        Pass
        {
            Name "BoatWakeFoamPass"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _FoamColor;
                float4 _DeepColor;
                float _Speed;
                float _WaveFreq;
                float _FoamTurbulence;
                float _EdgeFade;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float timeOffset = _Time.y * _Speed;

                // Forma de V expansiva de la estela en popa
                float distFromCenter = abs(uv.x - 0.5) * 2.0;
                float wakeLength = 1.0 - uv.y;

                // Patrón ondulante de espuma lateral
                float wave = sin((uv.y * _WaveFreq - timeOffset * 2.0) + distFromCenter * _FoamTurbulence);
                float foamPattern = saturate(wave * 0.5 + 0.5);

                // Disolución lateral y desvanecimiento longitudinal
                float edgeFade = pow(saturate(1.0 - distFromCenter), _EdgeFade);
                float lengthFade = saturate(wakeLength * 1.5);

                float combinedAlpha = foamPattern * edgeFade * lengthFade * _FoamColor.a;
                float3 finalRgb = lerp(_DeepColor.rgb, _FoamColor.rgb, foamPattern * edgeFade);

                return float4(finalRgb, combinedAlpha * input.color.a);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
