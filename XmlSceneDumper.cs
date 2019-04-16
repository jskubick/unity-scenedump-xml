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
His original code can be obtained from http://wiki.unity3d.com/index.php/SceneDumper
*/

using StreamWriter = System.IO.StreamWriter;

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

namespace scenedump {
	public static class XmlSceneDumper {

		static String OUTPUT_FILE_VERBOSE = @"C:\src\MyGithubProjects\unity-scenedump-xml-project\samples\scene-verbose.xml";
		static String OUTPUT_FILE_TERSE = @"C:\src\MyGithubProjects\unity-scenedump-xml-project\samples\scene-terse.xml";

		[MenuItem("Debug/Export Scene as Xml (terse)")]
		public static void dumpSceneTerse() {
			XmlSceneDumperOptions opt = new XmlSceneDumperOptions();
			opt.includeUntagged = false; // if the tag's value is "Untagged" (or null, or blank), omit the 'tag' attribute entirely
			opt.xmlPrefix = null;
			opt.superclassContainerTagName = null; // if null, the container tag is omitted entirely
			opt.superclassTagName = "extends";
			opt.interfaceContainerTagName = null; // container tag omitted entirely if null.
			opt.interfaceTagName = "implements";

			opt.includeValueStringAsProperty = true;
			opt.includeValueAsDiscreteElements = false;
			opt.compressArrays = true; // true == collapse elements with the same value at the tail end of an array into a single element to save space.

			// Whenever a Type name gets output, we rip through typeAbbreviations to do a search/replace on every pair (note: it's straight-up string, not regex)
			opt.typeAbbreviations = new String[,] { {"UnityEngine.", "µ." }, { "System.", "§." } };
			// ditto, for values. This example strips out newlines and replaces them with an alternative.
			opt.valueAbbreviations = new String[1, 2] { { "\n", " •¬ " } };

			dumpScene(opt, OUTPUT_FILE_TERSE);
		}

		[MenuItem("Debug/Export Scene as Xml (verbose)")]
		public static void dumpSceneVerbose() {
			XmlSceneDumperOptions opt = new XmlSceneDumperOptions();
			opt.includeUntagged = true; // if true, every GameObject has a 'tag' attribute. If false, omit tag attribute if value is null, blank, or "Untagged"
										//opt.xmlPrefix = "unity"; // prepended to the document's XML tags. Ex: <unity:GameObject ...>
			opt.superclassContainerTagName = "extends";
			opt.superclassTagName = "superclass";
			opt.interfaceContainerTagName = "implements";
			opt.interfaceTagName = "interface";

			// fyi, there's nothing that says you can't render values as BOTH properties AND discrete elements. 
			opt.includeValueStringAsProperty = false; // false == don't render values as properties with compact text values, like: position="(1,2,3)"
			opt.includeValueAsDiscreteElements = true; // true == render them as nested elements, with one value per element. Ex: <Vector3><x>1</x><y>2</y><z>3</z></Vector3>
			opt.compressArrays = false; // false == render every element of an array into its own XML element, even if they're all the same

			dumpScene(opt, OUTPUT_FILE_VERBOSE);
		}

		private static void dumpScene(XmlSceneDumperOptions options, String outputFile) {
			XmlSceneHierarchy tree = new XmlSceneHierarchy(options, 1);

			tree.parse();

			using (StreamWriter writer = new StreamWriter(outputFile, false)) {

				tree.document.Save(writer);
			}
			Debug.Log("Scene dumped to " + outputFile);
		}
	}
}