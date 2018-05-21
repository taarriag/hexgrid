using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexMesh : MonoBehaviour {
	private Mesh hexMesh;
	static List<Vector3> vertices = new List<Vector3>();
	static List<Color> colors = new List<Color>();
	static List<int> triangles = new List<int>();
	
	private MeshCollider meshCollider;

	private void Awake() {
		GetComponent<MeshFilter>().mesh = hexMesh = new Mesh();
		meshCollider = gameObject.AddComponent<MeshCollider>();
		hexMesh.name = "Hex Mesh";
	}

	public void Triangulate(HexCell[] cells) {
		hexMesh.Clear();
		vertices.Clear();
		triangles.Clear();
		colors.Clear();
		for (int i = 0; i < cells.Length; i++) {
			Triangulate(cells[i]);
		}
		hexMesh.vertices = vertices.ToArray();
		hexMesh.colors = colors.ToArray();
		hexMesh.triangles = triangles.ToArray();
		hexMesh.RecalculateNormals();

		meshCollider.sharedMesh = hexMesh;
	}

	private void Triangulate(HexCell cell) {
		for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
			Triangulate(d, cell);
		}
	}

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
				TriangulateWithRiver(direction, cell, center, e);
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
			Vector3 centerL = center + HexMetrics.GetFirstSolidCorner(direction.Previous()) * 0.25f;
			Vector3 centerR = center + HexMetrics.GetSecondSolidCorner(direction.Next()) * 0.25f;

			EdgeVertices m = new EdgeVertices(
				Vector3.Lerp(centerL, e.v1, 0.5f),
				Vector3.Lerp(centerR, e.v5, 0.5f));

			m.v3.y = center.y = e.v3.y;
			TriangulateEdgeStrip(m, cell.Color, e, cell.Color);
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

	private void TriangulateEdgeFan (Vector3 center, EdgeVertices edge, Color color) {
		AddTriangle(center, edge.v1, edge.v2);
		AddTriangleColor(color);
		AddTriangle(center, edge.v2, edge.v3);
		AddTriangleColor(color);
		AddTriangle(center, edge.v3, edge.v4);
		AddTriangleColor(color);
		AddTriangle(center, edge.v4, edge.v5);
		AddTriangleColor(color);
	}

	private void TriangulateEdgeStrip (
		EdgeVertices e1, Color c1,
		EdgeVertices e2, Color c2
	) {
		AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
		AddQuadColor(c1, c2);
		AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
		AddQuadColor(c1, c2);
		AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
		AddQuadColor(c1, c2);
		AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);
		AddQuadColor(c1, c2);
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
			AddTriangle(bottom, left, right);
			AddTriangleColor(bottomCell.Color, leftCell.Color, rightCell.Color);
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

		AddTriangle(begin, v3, v4);
		AddTriangleColor(beginCell.Color, c3, c4);

		for (int i = 2; i < HexMetrics.terraceSteps; i++) {
			Vector3 v1 = v3;
			Vector3 v2 = v4;
			Color c1 = c3;
			Color c2 = c4;
			v3 = HexMetrics.TerraceLerp(begin, left, i);
			v4 = HexMetrics.TerraceLerp(begin, right, i);
			c3 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, i);
			c4 = HexMetrics.TerraceLerp(beginCell.Color, rightCell.Color, i);
			AddQuad(v1, v2, v3, v4);
			AddQuadColor(c1, c2, c3, c4);
		}

		AddQuad(v3, v4, left, right);
		AddQuadColor(c3, c4, leftCell.Color, rightCell.Color);
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
		Vector3 boundary = Vector3.Lerp(Perturb(begin), Perturb(right), b);
		Color boundaryColor = Color.Lerp(beginCell.Color, rightCell.Color, b);

		// Complete the bottom part
		TriangulateBoundaryTriangle(begin, beginCell, left, leftCell, boundary, boundaryColor);
	
		// Complete the top part.
		// If slope, triangulate the boundary triangle, extending triangles from the end of the steps to the boundary
		if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope) {
			TriangulateBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryColor);

			// Otherwise the right side is a cliff. Draw a triangle directly.
		} else {
			AddTriangleUnperturbed(Perturb(left), Perturb(right), boundary);
			AddTriangleColor(leftCell.Color, rightCell.Color, boundaryColor);
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
		Vector3 boundary = Vector3.Lerp(Perturb(begin), Perturb(left), b);
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
			AddTriangleUnperturbed(Perturb(left), Perturb(right), boundary);
			AddTriangleColor(leftCell.Color, rightCell.Color, boundaryColor);
		}
	}

	private void TriangulateBoundaryTriangle(
		Vector3 begin, HexCell beginCell,
		Vector3 left, HexCell leftCell,
		Vector3 boundary, Color boundaryColor) {

		// v2 is not used to derive any other point, perturb it immediately rather than perturbing it every time
		Vector3 v2 = Perturb(HexMetrics.TerraceLerp(begin, left, 1));
		Color c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, 1);

		// Do not perturb the boundary. This is to prevent cracks in the cliff-slope-slope and slope-cliff-slope cases.
		AddTriangleUnperturbed(Perturb(begin), v2, boundary);
		AddTriangleColor(beginCell.Color, c2, boundaryColor);
		
		for (int i = 2; i < HexMetrics.terraceSteps; i++) {
			Vector3 v1 = v2;
			Color c1 = c2;
			v2 = Perturb(HexMetrics.TerraceLerp(begin, left, i));
			c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, i);
			AddTriangleUnperturbed(v1, v2, boundary);
			AddTriangleColor(c1, c2, boundaryColor);
		}

		AddTriangleUnperturbed(v2, Perturb(left), boundary);
		AddTriangleColor(c2, leftCell.Color, boundaryColor);
	}

	private void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3) {
		int vertexIndex = vertices.Count;
		vertices.Add(Perturb(v1));
		vertices.Add(Perturb(v2));
		vertices.Add(Perturb(v3));
		triangles.Add(vertexIndex);
		triangles.Add(vertexIndex + 1);
		triangles.Add(vertexIndex + 2);
	}

	void AddTriangleUnperturbed (Vector3 v1, Vector3 v2, Vector3 v3) {
		int vertexIndex = vertices.Count;
		vertices.Add(v1);
		vertices.Add(v2);
		vertices.Add(v3);
		triangles.Add(vertexIndex);
		triangles.Add(vertexIndex + 1);
		triangles.Add(vertexIndex + 2);
	}

	private void AddTriangleColor(Color c) {
		colors.Add(c);
		colors.Add(c);
		colors.Add(c);
	}

	private void AddTriangleColor (Color c1, Color c2, Color c3) {
		colors.Add(c1);
		colors.Add(c2);
		colors.Add(c3);
	}

	private void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4) {
		int vertexIndex = vertices.Count;
		vertices.Add(Perturb(v1));
		vertices.Add(Perturb(v2));
		vertices.Add(Perturb(v3));
		vertices.Add(Perturb(v4));
		triangles.Add(vertexIndex);
		triangles.Add(vertexIndex + 2);
		triangles.Add(vertexIndex + 1);		
		triangles.Add(vertexIndex + 1);
		triangles.Add(vertexIndex + 2);
		triangles.Add(vertexIndex + 3);
	}

	private void AddQuadColor(Color c1, Color c2) {
		colors.Add(c1);
		colors.Add(c1);
		colors.Add(c2);
		colors.Add(c2);
	}

	private void AddQuadColor(Color c1, Color c2, Color c3, Color c4) {
		colors.Add(c1);
		colors.Add(c2);
		colors.Add(c3);
		colors.Add(c4);
	}

	private Vector3 Perturb (Vector3 position) {
		Vector4 sample = HexMetrics.SampleNoise(position);
		position.x += (sample.x * 2f - 1f) * HexMetrics.cellPerturbStrength;
		//position.y += (sample.y * 2f - 1f) * HexMetrics.cellPerturbStrength;
		position.z += (sample.z * 2f - 1f) * HexMetrics.cellPerturbStrength;
		return position;
	}
}
