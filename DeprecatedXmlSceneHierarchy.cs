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

namespace scenedump {
	public class DeprecatedXmlSceneHierarchy {

		// If you set the traceId to the instanceID of an object you want to trace, it'll be logged (with call stack) and annotated in the XML output (via comment) whenever it's found.
		// This is mainly handy for debugging purposes.
		public static int traceId = unchecked((int)0);

		private XmlSceneDumperOptions opt;

		public int version { get; private set; }

		public XmlDocument document { get; private set; } = new XmlDocument();

		public DeprecatedXmlSceneHierarchy(XmlSceneDumperOptions options, int version) {
			this.opt = options;
			this.version = version;
			Debug.Log($"traceId = {traceId}");
		}

		/**	Creates XmlDocument from current scene */
		public void parse() {

			XmlElement sceneElement = document.CreateElement(opt.xmlPrefix, "Scene", opt.xmlNamespace);
			sceneElement.SetAttribute("version", $"{version}");
			document.AppendChild(sceneElement);

			XmlElement metaElement = addElement(sceneElement, "meta");
			if (opt.valueAbbreviations != null) {
				for (int x = 0; x < opt.valueAbbreviations.GetLength(0); x++) {
					XmlElement abbreviation = addElement(metaElement, "abbreviation");
					abbreviation.SetAttribute("before", opt.valueAbbreviations[x, 0]);
					abbreviation.SetAttribute("after", opt.valueAbbreviations[x, 1]);
				}
			}
			addElement(metaElement, "MajorVersion", "1");
			addElement(metaElement, "MinorVersion", "0");
			addElement(metaElement, "showTransformValuesAsGameObjectAttributes", opt.showTransformValuesAsGameObjectAttributes.ToString());
			addElement(metaElement, "useCompactValues", opt.useCompactValues.ToString());

			if (opt.useCompactValues) {
				XmlElement inheritance = addElement(metaElement, "inheritance");
				if (opt.superclassContainerTagName != null)
					inheritance.SetAttribute("superclassContainer", opt.superclassContainerTagName);
				inheritance.SetAttribute("extendsTag", opt.superclassTagName);
				inheritance.SetAttribute("maxInlineSuperclasses", opt.superclassInlineMax.ToString());
				if (opt.superclassSeparator != null)
					inheritance.SetAttribute("superclass-separator", opt.superclassSeparator);

				if (opt.interfaceContainerTagName != null)
					inheritance.SetAttribute("interfaceContainer", opt.interfaceContainerTagName);
				inheritance.SetAttribute("implementsTag", opt.interfaceTagName);
				inheritance.SetAttribute("maxInlineInterfaces", opt.interfaceInlineMax.ToString());
				if (opt.interfaceSeparator != null)
					inheritance.SetAttribute("interface-separator", opt.interfaceSeparator);
			}
			else {
				if (opt.superclassContainerTagName != null)
					addElement(metaElement, "superclassContainerTagName", opt.superclassContainerTagName);
				if (opt.superclassTagName != null)
					addElement(metaElement, "superclassTagName", opt.superclassTagName);
				if (opt.superclassSeparator != null)
					addElement(metaElement, "superclassSeparatorString", opt.superclassSeparator);
				addElement(metaElement, "superclassInlineMax", opt.superclassInlineMax);
				if (opt.interfaceContainerTagName != null)
					addElement(metaElement, "interfaceContainerTagName", opt.interfaceContainerTagName);
				if (opt.interfaceTagName != null)
					addElement(metaElement, "interfaceTagName", opt.interfaceTagName);
				addElement(metaElement, "interfaceInlineMax", opt.interfaceInlineMax);
			}


			GameObject[] gameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
			foreach (GameObject g in gameObjects) {
				add(sceneElement, g);
			}
		}

