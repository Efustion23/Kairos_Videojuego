Shader "Kairos/Pruebas/SonarScreenSweep"
{
    Properties
    {
        _SweepColor("Sweep Color", Color) = (0.247, 0.878, 0.816, 1.0) // Cian brillante (#3FE0D0)
        _Origin("Screen Origin", Vector) = (0.5, 0.5, 0.0, 0.0)
        _Progress("Sweep Progress", Range(0.0, 1.0)) = 0.0
        _Width("Wavefront Width", Float) = 0.08
        _Intensity("HDR Intensity", Float) = 2.5
        _Ripples("Ripple Frequency", Float) = 32.0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+100" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Name "SonarScreenSweepPass"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha One
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _SweepColor;
                float4 _Origin;
                float _Progress;
                float _Width;
                float _Intensity;
                float _Ripples;
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

            half4 frag(Varyings input) : SV_Target
            {
                if (_Progress <= 0.001 || _Progress >= 0.999) return half4(0, 0, 0, 0);

                float aspect = _ScreenParams.x / max(_ScreenParams.y, 1.0);
                float2 delta = input.uv - _Origin.xy;
                delta.x *= aspect;

                float dist = length(delta);
                float targetRadius = _Progress * 1.5;

                float waveDist = abs(dist - targetRadius);
                float width = max(_Width, 0.005);
                float leadingEdge = saturate(1.0 - (waveDist / width));
                leadingEdge = pow(leadingEdge, 1.8);

                float trailDist = targetRadius - dist;
                float trail = saturate(trailDist / max(width * 3.5, 0.01)) * saturate(1.0 - (trailDist / 0.4));
                float ripples = sin(trailDist * _Ripples) * 0.5 + 0.5;

                float globalFade = saturate(1.0 - _Progress * 0.85);

                half4 col = _SweepColor * input.color;
                float alpha = (leadingEdge * 1.6 + trail * ripples * 0.45) * globalFade;
                col.rgb *= _Intensity * (leadingEdge * 1.4 + 0.6);
                col.a *= saturate(alpha);

                return col;
            }
            ENDHLSL
        }
    }
}
