<p align="center">
  <img align="center" src="img/icon.png">
</p>

<p align="center">
  An in-game explorer and a suite of debugging tools for <a href="https://docs.unity3d.com/Manual/IL2CPP.html">IL2CPP</a> and <b>Mono</b> Unity games, to aid with modding development.
</p>
<p align="center">
  <a href="../../releases/latest">
    <img src="https://img.shields.io/github/release/sinai-dev/Explorer.svg" />
  </a>
 
  <img src="https://img.shields.io/github/downloads/sinai-dev/Explorer/total.svg" />
</p>

- [Releases](#releases)
- [Features](#features)
- [How to install](#how-to-install)
- [Mod Config](#mod-config)
- [Building](#building)
- [Credits](#credits)

## Releases

| Mod Loader  | IL2CPP | Mono |
| ----------- | ------ | ---- |
| [BepInEx](https://github.com/BepInEx/BepInEx) 6.X | ✅ [link](https://github.com/sinai-dev/UnityExplorer/releases/latest/download/UnityExplorer.BepInEx.Il2Cpp.zip) | ❔* [link](https://github.com/sinai-dev/UnityExplorer/releases/latest/download/UnityExplorer.BepInEx6.Mono.zip) |
| [BepInEx](https://github.com/BepInEx/BepInEx) 5.X | ❌ n/a | ✅ [link](https://github.com/sinai-dev/UnityExplorer/releases/latest/download/UnityExplorer.BepInEx5.Mono.zip) |
| [MelonLoader](https://github.com/HerpDerpinstine/MelonLoader) 0.3 | ✅ [link](https://github.com/sinai-dev/UnityExplorer/releases/latest/download/UnityExplorer.MelonLoader.Il2Cpp.zip) | ✅ [link](https://github.com/sinai-dev/UnityExplorer/releases/latest/download/UnityExplorer.MelonLoader.Mono.zip) | 
| Standalone | ✅ [link](https://github.com/sinai-dev/UnityExplorer/releases/latest/download/UnityExplorer.Standalone.Il2Cpp.zip) | ✅ [link](https://github.com/sinai-dev/UnityExplorer/releases/latest/download/UnityExplorer.Standalone.Mono.zip) | 

\* BepInEx 6.X Mono release may not work on all games yet.

## Features

<p align="center">
  <a href="https://raw.githubusercontent.com/sinai-dev/UnityExplorer/master/img/preview.png">
    <img src="img/preview.png" />
  </a>
</p>

* <b>Scene Explorer</b>: Simple menu to traverse the Transform heirarchy of the scene. 
* <b>GameObject Inspector</b>: Various helpful tools to see and manipulate the GameObject, similar to what you can do in the Editor.
* <b>Reflection Inspector</b>: Inspect Properties and Fields. Can also set primitive values and evaluate primitive methods.
* <b>Search</b>: Search for UnityEngine.Objects with various filters, or use the helpers for static Instances and Classes.
* <b>C# Console</b>: Interactive console for evaluating C# methods on the fly, with some basic helpers.
* <b>Inspect-under-mouse</b>: Hover over an object with a collider and inspect it by clicking on it.

## How to install

### BepInEx

Note: For IL2CPP you should use [BepInEx 6 (Bleeding Edge)](https://builds.bepis.io/projects/bepinex_be), for Mono you should use [BepInEx 5](https://github.com/BepInEx/BepInEx/releases) (until Mono support stabilizes in BepInEx 6).

0. Install [BepInEx](https://github.com/BepInEx/BepInEx) for your game.
1. Download the UnityExplorer release for BepInEx IL2CPP or Mono above.
2. Take the `UnityExplorer.BIE.___.dll` file and put it in `[GameFolder]\BepInEx\plugins\`
3. In IL2CPP, it is highly recommended to get the base Unity libs for the game's Unity version and put them in the `BepInEx\unity-libs\` folder. 

### MelonLoader

Note: You must use version 0.3 of MelonLoader or greater. Version 0.3 is currently in pre-release, so you must opt-in from your MelonLoader installer (enable alpha releases).

0. Install [MelonLoader](https://github.com/HerpDerpinstine/MelonLoader) for your game.
1. Download the UnityExplorer release for MelonLoader IL2CPP or Mono above.
2. Take the contents of the release and put it in the `[GameFolder]\Mods\` folder. It should look like `[GameFolder]\Mods\UnityExplorer.ML.___.dll`

### Standalone

0. Load the DLL from your mod or inject it. You must also make sure that the required libraries (Harmony, Unhollower for Il2Cpp, etc) are loaded.
1. Create an instance of Unity Explorer with `new ExplorerCore();`
2. You will need to call `ExplorerCore.Update()` (static method) from your Update method.
3. Subscribe to the `ExplorerCore.OnLog__` methods for logging.

## Settings

You can change the settings via the "Options" page of the main menu, or directly from the config file (generated after first launch). The config file will be found either inside a "UnityExplorer" folder in the same directory as where you put the DLL file, or for BepInEx it will be at `BepInEx\config\UnityExplorer\`.

`Main Menu Toggle` (KeyCode)
* Default: `F7`
* See [this article](https://docs.unity3d.com/ScriptReference/KeyCode.html) for a full list of all accepted KeyCodes.

`Force Unlock Mouse` (bool)
* Default: `true`
* Forces the cursor to be unlocked and visible while the UnityExplorer menu is open, and prevents anything else taking control.

`Default Page Limit` (int)
* Default: `25`
* Sets the default items per page when viewing lists or search results.
* <b>Requires a restart to take effect</b>, apart from Reflection Inspector tabs.

`Default Output Path` (string)
* Default: `Mods\UnityExplorer`
* Where output is generated to, by default (for Texture PNG saving, etc).

`Log Unity Debug` (bool)
* Default: `false`
* Listens for Unity `Debug.Log` messages and prints them to UnityExplorer's log.

`Hide on Startup` (bool)
* Default: `false`
* If true, UnityExplorer will be hidden when you start the game, you must open it via the keybind.

## Building

If you'd like to build this yourself, all you need to do is download this repository and build from Visual Studio. If you want to build for BepInEx or MelonLoader IL2CPP then you will need to install the mod loader for a game and set the directory in the `csproj` file.

For IL2CPP:
1. Install BepInEx or MelonLoader for your game.
2. Open the `src\UnityExplorer.csproj` file in a text editor.
3. Set `BIECppGameFolder` (for BepInEx) and/or `MLCppGameFolder` (for MelonLoader) so the project can locate the necessary references.

For all builds:
1. Open the `src\UnityExplorer.sln` project.
2. Select `Solution 'UnityExplorer' (1 of 1 project)` in the Solution Explorer panel, and set the <b>Active config</b> property to the version you want to build, then build it.
3. The DLLs are built to the `Release\` folder in the root of the repository.
4. If ILRepack fails or is missing, use the NuGet package manager to re-install `ILRepack.Lib.MSBuild.Task`, then re-build.

## Credits

Written by Sinai.

### Licensing

This project uses code from:
* (GPL) [ManlyMarco](https://github.com/ManlyMarco)'s [Runtime Unity Editor](https://github.com/ManlyMarco/RuntimeUnityEditor), which I used for some aspects of the C# Console and Auto-Complete features. The snippets I used are indicated with a comment.
* (MIT) [denikson](https://github.com/denikson) (aka Horse)'s [mcs-unity](https://github.com/denikson/mcs-unity). I commented out the `SkipVisibilityExt` constructor since it was causing an exception with the Hook it attempted in IL2CPP.
* (Apache) [InGameCodeEditor](https://assetstore.unity.com/packages/tools/gui/ingame-code-editor-144254) was used as the base for the syntax highlighting for UnityExplorer's C# console, although it has been heavily rewritten and optimized. Used classes are in the `UnityExplorer.CSConsole.Lexer` namespace.
