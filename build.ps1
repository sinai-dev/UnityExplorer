# MelonLoader IL2CPP (net6)
dotnet build src\UnityExplorer.sln -c Release_ML_Cpp_net6
# (cleanup and move files)
$Path = "Release\UnityExplorer.MelonLoader.IL2CPP.net6preview"
Remove-Item $Path\UnityExplorer.ML.IL2CPP.net6preview.deps.json
Remove-Item $Path\Tomlet.dll
New-Item -Path "$Path" -Name "Mods" -ItemType "directory" -Force
Move-Item -Path $Path\UnityExplorer.ML.IL2CPP.net6preview.dll -Destination $Path\Mods -Force
New-Item -Path "$Path" -Name "UserLibs" -ItemType "directory" -Force
Move-Item -Path $Path\mcs.dll -Destination $Path\UserLibs -Force
Move-Item -Path $Path\UniverseLib.IL2CPP.dll -Destination $Path\UserLibs -Force
# (create zip archive)
Compress-Archive -Path $Path\* -CompressionLevel Fastest -DestinationPath $Path\..\UnityExplorer.MelonLoader.IL2CPP.net6preview.zip -Force

# MelonLoader IL2CPP (net472)
dotnet build src\UnityExplorer.sln -c Release_ML_Cpp_net472
# (cleanup and move files)
$Path = "Release\UnityExplorer.MelonLoader.IL2CPP"
Remove-Item $Path\Tomlet.dll
New-Item -Path "$Path" -Name "Mods" -ItemType "directory" -Force
Move-Item -Path $Path\UnityExplorer.ML.IL2CPP.dll -Destination $Path\Mods -Force
New-Item -Path "$Path" -Name "UserLibs" -ItemType "directory" -Force
Move-Item -Path $Path\mcs.dll -Destination $Path\UserLibs -Force
Move-Item -Path $Path\UniverseLib.IL2CPP.dll -Destination $Path\UserLibs -Force
# (create zip archive)
Compress-Archive -Path $Path\* -CompressionLevel Fastest -DestinationPath $Path\..\UnityExplorer.MelonLoader.IL2CPP.zip -Force

# MelonLoader Mono
dotnet build src\UnityExplorer.sln -c Release_ML_Mono
# (cleanup and move files)
$Path = "Release\UnityExplorer.MelonLoader.Mono"
Remove-Item $Path\Tomlet.dll
New-Item -Path "$Path" -Name "Mods" -ItemType "directory" -Force
Move-Item -Path $Path\UnityExplorer.ML.Mono.dll -Destination $Path\Mods -Force
New-Item -Path "$Path" -Name "UserLibs" -ItemType "directory" -Force
Move-Item -Path $Path\mcs.dll -Destination $Path\UserLibs -Force
Move-Item -Path $Path\UniverseLib.Mono.dll -Destination $Path\UserLibs -Force
# (create zip archive)
Compress-Archive -Path $Path\* -CompressionLevel Fastest -DestinationPath $Path\..\UnityExplorer.MelonLoader.Mono.zip -Force

# BepInEx IL2CPP
dotnet build src\UnityExplorer.sln -c Release_BIE_Cpp
# (cleanup and move files)
$Path = "Release\UnityExplorer.BepInEx.IL2CPP"
New-Item -Path "$Path" -Name "plugins" -ItemType "directory" -Force
New-Item -Path "$Path" -Name "plugins\sinai-dev-UnityExplorer" -ItemType "directory" -Force
Move-Item -Path $Path\UnityExplorer.BIE.IL2CPP.dll -Destination $Path\plugins\sinai-dev-UnityExplorer -Force
Move-Item -Path $Path\mcs.dll -Destination $Path\plugins\sinai-dev-UnityExplorer -Force
Move-Item -Path $Path\Tomlet.dll -Destination $Path\plugins\sinai-dev-UnityExplorer -Force
Move-Item -Path $Path\UniverseLib.IL2CPP.dll -Destination $Path\plugins\sinai-dev-UnityExplorer -Force
# (create zip archive)
Compress-Archive -Path $Path\* -CompressionLevel Fastest -DestinationPath $Path\..\UnityExplorer.BepInEx.IL2CPP.zip -Force

