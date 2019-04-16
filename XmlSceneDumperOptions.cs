using System.Collections.Generic;
using System;

namespace  scenedump {

	public enum OmitWhen { NEVER, IF_EMPTY, ALWAYS };
	public class XmlSceneDumperOptions {

		public OmitWhen omitContainerFieldsProperties = OmitWhen.ALWAYS;
		public bool includeSerializedPropertyTypeMonoScript = false;

		// render values as attribute string
		public bool includeValueStringAsProperty = true;

		// render values as discrete child elements
		public bool includeValueAsDiscreteElements = false;

		// to omit container tag for superclasses, set superclassContainerTagName to null, and superclassTagName="extends"
		public string superclassContainerTagName { get; set; } = "extends";
		public string superclassTagName { get; set; } = "baseclass";

		// to omit container tag for interfaces, set interfaceContainerTagName to null, and interfaceTagName="implements"
		public string interfaceContainerTagName { get; set; } = "implements";
		public string interfaceTagName { get; set; } = "interface";

		// to put Components that extend MonoBehaviour into a Behaviour (or Component, if tagnameBehaviour==null) tag, set tagnameMonoBehaviour=null.
		public string tagnameMonoBehaviour = "MonoBehaviour";
		// to put Components that extend Behaviour into a Component tag, set tagnameBehaviour=null;
		public string tagnameBehaviour = "Behaviour";

		// when true, arrays where two or more of the final elements (or all of the elements) have the same value are collapsed into a single <same-value> tag
		public bool compressArrays = true;

		/**	if non-null, specifies the prefix used for all XML tags */
		public String xmlPrefix { get; set; } = null;

	
		public String xmlNamespace { get; set; } = "http://pantherkitty.software/xml/unity-scene/1.0";

		public String[,] typeAbbreviations = null;
		public String[,] valueAbbreviations = null;

		public bool terseValues = true;

		// Properties that get ignored when adding properties of components
		public HashSet<String> redundantProperties { get; private set; } = new HashSet<String>() { "position", "localPosition", "eulerAngles", "localEulerAngles", "rotation", "localRotation", "localScale", "parent", "hasChanged", "tag", "name", "hideFlags", "right", "up", "forward", "hierarchyCapacity", "anchorMin", "anchorMax", "anchoredPosition", "sizeDelta", "pivot", "anchoredPosition3D", "offsetMin", "offsetMax" };
		public HashSet<String> explicitlyIgnoredProperties { get; set; } = null;
		public bool ignoreRedundantProperties = true;

		/**	When null, don't abbreviate "UnityEngine" in class names; When non-null, replace "UnityEngine" with specified value. */
		public string unityengineAbbreviation { get; set; } = null;

		public string newlineSeparator = null;

		

		// if false, omits 'tag' attribute when value is 'Untagged', blank, or null
		public bool includeUntagged { get; set; } = false;

		public bool showTransformValuesAsGameObjectAttributes { get; set; } = false;
		public bool useCompactValues { get; set; } = false;

		// see use*Ancestry() methods for examples of how to use
		public string superclassSeparator { get; set; } = null; //":-»";
		
		public int superclassInlineMax = 0;

		public string interfaceSeparator { get; set; } = null; //", ";
		
		public int interfaceInlineMax = 0;

		

		public HashSet<String> obviousSuperclasses { get; set; } = new HashSet<String>();
		public HashSet<String> DEFAULT_OBVIOUS_SUPERCLASSES = new HashSet<String>() { "System.Object", "UnityEngine.Object", "UnityEngine.Component", "UnityEngine.MonoBehaviour", "UnityEngine.Behaviour" };
		private HashSet<String> DEFAULT_SILLY_SUPERCLASSES = new HashSet<String>() { "System.Object" };

		

		


		
		

		public bool isObviousSuperclass(System.Object o) {
			if (o == null)
				return false;
			return isObviousSuperclass(o.GetType());
		}

		public bool isObviousSuperclass(Type t) {
			if (t == null)
				return false;
			return obviousSuperclasses.Contains(t.FullName);
		}

		/**	Whenever a value gets rendered, it hooks through here, so this is where string substitutions are handled, and 
		 *		where you'd want to handle regular expressions as well.
		 */
		public string abbreviateValue(string src) {
			return abbreviate(src, valueAbbreviations);
			// or , if you decide to implement regex replacement, something like...
			// return abbreviate( applyRegex(src, valueRegexes, valueReplacements),  valueAbbreviations);
		}


		/**	Whenever a Type.FullName gets rendered, it hooks through here, so this is where string substitutions are handled, and 
		 *		where you'd want to handle regular expressions as well.
		 */
		public string abbreviateType(string src) {
			return abbreviate(src, typeAbbreviations);
			// see abbreviateValue for suggestions on implementing regular expression handling.
		}

		public string abbreviate(string src, string[,] abbreviations) {
			if (abbreviations == null)
				return src;

			String outcome = src;
			for (int x = 0; x < valueAbbreviations.GetLength(0); x++) {
				outcome = outcome.Replace(abbreviations[x, 0], abbreviations[x, 1]);
			}

			return outcome;
		}

		// add method here to apply regular expressions if desired.
		// public string applyRegex(String src, Regex[] regexes, etc...)
	}

	
}
