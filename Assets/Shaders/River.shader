Shader "Custom/River" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
	}
	SubShader {
		Tags { "RenderType"="Transparent" "Queue"="Transparent" }
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard alpha //fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

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
			// Sample the texture (instead of showing raw UV)
			// and multiply the material's color by the first channel
			// of said texture.
			float2 uv = IN.uv_MainTex;
			// Since V coordinates are stretched alongide the river, 
			// the noise texture looks stretched as well. We stretch
			// it alongside the U axis by scaling down the given U coordinates
			// by 1/16. This means we sample a narrow strip of the noise texture,
			// rather than the entire texture.
			//uv.x *= 0.0625;
			// Slide the strip across the texture
			uv.x = uv.x * 0.0625 + _Time.y * 0.005;
			uv.y -= _Time.y * 0.25;	// Slow the flow to a quarter of the texture per second.
			float4 noise = tex2D(_MainTex, uv);

			// Take a second sample of the texture, combine the samples.
			float2 uv2 = IN.uv_MainTex;
			uv2.x = uv2.x * 0.0625 - _Time.y * 0.0052;
			uv2.y -= _Time.y * 0.23;
			float4 noise2 = tex2D(_MainTex, uv2);
			
			//fixed4 c = _Color * (noise.r * noise2.a);
			// Use material color as base color. Noise increases brightness and opacity.
			fixed4 c = saturate(_Color + noise.r * noise2.a);	
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
