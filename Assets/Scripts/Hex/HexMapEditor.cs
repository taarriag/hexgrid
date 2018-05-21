﻿using UnityEngine;
using UnityEngine.EventSystems;

public class HexMapEditor : MonoBehaviour {
	private enum OptionalToggle {
		Ignore, Yes, No
	}

	public Color[] colors;
	public HexGrid hexGrid;
	private Color activeColor;
	private int activeElevation;
	private int brushSize;
	private bool applyColor = false;
	private bool applyElevation = true;
	private OptionalToggle riverMode;
	private bool isDrag;
	private HexDirection dragDirection;
	private HexCell previousCell;

	public void SetApplyElevation (bool toggle) {
		applyElevation = toggle;
	}

	public void SelectColor(int index) {
		applyColor = index >= 0;
		if (applyColor) {
			activeColor = colors[index];
		}
	}

	public void SetElevation(float elevation) {
		activeElevation = (int) elevation;
	}

	public void SetRiverMode (int mode) {
		riverMode = (OptionalToggle) mode;
	}

	public void SetBrushSize (float size) {
		brushSize = (int) size;
	}

	private void Awake() {
		SelectColor(-1);
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
			if (applyColor) {
				cell.Color = activeColor;
			}
			if (applyElevation) {
				cell.Elevation = activeElevation;
			}

			if (riverMode == OptionalToggle.No) {
				cell.RemoveRiver();
			} else if (isDrag && riverMode == OptionalToggle.Yes) {
				// Cell from which we come from.
				HexCell otherCell = cell.GetNeighbor(dragDirection.Opposite());
				if (otherCell) {
					otherCell.SetOutgoingRiver(dragDirection);
				}
			}
		}
	}

	public void ShowUI(bool visible) {
		hexGrid.ShowUI(visible);
	}
}
