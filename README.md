# CppExplorer [![Version](https://img.shields.io/badge/MelonLoader-0.2.6-green.svg)]()

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

### Note
Most games running on Unity 2017 to 2019 should be supported. If you find that the GUI does not display properly and you get errors in the MelonLoader console about it, then this is likely due to a bug with Il2CppAssemblyUnhollower's unstripping. This bug is known by the developer of the tool and they will fix it as soon as they are able to.

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
* Simply browse through the scene, search for objects, etc, it's pretty self-explanatory.

### Help! I can't use the mouse!

It is fairly common for games to override mouse control with their own mouse behaviour. Unfortunately, it's not feasible for CppExplorer to handle this due to how differently every game will go about it.

In order to fix this problem, you can:
* Use [VRCExplorerMouseControl](https://github.com/sinaioutlander/VRCExplorerMouseControl) (for VRChat)
* Use [HPExplorerMouseControl](https://github.com/sinaioutlander/Hellpoint-Mods/tree/master/HPExplorerMouseControl/HPExplorerMouseControl) (for Hellpoint)
* In general, pressing Escape (to open a menu) will usually give you temporary control over the mouse.
* Create your own mini-plugin using one of the two plugins above as an example. Usually only 1 or 2 simple Harmony patches are needed to fix the problem.

## Images

<b>Scene Explorer, GameObject Inspector, and Reflection Inspectors:</b>

[![](https://i.imgur.com/n8bkxVW.png)](https://i.imgur.com/n8bkxVW.png)

<b>Object Search:</b>

[![](https://i.imgur.com/lK2RthM.png)](https://i.imgur.com/lK2RthM.png)

<b>C# REPL console:</b>

[![](https://i.imgur.com/gYTor7C.png)](https://i.imgur.com/gYTor7C.png)

## Credits

Written by Sinai.

Thanks to:
* [ManlyMarco](https://github.com/ManlyMarco) for their [Runtime Unity Editor](https://github.com/ManlyMarco/RuntimeUnityEditor), which I used for the REPL Console and the "Find instances" snippet, and the UI style.
* [denikson](https://github.com/denikson) for [mcs-unity](https://github.com/denikson/mcs-unity). I commented out the `SkipVisibilityExt` constructor in `mcs.dll` since it was causing an exception with the Hook it attempted.
