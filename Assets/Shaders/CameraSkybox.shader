//Helps to display a flat 2D image that always remains on screen,
//regardless of where the camera is looking.
//from https://gist.github.com/aras-p/3d8218ef5d96d5984019
// Maintains aspect of the image (outter-envelops the viewport)

Shader "Skybox/Background Texture Aspect"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
		Cull Off ZWrite Off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			void vert (float4 pos : POSITION, out float4 outUV : TEXCOORD0, out float4 outPos : SV_POSITION)
			{
				outPos = UnityObjectToClipPos(pos);
				outUV = ComputeScreenPos(outPos);
			}

			sampler2D _MainTex;
			float4 _MainTex_TexelSize;

			fixed4 frag (float4 uv : TEXCOORD0) : SV_Target{
				// Compute aspect ratio of the texture and the screen
				float textureAspect = _MainTex_TexelSize.z / _MainTex_TexelSize.w; // Use z and w for texture width and height
				float screenAspect = _ScreenParams.x / _ScreenParams.y;

				uv /= uv.w;

				// Calculate scale and offset for UVs to keep the image centered
				float scale, offset;

				if (screenAspect < textureAspect){
					// Screen is narrower than texture - adjust UV.x
					//scale = screenAspect / textureAspect;
					//offset = (1.0f - scale) * 0.5f;
					//uv.x = uv.x * scale + offset;
				} else {// Screen is less tall than texture - adjust UV.y
					//scale = textureAspect / screenAspect;
					//offset = (1.0f - scale) * 0.5f;
					//uv.y = uv.y * scale + offset;
				}
				fixed4 col = tex2D(_MainTex, uv);
				return col;
			}

			ENDCG
		}
	}
}
