using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(HexCoordinates))]
public class HexCoordinatesDrawer : PropertyDrawer {
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
		HexCoordinates coordinates = new HexCoordinates(
			property.FindPropertyRelative("x").intValue,
			property.FindPropertyRelative("z").intValue
		);
		// This puts the control label and also returns the remainder of the rectangle to put the control itself. 
		position = EditorGUI.PrefixLabel(position, label);
		GUI.Label(position, coordinates.ToString());
	}
}
