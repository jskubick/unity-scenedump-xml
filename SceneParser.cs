using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace scenedump {
	public class SceneParser {
		XmlSceneDumperOptions opt;
		List<SceneToken> list = new List<SceneToken>();
		public ObjectReferences refs;

		public SceneParser(XmlSceneDumperOptions opt) {
			this.opt = opt;
		}

		public void parse() {

			GameObject[] gameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
			refs = new ObjectReferences();

			foreach (GameObject g in gameObjects) {
				refs.parse(g);
			}
			Debug.Log(refs.ToString());
			
			int n = 0;
			
			foreach (GameObject g in gameObjects) {
				list.Add(new GameObjectToken(0, n++, "", opt,  g));
			}

			foreach (SceneToken token in list) {
				Debug.Log(token.ToString());
			}

			//SerializedObject serializedObject = new SerializedObject(component);
			//SerializedProperty property = serializedObject.GetIterator();
		}
	}

	public class ObjectReferences {
		public Dictionary<int, HashSet<int>> objectReferences = new Dictionary<int, HashSet<int>>();
		
		public void add(int target, int referent) {
			if (objectReferences.ContainsKey(target) == false)
				objectReferences.Add(target, new HashSet<int>());
			HashSet<int> referrers = objectReferences[target];
			
			referrers.Add(referent);
		}

		public void parse(GameObject gameObject) {
			foreach (Component component in gameObject.GetComponents<Component>()) {
				parse(component);
			}
		}

		public void parse(Component component) {
			SerializedObject serializedObject = new SerializedObject(component);
			SerializedProperty property = serializedObject.GetIterator();
			if (property.NextVisible(true)) {
				do {
					if (property.propertyType == SerializedPropertyType.ObjectReference) {
						object target = property.objectReferenceValue;
						if (target != null) {
							if (target is GameObject)
								add(((GameObject)target).GetInstanceID(), component.gameObject.GetInstanceID());
							else if (target is Component)
								add(((Component)target).GetInstanceID(), component.gameObject.GetInstanceID());
						}
							
					}
					
				} while (property.NextVisible(false));
			}
		}

		public override String ToString() {
			StringBuilder s = new StringBuilder();
			foreach (int key in objectReferences.Keys) {
				HashSet<int> values = objectReferences[key];
				s.Append(key.ToString("X8"));
				s.Append(": ");
				foreach (int value  in values) {
					s.Append(value.ToString("X8"));
					s.Append("  ");
				}
			}
			return s.ToString();
		}
	}

	abstract class SceneToken {
		protected String name;
		protected int sequence;
		protected int depth;
		protected List<SceneToken> children = new List<SceneToken>();
		public String tabs;

		public SceneToken(int depth, int sequence) {
			this.depth = depth;
			this.sequence = sequence;
			StringBuilder s = new StringBuilder();
			for (int x=0; x < 255; x++) 
				s.Append("\t");
			tabs = s.ToString();
		}

		public override String ToString() {
			StringBuilder s = new StringBuilder();
			s.Append(tabs.Substring(0, depth));
			s.Append(name);
			s.Append("\n");

			foreach (SceneToken child in children) {
				s.Append(child.ToString());
			}

			return s.ToString();
		}
	}

	class GameObjectToken : SceneToken {
		static String[] labels;

		static GameObjectToken() {
			String[] alphabet = new String[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };
			String[] suffixes = new String[] { "", "²", "³", "ª", "º", "¹" };
			labels = new String[alphabet.Length * suffixes.Length];
			for (int suffix = 0; suffix < suffixes.Length; suffix++) {
				for (int letter = 0; letter < alphabet.Length; letter++) {
					labels[((suffix * alphabet.Length) + letter)] = $"{alphabet[letter]}{suffixes[suffix]}";
				}
			}

			StringBuilder s = new StringBuilder();
			for (int x = 0; x < labels.Length; x++) {
				s.Append($"{x} = '{labels[x]}'\n");
			}
			Debug.Log($"labels:\n{s}");
		}

		public GameObjectToken(int depth, int sequence, String parentName, XmlSceneDumperOptions opt, GameObject g) : base(depth, sequence) {
			this.name = $"{parentName}{labels[sequence]}";
			
			int n = 0;
			/*
			
			*/

			foreach (Component component in g.GetComponents<Component>()) {
				children.Add(new ComponentToken(depth + 1, n++, name, opt, component));
			}

			foreach (Transform child in g.transform) {
				children.Add(new GameObjectToken(depth + 1, n++, name, opt, child.gameObject));
			}
		}
	}

	class ComponentToken : SceneToken {
		static String[] labels;
		static ComponentToken() {
			String[] alphabet = new String[] { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z" };
			String[] suffixes = new String[] { "", "²", "³", "ª", "º", "¹" };
			labels = new String[alphabet.Length * suffixes.Length];
			for (int suffix = 0; suffix < suffixes.Length; suffix++) {
				for (int letter = 0; letter < alphabet.Length; letter++) {
					labels[((suffix * alphabet.Length) + letter)] = $"{alphabet[letter]}{suffixes[suffix]}";
				}
			}
		}
		public ComponentToken(int depth, int sequence, String parentName, XmlSceneDumperOptions opt, Component component) : base(depth, sequence) {
			this.name = (component is Transform) ? $"{parentName}†" : $"{parentName}{labels[sequence]}";

			int n = 0;
			SerializedObject serializedObject = new SerializedObject(component);
			SerializedProperty property = serializedObject.GetIterator();
			if (property.NextVisible(true)) {
				do {
					if (opt.includeProperty(property)) {
						children.Add(new PropertyToken(depth + 1, n++, name, opt, property));
					}
				} while (property.NextVisible(false));
			}
		}
	}

	class PropertyToken : SceneToken {

		public PropertyToken (int depth, int sequence, String parentName, XmlSceneDumperOptions opt, SerializedProperty property) : base(depth, sequence) {
			this.name = $"{parentName}{sequence}";
		}
	}
}
