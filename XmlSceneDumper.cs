﻿/*
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

		static String OUTPUT_FILE_VERBOSE = @"scene-verbose.xml";
		static String OUTPUT_FILE_TERSE = @"scene-terse.xml";

		[MenuItem("Debug/Export Scene as Xml (terse)")]
		public static void dumpSceneTerse() {
			XmlSceneDumperOptions opt = new XmlSceneDumperOptions();
			opt.xmlPrefix = null; // don't prefix tags
			opt.includeUntagged = false; // don't  include a "tag" attribute unless it actually has a meaningful value (eg, NOT null, blank, or "Untagged")
			opt.includeValueStringAsProperty = true; // render values of things like vector3 as string values of attributes
			opt.includeValueAsDiscreteElements = false; // don't verbosely render values of things like Vector3 as discrete child elements of container parent elements
			opt.compressArrays = true; // if the last (or all) elements of an array have the same value, collapse them all into a single element

			opt.superclassContainerTagName = null; // if null, the container tag is omitted entirely
			opt.superclassTagName = "extends";
			opt.interfaceContainerTagName = null; // container tag omitted entirely if null.
			opt.interfaceTagName = "implements";
			opt.omitContainerFieldsProperties = OmitWhen.ALWAYS; // don't wrap Component and GameObject properties in a <properties> container.

			// Whenever a Type name gets output, we rip through typeAbbreviations to do a search/replace on every pair (note: it's straight-up string, not regex)
			opt.typeAbbreviations = new String[,] { {"UnityEngine.", "µ." }, { "System.", "§." } };
			// ditto, for values. This example strips out newlines and replaces them with an alternative.
			opt.valueAbbreviations = new String[1, 2] { { "\n", " •¬ " } };

			dumpScene(opt, OUTPUT_FILE_TERSE);
		}

		[MenuItem("Debug/Export Scene as Xml (verbose)")]
		public static void dumpSceneVerbose() {
			XmlSceneDumperOptions opt = new XmlSceneDumperOptions();
			opt.xmlPrefix = "u"; // all XML tags will be prefixed, eg: "<u:GameObject >"
			opt.includeUntagged = true; // include "tag" attribute for all GameObjects, even if it's null/blank/Untagged
			opt.includeValueStringAsProperty = false; // false == don't render values as properties with compact text values, like: position="(1,2,3)"
			opt.includeValueAsDiscreteElements = true; // true == render them as nested elements, with one value per element. Ex: <Vector3><x>1</x><y>2</y><z>3</z></Vector3>
			opt.compressArrays = false; // render arrays with one element per value, even if the array's  tail end (or entirety) has the same values.

			opt.superclassContainerTagName = "extends";
			opt.superclassTagName = "superclass";
			opt.interfaceContainerTagName = "implements";
			opt.interfaceTagName = "interface";
			opt.omitContainerFieldsProperties = OmitWhen.NEVER; // wrap the properties for each Component and GameObject in a <properties> container.

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