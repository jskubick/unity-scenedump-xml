using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

namespace scenedump {
	public class SerializedPropertyValue {

		public static object parse(SerializedProperty property) {
			switch (property.propertyType) {
				case SerializedPropertyType.Integer: return property.intValue;
				case SerializedPropertyType.Boolean: return property.boolValue;
				case SerializedPropertyType.Float: return property.floatValue;
				case SerializedPropertyType.String: return property.stringValue;
				case SerializedPropertyType.Color: return property.colorValue;
				case SerializedPropertyType.ObjectReference: return property.objectReferenceValue;
				case SerializedPropertyType.Vector2: return property.vector2Value;
				case SerializedPropertyType.Vector3: return property.vector3Value;
				case SerializedPropertyType.Vector4: return property.vector4Value;
				case SerializedPropertyType.Rect: return property.rectValue;
				case SerializedPropertyType.ArraySize: return property.arraySize;
				case SerializedPropertyType.Character: return property.stringValue;
				case SerializedPropertyType.AnimationCurve: return property.animationCurveValue;
				case SerializedPropertyType.Bounds: return property.boundsValue;
				case SerializedPropertyType.Quaternion: return property.quaternionValue;
				case SerializedPropertyType.ExposedReference: return property.exposedReferenceValue;
				case SerializedPropertyType.FixedBufferSize: return property.fixedBufferSize;
				case SerializedPropertyType.Vector2Int: return property.vector2IntValue;
				case SerializedPropertyType.Vector3Int: return property.vector3IntValue;
				case SerializedPropertyType.RectInt: return property.rectIntValue;
				case SerializedPropertyType.BoundsInt: return property.boundsIntValue;

				case SerializedPropertyType.LayerMask: return (uint)property.longValue;

				case SerializedPropertyType.Enum: 
					if (property.enumValueIndex < 0) {
						return $"«error[enumValueIndex {property.enumValueIndex} is negative]»";
					}

					if (property.enumValueIndex > (property.enumDisplayNames.Length - 1))
						return $"«error[enumValueIndex={property.enumValueIndex} greater than size of enum ({property.enumDisplayNames.Length})]»";
					return property.enumDisplayNames[property.enumValueIndex];
				/*
					String[] names = property.enumDisplayNames;
					StringBuilder s = new StringBuilder($"{property.enumValueIndex} : ");
					foreach (String name in names)
						s.Append($"{name},");
					return s.ToString().Substring(0, s.ToString().Length - 1);
				*/
				case SerializedPropertyType.Generic:
					if (property.isArray) {
						var array = new object[property.arraySize];
						for (int x = 0; x < array.Length; x++) {
							array[x] = parse(property.GetArrayElementAtIndex(x));
						}
						return array;
					}
					break;
			}
			
			return new UnknownType($"«@ToDo:unknown-type[{property.propertyType.ToString()}]»");
		}
	}
}
