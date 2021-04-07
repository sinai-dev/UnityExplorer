<p align="center">
  <img align="center" src="img/icon.png">
</p>

<p align="center">
  An in-game explorer and a suite of debugging tools for <a href="https://docs.unity3d.com/Manual/IL2CPP.html">IL2CPP</a> and <b>Mono</b> Unity games, to aid with modding development.
</p>

## Releases [![](https://img.shields.io/github/release/sinai-dev/UnityExplorer.svg?label=release%20notes)](../../releases/latest) [![](https://img.shields.io/github/downloads/sinai-dev/UnityExplorer/total.svg)](../../releases) [![](https://img.shields.io/github/downloads/sinai-dev/UnityExplorer/latest/total.svg)](../../releases/latest)

| Mod Loader  | IL2CPP | Mono |
| ----------- | ------ | ---- |
| [BepInEx](https://github.com/BepInEx/BepInEx) 6.X | ✅ [link](https://github.com/sinai-dev/UnityExplorer/releases/latest/download/UnityExplorer.BepInEx.Il2Cpp.zip) | ✅ [link](https://github.com/sinai-dev/UnityExplorer/releases/latest/download/UnityExplorer.BepInEx6.Mono.zip) |
| [BepInEx](https://github.com/BepInEx/BepInEx) 5.X | ✖️ n/a | ✅ [link](https://github.com/sinai-dev/UnityExplorer/releases/latest/download/UnityExplorer.BepInEx5.Mono.zip) |
| [MelonLoader](https://github.com/HerpDerpinstine/MelonLoader) 0.3 | ✅ [link](https://github.com/sinai-dev/UnityExplorer/releases/latest/download/UnityExplorer.MelonLoader.Il2Cpp.zip) | ✅ [link](https://github.com/sinai-dev/UnityExplorer/releases/latest/download/UnityExplorer.MelonLoader.Mono.zip) | 
| Standalone | ✅ [link](https://github.com/sinai-dev/UnityExplorer/releases/latest/download/UnityExplorer.Standalone.Il2Cpp.zip) | ✅ [link](https://github.com/sinai-dev/UnityExplorer/releases/latest/download/UnityExplorer.Standalone.Mono.zip) | 

## How to install

### BepInEx

1. Install [BepInEx](https://github.com/BepInEx/BepInEx) for your game. For IL2CPP you should use [BepInEx 6 (Bleeding Edge)](https://builds.bepis.io/projects/bepinex_be), for Mono you should use [BepInEx 5](https://github.com/BepInEx/BepInEx/releases) (until Mono support stabilizes in BepInEx 6).
2. Download the UnityExplorer release for BepInEx IL2CPP or Mono above.
3. Take the `UnityExplorer.BIE.___.dll` file and put it in `[GameFolder]\BepInEx\plugins\`
4. In IL2CPP, you will need to download the [Unity libs](https://github.com/LavaGang/Unity-Runtime-Libraries) for the game's Unity version and put them in the `BepInEx\unity-libs\` folder. 

### MelonLoader

1. Install [MelonLoader](https://github.com/HerpDerpinstine/MelonLoader) 0.3+ for your game. Version 0.3 is currently in pre-release, so you must "Enable ALPHA Releases" in your MelonLoader Installer settings to see the option for it.
2. Download the UnityExplorer release for MelonLoader IL2CPP or Mono above.
3. Take the `UnityExplorer.ML.___.dll` file and put it in the `[GameFolder]\Mods\` folder.

### Standalone

The standalone release requires you to also load `0Harmony.dll` (HarmonyX, from the `lib\` folder) to function properly, and the IL2CPP also requires `UnhollowerBaseLib.dll` as well. The Mono release should be fairly easy to use with any loader, but the IL2CPP one may be tricky, I'd recommend just using BepInEx or MelonLoader for IL2CPP.

1. Load the UnityExplorer DLL from your mod or inject it, as well as `0Harmony.dll` and `UnhollowerBaseLib.dll` as required.
2. Create an instance of Unity Explorer with `UnityExplorer.ExplorerStandalone.CreateInstance();`
3. Optionally subscribe to the `ExplorerStandalone.OnLog` event to handle logging if you wish.

## Issues and contributions

Both issue reports and PR contributions are welcome in this repository.

### Issue reporting

To report an issue with UnityExplorer, please use the following template (as well as any other information / images you can provide).

Template:

```
* Game issue occurs on: 
* UnityExplorer version: (eg BIE.IL2CPP v3.3.3, etc)

* Description of issue:

* Unity log link:
* Mod Loader log link: 
```

Please upload your Unity log file to [Pastebin](https://pastebin.com/) (or equivalent service) and provide a link to the paste.

* The log should be found at `%userprofile%\AppData\LocalLow\[Company]\[Game]\` unless redirected by your Mod Loader.
* The file will be called either `output_log.txt` or `Player.log`

As well as the Unity log, please upload your Mod Loader's log:

* BepInEx: `BepInEx\LogOutput.log`
* MelonLoader: `MelonLoader\Latest.log`

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

## Building

Building the project should be straight-forward, the references are all inside the `lib\` folder.

1. Open the `src\UnityExplorer.sln` project in Visual Studio.
2. Select `Solution 'UnityExplorer' (1 of 1 project)` in the Solution Explorer panel, and set the <b>Active config</b> property to the version you want to build, then build it. Alternatively, use "Batch Build" and select all releases.
3. The DLLs are built to the `Release\` folder in the root of the repository.
4. If ILRepack complains about an error, just change the Active config to a different release and then back again. This sometimes happens for the first time you build the project.

## Acknowledgments

* [ManlyMarco](https://github.com/ManlyMarco) for [Runtime Unity Editor](https://github.com/ManlyMarco/RuntimeUnityEditor) \[[license](THIRDPARTY_LICENSES.md#runtimeunityeditor-license)\], snippets from the REPL Console were used for UnityExplorer's C# Console.
* [denikson](https://github.com/denikson) (aka Horse) for [mcs-unity](https://github.com/denikson/mcs-unity) \[no license\], used as the `Mono.CSharp` reference for the C# Console.
* [HerpDerpenstine](https://github.com/HerpDerpinstine) for [MelonCoroutines](https://github.com/LavaGang/MelonLoader/blob/6cc958ec23b5e2e8453a73bc2e0d5aa353d4f0d1/MelonLoader.Support.Il2Cpp/MelonCoroutines.cs) \[[license](THIRDPARTY_LICENSES.md#melonloader-license)\], they were included for standalone IL2CPP coroutine support.
* [InGameCodeEditor](https://assetstore.unity.com/packages/tools/gui/ingame-code-editor-144254) \[[license](THIRDPARTY_LICENSES.md#ingamecodeeditor-license)\] was used as the base for the syntax highlighting for UnityExplorer's C# console (`UnityExplorer.UI.Main.CSConsole.Lexer`).

### Disclaimer

UnityExplorer is in no way associated with Unity Technologies. "Unity", Unity logos, and other Unity trademarks are trademarks or registered trademarks of Unity Technologies or its affiliates in the U.S. and elsewhere.