		/**	Creates XmlDocument with selected Scenes */
		public void parse(Selection selection) {
			if ((Selection.gameObjects == null) || (Selection.gameObjects.Length == 0)) {
				throw new System.Exception("Please select the object(s) you wish to dump");
			}
			throw new System.NotImplementedException("@ToDo: XmlDocument.create(Selection)");
		}


		public virtual void add(XmlElement e, GameObject gameObject) {
			XmlElement gameObjectElement = addElement(e, "GameObject");

			if (gameObject.GetInstanceID() == traceId) {
				Debug.Log($"found traceId = {traceId.ToString("X8")}");
				addComment(e, "executing add(XmlElement, GameObject)");
			}

			// set name
			if ((gameObject.name != null) && (gameObject.name.Length > 0))
				gameObjectElement.SetAttribute("name", gameObject.name);

			// set tag
			setTagAttribute(gameObjectElement, gameObject.tag);

			// set layer	
			gameObjectElement.SetAttribute("layer", gameObject.layer.ToString());

			string idName = opt.showTransformValuesAsGameObjectAttributes ? "gameobject-id" : "id";
			gameObjectElement.SetAttribute(idName, $"0x{gameObject.GetInstanceID().ToString("X8")}");

			if (gameObject.activeInHierarchy == false)
				gameObjectElement.SetAttribute("active", "false");

			if (gameObject.isStatic)
				gameObjectElement.SetAttribute("isStatic", "true");

			XmlElement componentsElement = document.CreateElement(opt.xmlPrefix, "components", opt.xmlNamespace);
			foreach (Component component in gameObject.GetComponents<Component>()) {
				if (component is Transform)
					add(gameObjectElement, component);
				else
					add(componentsElement, component);
			}
			if (componentsElement.IsEmpty == false)
				gameObjectElement.AppendChild(componentsElement);

			foreach (Transform child in gameObject.transform) {
				add(gameObjectElement, child.gameObject);
			}

		}


		public void add(XmlElement parent, Component component) {

			if (component.GetInstanceID() == traceId) {
				Debug.Log($"found traceId = {traceId.ToString("X8")}");
				addComment(parent, "is traced in add(XmlElement, Component)");
				addComponentSuperclasses(parent, component);
				addComment(parent, "was traced");
			}

			if (component is RectTransform) {
				addRectTransform(parent, component);
				return;
			}
			else if (component is Transform) {
				addTransform(parent, component);
				return;
			}


			string elementName = "Component";
			if (component is UnityEngine.MonoBehaviour) {
				elementName = "MonoBehaviour";
			}
			else if (component is UnityEngine.Behaviour) {
				elementName = "Behaviour";
			}


			XmlElement e = document.CreateElement(opt.xmlPrefix, elementName, opt.xmlNamespace);
			//e.SetAttribute("name", component.GetType().Name);
			e.SetAttribute("type", opt.abbreviateValue(component.GetType().FullName));
			parent.AppendChild(e);

			e.SetAttribute("id", $"0x{component.GetInstanceID().ToString("X8")}");

			if ((component is UnityEngine.Behaviour) && (((UnityEngine.Behaviour)component).enabled == false))
				e.SetAttribute("enabled", "false");

			addComponentSuperclasses(e, component);
			addInterfacesImplementedByComponent(e, component.GetType());

			XmlElement componentsElement = addElement(e, "inspector-values");
			addFields(componentsElement, component);
			//if (opt.includeNonunityProperties || (component.GetType().FullName.StartsWith("UnityEngine") && opt.includeUnityProperties))
			addProperties(componentsElement, component);
			if (componentsElement.IsEmpty)
				e.RemoveChild(componentsElement);
		}







		public virtual void add(XmlElement parent, Transform child) {

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

			if (child.gameObject.activeInHierarchy == false)
				e.SetAttribute("active", "false");



			foreach (Component component in child.GetComponents<Component>()) {
				add(e, component);
			}
			foreach (Transform t in child) {
				add(e, t);
			}
		}

