# CppExplorer

[![Version](https://img.shields.io/badge/MelonLoader-0.2.6-green.svg)]()

Universal Runtime Inspector/Explorer for Unity IL2CPP games.

## Features
* Scene exploration (traverse in the same way as the Unity Editor)
* Inspect GameObjects/Transforms and manipulate them
* Inspect any object with Reflection, set primitive values, etc
* REPL Console for executing on-the-fly code

## Credits

Written by Sinai.

Credits to ManlyMarco for his [Runtime Unity Editor](https://github.com/ManlyMarco/RuntimeUnityEditor), which I used for the REPL Console and the "Find instances" snippet, and used the same MCS that he uses*.

<i>* note: I commented out the `SkipVisibilityExt` constructor since it was causing an exception for some reason.</i>

## How to install

This requires [MelonLoader](https://github.com/HerpDerpinstine/MelonLoader) to be installed for your game.

1. Download <b>CppExplorer.dll</b> from [Releases](https://github.com/sinaioutlander/CppExplorer/releases).
2. Put the file in your `MyGame/Mods/` folder.

## How to use

* Press F7 to show or hide the menu.
* Currently does <b>not</b> grant locked mouse or prevent clicking-through the menu, be careful of this.
* Simply browse through the scene, search for objects, etc, it's pretty self-explanatory.

If you have any specific questions about it you can contact me here, on NexusMods (Sinaioutlander), or on Discord (Sinai#4637, in MelonLoader discord).

## Images

Scene explorer, and inspection of a MonoBehaviour object.

[![](https://i.imgur.com/Yxizwcz.png)]()

Advanced search feature.

[![](https://i.imgur.com/F9ZfMvz.png)]()


REPL console.

[![](https://i.imgur.com/14Dbtf8.png)]()
