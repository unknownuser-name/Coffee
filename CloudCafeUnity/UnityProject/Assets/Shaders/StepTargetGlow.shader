Shader "CloudCafe/StepTargetGlow"
{
    Properties
    {
        [HDR] _GlowColor ("Glow color", Color) = (1, 0.68, 0.15, 1)
        _OutlineWidth ("World outline width", Float) = 0.018
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull mode", Float) = 1
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Depth test", Float) = 4
    }
    SubShader
    {
        Tags { "Queue" = "Transparent+150" "RenderType" = "Transparent" }
        ZWrite Off
        ZTest [_ZTest]
        Cull [_Cull]
        Blend SrcAlpha One

        CGINCLUDE
        #include "UnityCG.cginc"
        float4 _GlowColor;
        float _OutlineWidth;

        struct VertexInput
        {
            float4 vertex : POSITION;
            float3 normal : NORMAL;
        };

        struct VertexOutput
        {
            float4 position : SV_POSITION;
        };

        VertexOutput GlowVertex(VertexInput input, float expansion)
        {
            VertexOutput output;
            float3 worldPosition = mul(unity_ObjectToWorld, input.vertex).xyz;
            float3 worldNormal = UnityObjectToWorldNormal(input.normal);
            worldPosition += worldNormal * _OutlineWidth * expansion;
            output.position = UnityWorldToClipPos(worldPosition);
            return output;
        }

        VertexOutput SoftVertex(VertexInput input) { return GlowVertex(input, 2.2); }
        VertexOutput CoreVertex(VertexInput input) { return GlowVertex(input, 1.0); }
        float4 SoftFragment(VertexOutput input) : SV_Target
        {
            return float4(_GlowColor.rgb, _GlowColor.a * 0.15);
        }
        float4 CoreFragment(VertexOutput input) : SV_Target
        {
            return float4(_GlowColor.rgb, _GlowColor.a * 0.85);
        }
        ENDCG

        Pass
        {
            CGPROGRAM
            #pragma vertex SoftVertex
            #pragma fragment SoftFragment
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex CoreVertex
            #pragma fragment CoreFragment
            ENDCG
        }
    }
    Fallback Off
}
