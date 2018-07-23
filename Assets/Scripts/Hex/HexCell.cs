﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexCell : MonoBehaviour {
  public HexCoordinates coordinates;
  private Color color;
  public RectTransform uiRect;
  public HexGridChunk chunk;  // Chunk this cell belongs to.
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
      Vector3 position = transform.localPosition;
      position.y = value * HexMetrics.elevationStep;
      position.y += (HexMetrics.SampleNoise(position).y * 2f - 1f) 
                      * HexMetrics.elevationPerturbStrength;
      transform.localPosition = position;

      Vector3 uiPosition = uiRect.localPosition;
      uiPosition.z = -position.y;
      uiRect.localPosition = uiPosition;
      
      // Prevent UpHill Rivers. If the new elevation is below that of the neighbor, remove the
      // outgoing river.
      if (hasOutgoingRiver && elevation < GetNeighbor(outgoingRiver).elevation) {
        RemoveOutgoingRiver();
      }

      if (hasIncomingRiver && elevation > GetNeighbor(incomingRiver).elevation) {
        RemoveIncomingRiver();
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

// Always one level offset below the cell elevation.
  public float StreamBedY {
    get {
      return (elevation + HexMetrics.streamBedElevationOffset) * HexMetrics.elevationStep;
    }
  }

  public Color Color {
    get {
      return color;
    }
    set {
      if (color == value) {
        return;
      }
      color = value;
      Refresh();
    }
  }

  public Vector3 Position {
    get {
      return transform.localPosition;
    }
  }

  [SerializeField]
  HexCell[] neighbors;

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
    RefreshSelfOnly();

    neighbor.RemoveIncomingRiver();
    neighbor.hasIncomingRiver = true;
    neighbor.incomingRiver = direction.Opposite();
    neighbor.RefreshSelfOnly();
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
}

