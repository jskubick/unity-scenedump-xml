/*
XmlSceneDumper and associated classes, Copyright 2019 Jeff Skubick

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

This program was inspired by SceneDumper.cs, copyright December 2010 by Yossarian King.
The original code can be obtained from http://wiki.unity3d.com/index.php/SceneDumper
*/

using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Text;
using System;
using System.Reflection;
using System.Collections;

using UnityEditor;

namespace  scenedump {
	/**	This class parses the Scene's object hierarchy and generates an Xml Document to represent it. 
	 *		Its purpose is to assist with documentation-generation, and NOT actual serialization and deserialization.
	 *		While you're certainly free to extend it for that purpose, you're likely to be fighting an uphill (and largely pointless) battle.
	 */
	public class XmlSceneHierarchy {

		// If you set the traceId to the instanceID of an object you want to trace, it'll be logged (with call stack) and annotated in the XML output (via comment) whenever it's found.
		// This is mainly handy for debugging purposes.
		public static int traceId = unchecked((int)0);

		private XmlSceneDumperOptions opt;

		public int version { get; private set; }

		public XmlDocument document { get; private set; } = new XmlDocument();

		// don't log an error message if a SerializedProperty has one of these types and fails to match the observed Type.FullName.
		private HashSet<String> ignoredTypeMismatches = new HashSet<String>() { "Enum", "Generic", "ObjectReference" };

		private Dictionary<int, List<XmlElement>> referenceIndex = new Dictionary<int, List<XmlElement>>();
		private Dictionary<int, XmlElement> objectIndex = new Dictionary<int, XmlElement>();

		public XmlSceneHierarchy(XmlSceneDumperOptions options, int version) {
			this.opt = options;
			this.version = version;
		}

		/**	Creates XmlDocument from current scene */
		public void parse() {
					
			XmlElement sceneElement = document.CreateElement(opt.xmlPrefix, "Scene", opt.xmlNamespace);
			sceneElement.SetAttribute("version", $"{version}");
			document.AppendChild(sceneElement);

			if ((opt.valueAbbreviations != null) || (opt.typeAbbreviations != null)) {
				XmlElement meta = addElement(sceneElement, "meta");
				
				for (int x = 0; x < opt.valueAbbreviations.GetLength(0); x++) {
					XmlElement m = addElement(meta, "value-abbreviation");
					setAttribute(m, "before", opt.valueAbbreviations[x, 0]);
					setAttribute(m, "after", opt.valueAbbreviations[x, 1]);
				}
				for (int x = 0; x < opt.typeAbbreviations.GetLength(0); x++) {
					XmlElement m = addElement(meta, "type-abbreviation");
					setAttribute(m, "before", opt.typeAbbreviations[x, 0]);
					setAttribute(m, "after", opt.typeAbbreviations[x, 1]);
				}
			}

			GameObject[] gameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
			foreach (GameObject g in gameObjects) {
				add(sceneElement, g);
			}

			XmlElement refElement = addElement(sceneElement, "references");
			foreach(int key in referenceIndex.Keys) {
				if (objectIndex.ContainsKey(key)) {

					

					
					foreach (XmlElement child in referenceIndex[key]) {
						XmlElement targetElement = objectIndex[key];
						XmlElement referentElement = createElement("referenced-by");
						XmlElement parent = child.ParentNode as XmlElement;
						String match = (opt.xmlPrefix == null) ? "properties" : $"{opt.xmlPrefix}:properties";
						if (parent.Name.Equals(match))
							parent = parent.ParentNode as XmlElement;
						else
							Debug.Log(parent.Name);
						setAttribute(referentElement, "component-id", parent.GetAttribute("id"));
						setAttribute(referentElement, "property-name", child.GetAttribute("name"));
						targetElement.InsertBefore(referentElement, targetElement.FirstChild);

						StringBuilder path = new StringBuilder();
						

						path.Append(identifyParents(parent));
						
						path.Append($"{parent.Name}({parent.GetAttribute("type")})");
						path.Append($" •-» {child.GetAttribute("name")}");
						referentElement.InnerText = path.ToString();
	
					}
				}
			}

		}

