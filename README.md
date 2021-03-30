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

## Releases

| Mod Loader  | IL2CPP | Mono |
| ----------- | ------ | ---- |
| [BepInEx](https://github.com/BepInEx/BepInEx) 6.X | ✅ [link](https://github.com/sinai-dev/UnityExplorer/releases/latest/download/UnityExplorer.BepInEx.Il2Cpp.zip) | ❔* [link](https://github.com/sinai-dev/UnityExplorer/releases/latest/download/UnityExplorer.BepInEx6.Mono.zip) |
| [BepInEx](https://github.com/BepInEx/BepInEx) 5.X | ❌ n/a | ✅ [link](https://github.com/sinai-dev/UnityExplorer/releases/latest/download/UnityExplorer.BepInEx5.Mono.zip) |
| [MelonLoader](https://github.com/HerpDerpinstine/MelonLoader) 0.3 | ✅ [link](https://github.com/sinai-dev/UnityExplorer/releases/latest/download/UnityExplorer.MelonLoader.Il2Cpp.zip) | ✅ [link](https://github.com/sinai-dev/UnityExplorer/releases/latest/download/UnityExplorer.MelonLoader.Mono.zip) | 
| Standalone | ✅ [link](https://github.com/sinai-dev/UnityExplorer/releases/latest/download/UnityExplorer.Standalone.Il2Cpp.zip) | ✅ [link](https://github.com/sinai-dev/UnityExplorer/releases/latest/download/UnityExplorer.Standalone.Mono.zip) | 

\* BepInEx 6.X Mono release may not work on all games yet.

## How to install

### BepInEx

0. Install [BepInEx](https://github.com/BepInEx/BepInEx) for your game. For IL2CPP you should use [BepInEx 6 (Bleeding Edge)](https://builds.bepis.io/projects/bepinex_be), for Mono you should use [BepInEx 5](https://github.com/BepInEx/BepInEx/releases) (until Mono support stabilizes in BepInEx 6).
1. Download the UnityExplorer release for BepInEx IL2CPP or Mono above.
2. Take the `UnityExplorer.BIE.___.dll` file and put it in `[GameFolder]\BepInEx\plugins\`
3. In IL2CPP, you will need to download the [Unity libs](https://github.com/LavaGang/Unity-Runtime-Libraries) for the game's Unity version and put them in the `BepInEx\unity-libs\` folder. 

### MelonLoader

0. Install [MelonLoader](https://github.com/HerpDerpinstine/MelonLoader) 0.3+ for your game. Version 0.3 is currently in pre-release, so you must "Enable ALPHA Releases" in your MelonLoader Installer settings to see the option for it.
1. Download the UnityExplorer release for MelonLoader IL2CPP or Mono above.
2. Take the `UnityExplorer.ML.___.dll` file and put it in the `[GameFolder]\Mods\` folder.

### Standalone

The standalone release is based on the BepInEx build, so it requires Harmony 2.0 (or HarmonyX) to function properly.

0. Load the DLL from your mod or inject it. You must also make sure `0Harmony.dll` is loaded, and `UnhollowerBaseLib.dll` for IL2CPP as well.
1. Create an instance of Unity Explorer with `UnityExplorer.ExplorerStandalone.CreateInstance();`
2. Optionally subscribe to the `ExplorerStandalone.OnLog` event to handle logging if you wish.

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
* <b>Inspect-under-mouse</b>: Hover over an object with a collider and inspect it by clicking on it. There's also a UI mode to inspect UI objects.

### C# Console Tips

The C# Console can be used to define temporary classes and methods, or it can be used to evaluate an expression, but you cannot do both at the same time.

For example, you could run this code to define a temporary class (it will be visible within the console until you run `Reset();`).

```csharp
public class MyClass
{
    public static void Method()
    {
        UnityExplorer.ExplorerCore.Log("hello");
    }
}
```

You could then delete or comment out the class and run the following expression to run that method:

```csharp
MyClass.Method();
```

However, you cannot define a class and run it both at the same time. You must either define class(es) and run that, or define an expression and run that.

You can also make use of the helper methods in the console to simplify some tasks, which you can see listed when the console has nothing entered for input. These methods are **not** accessible within any temporary classes you define, they can only be used in the expression context.

### Logging

Explorer saves all logs to disk (only keeps the most recent 10 logs). They can be found in a "UnityExplorer" folder in the same place as where you put the DLL file.

These logs are also visible in the Debug Console part of the UI.

### Settings

You can change the settings via the "Options" page of the main menu, or directly from the config file.

Depending on the release you are using, the config file will be found at:
* BepInEx: `BepInEx\config\com.sinai.unityexplorer.cfg`
* MelonLoader: `UserData\MelonPreferences.cfg`
* Standalone `{DLL_location}\UnityExplorer\config.ini`

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

Building the project should be straight-forward, the references are all inside the `lib\` folder.

1. Open the `src\UnityExplorer.sln` project in Visual Studio.
2. Select `Solution 'UnityExplorer' (1 of 1 project)` in the Solution Explorer panel, and set the <b>Active config</b> property to the version you want to build, then build it. Alternatively, use "Batch Build" and select all releases.
3. The DLLs are built to the `Release\` folder in the root of the repository.
4. If ILRepack complains about an error, just change the Active config to a different release and then back again. This sometimes happens for the first time you build the project.

## Acknowledgments

* [ManlyMarco](https://github.com/ManlyMarco) for [Runtime Unity Editor](https://github.com/ManlyMarco/RuntimeUnityEditor) (GPL), snippets from the REPL Console were used for UnityExplorer's C# Console.
* [denikson](https://github.com/denikson) (aka Horse) for [mcs-unity](https://github.com/denikson/mcs-unity) (MIT), used as the `Mono.CSharp` reference for the C# Console.
* [HerpDerpenstine](https://github.com/HerpDerpinstine) for [MelonCoroutines](https://github.com/LavaGang/MelonLoader/blob/master/MelonLoader.Support.Il2Cpp/MelonCoroutines.cs) (Apache), they were included for standalone Il2CPP coroutine support.
* [InGameCodeEditor](https://assetstore.unity.com/packages/tools/gui/ingame-code-editor-144254) (Apache) was used as the base for the syntax highlighting for UnityExplorer's C# console (`UnityExplorer.UI.Main.CSConsole.Lexer`).

### Disclaimer

UnityExplorer is in no way associated with Unity Technologies. "Unity", Unity logos, and other Unity trademarks are trademarks or registered trademarks of Unity Technologies or its affiliates in the U.S. and elsewhere.
