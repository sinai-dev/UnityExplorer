<p align="center">
  <img align="center" src="img/icon.png">
</p>

<p align="center">
  An in-game explorer and a suite of debugging tools for <a href="https://docs.unity3d.com/Manual/IL2CPP.html">IL2CPP</a> and <b>Mono</b> Unity games, to aid with modding development.
</p>

<p align="center">
  Supports most Unity games from versions 5.2 to 2020+.
</p>

## Releases [![](https://img.shields.io/github/release/sinai-dev/UnityExplorer.svg?label=release%20notes)](../../releases/latest) [![](https://img.shields.io/github/downloads/sinai-dev/UnityExplorer/total.svg)](../../releases) [![](https://img.shields.io/github/downloads/sinai-dev/UnityExplorer/latest/total.svg)](../../releases/latest)

| Mod Loader  | IL2CPP | Mono |
| ----------- | ------ | ---- |
| [BepInEx](https://github.com/BepInEx/BepInEx) 6.X | ✅ [link](https://github.com/sinai-dev/UnityExplorer/releases/latest/download/UnityExplorer.BepInEx.Il2Cpp.zip) | ✅ [link](https://github.com/sinai-dev/UnityExplorer/releases/latest/download/UnityExplorer.BepInEx6.Mono.zip) |
| [BepInEx](https://github.com/BepInEx/BepInEx) 5.X | ✖️ n/a | ✅ [link](https://github.com/sinai-dev/UnityExplorer/releases/latest/download/UnityExplorer.BepInEx5.Mono.zip) |
| [MelonLoader](https://github.com/HerpDerpinstine/MelonLoader) 0.3.1 | ✅ [link](https://github.com/sinai-dev/UnityExplorer/releases/latest/download/UnityExplorer.MelonLoader.Il2Cpp.zip) | ✅ [link](https://github.com/sinai-dev/UnityExplorer/releases/latest/download/UnityExplorer.MelonLoader.Mono.zip) | 
| Standalone | ✅ [link](https://github.com/sinai-dev/UnityExplorer/releases/latest/download/UnityExplorer.Standalone.Il2Cpp.zip) | ✅ [link](https://github.com/sinai-dev/UnityExplorer/releases/latest/download/UnityExplorer.Standalone.Mono.zip) | 

### Known issues
* UI layouts broken/unusable after changing resolutions: delete the file `data.ini` in the UnityExplorer folder (same place as where you put the DLL). Better fix being worked on.
* Any `MissingMethodException` or `NotSupportedException`: please report the issue and provide a copy of your mod loader log and/or Unity log.
* The C# console may unexpectedly produce a GC Mark Overflow crash when calling certain outside methods. Not clear yet what is causing this, but it's being looked into.
* In IL2CPP, some IEnumerable and IDictionary types may fail enumeration. Waiting for the Unhollower rewrite to address this any further.
* In IL2CPP, the C# console might not suggest deobfuscated (or obfuscated) names. Being looked into.

## How to install

### BepInEx

1. Install [BepInEx](https://github.com/BepInEx/BepInEx) for your game. IL2CPP currently requires a [Bleeding Edge](https://builds.bepis.io/projects/bepinex_be) release.
2. Download the UnityExplorer release for BepInEx IL2CPP or Mono above.
3. Take the `UnityExplorer.BIE.___.dll` file and put it in `[GameFolder]\BepInEx\plugins\`
4. In IL2CPP, you will need to download the [Unity libs](https://github.com/LavaGang/Unity-Runtime-Libraries) for the game's Unity version and put them in the `BepInEx\unity-libs\` folder. 

### MelonLoader

1. Install [MelonLoader](https://github.com/HerpDerpinstine/MelonLoader) 0.3.1+ for your game. This version can currently be obtained from [here](https://github.com/LavaGang/MelonLoader/actions).
2. Download the UnityExplorer release for MelonLoader IL2CPP or Mono above.
3. Take the `UnityExplorer.ML.___.dll` file and put it in the `[GameFolder]\Mods\` folder.

### Standalone

The standalone release can be used with any injector or loader of your choice, but it requires you to load the dependencies manually: HarmonyX, and the IL2CPP version also requires that you set up an [Il2CppAssemblyUnhollower runtime](https://github.com/knah/Il2CppAssemblyUnhollower#required-external-setup).

1. Load the required libs - HarmonyX, and Il2CppAssemblyUnhollower if IL2CPP
2. Load the UnityExplorer DLL
3. Create an instance of Unity Explorer with `UnityExplorer.ExplorerStandalone.CreateInstance();`
4. Optionally subscribe to the `ExplorerStandalone.OnLog` event to handle logging if you wish

## Features

<p align="center">
  <a href="https://raw.githubusercontent.com/sinai-dev/UnityExplorer/master/img/preview.png">
    <img src="img/preview.png" />
  </a>
</p>

### Object Explorer

* Use the <b>Scene Explorer</b> tab to traverse the active scenes, as well as the DontDestroyOnLoad scene and the HideAndDontSave "scene" (assets and hidden objects).
* Use the <b>Object Search</b> tab to search for Unity objects (including GameObjects, Components, etc), C# Singletons or Static Classes.

### Inspector

The inspector is used to see detailed information on GameObjects (GameObject Inspector), C# objects (Reflection Inspector) and C# classes (Static Inspector).

For the GameObject Inspector, you can edit any of the input fields in the inspector (excluding readonly fields) and press <b>Enter</b> to apply your changes. You can also do this to the GameObject path as a way to change the GameObject's parent. Press the <b>Escape</b> key to cancel your edits.

In the Reflection Inspectors, automatic updating is not enabled by default, and you must press Apply for any changes you make to take effect. 

### C# Console

The C# Console uses the `Mono.CSharp.Evaluator` to define temporary classes or run immediate REPL code.

See the "Help" dropdown in the C# console menu for more detailed information.

### Mouse-Inspect

The "Mouse Inspect" dropdown on the main UnityExplorer nav-bar allows you to inspect objects under the mouse.

* <b>World</b>: uses Physics.Raycast to look for Colliders
* <b>UI</b>: uses GraphicRaycasters to find UI objects

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

* [ManlyMarco](https://github.com/ManlyMarco) for [Runtime Unity Editor](https://github.com/ManlyMarco/RuntimeUnityEditor) \[[license](THIRDPARTY_LICENSES.md#runtimeunityeditor-license)\], the ScriptEvaluator from RUE's REPL console was used as the base for UnityExplorer's C# console.
* [denikson](https://github.com/denikson) (aka Horse) for [mcs-unity](https://github.com/denikson/mcs-unity) \[no license\], used as the `Mono.CSharp` reference for the C# Console.
* [HerpDerpenstine](https://github.com/HerpDerpinstine) for [MelonCoroutines](https://github.com/LavaGang/MelonLoader/blob/6cc958ec23b5e2e8453a73bc2e0d5aa353d4f0d1/MelonLoader.Support.Il2Cpp/MelonCoroutines.cs) \[[license](THIRDPARTY_LICENSES.md#melonloader-license)\], they were included for standalone IL2CPP coroutine support.

### Disclaimer

UnityExplorer is in no way associated with Unity Technologies. "Unity", Unity logos, and other Unity trademarks are trademarks or registered trademarks of Unity Technologies or its affiliates in the U.S. and elsewhere.
