Shader "Kairos/SonarSweep"
{
    Properties
    {
        _SweepColor("Sweep Color", Color) = (0.247, 0.878, 0.816, 1.0)
        _Center("Center", Vector) = (0, 0, 0, 0)
        _Radius("Radius", Float) = 0.0
        _Width("Wavefront Width", Float) = 14.0
        _Falloff("Distance Falloff", Float) = 0.0025
        _Intensity("Intensity", Float) = 2.0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+50" "RenderType"="Transparent" }
        Pass
        {
            Name "SonarSweepPass"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha One
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _SweepColor;
                float4 _Center;
                float _Radius;
                float _Width;
                float _Falloff;
                float _Intensity;
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
                float3 positionWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float4 color : COLOR;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float dist = distance(input.positionWS.xz, _Center.xz);
                float waveDist = abs(dist - _Radius);
                float waveFront = saturate(1.0 - (waveDist / max(_Width, 0.001)));
                waveFront = pow(waveFront, 1.6);

                float distFade = exp(-_Radius * _Falloff);
                float ripples = sin(waveDist * 0.75) * 0.25 + 0.75;

                half4 col = _SweepColor * input.color;
                col.rgb *= _Intensity * ripples;
                col.a *= waveFront * distFade;

                return col;
            }
            ENDHLSL
        }
    }
}
