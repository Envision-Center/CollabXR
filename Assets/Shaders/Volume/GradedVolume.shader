Shader "Unlit/GradedVolume"
{
	Properties
	{
		_MainTex ("Texture", 3D) = "white" {}
		_AlphaGradient ("Alpha Gradient", 2D) = "white" {}
		_ColorGradient ("Color Gradient", 2D) = "white" {}
		_AlphaFactor ("Alpha Factor", float) = 0.02
		_StepSize ("Step Size", float) = 0.01
		_MaxSteps ("Max Steps", int) = 128
	}
	SubShader
	{
		Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
		Blend One OneMinusSrcAlpha
		LOD 100

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			// Allowed floating point inaccuracy
			#define EPSILON 0.00001f

			struct appdata
			{
				float4 vertex : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float3 objectVertex : TEXCOORD0;
				float3 vectorToSurface : TEXCOORD1;

				UNITY_VERTEX_OUTPUT_STEREO
			};

			Texture3D _MainTex;
			SamplerState sampler_MainTex;

			Texture2D _AlphaGradient;
			Texture2D _ColorGradient;
			SamplerState sampler_linear_clamp_ColorGradient;

			float4 _MainTex_ST;
			float _AlphaFactor;
			float _StepSize;
			int _MaxSteps;

			v2f vert (appdata v)
			{
				v2f o;

				UNITY_SETUP_INSTANCE_ID(v);
				// UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				// Vertex in object space this will be the starting point of raymarching
				o.objectVertex = v.vertex;

				// Calculate vector from camera to vertex in world space
				float3 worldVertex = mul(unity_ObjectToWorld, v.vertex).xyz;
				o.vectorToSurface = worldVertex - _WorldSpaceCameraPos;

				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				return o;
			}

			float4 BlendUnderGradient(float4 color, float value)
			{
				float gradientAlpha = _AlphaGradient.Sample(sampler_linear_clamp_ColorGradient, float2(value, 0.5f)).r * _AlphaFactor;
				float3 gradientColor = _ColorGradient.Sample(sampler_linear_clamp_ColorGradient, float2(value, 0.5f));

				color.rgb += (1.0 - color.a) * gradientAlpha * gradientColor;
				color.a += (1.0 - color.a) * gradientAlpha;
				return color;
			}

			float4 frag(v2f i) : SV_Target
			{
				// Start raymarching at the front surface of the object
				float3 rayOrigin = i.objectVertex;

				// Use vector from camera to object surface to get ray direction
				float3 rayDirection = mul(unity_WorldToObject, float4(normalize(i.vectorToSurface), 0));

				float4 color = float4(0, 0, 0, 0);
				float3 samplePosition = rayOrigin;

				// Raymarch through object space
				for (int i = 0; i < _MaxSteps; i++)
				{
					// Accumulate color only within unit cube bounds
					if(max(abs(samplePosition.x), max(abs(samplePosition.y), abs(samplePosition.z))) < 0.5f + EPSILON)
					{
						float density = _MainTex.Sample(sampler_MainTex, samplePosition + float3(0.5f, 0.5f, 0.5f)).r;
						color = BlendUnderGradient(color, density);
						samplePosition += rayDirection * _StepSize;
					}
				}

				return color;
			}
			ENDHLSL
		}
	}
}
