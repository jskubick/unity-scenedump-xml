Unity-Xml-Scenedump is a Unity editor extension that allows you to dump a scene's object hierarchy to XML. It consists of two related GitHub repositories:

* This one (**unity-scenedump-xml**), which contains the files you'll need to add to your Unity project to enable its use.

* A reference project ([unity-scenedump-xml-project](https://github.com/jskubick/unity-scenedump-xml-project) ), which is a complete ready-to-run Unity project with example scene that you can use to try out the extension before adding it to your own project.

Once you have it installed you can skip ahead:

* ["How to Use"](#How-to-Use)
* ["Troubleshooting"](#Troubleshooting)
* ["Customization & Advanced Features](#Customization)

# Installation

Choose the scenario that applies to you:

* [My Unity project is already in Git](#Project-already-in-Git)

* [My Unity project isn't in Git *yet*](#Project-not-yet-in-Git)

* [My Unity project is never going to be in Git](#Project-wont-ever-be-in-Git)

* [I installed the extension in my project without putting it in Git, and NOW want to put my own project in Git](#Retroactively-adding-project-to-Git)

### Project already in Git:

You'll want to add the extension to YOUR project as a Git *submodule*:

1. Create Assets/Editor/ directory within your project if it doesn't already exist

2. cd to Assets/Editor/

3. execute `git submodule add https://github.com/jskubick/unity-scenedump-xml.git unity-scenedump-xml`

4. If Assets/Editor/unity-scenedump-xml is still empty, execute `git submodule update --init --recursive`


### Project not (yet) in Git:

Unless you feel masochistic, please put your project in Git first, make sure everything is still working, *then* follow the instructions for [Project already in Git](#Project-already-in-Git).


### Project won't *ever* be in Git:

1. Create Assets/Editor within your project if it doesn't already exist.

2. cd to Assets/Editor

3. `git clone --recursive https://github.com/jskubick/unity-scenedump-xml.git`

4. If Assets/Editor/unity-scenedump-xml is still empty, execute `git submodule update --init --recursive`


### Retroactively adding project to Git

OK, you're a rebel. You get to be a pioneer as well, because I have absolutely no idea how to do this properly. I welcome anyone to document the correct procedure for doing this, but in the meantime, here's how *I'd* personally do it since all of the extension's files are in a single directory, and there aren't many of them to begin with:

1. If you've modified any of the extension's files, make a local copy of your Assets/Editor/unity-scenedump-xml/ directory

2. Delete it from your project's Assets/Editor/ directory.

3. Make sure **everything** Git-related is gone from Assets/Editor

4. Follow the instructions for ["Project not (yet) in Git"](#Project-not-yet-in-Git)

5. Once you have your project in Git, and unity-scenedump-xml added as a submodule, copy the files you changed (and saved a copy of in step 1) back over the files in Assets/Editor/unity-scenedump-xml/


# Troubleshooting:

### I cloned this repo, but the Assets/Editor/ directory is empty!

Oops. No worries, here's how to fix it:

1. cd to the Unity project's root folder (the one containing the Assets directory)

2. execute `git submodule update --init --recursive`


### My project is in a remote repo. Whenever I try to commit and push, I get a storm of error messages.

Depending upon your Git client, you might get a storm of error messages if you try to recursively commit-and-push. Basically, it commits the changes you made to your own project and to the extension's files to your local repo, and is then able to push the files to YOUR remote repo, but can't (obviously) push them to THIS project's repo on Github. If you encounter this problem and figure out how to configure your client to push changes ONLY to YOUR remote repo, please open an issue and let me know how you did it so I can update these directions.

### My project is in a remote repo. My Git client will allow me to commit, but no longer allows me to commit-and-push.

This is more or less the same issue as the previous problem... you have a project that's linked to two remote repositories, one of which you aren't allowed to change. Some clients will try anyway and let the errors fly, others will refuse to allow you to push at all until you fix the problem (by excluding the submodule's Github repo from "commit and push" actions).

As a stopgap measure, after you've committed the changes, try this if you have a commandline Git client installed:

1. cd to your project's root directory

2. `git push`

   This SHOULD push your changes to YOUR remote repository, while refraining from attempting to push the changes to the unity-scenedump-xml submodule to Github.
   
Alternatively, you could try something simpler: instead of checking out unity-scenedump-xml as a submodule, you COULD just check out the files from unity-scenedump-xml into a temp directory, delete the .git/ subdirectory, copy what's left to your project's Assets/Editor/ subdirectory, and let it be exclusively part of your own repo thereafter.

The truth is, submodule management is a fairly advanced Git topic. Long-term, it's something you'll probably want to learn how to deal with properly... but if you just want to make it work right now without investing days or weeks of research, it might be easier to just sever the files' link to Github and treat them like a normal part of your own project going forward.


### The XML generated by this extension is AWFUL to work with.

My apologies. I've used XML for years, but I don't have a lot of experience with parsing it using high-level frameworks. Usually, I just suck it into a Document object using Java or C#, and rip through it directly. If you can think of any specific structural improvements that will make it more useful and/or easier to parse with your favorite framework or app, please open an issue and let me know.

The truth is, this project began as a quick & dirty hack to generate human-readable dumps (now known as 'terse') that hapened to be well-formed XML as a bonus. The stuff to generate more verbose XML came later. I know it has plenty of room for improvement.

### The Debug menu isn't appearing!

1. Double-click Assets/Editor/unity-scenedump-xml/XmlSceneDumper.cs to launch Visual Studio
2. Build -> Build All
3. Exit VS, Exit Unity, relaunch Unity.


# How to Use:

By default, the extension (via XmlSceneDumper.cs) adds a new menu to Unity ("Debug") with four options:

* Export Scene as Xml (terse)
* Export Scene as Xml (compact)
* Export Scene as Xml (verbose)
* Export Scene as Xml (ultra-terse)

You can easily modify the menu and customize the options by editing XmlSceneDumper.cs (don't worry, it's easy).

* terse renders it into a form that's technically valid XML... but is primarily designed for human-readability. It generally favors attributes over child elements, and tries to abbreviate class names where it can.

* ultra-terse is like terse, but strips out repeating properties that aren't referenced by something else. Most of the time, this is good enough. If you're planning to print something out for reference, this is probably the option you'll want to use.

* compact strikes a balance between rendering things as attributes vs child elements.

* verbose generally renders everything as child elements with body text. If  you're planning to take this plugin's generated XML and use XSLT to transform it into another form, this is probably the option you'll want to use.

# Known Issues

* If you render values as named properties,  and the name is invalid for an XML attribute...
   * the invalid characters will be rendered as hex values in the form \_x####_, where "####" is a 4-digit hex value.
   * Space, which would otherwise be rendered as \_x0020_, is shortened to a single underscore ("\_").
   * If you have two properies with similar names that differ only by space-vs-underscore, they're going to collide, and no attempt is currently made to deal with that situation. The second will simply replace the first.
* Not all value types are rendered correctly. I tried to make it obvious when the renderer encounters something it doesn't know how to render by making it VERY visually-obvious when it happens (by including "@ToDo" in the value)

# Customization:

Most of the advanced/customization options are determined by values held by the XmlSceneDumperOptions object.

`xmlPrefix` -- when null, tags don't have a prefix. When non-null, this specifies their prefix. For example, if xmlPrefix=null, you might see a tag like `<GameObject >`. If xmlPrefix="unity", the same tag would render as `<unity:GameObject >`.

`includeValueStringAsProperty` -- when true, attempts to render things like Vector3 values as property strings. For example, position="(0,1,2)". In addition, when true, values associated with a GameObject's Transform get added to the GameObject tag itself.

`includeValueAsDiscreteElements` -- when true, renders values for things like Vector3 with an element for the object type, and a child element for each value.

Note that the previous two options aren't mutually exclusive. However, if both are 'true', the properties with the text values will be added to the `<Transform>` element, and NOT the `<GameObject>` element.

`compressArrays` -- Unity has a **lot** of arrays where the first element or two has a unique value, but the remainder all have the same value. If compressArrays=true, the renderer attempts to consolidate multiple elements with the same value into a single element. For terse XML, this is probably what you want. For XML that you're going to parse with code you didn't write yourself, it's probably not the behavior your want.

`xmlNamespace` -- I had to give the XML processor a namespace, so I made something plausible up. Feel free to change it to whatever you like. The actual URL doesn't matter, because there's nothing AT that URL anyway.

`superclassContainerTagName` and `superclassTagName`, together with `tagnameMonoBehaviour` and `MonoBehaviour`, determine how superclasses are represented: 

* If `superclassContainerTagName` is null, the container tag is omitted. Note that `superclassTagName` must NOT be null.

* If `tagnameBehaviour` and `tagnameMonoBehaviour` are null, any class that extends UnityEngine.Component gets rendered into a `<Component>` tag. When listing superclasses, we stop at the class whose Type.BaseType is UnityEngine.Component (since it's obvious by the `<Component>` tag itself that the class extends Component).

* If `tagnameBehaviour` is non-null, any Component that extends UnityEngine.Behaviour will render into a `<Behaviour>` (or whatever you set the value to) tag instead, and when listing the superclasses, we stop at the last class before Behaviour.

* If `tagnameMonoBehaviour` is non-null, any Behaviour that extends MonoBehaviour will render into a `<MonoBehaviour>` tag (or whatever you set the value to) instead, and when listing the superclasses, we stop at the last class before MonoBehaviour.

There's no real reason why I couldn't have done this for other classes as well. I just decided I've spent way too much time working on this, and stopped with MonoBehaviour for now. If you have lots of time to burn, feel free to implement support for other classes as well.

* `interfaceContainerTagName` and `interfaceTagName` work the same way as their 'superclass' counterpards. 

* `typeAbbreviations` and `valueAbbreviations` are 2-dimensional String[,] arrays defining name/value substitution pairs that get applied to... surprisingly... type names and values. If the array is null, no substitutions are made. Note that this is simple String.Replace(), and does NOT involve regular expressions (the original version did, but I decided to simplify it).