		private String identifyParents(XmlElement e) {
			if (e == null)
				return "";

			String match = (opt.xmlPrefix == null) ? "GameObject" : $"{opt.xmlPrefix}:GameObject";
			if (e.Name.Equals(match))
				return  identifyParents(e.ParentNode as XmlElement) + $"{e.GetAttribute("name")} -» ";
			else
				return identifyParents(e.ParentNode as XmlElement);
		}

		/**	Recursively add a GameObject and its child Components & GameObjects */
		public virtual void add(XmlElement e, GameObject gameObject) {
			XmlElement ge = addElement(e, "GameObject");
			objectIndex.Add(gameObject.GetInstanceID(), ge);

			if (gameObject.GetInstanceID() == traceId) {
				Debug.Log($"found traceId = {traceId.ToString("X8")}");
				addComment(e, "executing add(XmlElement, GameObject)");
			}

			ge.SetAttribute("name", gameObject.name);
			setTagAttribute(ge, gameObject.tag);
			setAttribute(ge, "id", gameObject.GetInstanceID(), 8);
			setAttribute(ge, "layer", gameObject.layer);
			setAttribute(ge, "activeInHierarchy", gameObject.activeInHierarchy);
			setAttribute(ge, "isStatic", gameObject.isStatic);

			PrefabAssetType prefabType = PrefabUtility.GetPrefabAssetType(gameObject);
			if (prefabType != PrefabAssetType.NotAPrefab)
				setAttribute(ge, "prefab", prefabType.ToString());

			if ((gameObject?.GetComponents<Component>()?.Length ?? 0) > 0) {
				XmlElement componentsElement = createElement("components");
				foreach (Component component in gameObject.GetComponents<Component>()) {
					if (component is Transform)
						add(ge, component);
					else
						add(componentsElement, component);
				}
				if (!componentsElement.IsEmpty)
					ge.AppendChild(componentsElement);
			}

			if (gameObject.transform.childCount > 0) {
				XmlElement childrenElement = addElement(ge, "gameobjects");
				foreach (Transform child in gameObject.transform) {
					add(childrenElement, child.gameObject);
				}
			}
		}

		/**	Recursively add a Component and its children */
		private void add(XmlElement parent, Component component) {

			if (component.GetInstanceID() == traceId) {
				Debug.Log($"found traceId = {traceId.ToString("X8")}");
				addComment(parent, "is traced in add(XmlElement, Component)");
				addComponentSuperclasses(parent, component, "UnityEngine.Component");
				addComment(parent, "was traced");
			}

			// we give special treatment to Transform and RectTransform Components
			if (component is RectTransform) {
				addRectTransform(parent, component);
				return;
			}
			else if (component is Transform) {
				addTransform(parent, component);
				return;
			}

			String tagName;
			String stopAt;

			if ((opt.tagnameMonoBehaviour != null) && (component is MonoBehaviour)) {
				tagName = opt.tagnameMonoBehaviour;
				stopAt = "UnityEngine.MonoBehaviour";
			}
			
			else if ((opt.tagnameBehaviour != null) && (component is Behaviour)) {
				tagName = opt.tagnameBehaviour;
				stopAt = "UnityEngine.Behaviour";
			}
			
			else {
				tagName = "Component";
				stopAt = "UnityEngine.Component";
			}

			XmlElement e = addElement(parent, tagName);
			objectIndex.Add(component.GetInstanceID(), e);
			setAttribute(e, "type", opt.abbreviateType(component.GetType().FullName));
			setAttribute(e, "id", component.GetInstanceID(), 8);
			PrefabAssetType prefabType = PrefabUtility.GetPrefabAssetType(component);
			if (prefabType != PrefabAssetType.NotAPrefab)
				setAttribute(e, "prefab", prefabType.ToString());

			if (component is Behaviour) {
				setAttribute(e, "enabled", ((Behaviour)component).enabled);
				setAttribute(e, "isActiveAndEnabled", ((Behaviour)component).isActiveAndEnabled);
			}

			addComponentSuperclasses(e, component, stopAt);
			addInterfacesImplementedByComponent(e, component.GetType());


			if (opt.omitContainerFieldsProperties == OmitWhen.ALWAYS) {
				addProperties(e, component);
			}
			else {
				XmlElement componentsElement = addElement(e, "properties");
				addProperties(componentsElement, component);
				if (componentsElement.IsEmpty && (opt.omitContainerFieldsProperties == OmitWhen.IF_EMPTY))
					e.RemoveChild(componentsElement);
			}


		}