# BepInEx 5 Mono
dotnet build src\UnityExplorer.sln -c Release_BIE5_Mono
# (cleanup and move files)
$Path = "Release\UnityExplorer.BepInEx5.Mono"
New-Item -Path "$Path" -Name "plugins" -ItemType "directory" -Force
New-Item -Path "$Path" -Name "plugins\sinai-dev-UnityExplorer" -ItemType "directory" -Force
Move-Item -Path $Path\UnityExplorer.BIE5.Mono.dll -Destination $Path\plugins\sinai-dev-UnityExplorer -Force
Move-Item -Path $Path\mcs.dll -Destination $Path\plugins\sinai-dev-UnityExplorer -Force
Move-Item -Path $Path\Tomlet.dll -Destination $Path\plugins\sinai-dev-UnityExplorer -Force
Move-Item -Path $Path\UniverseLib.Mono.dll -Destination $Path\plugins\sinai-dev-UnityExplorer -Force
# (create zip archive)
Compress-Archive -Path $Path\* -CompressionLevel Fastest -DestinationPath $Path\..\UnityExplorer.BepInEx5.Mono.zip -Force

# BepInEx 6 Mono
dotnet build src\UnityExplorer.sln -c Release_BIE6_Mono
# (cleanup and move files)
$Path = "Release\UnityExplorer.BepInEx6.Mono"
New-Item -Path "$Path" -Name "plugins" -ItemType "directory" -Force
New-Item -Path "$Path" -Name "plugins\sinai-dev-UnityExplorer" -ItemType "directory" -Force
Move-Item -Path $Path\UnityExplorer.BIE6.Mono.dll -Destination $Path\plugins\sinai-dev-UnityExplorer -Force
Move-Item -Path $Path\mcs.dll -Destination $Path\plugins\sinai-dev-UnityExplorer -Force
Move-Item -Path $Path\Tomlet.dll -Destination $Path\plugins\sinai-dev-UnityExplorer -Force
Move-Item -Path $Path\UniverseLib.Mono.dll -Destination $Path\plugins\sinai-dev-UnityExplorer -Force
# (create zip archive)
Compress-Archive -Path $Path\* -CompressionLevel Fastest -DestinationPath $Path\..\UnityExplorer.BepInEx6.Mono.zip -Force

# Standalone Mono
dotnet build src\UnityExplorer.sln -c Release_STANDALONE_Mono
$Path = "Release\UnityExplorer.Standalone.Mono"
Compress-Archive -Path $Path\* -CompressionLevel Fastest -DestinationPath $Path\..\UnityExplorer.Standalone.Mono.zip -Force

# Standalone IL2CPP
dotnet build src\UnityExplorer.sln -c Release_STANDALONE_Cpp
$Path = "Release\UnityExplorer.Standalone.IL2CPP"
Compress-Archive -Path $Path\* -CompressionLevel Fastest -DestinationPath $Path\..\UnityExplorer.Standalone.IL2CPP.zip -Force

# Editor (mono)
$Path1 = "Release\UnityExplorer.Standalone.Mono"
$Path2 = "Release\_UnityExplorer.Editor\Runtime"
Copy-Item $Path1\UnityExplorer.STANDALONE.Mono.dll -Destination $Path2
Copy-Item $Path1\mcs.dll -Destination $Path2
Copy-Item $Path1\Tomlet.dll -Destination $Path2
Copy-Item $Path1\UniverseLib.Mono.dll -Destination $Path2
Compress-Archive -Path Release\_UnityExplorer.Editor\* -CompressionLevel Fastest -DestinationPath Release\UnityExplorer.Editor.zip -Force