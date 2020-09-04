# CppExplorer [![Version](https://img.shields.io/badge/MelonLoader-0.2.7.1-green.svg)]()

<p align="center">
  <img align="center" src="https://sinai-dev.github.io/images/thumbs/02.png">
</p>

<p align="center">
  An in-game explorer and a suite of debugging tools for <a href="https://docs.unity3d.com/Manual/IL2CPP.html">IL2CPP</a> Unity games, using <a href="https://github.com/HerpDerpinstine/MelonLoader">MelonLoader</a>.<br><br>

  <a href="../../releases/latest">
    <img src="https://img.shields.io/github/release/sinai-dev/CppExplorer.svg" />
  </a>
 
  <img src="https://img.shields.io/github/downloads/sinai-dev/CppExplorer/total.svg" />
</p>

### Known issue
Due to limitations with [Il2CppAssemblyUnhollower](https://github.com/knah/Il2CppAssemblyUnhollower)'s Unstripping, CppExplorer may encounter a `MissingMethodException` when trying to use certain UnityEngine methods.

Since version [1.5.4](https://github.com/sinai-dev/CppExplorer/releases/tag/1.5.4), CppExplorer manually unstrips most of these methods itself. If you encounter more methods which failed unstripping, please let me know by opening an issue and I will do my best to fix it.

## Features
* Scene hierarchy explorer
* Search loaded assets with filters
* Traverse and manipulate GameObjects
* Generic Reflection inspector
* C# REPL Console
* Inspect-under-mouse

## How to install

Requires [MelonLoader](https://github.com/HerpDerpinstine/MelonLoader) to be installed for your game.

1. Download <b>CppExplorer.zip</b> from [Releases](https://github.com/sinaioutlander/CppExplorer/releases).
2. Unzip the file into the `Mods` folder in your game's installation directory, created by MelonLoader.
3. Make sure it's not in a sub-folder, `CppExplorer.dll` and `mcs.dll` should be directly in the `Mods\` folder.

## How to use

* Press F7 to show or hide the menu.
* Simply browse through the scene, search for objects, etc, most of it is pretty self-explanatory.

### Scene Explorer

* A simple menu which allows you to traverse the Transform heirarchy of the scene.
* Click on a GameObject to set it as the current path, or <b>Inspect</b> it to send it to an Inspector Window.

[![](https://i.imgur.com/2b0q0jL.png)](https://i.imgur.com/2b0q0jL.png)

### Inspectors

CppExplorer has two main inspector modes: <b>GameObject Inspector</b>, and <b>Reflection Inspector</b>.

<b>Tips:</b> 
* When in Tab View, GameObjects are denoted by a [G] prefix, and Reflection objects are denoted by a [R] prefix.
* Hold <b>Left Shift</b> when you click the Inspect button to force Reflection mode for GameObjects and Transforms.

### GameObject Inspector

* Allows you to see the children and components on a GameObject.
* Can use some basic GameObject Controls such as translating and rotating the object, destroy it, clone it, etc.

[![](https://i.imgur.com/JTxqlx4.png)](https://i.imgur.com/JTxqlx4.png)

### Reflection Inspector

* The Reflection Inspector is used for all other supported objects.
* Allows you to inspect Properties, Fields and basic Methods, as well as set primitive values and evaluate primitive methods.
* Can search and filter members for the ones you are interested in.

[![](https://i.imgur.com/iq92m0l.png)](https://i.imgur.com/iq92m0l.png)

### Object Search

* You can search for an `UnityEngine.Object` with the Object Search feature.
* Filter by name, type, etc.
* For GameObjects and Transforms you can filter which scene they are found in too.

[![](https://i.imgur.com/lK2RthM.png)](https://i.imgur.com/lK2RthM.png)

### C# REPL console

* A simple C# REPL console, allows you to execute a method body on the fly.

[![](https://i.imgur.com/5U4D1a8.png)](https://i.imgur.com/5U4D1a8.png)

### Inspect-under-mouse

* Press Shift+RMB (Right Mouse Button) while the CppExplorer menu is open to begin Inspect-Under-Mouse.
* Hover over your desired object, if you see the name appear then you can click on it to inspect it.
* Only objects with Colliders are supported.

### Mouse Control

CppExplorer can force the mouse to be visible and unlocked when the menu is open, if you have enabled "Force Unlock Mouse" (Left-Alt toggle). However, you may also want to prevent the mouse clicking-through onto the game behind CppExplorer, this is possible but it requires specific patches for that game.

* For VRChat, use [VRCExplorerMouseControl](https://github.com/sinai-dev/VRCExplorerMouseControl)
* For Hellpoint, use [HPExplorerMouseControl](https://github.com/sinai-dev/Hellpoint-Mods/tree/master/HPExplorerMouseControl/HPExplorerMouseControl)
* You can create your own plugin using one of the two plugins above as an example. Usually only a few simple Harmony patches are needed to fix the problem.

## Credits

Written by Sinai.

Thanks to:
* [ManlyMarco](https://github.com/ManlyMarco) for their [Runtime Unity Editor](https://github.com/ManlyMarco/RuntimeUnityEditor), which I used for the REPL Console and the "Find instances" snippet, and the UI style.
* [denikson](https://github.com/denikson) for [mcs-unity](https://github.com/denikson/mcs-unity). I commented out the `SkipVisibilityExt` constructor in `mcs.dll` since it was causing an exception with the Hook it attempted.
