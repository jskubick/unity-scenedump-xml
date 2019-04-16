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

		public static XmlSceneDumperOptions FULL { get; private set; } = XmlSceneDumperOptions.initFull();
		public static XmlSceneDumperOptions COMPACT { get; private set; } = XmlSceneDumperOptions.initCompact();
		public static XmlSceneDumperOptions ULTRACOMPACT { get; private set; } = XmlSceneDumperOptions.initUltraCompact(5, 5);

		public HashSet<String> obviousSuperclasses { get; set; } = new HashSet<String>();
		public HashSet<String> DEFAULT_OBVIOUS_SUPERCLASSES = new HashSet<String>() { "System.Object", "UnityEngine.Object", "UnityEngine.Component", "UnityEngine.MonoBehaviour", "UnityEngine.Behaviour" };
		private HashSet<String> DEFAULT_SILLY_SUPERCLASSES = new HashSet<String>() { "System.Object" };

		

		public HashSet<String> EMPTY_SET = new HashSet<String>();

		/* enumerates superclasses and interfaces as follows:
		 *	<extends>
		 *		<baseclass>UnityEngine.Whatever</baseclass>
		 *		<!-- if UnityEngine.Behaviour is in obviousSuperclasses, we omit it and the remaining superclasses -->
		 *		<baseclass>UnityEngine.Behaviour</baseclass> 
		 *		<baseclass>UnityEngine.Component</baseclass>
		 *		<baseclass>UnityEngine.Object</baseclass>
		 *	</extends>
		 *	<implements>
		 *		<interface>UnityEngine.SomeInterface</interface>
		 *		<interface>UnityEngine.AnotherInterface</interface>
		 *		<!-- note that interface-ancestry is NOT enumerated. feel free to implement it if you genuinely care. -->
		 *	</implements>
		 */
		private void useVerboseAncestry() {
			superclassSeparator = null; // setting to null prevents rendering list as a single-line string
			superclassContainerTagName = "extends";
			superclassTagName = "baseclass";
			superclassInlineMax = 0;
			interfaceSeparator = null;
			interfaceContainerTagName = "implements";
			interfaceTagName = "interface";
			interfaceInlineMax = 0;
		}

		/* enumerates superclasses and interfaces as follows:
		 * <GameObject ...>
		 *		<!-- Transform tag goes here, unless transforms are inline -->
		 *		<extends>UnityEngine.Whatever</extends>
		 *		<implements>UnityEngine.SomeInterface</implements>
		 *		<implements>UnityEngine.AnotherInterface</implements>
		 *		<!-- remainder of Component and GameObject tags follow... -->
		 */
		private void useSemiVerboseAncestry() {
			superclassSeparator = null; // setting to null forces one superclass per tag
			superclassContainerTagName = null; // setting to null eliminates outer tag container
			superclassTagName = "extends";
			superclassInlineMax = 0;
			interfaceSeparator = null;
			interfaceContainerTagName = null;
			interfaceTagName = "implements";
			interfaceInlineMax = 0;
		}

		/*	enumerates superclasses and interfaces as follows:
		 *	<GameObject ...>
		 *		<extends>UnityEngine.Whatever:-»UnityEngine.Component</extends>
		 *		<implements>UnityEngine.SomeInterface, UnityEngine.AnotherInterface</implements>
		 */
		private void useCompactAncestry() {
			superclassSeparator = " -» ";
			superclassContainerTagName = null;
			superclassTagName = "extends";
			superclassInlineMax = 0;
			interfaceSeparator = ", ";
			interfaceContainerTagName = null;
			interfaceTagName = "implements";
			interfaceInlineMax = 0;
		}

		/*	Enumerates superclasses and interface inline with GameObject tag when possible, or as per CompactAncestry otherwise:
		 *	<!-- the "µ" is substitured for "UnityEngine" when unityengineAbbreviation = "µ" -->
		 *	<GameObject extends="µ.Behaviour" implements="µ.SomeInterface, µ.AnotherInterface">
		 *	
		 *	<!-- example where inline limit is 1 for both superclasses and interfaces, there's one superclass, and two interfaces:
		 *	<GameObject extends="µ.Behaviour">
		 *		<implements>µ.SomeInterface, µ.AnotherInterface</implements>
		 */

		private void useUltraCompactAncestry(int maxInlineSuperclasses, int maxInlineInterfaces) {
			useCompactAncestry();
			this.superclassInlineMax = maxInlineSuperclasses;
			this.interfaceInlineMax = maxInlineInterfaces;
		}


		public static XmlSceneDumperOptions initFull() {
			XmlSceneDumperOptions opt = new XmlSceneDumperOptions();
			opt.useVerboseAncestry();
			return opt;
		}

		public static XmlSceneDumperOptions initCompact() {
			XmlSceneDumperOptions opt = new XmlSceneDumperOptions();
			opt.xmlPrefix = null;

			opt.obviousSuperclasses = opt.DEFAULT_OBVIOUS_SUPERCLASSES;
			opt.includeUntagged = false;
			opt.showTransformValuesAsGameObjectAttributes = true;
			opt.useCompactValues = true; // render most property values as Strings
			opt.useSemiVerboseAncestry();

			opt.valueAbbreviations = new String[1, 2] { { "\n", "¬" } };

			return opt;
		}

		public static XmlSceneDumperOptions initUltraCompact(int maxInlineSuperclasses, int maxInlineInterfaces) {
			XmlSceneDumperOptions opt = initCompact();
			opt.valueAbbreviations = new String[2, 2] { { "UnityEngine.", "µ." }, { "\n", "¬" } };
			opt.useUltraCompactAncestry(maxInlineSuperclasses, maxInlineInterfaces);
			return opt;
		}

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

		public HashSet<string> getIgnoredProperties() {
			if (explicitlyIgnoredProperties != null)
				return explicitlyIgnoredProperties;

			if (ignoreRedundantProperties)
				return redundantProperties;

			return EMPTY_SET;
		}

		public string abbreviateValue(string src) {
			if (valueAbbreviations == null)
				return src;

			String outcome = src;
			for (int x = 0; x < valueAbbreviations.GetLength(0); x++) {
				outcome = outcome.Replace(valueAbbreviations[x, 0], valueAbbreviations[x, 1]);
			}

			return outcome;
		}

		public string abbreviateType(string src) {
			if (typeAbbreviations == null)
				return src;
			String outcome = src;
			for (int x = 0; x < typeAbbreviations.GetLength(0); x++)
				outcome = outcome.Replace(typeAbbreviations[x, 0], typeAbbreviations[x, 1]);
			return outcome;
		}
	}
}