		public virtual void add(XmlElement parent, Transform child) {

			throw new NotImplementedException("Uh oh, something actually called add(XmlElement, Transform), so it wasn't unused after all!");
			/*
			if (child.GetInstanceID() == traceId) {
				Debug.Log($"found traceId = {traceId.ToString("X8")}");
				addComment(parent, "executing add(XmlElement, Transform)");
			}

			XmlElement e = addElement(parent, "Transform-Component");

			if ((child.name != null) && (child.name.Length > 0))
				e.SetAttribute("name", child.gameObject.name);

			setTagAttribute(e, child.gameObject.tag);

			e.SetAttribute("layer", child.gameObject.layer.ToString());

			e.SetAttribute("instance", $"0x{child.GetInstanceID().ToString("X8")}");

			PrefabAssetType prefabType = PrefabUtility.GetPrefabAssetType(child);
			if (prefabType != PrefabAssetType.NotAPrefab)
				setAttribute(e, "prefab", prefabType.ToString());


			if (child.gameObject.activeInHierarchy == false)
				e.SetAttribute("active", "false");



			foreach (Component component in child.GetComponents<Component>()) {
				add(e, component);
			}
			foreach (Transform t in child) {
				add(e, t);
			}
			*/
		}



		private void addTransform(XmlElement parent, Component transform) {

			if (transform.GetInstanceID() == traceId) {
				Debug.Log($"found traceId = {traceId.ToString("X8")}");
				addComment(parent, "executing addTransform(XmlElement, Component)");
			}


			XmlElement e = addElement(parent, "Transform");
			objectIndex.Add(transform.GetInstanceID(), e);
			setAttribute(e, "id", transform.GetInstanceID(), 8);
			
			addVector3(e, transform, "position", "position");
			addVector3(e, transform, "eulerAngles", "rotation");
			addVector3(e, transform, "localScale", "scale");
		}

		private void addRectTransform(XmlElement parent, Component transform) {
			XmlElement e = addElement(parent, "RectTransform");
			objectIndex.Add(transform.GetInstanceID(), e);
			setAttribute(e, "id", transform.GetInstanceID(), 8);

			
			addVector3(e, transform, "anchoredPosition3D", "pos");
			addVector2(e, transform, "anchorMin", "anchorMin");
			addVector2(e, transform, "anchorMax", "anchorMax");
			addVector2(e, transform, "pivot", "pivot");
			addVector3(e, transform, "eulerAngles", "rotation");
			addVector3(e, transform, "localScale", "scale");
			
		}



		// types that can be sensibly rendered as property values, child elements, or both

		private void addVector2(XmlElement parent, Component component, string propertyName, string displayName) {
			addVector2(parent, component.GetType().GetProperty(propertyName).GetValue(component), displayName);
		}

		private void addVector2(XmlElement parent, object arg, string name) {
			Vector2 value = (Vector2)arg;
			

			if (opt.includeValueStringAsProperty)
				setAttribute(parent, name, value.ToString());

			if (opt.includeValueAsDiscreteElements) {
				XmlElement t = addElement(parent, "Vector2");
				setAttribute(t, "name", name);
				addElement(t, "x", value.x);
				addElement(t, "y", value.y);
			}
		}

		private void addVector3(XmlElement parent, Component component, string propertyName, string displayName) {
			addVector3(parent, displayName, component.GetType().GetProperty(propertyName).GetValue(component));
		}

		private void addVector3(XmlElement parent, string name, object arg) {
			Vector3 value = (Vector3)arg;
			
			if (opt.includeValueStringAsProperty)
				setAttribute(parent, name, value.ToString());

			if (opt.includeValueAsDiscreteElements) {
				XmlElement e = addElement(parent, "Vector3");
				setAttribute(e, "name", name);
				addElement(e, "x", value.x.ToString());
				addElement(e, "y", value.y.ToString());
				addElement(e, "z", value.z.ToString());
			}
		}

