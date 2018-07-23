using UnityEngine;

public class HexGridChunk : MonoBehaviour {

  private HexCell[] cells;
  public HexMesh terrain, rivers;
  private Canvas gridCanvas;

  void Awake() {
    gridCanvas = GetComponentInChildren<Canvas>();
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
    Triangulate();
    enabled = false;
  }

  public void Triangulate() {
    terrain.Clear();
		rivers.Clear();
    for (int i = 0; i < cells.Length; i++) {
      Triangulate(cells[i]);
    }
    terrain.Apply();
		rivers.Apply();	
  }

  public void ShowUI (bool visible) {
    gridCanvas.gameObject.SetActive(visible);
  }

	private void Triangulate(HexCell cell) {
		for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
			Triangulate(d, cell);
		}
	}

	/// <summary>
	/// Triangulates the area of the Cell that faces a given direction, including the
	/// connection with the Neighbor in said direction. 
	/// </summary>
	/// <param name="direction"></param>
	/// <param name="cell"></param>
	void Triangulate (HexDirection direction, HexCell cell) {
		Vector3 center = cell.Position;
		EdgeVertices e = new EdgeVertices(
			center + HexMetrics.GetFirstSolidCorner(direction),
	    center + HexMetrics.GetSecondSolidCorner(direction));

		// Basic hexagonal map, one triangle per direction.
		/* AddTriangle(center, v1, v2);
		AddTriangleColor(cell.color); */

		// If there is a river going through this cell, drop the middle
		// edge vertex to the stream bed's height. Remember that we also lower 
		// the other edge when triangulating a connection.
		if (cell.HasRiver) {
			if (cell.HasRiverThroughEdge(direction)) {
				e.v3.y = cell.StreamBedY;
				if (cell.HasRiverBeginOrEnd) {
					TriangulateWithRiverBeginOrEnd(direction, cell, center, e);
				} else {
					TriangulateWithRiver(direction, cell, center, e);
				}
			} else {
				TriangulateAdjacentToRiver(direction, cell, center, e);
			}
		} else {
			// Three triangles per direction to give more varied, non-strictly hexagonal shapes
			// for each cell
			TriangulateEdgeFan(center, e, cell.Color);
		}
		if (direction <= HexDirection.SE) {
			TriangulateConnection(direction, cell, e);
		}
	}

	private void TriangulateWithRiver(
		HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)  {

			Vector3 centerL, centerR;
		
			// Two cases:
			// If the cell has a river going through the opposite direction as well as the direction 
			// that we are working with, the cell holds a straight river.
			if (cell.HasRiverThroughEdge(direction.Opposite())) {

				// Stretch the center into a line. Move 25% from the center to the first corner of the previous
				// direction, and 25% from the center to the second corner of the second direction.
				// One simple way of visualizing this is having a top-down view of the HexMesh and imagining
				// a triangulation in the eastern direction.
				centerL = center + HexMetrics.GetFirstSolidCorner(direction.Previous()) * 0.25f;
				centerR = center + HexMetrics.GetSecondSolidCorner(direction.Next()) * 0.25f;

				// Otherwise, the river does not flow in a straight line, check special cases:
				// Detect sharp turns by checking whether the cell has a rive going through the next
				// or previous cell part.
				// When there is a sharp turn, align the center line with the edge between this and the adjacent part.
			} else if (cell.HasRiverThroughEdge(direction.Next())) {
				centerL = center;
				centerR = Vector3.Lerp(center, e.v5, 2f / 3f /* Increased center line width from 1/2 to 2/3 */);
			} else if (cell.HasRiverThroughEdge(direction.Previous())) {
				centerL = Vector3.Lerp(center, e.v1, 2f / 3f /* Increased center line width from 1/2 to 2/3 */);
				centerR = center;

				// 2 step rotations, these produce gently curving rivers.
				// A river is going through two directions after this one.
				// Expand the center towards the center of the next direction's edge.
			} else if (cell.HasRiverThroughEdge(direction.Next2())) {
				centerL = center;
				// Note that the edge middle point is closer that the vertex itself (it's within the inner radius)
				// which is why extending the center towards that edge will end up with a pinched section.
				// We extend the center edge towards the middle edge times innerToOuter radius to keep
				// the channel width constant. 
				centerR = center + HexMetrics.GetSolidEdgeMiddle(direction.Next()) 
															* (0.5f * HexMetrics.innerToOuter);
				// 2 step rotation, a river is going through two directions before this one.
			} else {
				centerL  = center + HexMetrics.GetSolidEdgeMiddle(direction.Previous()) 
															* (0.5f * HexMetrics.innerToOuter);
				centerR = center;
			}

			// Determine the final center by averaging the Left and Right centers
			center = Vector3.Lerp(centerL, centerR, 0.5f);

			// We lerp the second and fourth vertices using 1/6 instead of the usual 1/4 as the
			// length from the edges is 1/8 instead of 1/4. This is because the channel has a
			// the same width along the river, so the relative edge length of the outer edges
			// will be 1/6 relative to the edge's middle length. (1/8 of 3/4) 
			EdgeVertices m = new EdgeVertices(
				Vector3.Lerp(centerL, e.v1, 0.5f),
				Vector3.Lerp(centerR, e.v5, 0.5f),
				1f / 6f);	

			m.v3.y = center.y = e.v3.y;
			TriangulateEdgeStrip(m, cell.Color, e, cell.Color);

			// Add the triangles and quads that reached the center.
			terrain.AddTriangle(centerL, m.v1, m.v2);
			terrain.AddTriangleColor(cell.Color);

			terrain.AddQuad(centerL, center, m.v2, m.v3);
			terrain.AddQuadColor(cell.Color);
			terrain.AddQuad(center, centerR, m.v3, m.v4);
			terrain.AddQuadColor(cell.Color);

			terrain.AddTriangle(centerR, m.v4, m.v5);
			terrain.AddTriangleColor(cell.Color);

			TriangulateRiverQuad(centerL, centerR, m.v2, m.v4, cell.RiverSurfaceY);
			TriangulateRiverQuad(m.v2, m.v4, e.v2, e.v4, cell.RiverSurfaceY);
	}

	private void TriangulateWithRiverBeginOrEnd (
			HexDirection direction, 
			HexCell cell, 
			Vector3 center, 
			EdgeVertices e) {

		EdgeVertices m = new EdgeVertices(
			Vector3.Lerp(center, e.v1, 0.5f),
			Vector3.Lerp(center, e.v5, 0.5f));

		// To make sure that the channel doesn't become too shallow too fast, the middle
		// vertex is also set to the stream bed height, however the center vertex is not adjusted.
		m.v3.y = e.v3.y;

		TriangulateEdgeStrip(m, cell.Color, e, cell.Color);
		TriangulateEdgeFan(center, m, cell.Color);
	}

	private void TriangulateConnection(
		HexDirection direction, HexCell cell, EdgeVertices e1) {

		HexCell neighbor = cell.GetNeighbor(direction);
		if (neighbor == null) {
			return;
		}

		Vector3 bridge = HexMetrics.GetBridge(direction);
		bridge.y = neighbor.Position.y - cell.Position.y;
		EdgeVertices e2 = new EdgeVertices(
			e1.v1 + bridge,
			e1.v5 + bridge
		);

		if (cell.HasRiverThroughEdge(direction)) {
			e2.v3.y = neighbor.StreamBedY;
		}
		
		if (cell.GetEdgeType(direction) == HexEdgeType.Slope) {
			TriangulateEdgeTerraces(e1, cell, e2, neighbor);
		}
		else {
			TriangulateEdgeStrip(e1, cell.Color, e2, neighbor.Color);
		}
		
		HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
		if (direction <= HexDirection.E && nextNeighbor != null) {
			Vector3 v5 = e1.v5 + HexMetrics.GetBridge(direction.Next());
			v5.y = nextNeighbor.Position.y;

			if (cell.Elevation <= neighbor.Elevation) {
				if (cell.Elevation <= nextNeighbor.Elevation) {
					TriangulateCorner(
						e1.v5, cell, e2.v5, neighbor, v5, nextNeighbor
					);
				}
				else {
					TriangulateCorner(
						v5, nextNeighbor, e1.v5, cell, e2.v5, neighbor
					);
				}
			}
			else if (neighbor.Elevation <= nextNeighbor.Elevation) {
				TriangulateCorner(
					e2.v5, neighbor, v5, nextNeighbor, e1.v5, cell
				);
			}
			else {
				TriangulateCorner(
					v5, nextNeighbor, e1.v5, cell, e2.v5, neighbor
				);
			}
		}
	}

	private void TriangulateAdjacentToRiver(
		HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e) {

		if (cell.HasRiverThroughEdge(direction.Next())) {
			// Check whether we are on the inside of a curve.  This occurs when
			// both the previous and next direction contai a river. In this case teh center
			// will be moved towards the edge.
			if (cell.HasRiverThroughEdge(direction.Previous())) {
				center += HexMetrics.GetSolidEdgeMiddle(direction) 
										* (HexMetrics.innerToOuter * 0.5f);
				// Check whether this is a straight river, in that case move the center
				// towards the first corner.
			} else if (cell.HasRiverThroughEdge(direction.Previous2())) {
				center += HexMetrics.GetFirstSolidCorner(direction) * 0.25f;
			}
		} else if (
				cell.HasRiverThroughEdge(direction.Previous()) 
				&& cell.HasRiverThroughEdge(direction.Next2())) {
			center += HexMetrics.GetSecondSolidCorner(direction) * 0.25f;
		}
	

		EdgeVertices m = new EdgeVertices(
			Vector3.Lerp(center, e.v1, 0.5f),
			Vector3.Lerp(center, e.v5, 0.5f));

		TriangulateEdgeStrip(m, cell.Color, e, cell.Color);
		TriangulateEdgeFan(center, m, cell.Color);
	}

	private void TriangulateEdgeFan (Vector3 center, EdgeVertices edge, Color color) {
		terrain.AddTriangle(center, edge.v1, edge.v2);
		terrain.AddTriangleColor(color);
		terrain.AddTriangle(center, edge.v2, edge.v3);
		terrain.AddTriangleColor(color);
		terrain.AddTriangle(center, edge.v3, edge.v4);
		terrain.AddTriangleColor(color);
		terrain.AddTriangle(center, edge.v4, edge.v5);
		terrain.AddTriangleColor(color);
	}

	private void TriangulateEdgeStrip (
		EdgeVertices e1, Color c1,
		EdgeVertices e2, Color c2
	) {
		terrain.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
		terrain.AddQuadColor(c1, c2);
		terrain.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
		terrain.AddQuadColor(c1, c2);
		terrain.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
		terrain.AddQuadColor(c1, c2);
		terrain.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);
		terrain.AddQuadColor(c1, c2);
	}

	private void TriangulateEdgeTerraces (
		EdgeVertices begin, HexCell beginCell,
		EdgeVertices end, HexCell endCell
	) {
		EdgeVertices e2 = EdgeVertices.TerraceLerp(begin, end, 1);
		Color c2 = HexMetrics.TerraceLerp(beginCell.Color, endCell.Color, 1);

		TriangulateEdgeStrip(begin, beginCell.Color, e2, c2);

		for (int i = 2; i < HexMetrics.terraceSteps; i++) {
			EdgeVertices e1 = e2;
			Color c1 = c2;
			e2 = EdgeVertices.TerraceLerp(begin, end, i);
			c2 = HexMetrics.TerraceLerp(beginCell.Color, endCell.Color, i);
			TriangulateEdgeStrip(e1, c1, e2, c2);
		}
		TriangulateEdgeStrip(e2, c2, end, endCell.Color);
	}

	private void TriangulateCorner(
			Vector3 bottom, HexCell bottomCell,
			Vector3 left, HexCell leftCell,
			Vector3 right, HexCell rightCell
	) {

		HexEdgeType leftEdgeType = bottomCell.GetEdgeType(leftCell);
		HexEdgeType rightEdgeType = bottomCell.GetEdgeType(rightCell);

		// If both edges are slopes, we have terraces on both the left and right sides.
		// The slopes go up. Also left and right side have the same elevation, so top edge 
		// connection is flat. 

		// Note that we always have to start by adding a triangle, and then the subsequent quads
		// that make up the corner slopes, which is why we detect what type of slope we have.
		
		if (leftEdgeType == HexEdgeType.Slope) {
			// Slope-Slope-Flat (SSF)
			if (rightEdgeType == HexEdgeType.Slope) {
				TriangulateCornerTerraces(
					bottom, bottomCell, left, leftCell, right, rightCell
				);
			}

			// Slope-Flat-Slope (SFS)
			else if (rightEdgeType == HexEdgeType.Flat) {
				TriangulateCornerTerraces(
					left, leftCell, right, rightCell, bottom, bottomCell);
			}
			
			// TerraceCliff corners
			else {
				TriangulateCornerTerracesCliff(bottom, bottomCell, left, leftCell, right, rightCell);
			}
		}

		// Flat-Slope-Slope (FSS)
		else if (rightEdgeType == HexEdgeType.Slope) {
			if (leftEdgeType == HexEdgeType.Flat) {
				TriangulateCornerTerraces(
						right, rightCell, bottom, bottomCell, left, leftCell);
			}
			else {
				TriangulateCornerCliffTerraces(
						bottom, bottomCell, left, leftCell, right, rightCell);
			}
		}

		// Cliff-Cliff cases: Note that here we will be triangulating top to bottom
		else if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope) {
			// Cliff-Cliff-Slope-Right(Higher)
			if (leftCell.Elevation < rightCell.Elevation) {
				TriangulateCornerCliffTerraces(
					right, rightCell, bottom, bottomCell, left, leftCell);
			
			// Cliff-Cliff-Slope-Left(Higher)
			} else {
				TriangulateCornerTerracesCliff(
					left, leftCell, right, rightCell, bottom, bottomCell);
			}
		}
		
		// Non-edge case (FFF, CCF, CCCR, CCCL), a triangle is enough
		else {
			terrain.AddTriangle(bottom, left, right);
			terrain.AddTriangleColor(bottomCell.Color, leftCell.Color, rightCell.Color);
		}
	}

	private void TriangulateCornerTerraces(
		Vector3 begin, HexCell beginCell,
		Vector3 left, HexCell leftCell,
		Vector3 right, HexCell rightCell) {

		Vector3 v3 = HexMetrics.TerraceLerp(begin, left, 1);
		Vector3 v4 = HexMetrics.TerraceLerp(begin, right, 1);
		Color c3 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, 1);
		Color c4 = HexMetrics.TerraceLerp(beginCell.Color, rightCell.Color, 1);

		terrain.AddTriangle(begin, v3, v4);
		terrain.AddTriangleColor(beginCell.Color, c3, c4);

		for (int i = 2; i < HexMetrics.terraceSteps; i++) {
			Vector3 v1 = v3;
			Vector3 v2 = v4;
			Color c1 = c3;
			Color c2 = c4;
			v3 = HexMetrics.TerraceLerp(begin, left, i);
			v4 = HexMetrics.TerraceLerp(begin, right, i);
			c3 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, i);
			c4 = HexMetrics.TerraceLerp(beginCell.Color, rightCell.Color, i);
			terrain.AddQuad(v1, v2, v3, v4);
			terrain.AddQuadColor(c1, c2, c3, c4);
		}

		terrain.AddQuad(v3, v4, left, right);
		terrain.AddQuadColor(c3, c4, leftCell.Color, rightCell.Color);
	}

	private void TriangulateCornerTerracesCliff (
		Vector3 begin, HexCell beginCell,
		Vector3 left, HexCell leftCell,
		Vector3 right, HexCell rightCell) {
		
		// Collapsing terraces at a boundary point rather than at the right point, since this could
		// interfere with terraces on top and would also create very thin triagles with edges between the terrace
		// endpoints and the right point of the corner.

		// Boundary point will be placed one elevation level above the bottom cell.
		float b = 1f / (rightCell.Elevation - beginCell.Elevation);
		if (b < 0) {
			b = -b;
		}
		Vector3 boundary = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(right), b);
		Color boundaryColor = Color.Lerp(beginCell.Color, rightCell.Color, b);

		// Complete the bottom part
		TriangulateBoundaryTriangle(begin, beginCell, left, leftCell, boundary, boundaryColor);
	
		// Complete the top part.
		// If slope, triangulate the boundary triangle, extending triangles from the end of the steps to the boundary
		if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope) {
			TriangulateBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryColor);

			// Otherwise the right side is a cliff. Draw a triangle directly.
		} else {
			terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
			terrain.AddTriangleColor(leftCell.Color, rightCell.Color, boundaryColor);
		}
	}

	private void TriangulateCornerCliffTerraces (
		Vector3 begin, HexCell beginCell,
		Vector3 left, HexCell leftCell,
		Vector3 right, HexCell rightCell
	) {
		float b = 1f / (leftCell.Elevation - beginCell.Elevation);
		if (b < 0) {
			b = -b;
		}
		Vector3 boundary = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(left), b);
		Color boundaryColor = Color.Lerp(beginCell.Color, leftCell.Color, b);

		TriangulateBoundaryTriangle(
			right, rightCell, begin, beginCell, boundary, boundaryColor
		);

		if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope) {
			TriangulateBoundaryTriangle(
				left, leftCell, right, rightCell, boundary, boundaryColor
			);
		}
		else {
			terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
			terrain.AddTriangleColor(leftCell.Color, rightCell.Color, boundaryColor);
		}
	}

	private void TriangulateBoundaryTriangle(
		Vector3 begin, HexCell beginCell,
		Vector3 left, HexCell leftCell,
		Vector3 boundary, Color boundaryColor) {

		// v2 is not used to derive any other point, perturb it immediately rather than perturbing it every time
		Vector3 v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, 1));
		Color c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, 1);

		// Do not perturb the boundary. This is to prevent cracks in the cliff-slope-slope and slope-cliff-slope cases.
		terrain.AddTriangleUnperturbed(HexMetrics.Perturb(begin), v2, boundary);
		terrain.AddTriangleColor(beginCell.Color, c2, boundaryColor);
		
		for (int i = 2; i < HexMetrics.terraceSteps; i++) {
			Vector3 v1 = v2;
			Color c1 = c2;
			v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, i));
			c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, i);
			terrain.AddTriangleUnperturbed(v1, v2, boundary);
			terrain.AddTriangleColor(c1, c2, boundaryColor);
		}

		terrain.AddTriangleUnperturbed(v2, HexMetrics.Perturb(left), boundary);
		terrain.AddTriangleColor(c2, leftCell.Color, boundaryColor);
	}

	private void TriangulateRiverQuad(
		Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float y) {

		v1.y = v2.y = v3.y = v4.y = y;
		rivers.AddQuad(v1, v2, v3, v4);
		rivers.AddQuadUV(0f, 1f, 0f, 1f);
	}
}