		public void addTransform(XmlElement parent, String name, object transform) {

			Transform value = (Transform)transform;

			if (value.GetInstanceID() == traceId) {
				Debug.Log($"found traceId = {traceId.ToString("X8")}");
				addComment(parent, "executing addTransform(XmlElement, String, object)");
			}

			XmlElement e = document.CreateElement(opt.xmlPrefix, "Transform", opt.xmlNamespace);
			parent.AppendChild(e);

			addVector3(e, value, "position", "position");
			addVector3(e, value, "eulerAngles", "rotation");
			addVector3(e, value, "localScale", "scale");
		}

		public void addTransform(XmlElement parent, Component transform) {
			XmlElement e = parent;
			if (!opt.showTransformValuesAsGameObjectAttributes) {
				e = document.CreateElement(opt.xmlPrefix, "Transform", opt.xmlNamespace);
				parent.AppendChild(e);
			}

			if (transform.GetInstanceID() == traceId) {
				Debug.Log($"found traceId = {traceId.ToString("X8")}");
				addComment(parent, "executing addTransform(XmlElement, Component)");
			}

			e.SetAttribute("name", transform.name);
			setTagAttribute(e, transform.tag);

			string idName = opt.showTransformValuesAsGameObjectAttributes ? "transform-id" : "id";
			e.SetAttribute(idName, $"0x{transform.GetInstanceID().ToString("X8")}");


			addVector3(e, transform, "position", "position");
			addVector3(e, transform, "eulerAngles", "rotation");
			addVector3(e, transform, "localScale", "scale");
		}

		public void addRectTransform(XmlElement parent, Component transform) {
			XmlElement e = parent;
			if (!opt.showTransformValuesAsGameObjectAttributes) {
				e = document.CreateElement(opt.xmlPrefix, "RectTransform", opt.xmlNamespace);
				parent.AppendChild(e);
			}

			addVector3(e, transform, "anchoredPosition3D", "pos");
			addVector2(e, transform, "anchorMin", "anchorMin");
			addVector2(e, transform, "anchorMax", "anchorMax");
			addVector2(e, transform, "pivot", "pivot");
			addVector3(e, transform, "eulerAngles", "rotation");
			addVector3(e, transform, "localScale", "scale");
		}

		public void addVector2(XmlElement parent, Component component, string propertyName, string displayName) {
			Vector2 v = (Vector2)component.GetType().GetProperty(propertyName).GetValue(component);
			if (opt.showTransformValuesAsGameObjectAttributes) {
				parent.SetAttribute(displayName, v.ToString());
				return;
			}

			XmlElement t = document.CreateElement(opt.xmlPrefix, "Vector2", opt.xmlNamespace);
			t.SetAttribute("name", displayName);
			parent.AppendChild(t);

			XmlElement x = document.CreateElement(opt.xmlPrefix, "x", opt.xmlNamespace);
			t.AppendChild(x);
			x.InnerText = v.x.ToString();

			XmlElement y = document.CreateElement(opt.xmlPrefix, "y", opt.xmlNamespace);
			y.InnerText = v.y.ToString();
			t.AppendChild(y);
		}

		public void addVector2(XmlElement parent, object arg, string name) {
			Vector2 value = (Vector2)arg;
			// @ToDo

			XmlElement e = document.CreateElement(opt.xmlPrefix, "Vector2", opt.xmlNamespace);
			addElement(e, "x", value.x);
			addElement(e, "y", value.y);
		}

		public void addVector3(XmlElement parent, Component component, string propertyName, string displayName) {

			Vector3 value = (Vector3)component.GetType().GetProperty(propertyName).GetValue(component);
			if (opt.showTransformValuesAsGameObjectAttributes) {
				parent.SetAttribute(displayName, value.ToString());
				return;
			}

			XmlElement e = opt.useCompactValues ? parent : addElement(parent, "Vector3");

			e.SetAttribute("name", displayName);

			if (opt.useCompactValues) {
				e.SetAttribute(displayName, value.ToString());
				return;
			}

			XmlElement x = document.CreateElement(opt.xmlPrefix, "x", opt.xmlNamespace);
			x.InnerText = value.x.ToString();
			XmlElement y = document.CreateElement(opt.xmlPrefix, "y", opt.xmlNamespace);
			y.InnerText = value.y.ToString();
			XmlElement z = document.CreateElement(opt.xmlPrefix, "z", opt.xmlNamespace);
			z.InnerText = value.z.ToString();
			e.AppendChild(x);
			e.AppendChild(y);
			e.AppendChild(z);
		}