		private void addVector4(XmlElement parent, string name, object arg) {
			Vector4 value = (Vector4)arg;
			

			if (opt.includeValueStringAsProperty) 
				setAttribute(parent, name, value.ToString());

			if (opt.includeValueAsDiscreteElements) {
				XmlElement e = addElement(parent, "Vector4");
				setAttribute(e, "name", name);
				addElement(e, "w", value.w);
				addElement(e, "x", value.x);
				addElement(e, "y", value.y);
				addElement(e, "z", value.z);
			}
		}

		private void addColor(XmlElement parent, System.Object arg, String name) {
			Color value = (Color)arg;
			

			if (opt.includeValueStringAsProperty)
			setAttribute(parent, name, value.ToString());

			if (opt.includeValueAsDiscreteElements) {
				XmlElement e = addElement(parent, "Color");
				setAttribute(e, "name", name);
				addElement(e, "r", value.r);
				addElement(e, "g", value.g);
				addElement(e, "b", value.b);
				addElement(e, "a", value.a);
			}
		}

		private void addBounds(XmlElement parent, String name, System.Object arg) {
			Bounds value = (Bounds)arg;
			XmlElement e = addElement(parent, "Bounds");
			
			addVector3(e, "center", value.center);
			addVector3(e, "extents", value.extents);
			addVector3(e, "max", value.max);
			addVector3(e, "min", value.min);
			addVector3(e, "size", value.size);
			
		}


		// types whose values are likely to be too long or complex to concisely render as a 'value' property instead of as discrete child elements

		private void addMatrix4x4(XmlElement parent, object o, String name) {

			UnityEngine.Matrix4x4 matrix = (UnityEngine.Matrix4x4)o;
			

			if (opt.includeValueStringAsProperty) {
				StringBuilder s = new StringBuilder();
				s.Append("【");

				for (int y=0; y < 4; y++) {
					s.Append($"[{matrix[y,0]}, {matrix[y,1]}, {matrix[y,2]}, {matrix[y,3]}]");
					if (y < 3)
						s.Append(" , ");
				}

				s.Append(" 】");
				setAttribute(parent, name, s.ToString());
			}

			if (opt.includeValueAsDiscreteElements) {
				XmlElement m = addElement(parent, "Matrix4x4");
				for (int x = 0; x < 4; x++) {
					XmlElement row = addElement(m, "row");
					for (int y = 0; y < 4; y++) {
						addElement(row, "col").InnerText = matrix[x, y].ToString();
					}
				}
			}
		}

		/**	determines the superclasses of a given class */
		private void getAncestry(System.Type type, List<Type> ancestors, String stopAt) {
			if (type == null)
				return;

			if (ancestors.Count > 100) {
				Debug.LogError("recursion overflow with getAncestry");
				return;
			}

			if (type.FullName.Equals("System.Object"))
				return;

			if (type.FullName.Equals(stopAt))
				return;

			ancestors.Add(type);

			if (type.BaseType.FullName.Equals("System.Object"))
				return;

			if (type.BaseType.FullName.Equals(stopAt))
				return;

			
			getAncestry(type.BaseType, ancestors, stopAt);
		}

		private void addComponentSuperclasses(XmlElement parent, Component component, String stopAt) {
			List<Type> ancestors = new List<Type>();
			getAncestry(component.GetType().BaseType, ancestors, stopAt);

			if (ancestors.Count == 0)
				return;

			XmlElement container = (opt.superclassContainerTagName == null) ? parent : addElement(parent, "extends");
			foreach (Type t in ancestors) {
				addElement(container, opt.superclassTagName, opt.abbreviateType(t.FullName));
			}
		}

		private void addInterfacesImplementedByComponent(XmlElement parent, Type type) {
			System.Type[] interfaces = type.GetInterfaces();
			if (interfaces.Length == 0)
				return;

			XmlElement container = (opt.interfaceContainerTagName == null) ? parent : addElement(parent, "implements");

			foreach (Type t in interfaces)
				addElement(container, opt.interfaceTagName, opt.abbreviateType(t.FullName));
		}





		private XmlElement addElement(XmlElement parent, String name, double value) {
			return addElement(parent, name, value.ToString());
		}

