Shader "TinyRiders/PrintedPaint"
{
 Properties {
  _BaseColor("Panel paint",Color)=(0.7,0.08,0.12,1)
  _BaseMap("Printed artwork (alpha masks ink)",2D)="black"{}
  _Smoothness("Smoothness",Range(0,1))=0.6
  _Metallic("Metallic",Range(0,1))=0.12
 }
 SubShader {
  Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
  Pass {
   Name "ForwardLit"
   Tags { "LightMode"="UniversalForward" }
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
   #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
   #pragma multi_compile_fragment _ _SHADOWS_SOFT
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
   float4 _BaseColor; float4 _BaseMap_ST; float _Smoothness; float _Metallic;
   CBUFFER_END
   struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; };
   struct V { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; half3 normalWS:TEXCOORD1; float2 uv:TEXCOORD2; };
   V vert(A v) { V o; o.positionWS=TransformObjectToWorld(v.positionOS.xyz); o.positionCS=TransformWorldToHClip(o.positionWS); o.normalWS=TransformObjectToWorldNormal(v.normalOS); o.uv=TRANSFORM_TEX(v.uv,_BaseMap); return o; }
   half4 frag(V i):SV_Target {
    half4 art=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv);
    InputData d=(InputData)0; d.positionWS=i.positionWS; d.normalWS=normalize(i.normalWS); d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS); d.shadowCoord=TransformWorldToShadowCoord(i.positionWS); d.bakedGI=SampleSH(d.normalWS); d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS); d.shadowMask=half4(1,1,1,1);
    SurfaceData s=(SurfaceData)0; s.albedo=lerp(_BaseColor.rgb,art.rgb,art.a); s.metallic=_Metallic; s.smoothness=_Smoothness; s.normalTS=half3(0,0,1); s.occlusion=1; s.alpha=1;
    return UniversalFragmentPBR(d,s);
   }
   ENDHLSL
  }
  UsePass "Universal Render Pipeline/Lit/ShadowCaster"
  UsePass "Universal Render Pipeline/Lit/DepthOnly"
 }
}