		public void addVector3(XmlElement parent, string name, object arg) {
			Vector3 value = (Vector3)arg;
			// @ToDo
			if (opt.useCompactValues) {
				parent.SetAttribute(name, value.ToString());
				return;
			}
			XmlElement e = document.CreateElement(opt.xmlPrefix, "Vector3", opt.xmlNamespace);
			e.SetAttribute("name", name);
			parent.AppendChild(e);
			addElement(e, "x", value.x);
			addElement(e, "y", value.y);
			addElement(e, "z", value.z);
		}

		public void addVector4(XmlElement parent, string name, object arg) {
			Vector4 value = (Vector4)arg;
			if (opt.useCompactValues) {
				parent.SetAttribute(name, value.ToString());
				return;
			}

			XmlElement e = document.CreateElement(opt.xmlPrefix, "Vector4", opt.xmlNamespace);
			parent.AppendChild(e);
			e.SetAttribute("name", name);

			addElement(e, "w", value.w);
			addElement(e, "x", value.x);
			addElement(e, "y", value.y);
			addElement(e, "z", value.z);
		}

		public void addColor(XmlElement parent, System.Object arg, String name) {

			Color value = (Color)arg;
			if (opt.useCompactValues) {
				parent.SetAttribute("color", value.ToString());
				return;
			}

			XmlElement e = document.CreateElement(opt.xmlPrefix, "Color", opt.xmlNamespace);
			parent.AppendChild(e);

			addElement(e, "r", value.r);
			addElement(e, "g", value.g);
			addElement(e, "b", value.b);
			addElement(e, "a", value.a);
		}

		public void addBounds(XmlElement parent, String name, System.Object arg) {
			Bounds value = (Bounds)arg;

			if (opt.useCompactValues) {
				parent.SetAttribute(name, value.ToString());
				return;
			}

			XmlElement e = document.CreateElement(opt.xmlPrefix, "Bounds", opt.xmlNamespace);
			parent.AppendChild(e);

			addVector3(e, "center", value.center);
			addVector3(e, "extents", value.extents);
			addVector3(e, "max", value.max);
			addVector3(e, "min", value.min);
			addVector3(e, "size", value.size);
		}

		public void addMatrix4x4(XmlElement parent, object o, String name) {

			UnityEngine.Matrix4x4 matrix = (UnityEngine.Matrix4x4)o;

			if (opt.useCompactValues) {

				StringBuilder s = new StringBuilder();
				for (int y = 0; y < 4; y++) {
					if (y > 0)
						s.Append("\n");
					s.Append("[");
					for (int x = 0; x < 4; x++) {
						if (x > 0)
							s.Append(",");
						s.Append(matrix[x, y]);
					}
					s.Append("]");
				}
				parent.InnerText = opt.abbreviateValue(s.ToString());
				return;

			}
			XmlElement m = document.CreateElement(opt.xmlPrefix, "Matrix4x4", opt.xmlNamespace);
			parent.AppendChild(m);
			for (int x = 0; x < 4; x++) {
				XmlElement row = document.CreateElement(opt.xmlPrefix, "row", opt.xmlNamespace);
				m.AppendChild(row);
				for (int y = 0; y < 4; y++) {
					XmlElement col = document.CreateElement(opt.xmlPrefix, "col", opt.xmlNamespace);
					row.AppendChild(col);
					col.InnerText = matrix[x, y].ToString();
				}
			}
		}



