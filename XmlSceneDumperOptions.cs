using System.Collections.Generic;
using System;
using UnityEditor;

namespace  scenedump {

	public enum OmitWhen { NEVER, IF_EMPTY, ALWAYS };
	public class XmlSceneDumperOptions {

		// if non-null, only SerializedProperty.propertyType.ToString() values found in the HashSet are included when rendering <property> values.
		public HashSet<String> propertyTypesToInclude = null; 

		// NEVER: every child of a <Component> (or <Behaviour, or MonoBehaviour, or ...) has a <properties> tag containing <property> tags for each property.
		// IF_EMPTY: same, but components with NO properties have the empty <properties/> tag omitted
		// ALWAYS: <property> and <array> children go directly under <Component> (or <Behaviour>, or ...) tag.
		public OmitWhen omitContainerFieldsProperties = OmitWhen.ALWAYS;

		// if true, the sourcecode of your scripts will be embedded in the XML. You'll almost ALWAYS want this to be false, even when generating verbose XML.
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

		// this URL is a complete fiction... reqired by XML, but doesn't refer to any actual file (at least, as of April 16, 2019)
		public String xmlNamespace { get; set; } = "http://pantherkitty.software/xml/unity-scene/1.0";

		public String[,] typeAbbreviations = null;
		public String[,] valueAbbreviations = null;
		
		// if false, omits 'tag' attribute when value is 'Untagged', blank, or null
		public bool includeUntagged { get; set; } = false;


		public HashSet<String> obviousSuperclasses { get; set; } = new HashSet<String>() { "System.Object" };
	

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

		public bool includeProperty(SerializedProperty property) {
			object value = SerializedPropertyValue.parse(property);
			if (value == null)
				return false;

			if ((property.propertyType == SerializedPropertyType.ObjectReference) && value.GetType().FullName.Equals("UnityEditor.MonoScript"))
				return includeSerializedPropertyTypeMonoScript;

			if (propertyTypesToInclude == null)
				return true;

			return propertyTypesToInclude.Contains(property.propertyType.ToString());
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
			for (int x = 0; x < abbreviations.GetLength(0); x++) {
				outcome = outcome.Replace(abbreviations[x, 0], abbreviations[x, 1]);
			}

			return outcome;
		}

		// add method here to apply regular expressions if desired.
		// public string applyRegex(String src, Regex[] regexes, etc...)
	}

	
}
