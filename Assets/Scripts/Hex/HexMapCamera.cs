﻿using UnityEngine;

public class HexMapCamera : MonoBehaviour {
	public float stickMinZoom = -250f, stickMaxZoom = -45;
	public float swivelMinZoom = 90, swivelMaxZoom = 45;
	public float moveSpeedMinZoom = 400, moveSpeedMaxZoom = 100;
	public float rotationSpeed = 180;
	public HexGrid grid;

	private float zoom = 1f;
	private Transform swivel, stick;
	private float rotationAngle;

	protected void Awake() {
		swivel = transform.GetChild(0);
		stick = swivel.GetChild(0);
	}

	protected void Update () {
		float zoomDelta = Input.GetAxis("Mouse ScrollWheel");
		if (zoomDelta != 0f) {
			AdjustZoom(zoomDelta);
		}

		float rotationDelta = Input.GetAxis("Rotation");
		if (rotationDelta != 0f) {
			AdjustRotation(rotationDelta);
		}

		float xDelta = Input.GetAxis("Horizontal");
		float yDelta = Input.GetAxis("Vertical");
		if (xDelta != 0 || yDelta != 0) {
			AdjustPosition(xDelta, yDelta);
		}
	}

	private void AdjustZoom(float delta) {
		zoom = Mathf.Clamp01(zoom + delta);
		float distance = Mathf.Lerp(stickMinZoom, stickMaxZoom, zoom);
		stick.localPosition = new Vector3(0f, 0f, distance);

		float angle = Mathf.Lerp(swivelMinZoom, swivelMaxZoom, zoom);
		swivel.localRotation = Quaternion.Euler(angle, 0f, 0f);
	}

	private void AdjustRotation(float delta) {
		rotationAngle += delta * rotationSpeed * Time.deltaTime;
		if (rotationAngle < 0f) {
			rotationAngle += 360f;
		} else if (rotationAngle >= 360f) {
			rotationAngle -= 360f;
		}
		transform.localRotation = Quaternion.Euler(0f, rotationAngle, 0f);
	}

	private void AdjustPosition(float xDelta, float zDelta) {
		// When rotating 180 degress, the movement will now follow the direction one expects, 
		// which is forward following the current direction
		Vector3 direction = transform.localRotation * new Vector3(xDelta, 0f, zDelta).normalized;

		// Since we are normalizing the displacement, the camera moves at maximum velocity all the time when 
		// After releasing the keys. The reason is that there is a delay applied when going between extremes values
		// even with the keys. It takes a while before the axes return to 0.

		// One option would be getting rid of the delays, but since they feel smooth we 
		// apply the most extreme axis value as a damping factor to the movement.
		float damping = Mathf.Max(Mathf.Abs(xDelta), Mathf.Abs(zDelta)); 

		float distance = Mathf.Lerp(moveSpeedMinZoom, moveSpeedMaxZoom, zoom) 
										* damping * Time.deltaTime;
		Vector3 position = transform.localPosition;
		position += direction * distance;
		transform.localPosition = ClampPosition(position);
	}

	private Vector3 ClampPosition(Vector3 position) {
		// Note: The origin lies at the center of a cell. The camera should stop at
		// the center of the rightmost cell. To do so, substract half a cell from the xMax.
		float xMax = (grid.chunkCountX * HexMetrics.chunkSizeX - 0.5f) * (2f * HexMetrics.innerRadius);
		position.x = Mathf.Clamp(position.x, 0f, xMax);

		float zMax = (grid.chunkCountZ * HexMetrics.chunkSizeZ - 1) * (1.5f * HexMetrics.outerRadius);
		position.z = Mathf.Clamp(position.z, 0f, zMax);
		return position;
	}
}
