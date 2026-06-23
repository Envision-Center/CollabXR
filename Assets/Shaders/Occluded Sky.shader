// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "CollabXR/Occluded Skybox" {
Properties {
    _Tint ("Tint Color", Color) = (.5, .5, .5, .5)
    [Gamma] _Exposure ("Exposure", Range(0, 8)) = 1.0
    _Rotation ("Rotation", Range(0, 360)) = 0
    [NoScaleOffset] _Tex ("Cubemap   (HDR)", Cube) = "grey" {}
}

SubShader {
    Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
    Cull Off ZWrite Off

    Pass {

        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #pragma target 2.0

        #include "UnityCG.cginc"
		#include "Packages/com.meta.xr.sdk.core/Shaders/EnvironmentDepth/BiRP/EnvironmentOcclusionBiRP.cginc"

		#pragma multi_compile _ HARD_OCCLUSION SOFT_OCCLUSION

        samplerCUBE _Tex;
        half4 _Tex_HDR;
        half4 _Tint;
        half _Exposure;
        float _Rotation;

        float3 RotateAroundYInDegrees(float3 vertex, float degrees)
        {
            float alpha = degrees * UNITY_PI / 180.0;
            float sina, cosa;
            sincos(alpha, sina, cosa);
            float2x2 m = float2x2(cosa, -sina, sina, cosa);
            return float3(mul(m, vertex.xz), vertex.y).xzy;
        }

        struct appdata_t {
            float4 vertex : POSITION;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct v2f {
            float4 vertex : SV_POSITION;
            float3 texcoord : TEXCOORD0;
			float3 pseudoWorldPos : TEXCOORD1;
			META_DEPTH_VERTEX_OUTPUT(2)
            UNITY_VERTEX_OUTPUT_STEREO
        };

        v2f vert (appdata_t v)
        {
            v2f o;

            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

            float3 rotated = RotateAroundYInDegrees(v.vertex.xyz, -_Rotation);
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.texcoord = rotated;
			// for depth occlusion. this is gross :(
			o.pseudoWorldPos = _WorldSpaceCameraPos + v.vertex * 100;

			META_DEPTH_INITIALIZE_VERTEX_OUTPUT(o, v.vertex);
            return o;
        }

        fixed4 frag (v2f i) : SV_Target
        {
			UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

            half4 tex = texCUBE (_Tex, i.texcoord);
            half4 c = half4(DecodeHDR(tex, _Tex_HDR), 1);
            c.rgb = c.rgb * _Tint.rgb * unity_ColorSpaceDouble.rgb;
            c *= _Exposure;

			META_DEPTH_OCCLUDE_OUTPUT_PREMULTIPLY_WORLDPOS(i.pseudoWorldPos, c, 0.0);

            return c;
        }
        ENDCG
    }
}


Fallback Off

}
