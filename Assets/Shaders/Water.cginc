float Foam (float shore, float2 worldXZ, sampler2D noiseTex) {
  shore = sqrt(shore) * 0.9;

  float2 noiseUV = worldXZ + _Time.y * 0.25;
  float4 noise = tex2D(noiseTex, noiseUV * 0.015);

  // Distortion is lower near the coast
  float distortion1 = noise.x * (1 - shore);
  float foam1 = sin((shore + distortion1) * 10 - _Time.y);  // Forward foam
	foam1 *= foam1;

	float distortion2 = noise.y * (1 - shore);
	float foam2 = sin((shore + distortion2) * 10 + _Time.y + 2); // Recceeding foam
	foam2 *= foam2 * 0.7;

	return max(foam1, foam2) * shore;
}

float Waves (float2 worldXZ, sampler2D noiseTex) {
	float2 uv1 = worldXZ;
	uv1.y += _Time.y;
	float4 noise1 = tex2D(noiseTex, uv1 * 0.025);

	float2 uv2 = worldXZ;
	uv2.x += _Time.y;
	float4 noise2 = tex2D(noiseTex, uv2 * 0.025);

	// Produce a blend wave by creating a sine wave that runs
  // diagonally across the water surface.
	float blendWave = sin(
		(worldXZ.x + worldXZ.y) * 0.1 +
		(noise1.y + noise2.z) 
    + _Time.y
	);

  // Sine waves undulate between -1 an 1. Square it to bring it to the 0 to 1 range.
	blendWave *= blendWave;

	float waves =
		lerp(noise1.z, noise1.w, blendWave) +
		lerp(noise2.x, noise2.y, blendWave);

    // Map the range (0.75, 2) to (0,1) so that part of the water surface ends up without visible waves. 
	return smoothstep(0.75, 2, waves);
}

float River (float2 riverUV, sampler2D noiseTex) {
	float2 uv = riverUV;
	// Since V coordinates are stretched alongide the river, 
	// the noise texture looks stretched as well. We stretch
	// it alongside the U axis by scaling down the given U coordinates
	// by 1/16. This means we sample a narrow strip of the noise texture,
	// rather than the entire texture.
	//uv.x *= 0.0625;
	// Slide the strip across the texture
	uv.x = uv.x * 0.0625 + _Time.y * 0.005;
	uv.y -= _Time.y * 0.25;	// Slow the flow to a quarter of the texture per second.
	float4 noise = tex2D(noiseTex, uv);

	// Take a second sample of the texture, combine the samples.
	float2 uv2 = riverUV;
	uv2.x = uv2.x * 0.0625 - _Time.y * 0.0052;
	uv2.y -= _Time.y * 0.23;
	float4 noise2 = tex2D(noiseTex, uv2);
	return noise.x * noise2.w;
} 