		private XmlElement addElement(XmlElement parent, String name, float value) {
			return addElement(parent, name, value.ToString());
		}

		private XmlElement addElement(XmlElement parent, String name, int value) {
			return addElement(parent, name, value.ToString());
		}

		private XmlElement addElement(XmlElement parent, String name, int value, int hexDigits) {
			return addElement(parent, name, $"0x{value.ToString($"X{hexDigits}")}");
		}

		private XmlElement addElement(XmlElement parent, String name, uint value, int hexDigits) {
			return addElement(parent, name, $"0x{value.ToString($"X{hexDigits}")}");
		}

		private void setAttribute(XmlElement element, String name, int value) {
			setAttribute(element, name, value.ToString());
		}

		private void setAttribute(XmlElement element, String name, int value, int hexDigits) {
			setAttribute(element, name, $"0x{value.ToString($"X{hexDigits}")}");
		}

		private void setAttribute(XmlElement element, String name, uint value, int hexDigits) {
			setAttribute(element, name, $"0x{value.ToString($"X{hexDigits}")}");
		}

		private XmlElement addElement(XmlElement parent, String name, bool value) {
			return addElement(parent, XmlConvert.EncodeName(name), value.ToString());
		}

		private void setAttribute(XmlElement element, String name, bool value) {
			setAttribute(element, name, value.ToString());
		}

		private XmlElement addElement(XmlElement parent, String name, String value) {
			XmlElement e = addElement(parent, name);
			e.InnerText = value ?? "«null»";
			if (e.InnerText.Equals("null"))
				e.InnerText = "«null»";
			return e;
		}

		private XmlElement addElement(XmlElement parent, String name, System.Object value) {
			return addElement(parent, name, value?.ToString());
		}

		private void setAttribute(XmlElement parent, String name, String value) {
			parent.SetAttribute(XmlConvert.EncodeName(name).Replace("_x0020_", "_"), value);
		}

		private XmlElement addElement(XmlElement parent, String name) {
			XmlElement e = createElement(name);
			parent.AppendChild(e);
			return e;
		}

		private XmlElement createElement(String name) {
			return document.CreateElement(opt.xmlPrefix, XmlConvert.EncodeName(name), opt.xmlNamespace);
		}

		private void setTagAttribute(XmlElement parent, String tag) {
			if (opt.includeUntagged || (((tag?.Length ?? 0) > 0) && (!"Untagged".Equals(tag))))
				parent.SetAttribute("tag", tag);
		}



		private XmlComment addComment(XmlElement parent, String text) {
			XmlComment e = document.CreateComment(text);
			parent.AppendChild(e);
			return e;
		}


		private void addProperties(XmlElement parent, Component component) {
			SerializedObject serializedObject = new SerializedObject(component);
			SerializedProperty property = serializedObject.GetIterator();
			//Debug.Log($"properties for {component.name}");
			if (property.NextVisible(true)) {
				do {
					if (opt.includeProperty(property)) {
						object spValue = SerializedPropertyValue.parse(property);
						XmlElement valueElement = add("property", parent, spValue?.GetType().FullName, spValue, property.name);
						setAttribute(valueElement, "sp-name", property.displayName);
						if (isSameType(property, spValue) == false)
							setAttribute(valueElement, "sp-type", opt.abbreviateType(property.propertyType.ToString()));
						if (property.propertyType == SerializedPropertyType.Enum)
							setAttribute(valueElement, "sp-enum-index", property.enumValueIndex);
						if (property.propertyType == SerializedPropertyType.ObjectReference) { 
								setAttribute(valueElement, "target-id", property.objectReferenceValue.GetInstanceID(), 8);
							addReference(property.objectReferenceValue.GetInstanceID(), valueElement);
						}
							
						//setAttribute(valueElement, "propertyPath", property.propertyPath);
					}
				} while (property.NextVisible(false));
			}			
		}

		

		private bool isSameType(SerializedProperty property, object value) {
			if ((property == null) && (value == null))
				return true;
			if (value == null)
				return false;


			if (normalizeType(property).Equals(value.GetType().FullName))
				return true;

			if (ignoredTypeMismatches.Contains(property.propertyType.ToString()))
				return false;

			Debug.LogError($"types don't match -- {normalizeType(property)} != {value.GetType().FullName}");
			return false;
		}

