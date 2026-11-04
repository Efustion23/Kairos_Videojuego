Shader "Kairos/Glow"
{
    Properties
    {
        _MainTex("Tex", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)
        _Src("Src", Float) = 5
        _Dst("Dst", Float) = 1
        _ZT("ZTest", Float) = 4
        _ZW("ZWrite", Float) = 0
        _Round("Round", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Name "Glow"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend [_Src] [_Dst]
            ZWrite [_ZW]
            ZTest [_ZT]
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST; float4 _Color; float _Round;
                float _Src, _Dst, _ZT, _ZW;
            CBUFFER_END
            struct A { float4 pos : POSITION; float2 uv : TEXCOORD0; float4 col : COLOR; };
            struct V { float4 cs : SV_POSITION; float2 uv : TEXCOORD0; float4 col : COLOR; };
            V vert(A a)
            {
                V o; o.cs = TransformObjectToHClip(a.pos.xyz); o.uv = a.uv; o.col = a.col; return o;
            }
            half4 frag(V i) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv * _MainTex_ST.xy + _MainTex_ST.zw) * _Color * i.col;
                if (_Round > .5) c.a *= saturate(1 - length(i.uv * 2 - 1));
                return c;
            }
            ENDHLSL
        }
    }
}