		public void getAncestry(System.Type type, List<Type> ancestors) {
			if (type == null)
				return;

			if (type.BaseType == null)
				return;

			if (ancestors.Count > 100) {
				Debug.LogError("recursion overflow with getAncestry");
				return;
			}

			if (opt.isObviousSuperclass(type.BaseType))
				return;

			ancestors.Add(type.BaseType);
			getAncestry(type.BaseType, ancestors);
		}

		public void addComponentSuperclasses(XmlElement parent, Component component) {
			List<Type> ancestors = new List<Type>();
			getAncestry(component.GetType().BaseType, ancestors);

			if (ancestors.Count == 0)
				return;

			if ((opt.superclassSeparator != null) && (opt.superclassSeparator.Length > 0)) {
				StringBuilder s = new StringBuilder();
				foreach (Type t in ancestors) {
					if (s.Length == 0)
						s.Append(t.FullName);
					else
						s.Append($"{opt.superclassSeparator}{t.FullName}");
				}

				if (ancestors.Count <= opt.superclassInlineMax)
					parent.SetAttribute("extends", opt.abbreviateValue(s.ToString()));
				else
					addElement(parent, opt.superclassTagName, opt.abbreviateValue(s.ToString()));
				return;
			}

			XmlElement container = (opt.superclassContainerTagName != null) ? addElement(parent, opt.superclassContainerTagName) : parent;
			foreach (Type t in ancestors) {
				addElement(container, opt.superclassTagName, t.FullName);
			}
		}

		public void addInterfacesImplementedByComponent(XmlElement parent, Type type) {
			System.Type[] interfaces = type.GetInterfaces();
			if (interfaces.Length == 0)
				return;

			if ((opt.interfaceSeparator != null) && (opt.interfaceSeparator.Length > 0)) {
				StringBuilder s = new StringBuilder();
				foreach (System.Type interfaceType in interfaces) {
					if (s.Length == 0)
						s.Append(interfaceType.FullName);
					else
						s.Append($"{opt.interfaceSeparator}{interfaceType.FullName}");
				}

				if (interfaces.Length <= opt.interfaceInlineMax)
					parent.SetAttribute("implements", opt.abbreviateValue(s.ToString()));
				else
					addElement(parent, opt.interfaceTagName, opt.abbreviateValue(s.ToString()));
				return;
			}

			XmlElement container = (opt.interfaceContainerTagName != null) ? addElement(parent, opt.interfaceContainerTagName) : parent;

			foreach (Type t in interfaces)
				addElement(container, opt.interfaceTagName, t.FullName);
		}



		private void setTagAttribute(XmlElement element, String tag) {
			if ((tag == null) || (tag.Length == 0)) {
				if (opt.includeUntagged)
					element.SetAttribute("tag", "");
				return;
			}

			if (tag.Equals("Untagged")) {
				if (opt.includeUntagged)
					element.SetAttribute("tag", "Untagged");
				return;
			}

			element.SetAttribute("tag", tag);
		}

		public XmlElement addElement(XmlElement parent, String name, double value) {
			return addElement(parent, name, value.ToString());
		}

		public XmlElement addElement(XmlElement parent, String name, float value) {
			return addElement(parent, name, value.ToString());
		}

		public XmlElement addElement(XmlElement parent, String name, int value) {
			return addElement(parent, name, value.ToString());
		}

		public XmlElement addElement(XmlElement parent, String name, String value) {
			XmlElement e = addElement(parent, name);
			e.InnerText = value;
			return e;
		}

		public XmlElement addElement(XmlElement parent, String name) {
			XmlElement e = document.CreateElement(opt.xmlPrefix, name, opt.xmlNamespace);
			parent.AppendChild(e);
			return e;
		}

		public XmlComment addComment(XmlElement parent, String text) {
			XmlComment e = document.CreateComment(text);
			parent.AppendChild(e);
			return e;
		}

