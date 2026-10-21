Shader "TinyRiders/Car Spatial Proxy"
{
    Properties
    {
        _BaseMap ("Car Atlas", 2D) = "white" {}
        _TileSize ("Atlas Tile Size", Vector) = (0.03125,0.0625,0,0)
        _ProxyTint ("Proxy Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            ZWrite On
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            float4 _TileSize;
            half4 _ProxyTint;
            struct Attributes
            {
                float4 positionOS : POSITION;
                half3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float2 tile : TEXCOORD1;
                half4 color : COLOR;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                half light : TEXCOORD1;
            };
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.tile + (frac(input.uv) * 0.984375 + 0.0078125) * _TileSize.xy;
                output.color = input.color;
                half3 normal = normalize(TransformObjectToWorldNormal(input.normalOS));
                output.light = 0.68h + 0.32h * saturate(dot(normal, normalize(half3(0.4h, 0.8h, 0.25h))));
                return output;
            }
            half4 frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * input.color;
                return half4(color.rgb * input.light * _ProxyTint.rgb, 1);
            }
            ENDHLSL
        }
    }
}
