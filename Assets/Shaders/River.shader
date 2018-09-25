Shader "Custom/River" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
	}
	SubShader {
		// Rivers should always be rendered above the water on top 
		Tags { "RenderType"="Transparent" "Queue"="Transparent+1" }
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard alpha //fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		#include "Water.cginc"

		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		void surf (Input IN, inout SurfaceOutputStandard o) {
			float river = River(IN.uv_MainTex, _MainTex);

			//fixed4 c = _Color * (noise.r * noise2.a);
			// Use material color as base color. Noise increases brightness and opacity.
			fixed4 c = saturate(_Color + river); //noise.r * noise2.a);	
			o.Albedo = c.rgb;
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;

			// Old: Directly using the UV colors.
			/*// Albedo comes from a texture tinted by color
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb;
			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
			// Properly wrap the V coordinate
			// if (IN.uv_MainTex.y < 0) {
			//	IN.uv_MainTex.y += 1;
			//}
			// Animated river using the Time variable exposed by unity.
			// Note that we use _Time.y because this gives the unmodified elapsed time.
			// since level load.
			IN.uv_MainTex.y -= _Time.y;
			// Take the fractional part of the new V value, the reason is that
			// the texture filtering mode is set to Repeat (rather than Wrap) so
			// values below 0 will just repeat 0.
			IN.uv_MainTex.y = frac(IN.uv_MainTex.y);	
			
			o.Albedo.rg = IN.uv_MainTex;*/
		}
		ENDCG
	}
	FallBack "Diffuse"
}
