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

using StreamWriter = System.IO.StreamWriter;

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

namespace scenedump {
	public static class XmlSceneDumper {

		static String OUTPUT_FILE = @"C:\src\unity\SceneDumper\SceneDumperExampleProject\unity-scene.xml";

		private static int lastOperation = 0;

		[MenuItem("Debug/Export Scene Again")]
		public static void dumpScene() {
			if (lastOperation == 0) {
				Debug.LogError("You need to call another option first");
				return;
			}
			if (lastOperation == 1)
				dumpSceneVerbose();
			else if (lastOperation == 2)
				dumpSceneTerse();
	
			return;
		}

		[MenuItem("Debug/Export Scene/as Xml (verbose)")]
		public static void dumpSceneVerbose() {
			XmlSceneDumperOptions opt = XmlSceneDumperOptions.FULL;
			opt.includeUntagged = false;
			opt.xmlPrefix = null;
			opt.superclassContainerTagName = "extends";
			opt.superclassTagName = "superclass";
			opt.interfaceContainerTagName = "implements";
			opt.interfaceTagName = "interface";

			opt.includeValueStringAsProperty = false;
			opt.includeValueAsDiscreteElements = true;
			opt.compressArrays = false;

			dumpScene(opt);
			lastOperation = 1;
		}

		[MenuItem("Debug/Export Scene/as Xml (terse)")]
		public static void dumpSceneTerse() {
			XmlSceneDumperOptions opt = new XmlSceneDumperOptions();
			opt.includeUntagged = false;
			opt.xmlPrefix = null;
			opt.superclassContainerTagName = null;
			opt.superclassTagName = "extends";
			opt.interfaceContainerTagName = null;
			opt.interfaceTagName = "implements";

			opt.includeValueStringAsProperty = true;
			opt.includeValueAsDiscreteElements = false;
			opt.compressArrays = true;

			opt.typeAbbreviations = new String[,] { {"UnityEngine.", "µ." }, { "System.", "§." } };
			opt.valueAbbreviations = new String[1, 2] { { "\n", " •¬ " } };

			dumpScene(opt);
			lastOperation = 2;
		}

		

		private static void dumpScene(XmlSceneDumperOptions options) {
			XmlSceneHierarchy tree = new XmlSceneHierarchy(options, 1);

			tree.parse();

			Debug.Log("Dumping scene to " + OUTPUT_FILE + " ...");
			using (StreamWriter writer = new StreamWriter(OUTPUT_FILE, false)) {

				tree.document.Save(writer);
			}
			Debug.Log("Scene dumped to " + OUTPUT_FILE);
		}
	}
}