<p align="center">
  <img align="center" src="icon.png">
</p>

<p align="center">
  An in-game explorer and a suite of debugging tools for <a href="https://docs.unity3d.com/Manual/IL2CPP.html">IL2CPP</a> and <b>Mono</b> Unity games, using <a href="https://github.com/HerpDerpinstine/MelonLoader">MelonLoader</a> and <a href="https://github.com/BepInEx/BepInEx">BepInEx</a>.<br><br>

  <a href="../../releases/latest">
    <img src="https://img.shields.io/github/release/sinai-dev/Explorer.svg" />
  </a>
 
  <img src="https://img.shields.io/github/downloads/sinai-dev/Explorer/total.svg" />
</p>

- [Releases](#releases)
- [Features](#features)
- [How to install](#how-to-install)
- [Mod Config](#mod-config)
- [Mouse Control](#mouse-control)
- [Building](#building)
- [Credits](#credits)

## Releases

| Mod Loader  | Il2Cpp | Mono |
| ----------- | ------ | ---- |
| [MelonLoader](https://github.com/HerpDerpinstine/MelonLoader) | ✔️ [link](https://github.com/sinai-dev/Explorer/releases/latest/download/Explorer.MelonLoader.Il2Cpp.zip) | ✔️ [link](https://github.com/sinai-dev/Explorer/releases/latest/download/Explorer.MelonLoader.Mono.zip) | 
| [BepInEx](https://github.com/BepInEx/BepInEx) | ❔ [link](https://github.com/sinai-dev/Explorer/releases/latest/download/Explorer.BepInEx.Il2Cpp.zip) | ✔️ [link](https://github.com/sinai-dev/Explorer/releases/latest/download/Explorer.BepInEx.Mono.zip) |

<b>Il2Cpp Issues:</b>
* Some methods may still fail with a `MissingMethodException`, please let me know if you experience this (with full debug log please).
* Reflection may fail with certain types, see [here](https://github.com/knah/Il2CppAssemblyUnhollower#known-issues) for more details.
* Scrolling with mouse wheel in the Explorer menu may not work on all games at the moment.

## Features

<p align="center">
  <img src="https://raw.githubusercontent.com/sinai-dev/Explorer/master/overview.png">  
</p>

* <b>Scene Explorer</b>: Simple menu to traverse the Transform heirarchy of the scene. 
* <b>GameObject Inspector</b>: Various helpful tools to see and manipulate the GameObject, similar to what you can do in the Editor.
* <b>Reflection Inspector</b>: Inspect Properties and Fields. Can also set primitive values and evaluate primitive methods.
* <b>Search</b>: Search for UnityEngine.Objects with various filters, or use the helpers for static Instances and Classes.
* <b>C# Console</b>: Interactive console for evaluating C# methods on the fly, with some basic helpers.
* <b>Inspect-under-mouse</b>: Hover over an object with a collider and inspect it by clicking on it.

## How to install

### MelonLoader
Requires [MelonLoader](https://github.com/HerpDerpinstine/MelonLoader) to be installed for your game.

1. Download the relevant release from above.
2. Unzip the file into the `Mods` folder in your game's installation directory, created by MelonLoader.
3. Make sure it's not in a sub-folder, `Explorer.dll` should be directly in the `Mods\` folder.

### BepInEx
Requires [BepInEx](https://github.com/BepInEx/BepInEx) to be installed for your game.

1. Download the relevant release from above.
2. Unzip the file into the `BepInEx\plugins\` folder in your game's installation directory, created by BepInEx.
3. Make sure it's not in a sub-folder, `Explorer.dll` should be directly in the `plugins\` folder.

## Mod Config

There is a simple Mod Config for the Explorer. You can access the settings via the "Options" page of the main menu.

`Main Menu Toggle` (KeyCode) | Default: `F7`
* See [this article](https://docs.unity3d.com/ScriptReference/KeyCode.html) for a full list of all accepted KeyCodes.

`Default Window Size` (Vector2) | Default: `x: 550, y: 700`
* Sets the default width and height for all Explorer windows when created.

`Default Items per Page` (int) | Default: `20`
* Sets the default items per page when viewing lists or search results.

`Enable Bitwise Editing` (bool) | Default: `false`
* Whether or not to show the Bitwise Editing helper when inspecting integers

`Enable Tab View` (bool) | Default: `true`
* Whether or not all inspector windows a grouped into a single window with tabs.

`Default Output Path` (string) | Default: `Mods\Explorer`
* Where output is generated to, by default (for Texture PNG saving, etc).

## Mouse Control

Explorer can force the mouse to be visible and unlocked when the menu is open, if you have enabled "Force Unlock Mouse" (Left-Alt toggle). Explorer also attempts to prevent clicking-through onto the game behind the Explorer menu.

If you need more mouse control:

* For VRChat, use [VRCExplorerMouseControl](https://github.com/sinai-dev/VRCExplorerMouseControl)
* For Hellpoint, use [HPExplorerMouseControl](https://github.com/sinai-dev/Hellpoint-Mods/tree/master/HPExplorerMouseControl/HPExplorerMouseControl)
* You can create your own plugin using one of the two plugins above as an example. Usually only a few simple Harmony patches are needed to fix the problem.

For example:
```csharp
using Explorer;
using Harmony; // or 'using HarmonyLib;' for BepInEx
// ...
// You will need to figure out the relevant Class and Method for your game using dnSpy.
[HarmonyPatch(typeof(MyGame.InputManager), nameof(MyGame.InputManager.Update))]
public class InputManager_Update
{
    [HarmonyPrefix]
    public static bool Prefix()
    {
        // prevent method running if menu open, let it run if not.
        return !ExplorerCore.ShowMenu;
    }
}
```

## Building

If you'd like to build this yourself, you will need to have installed BepInEx and/or MelonLoader for at least one Unity game. If you want to build all 4 versions, you will need at least one Il2Cpp and one Mono game, with BepInEx and MelonLoader installed for both.

1. Install MelonLoader or BepInEx for your game.
2. Open the `src\Explorer.csproj` file in a text editor.
3. Set the relevant `GameFolder` values for the versions you want to build, eg. set `MLCppGameFolder` if you want to build for a MelonLoader Il2Cpp game.
4. Open the `src\Explorer.sln` project.
5. Select `Solution 'Explorer' (1 of 1 project)` in the Solution Explorer panel, and set the <b>Active config</b> property to the version you want to build, then build it.
5. The DLLs are built to the `Release\` folder in the root of the repository.
6. If ILRepack fails or is missing, use the NuGet package manager to re-install `ILRepack.Lib.MSBuild.Task`, then re-build.

## Credits

Written by Sinai.

Thanks to:
* [ManlyMarco](https://github.com/ManlyMarco) for their [Runtime Unity Editor](https://github.com/ManlyMarco/RuntimeUnityEditor), which I used for the REPL Console and the "Find instances" snippet, and the UI style.
* [denikson](https://github.com/denikson) for [mcs-unity](https://github.com/denikson/mcs-unity). I commented out the `SkipVisibilityExt` constructor since it was causing an exception with the Hook it attempted.
