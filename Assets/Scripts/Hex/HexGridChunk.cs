using UnityEngine;

public class HexGridChunk : MonoBehaviour {

  private HexCell[] cells;
  private HexMesh hexMesh;
  private Canvas gridCanvas;

  void Awake() {
    gridCanvas = GetComponentInChildren<Canvas>();
    hexMesh = GetComponentInChildren<HexMesh>();
    cells = new HexCell[HexMetrics.chunkSizeX * HexMetrics.chunkSizeZ];
    ShowUI(false);
  }

  public void AddCell (int index, HexCell cell) {
    cells[index] = cell;
    cell.chunk = this;
    cell.transform.SetParent(transform, false);
    cell.uiRect.SetParent(gridCanvas.transform, false);
  }

  /// <summary>
  /// Call this after updating the entire chunk.
  /// </summary>
  public void Refresh() {
    enabled = true;
  }

  // Triangulation happens after editing is finished for the current frame.
  void LateUpdate() {
    // Since the component is enabled by default, this will be called after the first frame.
    hexMesh.Triangulate(cells);
    enabled = false;
  }

  public void ShowUI (bool visible) {
    gridCanvas.gameObject.SetActive(visible);
  }
}
