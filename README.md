<p align="center">
  <img align="center" src="img/icon.png">
</p>

<p align="center">
  🔍 An in-game UI for exploring, debugging and modifying Unity games.
</p>
<p align="center">
  ✔️ Supports most Unity games from versions 5.2 to 2020+ (IL2CPP and Mono).
</p>
<p align="center">
  💜 Enjoy this tool? Consider supporting me on <a href="https://ko-fi.com/sinaidev">ko-fi</a>!
</p>

## Releases  [![](https://img.shields.io/github/downloads/sinai-dev/UnityExplorer/total.svg)](../../releases) 

[![](https://img.shields.io/github/release/sinai-dev/UnityExplorer.svg?label=version)](../../releases/latest) [![](https://img.shields.io/github/downloads/sinai-dev/UnityExplorer/latest/total.svg)](../../releases/latest)
| Mod Loader  | IL2CPP | Mono |
| ----------- | ------ | ---- |
| [BepInEx](https://github.com/BepInEx/BepInEx) 6.X | ✅ [link](https://github.com/sinai-dev/UnityExplorer/releases/latest/download/UnityExplorer.BepInEx.Il2Cpp.zip) | ✅ [link](https://github.com/sinai-dev/UnityExplorer/releases/latest/download/UnityExplorer.BepInEx6.Mono.zip) |
| [BepInEx](https://github.com/BepInEx/BepInEx) 5.X | ✖️ n/a | ✅ [link](https://github.com/sinai-dev/UnityExplorer/releases/latest/download/UnityExplorer.BepInEx5.Mono.zip) |
| [MelonLoader](https://github.com/HerpDerpinstine/MelonLoader) 0.3.1 | ✅ [link](https://github.com/sinai-dev/UnityExplorer/releases/latest/download/UnityExplorer.MelonLoader.Il2Cpp.zip) | ✅ [link](https://github.com/sinai-dev/UnityExplorer/releases/latest/download/UnityExplorer.MelonLoader.Mono.zip) | 
| [MelonLoader](https://github.com/HerpDerpinstine/MelonLoader) 0.3.0 | ✅ [link](https://github.com/sinai-dev/UnityExplorer/releases/latest/download/UnityExplorer.MelonLoader_Legacy.Il2Cpp.zip) | ✅ [link](https://github.com/sinai-dev/UnityExplorer/releases/latest/download/UnityExplorer.MelonLoader_Legacy.Mono.zip) | 
| Standalone | ✅ [link](https://github.com/sinai-dev/UnityExplorer/releases/latest/download/UnityExplorer.Standalone.Il2Cpp.zip) | ✅ [link](https://github.com/sinai-dev/UnityExplorer/releases/latest/download/UnityExplorer.Standalone.Mono.zip) | 

* [Click here for Bleeding Edge releases](https://github.com/sinai-dev/UnityExplorer/actions) (click on the latest workflow and scroll down to Artifacts)

### Known issues
* Any `MissingMethodException` or `NotSupportedException`: please report the issue and provide a copy of your mod loader log and/or Unity log.
* In IL2CPP, some IEnumerable and IDictionary types may fail enumeration. Waiting for the Unhollower rewrite to address this any further.
* The C# Console's completions have some minor issues such as not suggestion global classes which have no namespace, and erronously suggesting classes from using directives when they shouldn't be suggested. These are issues with mcs itself which I am looking into.

## How to install

### BepInEx

1. Install [BepInEx](https://github.com/BepInEx/BepInEx) for your game. IL2CPP currently requires a [Bleeding Edge](https://builds.bepis.io/projects/bepinex_be) release.
2. Download the UnityExplorer release for BepInEx IL2CPP or Mono above.
3. Take the `UnityExplorer.BIE.___.dll` file and put it in `[GameFolder]\BepInEx\plugins\`
4. In IL2CPP, you will need to download the [Unity libs](https://github.com/LavaGang/Unity-Runtime-Libraries) for the game's Unity version and put them in the `BepInEx\unity-libs\` folder. 

### MelonLoader

1. Install [MelonLoader](https://github.com/HerpDerpinstine/MelonLoader) 0.3.1+ for your game (or use `MelonLoader_Legacy` for `0.3.0`). This version can currently be obtained from [here](https://github.com/LavaGang/MelonLoader/actions).
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

* In the GameObject Inspector, you can edit any of the input fields in the inspector (excluding readonly fields) and press <b>Enter</b> to apply your changes. You can also do this to the GameObject path as a way to change the GameObject's parent. Press the <b>Escape</b> key to cancel your edits.
* In the Reflection Inspectors, automatic updating is not enabled by default, and you must press Apply for any changes you make to take effect. 

### C# Console

The C# Console uses the `Mono.CSharp.Evaluator` to define temporary classes or run immediate REPL code.

See the "Help" dropdown in the C# console menu for more detailed information.

### Mouse-Inspect

The "Mouse Inspect" dropdown on the main UnityExplorer navbar allows you to inspect objects under the mouse.

* <b>World</b>: uses Physics.Raycast to look for Colliders
* <b>UI</b>: uses GraphicRaycasters to find UI objects

### Settings

You can change the settings via the "Options" page of the main menu, or directly from the config file.

Depending on the release you are using, the config file will be found at:
* BepInEx: `BepInEx\config\com.sinai.unityexplorer.cfg`
* MelonLoader: `UserData\MelonPreferences.cfg`
* Standalone `{DLL_location}\UnityExplorer\config.ini`

## Building

If you fork the repository on GitHub you can build using the [dotnet workflow](https://github.com/sinai-dev/UnityExplorer/blob/master/.github/workflows/dotnet.yml):

0. Click on the Actions tab and enable workflows in your repository
1. Click on the "Build UnityExplorer" workflow, then click "Run Workflow" and run it manually, or make a new commit to trigger the workflow.
2. Take the artifact from the completed run.

For Visual Studio:

0. Clone the repository and run `git submodule update --init --recursive` to get the submodules.
1. Open the `src\UnityExplorer.sln` project.
2. Build `mcs`, and if using IL2CPP then build `UnhollowerBaseLib` as well.
3. Build the UnityExplorer release(s) you want to use, either by selecting the config as the Active Config, or batch-building.

## Acknowledgments

* [ManlyMarco](https://github.com/ManlyMarco) for [Runtime Unity Editor](https://github.com/ManlyMarco/RuntimeUnityEditor) \[[license](THIRDPARTY_LICENSES.md#runtimeunityeditor-license)\], the ScriptEvaluator from RUE's REPL console was used as the base for UnityExplorer's C# console.
* [denikson](https://github.com/denikson) (aka Horse) for [mcs-unity](https://github.com/denikson/mcs-unity) \[no license\], used as the `Mono.CSharp` reference for the C# Console.
* [HerpDerpenstine](https://github.com/HerpDerpinstine) for [MelonCoroutines](https://github.com/LavaGang/MelonLoader/blob/6cc958ec23b5e2e8453a73bc2e0d5aa353d4f0d1/MelonLoader.Support.Il2Cpp/MelonCoroutines.cs) \[[license](THIRDPARTY_LICENSES.md#melonloader-license)\], they were included for standalone IL2CPP coroutine support.

### Disclaimer

UnityExplorer is in no way associated with Unity Technologies. "Unity", Unity logos, and other Unity trademarks are trademarks or registered trademarks of Unity Technologies or its affiliates in the U.S. and elsewhere.