		private String normalizeType(SerializedProperty property) {
			if ("String".Equals(property.propertyType.ToString()))
				return "System.String";
			if ("Float".Equals(property.propertyType.ToString()))
				return "System.Single";
			if ("Integer".Equals(property.propertyType.ToString()))
				return "System.Int32";
			if ("Boolean".Equals(property.propertyType.ToString()))
				return "System.Boolean";
			if ("Color".Equals(property.propertyType.ToString()))
				return "UnityEngine.Color";
			if ("Vector2".Equals(property.propertyType.ToString()))
				return "UnityEngine.Vector2";
			if ("Vector3".Equals(property.propertyType.ToString()))
				return "UnityEngine.Vector3";
			if ("Vector4".Equals(property.propertyType.ToString()))
				return "UnityEngine.Vector4";
			if ("Rect".Equals(property.propertyType.ToString()))
				return "UnityEngine.Rect";
			if ("AnimationCurve".Equals(property.propertyType.ToString()))
				return "UnityEngine.AnimationCurve";
			if ("LayerMask".Equals(property.propertyType.ToString()))
				return "System.UInt32";
			if ("Bounds".Equals(property.propertyType.ToString()))
				return "UnityEngine.Bounds";
			return property.propertyType.ToString();
		}

		private XmlElement add(String et, XmlElement parent, String typeFullName, System.Object value, String name) {
			
			if (value == null) {
				XmlElement nullElement = addElement(parent, et, "«null»");
				setAttribute(nullElement, "name", name);
				setAttribute(nullElement, "type", opt.abbreviateType(typeFullName) ?? "«null»");
				return nullElement;
			}

			if (typeFullName == null)
				typeFullName = value.GetType().FullName;


			if (value.GetType().IsArray) {
				return addArray(parent, (Array)value, name);
			}

			XmlElement e = document.CreateElement(opt.xmlPrefix, et, opt.xmlNamespace);
			parent.AppendChild(e);
			e.SetAttribute("name", name);
			e.SetAttribute("type", opt.abbreviateType(typeFullName));

			if (value is UnityEngine.Vector2)
				addVector2(e, value, name);
			else if (value is UnityEngine.Vector3)
				addVector3(e, name, value);
			else if (value is UnityEngine.Vector4)
				addVector4(e, name, value);
			else if (value is UnityEngine.Matrix4x4)
				addMatrix4x4(e, value, name);
			else if (value is UnityEngine.Color) {
				addColor(e, value, name);
			}
			else if (value is System.UInt32)
				e.InnerText = ((System.UInt32)value).ToString("X8");
				

			else if (value is GameObject) {
				e.SetAttribute("target-name", getGameObjectName(value));
				e.SetAttribute("target-id", ($"0x{((GameObject)value).GetInstanceID().ToString("X8")}"));
			}
			else if (value is Component) {
				e.SetAttribute("target-name", getComponentName(value));
				e.SetAttribute("target-id", ($"0x{((Component)value).GetInstanceID().ToString("X8")}"));
				e.InnerText = opt.abbreviateValue(value?.ToString() ?? "«null»");
			}

			else if (value is UnityEngine.Bounds) {
				addBounds(e, name, value);
			}

			else {
				if (value != null) {
					MethodInfo mi = value.GetType().GetMethod("dumpToXml");
					if (mi != null) {
						mi.Invoke(value, new System.Object[] { e });
					}
					else {
						e.InnerText = opt.abbreviateValue(value?.ToString() ?? "«null»");
						if (e.InnerText.Equals("null"))
							e.InnerText = "«null»";
					}
				}
				else
					e.InnerText = "«null»";
			}
			return e;
		}

		public virtual String getGameObjectName(System.Object arg) {
			try {
				return ((GameObject)arg).name;
			}
			catch (UnassignedReferenceException e) {
				return ("«WARNING:unassigned»");
			}
		}

		public virtual String getComponentName(System.Object arg) {
			try {
				return ((Component)arg).name;
			}
			catch (UnassignedReferenceException e) {
				return ("«WARNING:unassigned»");
			}
		}

