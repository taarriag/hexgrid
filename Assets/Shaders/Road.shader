Shader "Custom/Road" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
	}
	SubShader {
		// The road triangles will always be drawn after the terrain triangles.
		Tags { "RenderType"="Opaque" "Queue" = "Geometry+1"}
		LOD 200
		// Make sure the roads are drawn above the terrain triangles that sit
		// in the same position. The triangles will be treated as if they were
		// closer even though there in the same position. 
		Offset -1, -1

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		// Shader needs to be alpha blended instead of Opaque. Turn the shader into a decal blended one.
		#pragma surface surf Standard fullforwardshadows decal:blend

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
			float3 worldPos;
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
			// Sample some noise from the albedo texture.
			float4 noise = tex2D(_MainTex, IN.worldPos.xz * 0.025);

			// Perturb the color by the Y noise channel.
			fixed4 c = _Color * (noise.y * 0.75 + 0.25); 

			// Blend the road with the terrain
			float blend = IN.uv_MainTex.x;
			// Perturb the transitioj by multiplying the U coordinate with noise.x, but since 
			// the noise values are 0.5 on average, that would wipe out most of the roads.
			// Add 0.5 to the noise before multiplying.
			blend *= noise.x + 0.5;
			// U coordinates from 0 to 0.4 will be fully transparent, 
			// U coordinates from 0.7 to 1 will be fully opaque.
			blend = smoothstep(0.4, 0.7, blend);

			o.Albedo = c.rgb;
			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = blend;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
