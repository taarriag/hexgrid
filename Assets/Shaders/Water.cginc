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