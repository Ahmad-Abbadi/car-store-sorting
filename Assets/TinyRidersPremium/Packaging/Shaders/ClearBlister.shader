Shader "TinyRiders/PET Blister"
{
    Properties
    {
        _BaseColor("Plastic tint", Color) = (0.85,0.94,1,0.035)
        _EdgeOpacity("Grazing opacity", Range(0,1)) = 0.32
        _Gloss("Specular gloss", Range(8,512)) = 160
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend One OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _EdgeOpacity;
                half _Gloss;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; half3 normalWS : TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS=TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS=TransformWorldToHClip(output.positionWS);
                output.normalWS=TransformObjectToWorldNormal(input.normalOS);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half3 normal=normalize(input.normalWS);
                half3 view=normalize(GetWorldSpaceViewDir(input.positionWS));
                half facing=saturate(dot(normal,view));
                half fresnel=0.04h+0.96h*pow(1-facing,5);
                half alpha=saturate(_BaseColor.a+_EdgeOpacity*fresnel);
                Light key=GetMainLight();
                half3 halfway=normalize(view+key.direction);
                half specular=pow(saturate(dot(normal,halfway)),_Gloss);
                // Glancing reflection and key-light reflection arise from the real
                // shell normals. No printed or geometry-based shine decorations.
                half3 reflection=_BaseColor.rgb*(0.4h+0.6h*fresnel);
                half3 color=reflection*alpha+key.color*specular*0.45h;
                return half4(color,alpha);
            }
            ENDHLSL
        }
    }
}
