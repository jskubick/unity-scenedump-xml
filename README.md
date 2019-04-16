Handy Unity editor extension that allows you to dump an entire scene hierarchy to XML.

### I stumbled across this via Google for the first time

Go to https://github.com/jskubick/unity-screendump-xml-project


### I want to use this with my own Unity project that's already in a Git repo:

1. cd to your project's root directory.

2. create Assets/Editor/ if it doesn't already exist.

3. cd Assets/Editor/

4. execute `git submodule add https://github.com/jskubick/unity-scenedump-xml.git unity-scenedump-xml`

5. If Assets/Editor/unity-scenedump-xml is still empty, execute `git submodule update --init --recursive`

### I want to use it with my own Unity project that's NOT already in a Git repo:

1. cd to your project's root directory.

2. create Assets/Editor/ if it doesn't already exist.

3. cd Assets/Editor/

4. execute `git clone https://github.com/jskubick/unity-scenedump-xml.git`

## I have some other question

Go to https://github.com/jskubick/unity-scenedump-xml-project

### When committing changes, use:

git push --recurse-submodules=on-demand