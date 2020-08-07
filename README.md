# CppExplorer

[![Version](https://img.shields.io/badge/MelonLoader-0.2.6-green.svg)]()

A simple, universal Runtime Explorer for Unity IL2CPP games.

## Features
* Scene hierarchy explorer
* Search loaded assets with filters
* Traverse and manipulate GameObjects
* Generic Reflection inspector
* REPL Console
* Inspect-under-mouse

### Known Issues / Todo
* Fix `List` and `Array` support, need to use IL2CPPSystem types.
* Add mouse lock and prevent click-through

## How to install

Requires [MelonLoader](https://github.com/HerpDerpinstine/MelonLoader) to be installed for your game.

1. Download <b>CppExplorer.zip</b> from [Releases](https://github.com/sinaioutlander/CppExplorer/releases).
2. Put the file in your `MyGame\Mods\` folder, and unzip with <b>"Extract here"</b> option.
3. It should not go into a sub-folder, you should see `CppExplorer.dll` and `mcs.dll` in your `Mods\` folder.

## How to use

* Press F7 to show or hide the menu.
* Simply browse through the scene, search for objects, etc, it's pretty self-explanatory.

If you have any specific questions about it you can contact me here, on NexusMods (Sinaioutlander), or on Discord (Sinai#4637, in MelonLoader discord).

## Images

Scene explorer, and inspection of a MonoBehaviour object:

[![](https://i.imgur.com/Yxizwcz.png)](https://i.imgur.com/Yxizwcz.png)

Search feature:

[![](https://i.imgur.com/F9ZfMvz.png)](https://i.imgur.com/F9ZfMvz.png)


REPL console:

[![](https://i.imgur.com/14Dbtf8.png)](https://i.imgur.com/14Dbtf8.png)

## Credits

Written by Sinai.

Credits to ManlyMarco for their [Runtime Unity Editor](https://github.com/ManlyMarco/RuntimeUnityEditor), which I used for the REPL Console and the "Find instances" snippet, and the MCS* version.

<i>* note: I commented out the `SkipVisibilityExt` constructor in `mcs.dll` since it was causing an exception with the Hook it attempted.</i>
