using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class HexCell : MonoBehaviour {
  public HexCoordinates coordinates;
  int terrainTypeIndex;
  public RectTransform uiRect;
  public HexGridChunk chunk;  // Chunk this cell belongs to.
  [SerializeField]
  HexCell[] neighbors;
  [SerializeField]
  bool[] roads;
  private int elevation = int.MinValue;
  private bool hasIncomingRiver, hasOutgoingRiver;
  private HexDirection incomingRiver, outgoingRiver;

  public int Elevation {
    get {
      return elevation;
    }
    set {
      if (elevation == value) {
        return;
      }
      elevation = value;
      RefreshPosition();

      // Prevent UpHill Rivers. If the new elevation is below that of the neighbor, remove the
      // outgoing river.
      if (hasOutgoingRiver && elevation < GetNeighbor(outgoingRiver).elevation) {
        RemoveOutgoingRiver();
      }

      if (hasIncomingRiver && elevation > GetNeighbor(incomingRiver).elevation) {
        RemoveIncomingRiver();
      }

      for (int i = 0; i < roads.Length; i++) {
        if (roads[i] && GetElevationDifference((HexDirection)i) > 1) {
          SetRoad(i, false);
        }
      }
      
      Refresh();
    }
  }

  public float RiverSurfaceY {
    get {
      return (elevation + HexMetrics.riverSurfaceElevationOffset) * HexMetrics.elevationStep;
    }
  }

  public bool HasIncomingRiver {
    get {
      return hasIncomingRiver;
    }
  }

  public bool HasOutgingRiver {
    get {
      return hasOutgoingRiver;
    }
  }

  public HexDirection IncomingRiver {
    get {
      return incomingRiver;
    }
  }

  public HexDirection OutgoingRiver {
    get {
      return outgoingRiver;
    }
  }

  public bool HasRiver {
    get {
      return hasIncomingRiver || hasOutgoingRiver;
    }
  }

  public bool HasRiverBeginOrEnd {
    get {
      return hasIncomingRiver != hasOutgoingRiver;
    }
  }

  public bool HasRiverThroughEdge (HexDirection direction) {
    return hasIncomingRiver && incomingRiver == direction ||
           hasOutgoingRiver && outgoingRiver == direction;
  }

  public bool HasRoadThroughEdge (HexDirection direction) {
    return roads[(int)direction];
  }

  public bool HasRoads {
    get {
      for (int i = 0; i < roads.Length; i++) {
        if (roads[i]) {
          return true;
        }
      }
      return false;
    }
  }

// Always one level offset below the cell elevation.
  public float StreamBedY {
    get {
      return (elevation + HexMetrics.streamBedElevationOffset) * HexMetrics.elevationStep;
    }
  }

  public HexDirection RiverBeginOrEndDirection {
    get {
      return hasIncomingRiver ? incomingRiver : outgoingRiver;
    }
  }

  public Color Color {
    get {
      return HexMetrics.colors[terrainTypeIndex];
    }
  }

  public int TerrainTypeIndex {
    get {
      return terrainTypeIndex;
    }
    set {
      if (terrainTypeIndex != value) {
        terrainTypeIndex = value;
        Refresh();
      }
    }
  }

  public Vector3 Position {
    get {
      return transform.localPosition;
    }
  }

  private void RefreshPosition() {
      Vector3 position = transform.localPosition;
      position.y = elevation * HexMetrics.elevationStep;
      position.y += (HexMetrics.SampleNoise(position).y * 2f - 1f) 
                      * HexMetrics.elevationPerturbStrength;
      transform.localPosition = position;

      Vector3 uiPosition = uiRect.localPosition;
      uiPosition.z = -position.y;
      uiRect.localPosition = uiPosition;
  }

  public HexCell GetNeighbor (HexDirection direction) {
    return neighbors[(int)direction];
  }

  public void SetNeighbor(HexDirection direction, HexCell cell) {
    neighbors[(int)direction] = cell;
    cell.neighbors[(int)direction.Opposite()] = this;
  }

  public HexEdgeType GetEdgeType (HexDirection direction) {
    return HexMetrics.GetEdgeType(elevation, neighbors[(int)direction].elevation);
  }

  public HexEdgeType GetEdgeType (HexCell otherCell) {
    return HexMetrics.GetEdgeType(elevation, otherCell.elevation);
  }

  public void RemoveOutgoingRiver() {
    if (!hasOutgoingRiver) {
      return;
    }
    hasOutgoingRiver = false;
    // We call refresh self only because we don't want to refresh the neighbors.
    RefreshSelfOnly();

    HexCell neighbor = GetNeighbor(outgoingRiver);
    neighbor.hasIncomingRiver = false;
    neighbor.RefreshSelfOnly();
  }

  public void RemoveIncomingRiver () {
    if (!hasIncomingRiver) {
      return;
    }
    hasIncomingRiver = false;
    RefreshSelfOnly();

    HexCell neighbor = GetNeighbor(incomingRiver);
    neighbor.hasOutgoingRiver = false;
    neighbor.RefreshSelfOnly();
  }

  public void RemoveRiver() {
    RemoveOutgoingRiver();
    RemoveIncomingRiver();
  }

  public void SetOutgoingRiver(HexDirection direction) {
    if (hasOutgoingRiver && outgoingRiver == direction) {
      return;
    }

    HexCell neighbor = GetNeighbor(direction);
    if (!neighbor || elevation < neighbor.elevation) {
      return;
    }

    RemoveOutgoingRiver();
    if (hasIncomingRiver && incomingRiver == direction) {
      RemoveIncomingRiver();
    }

    hasOutgoingRiver = true;
    outgoingRiver = direction;

    neighbor.RemoveIncomingRiver();
    neighbor.hasIncomingRiver = true;
    neighbor.incomingRiver = direction.Opposite();
    
    SetRoad((int)direction, false);
  }

  public void AddRoad (HexDirection direction) {
    if (!roads[(int)direction] 
        && !HasRiverThroughEdge(direction)
        && GetElevationDifference(direction) <= 1) {
      SetRoad((int)direction, true);
    }
  }

  public void RemoveRoads() {
    for (int i = 0; i < neighbors.Length; i++) {
      if (roads[i]) {
        SetRoad(i, false);
      }
    }
  }

  public void SetRoad(int index, bool state) {
    roads[index] = state;
    neighbors[index].roads[(int)((HexDirection)index).Opposite()] = state;
    neighbors[index].RefreshSelfOnly();
    RefreshSelfOnly();
  }

  public int GetElevationDifference (HexDirection direction) {
    int difference = elevation - GetNeighbor(direction).elevation;
    return difference >= 0 ? difference : - difference;
  }

  private void RefreshSelfOnly() {
    chunk.Refresh();
  }

  private void Refresh() {
    if (chunk) {
      chunk.Refresh();
      for (int i = 0; i < neighbors.Length; i++) {
        HexCell neighbor = neighbors[i];
        if (neighbor != null && neighbor.chunk != chunk) {
          neighbor.chunk.Refresh();
        }
      }
    }
  }

  public void Save (BinaryWriter writer) {
    writer.Write(((byte)terrainTypeIndex));
    writer.Write((byte)elevation);
    /* writer.Write((byte)waterLevel);
		writer.Write((byte)urbanLevel);
		writer.Write((byte)farmLevel);
		writer.Write((byte)plantLevel);
		writer.Write((byte)specialIndex);
    write.Write(walled); */

    /*writer.Write(hasIncomingRiver);
    writer.Write((byte)incomingRiver);
    writer.Write(hasOutgoingRiver);
    writer.Write((byte)outgoingRiver);*/
    // Instead of two bytes per river, we can store everything in 1 byte:
    // Lower bits 000 through 101 store the direction, highest bit stores whether
    // there is a river or not.
    
    if (hasIncomingRiver) {
      writer.Write((byte)(incomingRiver + 128));
    } else {
      writer.Write((byte)0);
    }

    if (hasOutgoingRiver) {
      writer.Write((byte)(outgoingRiver + 128));
    } else {
      writer.Write((byte)0);
    }

    // Store the road flags in one byte.
    int roadFlags = 0;
    for (int i = 0; i < roads.Length; i++) {
      //writer.Write(roads[i]);
      if (roads[i]) {
        roadFlags |= 1 << i;
      }
    }
    writer.Write((byte)roadFlags);
  }

  public void Load (BinaryReader reader) {
    terrainTypeIndex = reader.ReadByte();
    elevation = reader.ReadByte();
    RefreshPosition();
    /* waterLevel = reader.ReadByte();
		urbanLevel = reader.ReadByte();
		farmLevel = reader.ReadByte();
		plantLevel = reader.ReadByte();
		specialIndex = reader.ReadByte();
    waller = reader.ReadByte(); */

    // hasIncomingRiver = reader.ReadBoolean();
    // incomingRiver = (HexDirection) reader.ReadByte();
    byte riverData = reader.ReadByte();
    if (riverData >= 128) {
      hasIncomingRiver = true;
      incomingRiver = (HexDirection)(riverData - 128);
    } else {
      hasIncomingRiver = false;
    }

    riverData = reader.ReadByte();
    if (riverData >= 128) {
      hasOutgoingRiver = true;
      outgoingRiver = (HexDirection)(riverData - 128);
    } else {
      hasOutgoingRiver = false;
    }

    int roadFlags = reader.ReadByte();
    for (int i = 0; i < roads.Length; i++) {
      roads[i] = (roadFlags & (1 << i)) != 0;
    }
  }
}