		public void addFields(XmlElement parent, Component component) {
			foreach (FieldInfo fi in component.GetType().GetFields()) {
				System.Object value = fi.GetValue((System.Object)component);
				add("field", parent, fi.FieldType, value, fi.Name);
			}
		}



		public void addProperties(XmlElement parent, Component component) {
			foreach (PropertyInfo pi in component.GetType().GetProperties()) {
				try {
					//if ((!opt.getIgnoredProperties().Contains(pi.Name)) && (opt.includeNonpublicProperties || pi.CanWrite)) {
						object value = pi.GetValue(component);
						add("property", parent, pi.PropertyType, value, pi.Name);
					//}
				}
				catch (System.Exception e) {
					Debug.LogError($"propertyType = {pi.PropertyType.FullName}, name={pi.Name}, {e.ToString()}");
				}
			}
		}

		private void add(String et, XmlElement parent, System.Type type, System.Object value, String name) {

			XmlElement e;
			if (opt.terseValues) {
				if ((value is System.Object[])) {
					e = addElement(parent, "array");

					e.SetAttribute("name", name);
					e.SetAttribute("type", type.FullName);
					addArray(e, value, name);
					return;
				}
				if (value == null) {
					e = addElement(parent, XmlConvert.EncodeName(opt.abbreviateValue(type.FullName)));
					e.SetAttribute("name", name);
					e.SetAttribute("value", "null");
					return;
				}

				e = addElement(parent, XmlConvert.EncodeName(opt.abbreviateValue(type.FullName)));
				e.SetAttribute("name", name);
				e.SetAttribute("value", opt.abbreviateValue(value.ToString()));
				return;
			}

			e = document.CreateElement(opt.xmlPrefix, et, opt.xmlNamespace);
			parent.AppendChild(e);
			e.SetAttribute("name", name);
			e.SetAttribute("type", opt.abbreviateValue(type.FullName));

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
			else if (value is UnityEngine.Component) {
				e.SetAttribute("target-name", ((Component)value).name);
				e.SetAttribute("target-id", ($"0x{((Component)value).GetInstanceID().ToString("X8")}"));
			}
			else if (value is UnityEngine.Bounds) {
				addBounds(e, name, value);
			}

			else if (value is System.Object[]) {
				addArray(e, value, name);
			}
			else {
				if (value != null)
					if (opt.terseValues)
						e.InnerText = opt.abbreviateValue(value.ToString());
					else
						e.InnerText = "(null)";
			}

			// @ToDo inner text if compact
		}



		public void addArray(XmlElement parent, System.Object arg, String name) {
			System.Object[] o = (System.Object[])arg;

			String nom = arg.GetType().FullName.Replace("[", "").Replace("]", "");

			XmlElement e = null;

			if (opt.useCompactValues) {
				e = parent;
				e.SetAttribute("type", opt.abbreviateValue(arg.GetType().FullName));
			}
			else {
				e = document.CreateElement(opt.xmlPrefix, "array", opt.xmlNamespace);
				parent.AppendChild(e);
				e.SetAttribute("type", nom);
			}

			e.SetAttribute("size", o.Length.ToString());

			int n = 0;
			foreach (object element in o) {
				if (o[n] is Transform)
					addTransform(e, nom, o[n++]);
				else if (o[n] is UnityEngine.Object) {
					XmlElement newElement;
					if (opt.terseValues)
						newElement = addElement(e, opt.abbreviateValue(XmlConvert.EncodeName(o[n].GetType().FullName)));
					else
						newElement = addElement(e, "array-element", o[n].ToString());
					newElement.SetAttribute("reference-id", $"0x{((UnityEngine.Object)o[n]).GetInstanceID().ToString("X8")}");
					//if (o[n].ToString().Equals(((UnityEngine.Object)o[n]).name) == false)
					//	newElement.SetAttribute("name", ((UnityEngine.Object)o[n]).name);
					n++;
				}
				else {
					XmlElement newElement = addElement(e, "element", o[n++].ToString());
				}
			}
		}
	}
}