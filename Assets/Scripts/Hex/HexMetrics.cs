using UnityEngine;

public static class HexMetrics {
	public const float outerToInner = 0.866025404f; //sqrt(3)/2 times outer radius.
	public const float innerToOuter = 1f / outerToInner;

	public const float outerRadius = 10f;
	public const float innerRadius = outerRadius  * outerToInner; 
	
	public const float solidFactor = 0.8f;	// Recommended tweak
	public const float blendFactor = 1f - solidFactor;
	public const float waterFactor = 0.6f;
	public const float waterBlendFactor = 1f - waterFactor;

	public const float elevationStep = 3f; // Recommended tweak
	
	public const int terracesPerSlope = 2;
	public const int terraceSteps = terracesPerSlope * 2 + 1;
	public const float horizontalTerraceStepSize = 1f / terraceSteps;
	public const float verticalTerraceStepSize = 1f / (terracesPerSlope + 1);
	
	public const float cellPerturbStrength = 4f; //0f;	// Recommended tweek
	public const float elevationPerturbStrength = 0f; //1.5f;	// Recommended tweak
	
	public const int chunkSizeX = 5, chunkSizeZ = 5;

	public const float streamBedElevationOffset = -1.75f; //-1f;
	public const float waterElevationOffset = -0.5f;

	// We are using world coordinates to sample the noise texture, which causes the texture
	// to tile every unit, while our cells are much larger (radious = 10 units). This means we have
	// to scale the noise sampling so that the texture covers a much larger area. 
	public const float noiseScale = 0.003f; // The texture will cover 333.3 square units rather than 1 unit repeating, which means we can sample directly using world coordinates now.
	public static Texture2D noiseSource;
	public static Color[] colors;

	// Orientation with point at the too, consider center at (0,0,0)
	public static Vector3[] corners = {
		new Vector3(0f, 0f, outerRadius),
		new Vector3(innerRadius, 0f, 0.5f * outerRadius),
		new Vector3(innerRadius, 0f, -0.5f * outerRadius),
		new Vector3(0f, 0f, -outerRadius),
		new Vector3(-innerRadius, 0f, -0.5f * outerRadius),
		new Vector3(-innerRadius, 0f, 0.5f * outerRadius),
		// Repeat the first one so we can loop across all vertices, going back to the first.
		new Vector3(0f, 0f, outerRadius)
	};

	public static Vector3 GetFirstCorner (HexDirection direction) {
		return corners[(int)direction];
	}

	public static Vector3 GetSecondCorner (HexDirection direction) {
		return corners[(int)direction + 1];
	}

	public static Vector3 GetFirstSolidCorner (HexDirection direction) {
		return corners[(int)direction] * solidFactor;
	}

	public static Vector3 GetSecondSolidCorner (HexDirection direction) {
		return corners[(int)direction + 1] * solidFactor;
	}

	public static Vector3 GetBridge (HexDirection direction) {
		return (corners[(int)direction] + corners[(int)direction + 1]) * blendFactor;
	}

	public static Vector3 GetSolidEdgeMiddle (HexDirection direction) {
		return(corners[(int)direction] + corners[(int)direction + 1]) * (0.5f * solidFactor);
	}

	public static Vector3 GetFirstWaterCorner (HexDirection direction) {
		return corners[(int)direction] * waterFactor;
	}

	public static Vector3 GetSecondWaterCorner (HexDirection direction) {
		return corners[(int)direction + 1] * waterFactor;
	}

	public static Vector3 GetWaterBridge (HexDirection direction) {
		return (corners[(int)direction] + corners[(int)direction + 1]) * waterBlendFactor; 
	}

	public static Vector3 TerraceLerp (Vector3 a, Vector3 b, int step) {
		// Interpolation c = (1 - t) * a + tb
		// Note that (1 - t) * a + tb = a - ta + tb = a + t(b -a)
		// Note that this will give us the position of the new point towards
		// which we should draw from the previous step. Note that the step is the index
		// of the step we currently sit at, e.g. step = 1 will correspond to he first slope
		// leading to a terrace.
		float h = step * HexMetrics.horizontalTerraceStepSize;
		a.x = a.x + (b.x - a.x) * h;
		a.z = a.z + (b.z - a.z) * h;
		// Adjust Y only on odd steps, since the other steps are terraces.
		// Trick: Only adjusting Y in od steps, using (step + 1)/2 will convert the sequence
		// 1,2,3,4 into 1,1,2,2 (i.e. it will only elevate the terrace in odd steps)
		float v = ((step + 1)/2) * HexMetrics.verticalTerraceStepSize;
		a.y = a.y + (b.y - a.y) * v;
		return a;
	}

	public static Color TerraceLerp(Color a, Color b, int step) {
		float h = step * HexMetrics.horizontalTerraceStepSize;
		return Color.Lerp(a, b, h);
	} 

	public static HexEdgeType GetEdgeType (int elevation1, int elevation2) {
		if (elevation1 == elevation2) {
			return HexEdgeType.Flat;
		}

		int delta = elevation2 - elevation1;
		if (delta == 1 || delta == -1) {
			return HexEdgeType.Slope;
		}
		return HexEdgeType.Cliff;
	}

	public static Vector4 SampleNoise(Vector3 position) {
		// Samples are produced by sampling the texture using bilinear filtering, using the X and Z 
		// world coordinates as UV coordinates. Our noise souce is @d so we ignore the third world coordinate.
		// If the noise source had been 3d, then we would have used the Y world coordinate
		return noiseSource.GetPixelBilinear(
			position.x * noiseScale, 
			position.z * noiseScale);
	}

	public static Vector3 Perturb (Vector3 position) {
		Vector4 sample = SampleNoise(position);
		position.x += (sample.x * 2f - 1f) * cellPerturbStrength;
		//position.y += (sample.y * 2f - 1f) * HexMetrics.cellPerturbStrength;
		position.z += (sample.z * 2f - 1f) * cellPerturbStrength;
		return position;
	}
}
