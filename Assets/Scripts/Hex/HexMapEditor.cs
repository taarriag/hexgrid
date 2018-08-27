using UnityEngine;
using UnityEngine.EventSystems;
using System.IO;
using System;

public class HexMapEditor : MonoBehaviour {
	private enum OptionalToggle {
		Ignore, Yes, No
	}

	public HexGrid hexGrid;
	private int activeTerrainTypeIndex;
	private int activeElevation;
	private int activeWaterLevel;
	private int brushSize;
	private bool applyElevation = true;
	private bool applyWaterLevel = true;
	private OptionalToggle riverMode, roadMode;
	private bool isDrag;
	private HexDirection dragDirection;
	private HexCell previousCell;

	public void SetApplyElevation (bool toggle) {
		applyElevation = toggle;
	}

	public void SetElevation(float elevation) {
		activeElevation = (int) elevation;
	}

	public void SetApplyWaterLevel (bool toggle) {
		applyWaterLevel = toggle;
	}

	public void SetWaterLevel (float level) {
		activeWaterLevel = (int) level;
	}

	public void SetRiverMode (int mode) {
		riverMode = (OptionalToggle) mode;
	}

	public void SetRoadMode(int mode) {
		roadMode = (OptionalToggle) mode;
	}

	public void SetBrushSize (float size) {
		brushSize = (int) size;
	}

	public void SetTerrainType (int index) {
		activeTerrainTypeIndex = index;
	}

	private void Update() {
		if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject()) {
			HandleInput();
		} else {
			previousCell = null;
		}
	}

	private void HandleInput () {
		Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
		RaycastHit hit;
		if (Physics.Raycast(inputRay, out hit)) {
			HexCell currentCell = hexGrid.GetCell(hit.point);
			if (previousCell && previousCell != currentCell) {
				ValidateDrag(currentCell);
			} else {
				isDrag = false;
			}
			EditCells(currentCell);
			previousCell = currentCell; // Previous cell of the next update.
		} else {
			previousCell = null;
		}
	}

	private void ValidateDrag (HexCell currentCell) {
		for (
			dragDirection = HexDirection.NE;
			dragDirection <= HexDirection.NW;
			dragDirection++) {
				if (previousCell.GetNeighbor(dragDirection) == currentCell) {
					isDrag = true;
					return;
				}
		}
		isDrag = false;
	}

	private void EditCells (HexCell center) {
		int centerX = center.coordinates.X;
		int centerZ = center.coordinates.Z;

		// Find the minimum z coordinate by substracting the radius.
		for (int r = 0, z = centerZ - brushSize; z <= centerZ; z++, r++) {
			// X decreases as the row number increases. (see http://catlikecoding.com/unity/tutorials/hex-map/part-5/advanced-editing/brush-diagram.png)
			for (int x  = centerX - r; x <= centerX + brushSize; x++) {
				EditCell(hexGrid.GetCell(new HexCoordinates(x,z)));
			}
		}

		for (int r = 0, z = centerZ + brushSize; z > centerZ; z--, r++) {
			for (int x = centerX - brushSize; x <= centerX + r; x++) {
				EditCell(hexGrid.GetCell(new HexCoordinates(x,z)));
			}
		}
	}

	private void EditCell (HexCell cell) {
		if (cell) {
			if (activeTerrainTypeIndex >= 0) {
				cell.TerrainTypeIndex = activeTerrainTypeIndex;
			}
			if (applyElevation) {
				cell.Elevation = activeElevation;
			}
			if (applyWaterLevel) {
				cell.WaterLevel = activeWaterLevel;
			}
			if (riverMode == OptionalToggle.No) {
				cell.RemoveRiver();
			}
			if (roadMode == OptionalToggle.No) {
				cell.RemoveRoads();
			}
			
			if (isDrag) {
				// Cell from which we come from.
				HexCell otherCell = cell.GetNeighbor(dragDirection.Opposite());
				if (otherCell) {
					if (riverMode == OptionalToggle.Yes) {
						otherCell.SetOutgoingRiver(dragDirection);
					}
					if (roadMode == OptionalToggle.Yes) {
						otherCell.AddRoad(dragDirection);
					}
				}
			}
		}
	}

	public void ShowUI(bool visible) {
		hexGrid.ShowUI(visible);
	}

	public void Save() {
		string path = Path.Combine(Application.persistentDataPath, "test.map");
		Debug.Log("Saved file to " + path);
		using(
			BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Create))
		) {
			// Write the version header.
			writer.Write(0);
			hexGrid.Save(writer);
		}
	}

	public void Load() {
		string path = Path.Combine(Application.persistentDataPath, "test.map");
		using(
			BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open))
		) {
			int header = reader.ReadInt32();
			if (header == 0) {
				hexGrid.Load(reader);
			} else {
				Debug.LogWarning("Unknown map format " + header);
			}
		}
	}
}
