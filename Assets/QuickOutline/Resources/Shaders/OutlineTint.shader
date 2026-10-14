//
//  OutlineTint.shader
//  QuickOutline
//
//  Optional tint overlay add-on. Draws a flat, semi-transparent color
//  over the object's visible surface to accompany the outline.
//

Shader "Custom/Outline Tint" {
  Properties {
    [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 4

    _TintColor("Tint Color", Color) = (1, 1, 1, 0.35)
  }

  SubShader {
    Tags {
      "Queue" = "Transparent+109"
      "RenderType" = "Transparent"
      "DisableBatching" = "True"
    }

    Pass {
      Name "Tint"
      Cull Back
      ZTest [_ZTest]
      ZWrite Off
      Blend SrcAlpha OneMinusSrcAlpha
      ColorMask RGB

      CGPROGRAM
      #include "UnityCG.cginc"

      #pragma vertex vert
      #pragma fragment frag

      struct appdata {
        float4 vertex : POSITION;
        UNITY_VERTEX_INPUT_INSTANCE_ID
      };

      struct v2f {
        float4 position : SV_POSITION;
        UNITY_VERTEX_OUTPUT_STEREO
      };

      uniform fixed4 _TintColor;

      v2f vert(appdata input) {
        v2f output;

        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

        output.position = UnityObjectToClipPos(input.vertex);

        return output;
      }

      fixed4 frag(v2f input) : SV_Target {
        return _TintColor;
      }
      ENDCG
    }
  }
}
