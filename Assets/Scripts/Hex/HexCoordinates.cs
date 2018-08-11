using UnityEngine;

[System.Serializable]
public class HexCoordinates {

	// This would show the coordinates in the editor, but as editables.
	[SerializeField]
	private int x, z;

	public int X { 
		get {
			return x;
		}
	}

	public int Y {
		get {
			return -X - Z;
		}
	}
	public int Z { 
		get {
			return z;

		}
	}

	public HexCoordinates (int x, int z) {
		this.x = x;
		this.z = z;
	}

	public static HexCoordinates FromOffsetCoordinates (int x, int z) {
		return new HexCoordinates(x - z / 2, z);
	}

	public static HexCoordinates FromPosition(Vector3 position) {
		float x = position.x / (HexMetrics.innerRadius * 2f);
		// Y is the coordinate mirror of the X coordinate.
		float y = -x;
		// This is only correct for Z = 0 though.
		// We have to offset as we move along Z. Every two rows we shift an entire unit to the left 
		float offset = position.z / (HexMetrics.outerRadius * 3f);
		x -= offset;
		y -= offset;

		int iX = Mathf.RoundToInt(x);
		int iY = Mathf.RoundToInt(y);
		int iZ = Mathf.RoundToInt(-x - y);

		// Sometimes coordinates will not add to zero.
		// This happens near the edges between hexagons.
		if (iX + iY + iZ != 0) {
			Debug.Log("rounding error! Fixing rounding. Rounded values before " + debugRoundingString(iX, iY, iZ));
			// Discard the coordinate with the largest rounding delta and reconstruct it from the other two.
			float dX = Mathf.Abs(x - iX);
			float dY = Mathf.Abs(y - iY);
			float dZ = Mathf.Abs(-x - y - iZ);

			if (dX > dY && dX > dZ) {
				iX = -iY - iZ;
			} else if (dZ > dY) {
				iZ = -iX  - iY;
			}
			Debug.Log("Rounded Values after" + debugRoundingString(iX, iY, iZ) + ". Y will be recalculated as -X - Z");
		}
		return new HexCoordinates(iX, iZ);
	}

	public override string ToString() {
		return "(" + X.ToString() + ", " + Y.ToString() + ", " + Z.ToString() + ")";
	}

	public string ToStringOnSeparateLines() {
		return X.ToString() + "\n" + Y.ToString() + "\n" + Z.ToString();
	}

	private static string debugRoundingString(int iX, int iY, int iZ) {
		return "(" + iX.ToString() + ", " + iY.ToString() + ", " + iZ.ToString() + ")";
	}
}
