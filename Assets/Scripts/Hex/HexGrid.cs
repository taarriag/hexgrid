using UnityEngine;
using UnityEngine.UI;

public class HexGrid : MonoBehaviour {
	public int chunkCountX = 4, chunkCountZ = 3;
	public Color defaultColor = Color.white;
	public HexCell cellPrefab;
	public Text cellLabelPrefab;
	public HexGridChunk chunkPrefab;
	
	public Texture2D noiseSource;

	private int cellCountX, cellCountZ;
	private HexCell[] cells;

	private HexGridChunk[] chunks;

	private void Awake() {
		HexMetrics.noiseSource = noiseSource;

		cellCountX = chunkCountX * HexMetrics.chunkSizeX;
		cellCountZ = chunkCountZ * HexMetrics.chunkSizeZ;

		CreateChunks();
		CreateCells();
	}

	private void CreateChunks() {
		chunks = new HexGridChunk[chunkCountX * chunkCountZ];

		for (int z = 0, i = 0;  z < chunkCountZ; z++) {
			for (int x = 0; x < chunkCountX; x++) {
				HexGridChunk chunk = chunks[i++] = Instantiate(chunkPrefab);
				chunk.transform.SetParent(transform);
			}
		}
	}

	private void CreateCells() {
		cells = new HexCell[cellCountX * cellCountZ];
		for (int i = 0, z = 0; z < cellCountZ; z++) {
			for (int x = 0; x < cellCountX; x++, i++) {
				CreateCell(x, z, i);
			}
		}
	}

	private void OnEnable() {
		// Static variables do not survive recompiles while in play mode, as static variables aren't serialized by unity.
		// We need to reassign the texture in OnEnable as well, as this method gets invoked after a recompile.
		HexMetrics.noiseSource = noiseSource;
	}

	public HexCell GetCell (Vector3 position) {
		position = transform.InverseTransformPoint(position);
		HexCoordinates coordinates = HexCoordinates.FromPosition(position);
		// Convert the coordinates to the appropiate array index, remembering to add the half-Z offset.
		int index = coordinates.X + coordinates.Z * cellCountX + coordinates.Z / 2;
		return cells[index];
	}

	public HexCell GetCell (HexCoordinates coordinates) {
		int z = coordinates.Z;
		if (z < 0 || z >= cellCountZ) {
			return null;
		}
		int x = coordinates.X + z / 2;
		if (x < 0 || x >= cellCountX) {
			return null;
		}
		return cells[x + z * cellCountX];
	}

	private void CreateCell(int x, int z, int i) {
		Vector3 position;
		// Distance to the next adjacent hexagon on the X axis is twice the inner radius.
		// Distance to the next adjacent hexagon on the Z axis be 1.5 times the outer radius.
		// Hexagons are not immediately on top but rather, they are offset along the X axis by the inner radius.
		// Also, we need to fill a rectangular grid rather than a rombus, which is why we undo part of the offset, 
		// depending on the row. Note that the division is the integer division.
		position.x = (x + z * 0.5f - z / 2) * (HexMetrics.innerRadius * 2f);
		position.y = 0f;
		position.z = z * (HexMetrics.outerRadius * 1.5f);

		HexCell cell = cells[i] = Instantiate<HexCell>(cellPrefab);
		//cell.transform.SetParent(transform, false);
		cell.transform.localPosition = position;
		cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
		cell.Color = defaultColor;

		// East West connections
		if (x > 0) {
			cell.SetNeighbor(HexDirection.W, cells[i - 1]);
		}

		if (z > 0) {
			// Even rows, set the south east neighbor 
			if ((z & 1) == 0) { /* Binary AND mask, ignoring everything but the first bit. */
				cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX]);
				if (x > 0) {
					cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX - 1]);
				}			
			} else {
				cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX]);
				if (x < cellCountX - 1) {
					cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX + 1]);
				}
			}
		}

		Text label = Instantiate<Text>(cellLabelPrefab);
		//label.rectTransform.SetParent(gridCanvas.transform, false);
		label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);
		label.text = cell.coordinates.ToStringOnSeparateLines();
		cell.uiRect = label.rectTransform;
		cell.Elevation = 0;	// This will perturb the cell elevation.
	
		AddCellToChunk(x, z, cell);
	}

	private void AddCellToChunk (int x, int z, HexCell cell) {
		int chunkX = x / HexMetrics.chunkSizeX;
		int chunkZ = z / HexMetrics.chunkSizeZ;
		HexGridChunk chunk = chunks[chunkX + chunkZ * chunkCountX];

		int localX = x - chunkX * HexMetrics.chunkSizeX;
		int localZ = z - chunkZ * HexMetrics.chunkSizeZ;
		chunk.AddCell (localX + localZ * HexMetrics.chunkSizeX, cell);
	}

	public void ShowUI(bool visible) {
		for (int i = 0; i < chunks.Length;i++) {
			chunks[i].ShowUI(visible);
		}
	}
}