		public virtual XmlElement addArray(XmlElement parent, Array arg, String name) {
			if (arg == null) {
				Debug.Log("null, skipping");
				return addElement(parent, "array", "«null»");
			}
			XmlElement e = addElement(parent, "property-array");
			e.SetAttribute("name", name);
			e.SetAttribute("type", opt.abbreviateType(arg.GetType().FullName));
			setAttribute(e, "dimensions", arg.Rank);
			setAttribute(e, "length", arg.GetLength(0));

			if (arg.Rank == 1) {

				// special case for zero-length array
				if (arg.GetLength(0) == 0) {
					setAttribute(e, "size", 0);
					setAttribute(e, "isEmpty", true);
					return e;
				}

				if (arg.GetLength(0) == 1) {
					XmlElement valueElement;
					if (arg.GetValue(0).GetType().IsArray)
						valueElement = addArray(e, (Array)arg.GetValue(0), $"{name}[]");
					else
						valueElement = add("value", e, arg.GetValue(0).GetType().FullName, arg.GetValue(0), $"{name}[0]");
				
					setAttribute(valueElement, "index", 0);
					return e;
				}

				int lastElementWithSameValue = arg.GetLength(0) - 1;
				String lastElementValue = "";

				// @ToDo: find out what occasionally causes this to blow up with a null
				object o = arg.GetValue(0);
				Type t = (o == null) ? null : o.GetType();
				bool isNotArray = (t == null) ? true : ! t.IsArray;

				if (opt.compressArrays && isNotArray) { 
					lastElementValue = arg.GetValue(arg.GetLength(0) - 1)?.ToString() ?? "«null»";
					lastElementWithSameValue = arg.GetLength(0) - 1;
					for (int currentElement = lastElementWithSameValue; currentElement >= 0; currentElement--) {
						String currentElementValue = arg.GetValue(currentElement)?.ToString() ?? "«null»";
						if (currentElementValue.Equals(lastElementValue) == false) 
							break;
						else
							lastElementWithSameValue = currentElement;
						
					}
				}
				
				if (lastElementWithSameValue > 0) {
					for (int x = 0; x <= lastElementWithSameValue; x++) {
						System.Object value = arg.GetValue(x);
						// @ToDo: find out why value or GetType is occasionally null
						if (value?.GetType()?.IsArray ?? false)
							addArray(e, (Array)value, $"{name}[{x}]");
						else {
							// @ToDo: figure out what causes this to occasionally be null
							add("value", e, value?.GetType()?.FullName, value, $"{name}[{x}]");
						}
					}
				}

				if (lastElementWithSameValue < (arg.GetLength(0)-1)) {
					XmlElement element = addElement(e, "same-value", lastElementValue);
					setAttribute(element, "name", $"«{name}[{lastElementWithSameValue}]»…«{name}[{arg.GetLength(0)-1}]»");
					setAttribute(element, "type", opt.abbreviateType(arg?.GetValue(0)?.GetType()?.FullName ?? "«null»"));
					setAttribute(element, "index-from", lastElementWithSameValue);
					setAttribute(element, "index-to", arg.GetLength(0) - 1);
					
				}			
			}
			else if (arg.Rank == 2) {
				int rows = arg.GetLength(0);
				int cols = arg.GetLength(1);
				for (int row = 0; row < rows; row++) {
					XmlElement rowElement = addElement(e, "row");
					setAttribute(rowElement, "row", row);
					for (int col = 0; col < cols; col++) {
						XmlElement colElement = add("col", rowElement, arg.GetType().FullName, arg.GetValue(row, col), $"{name}[{row},{col}]");
						setAttribute(colElement, "col", col);
					}
				}
			}
			else if (arg.Rank > 2) {
				addElement(e, "(array too large to render)");
			}
			return e;
		}

		private void addTransformReference(XmlElement parent, String name, object o) {
			addElement(parent, "ToDo", "addTransformReference");
		}

		private void addReference(int targetId, XmlElement referer) {
			if (referenceIndex.ContainsKey(targetId) == false)
				referenceIndex.Add(targetId, new List<XmlElement>());
			referenceIndex[targetId].Add(referer);
		}
		
	}
}