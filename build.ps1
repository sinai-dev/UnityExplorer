# ----------- MelonLoader IL2CPP (net6) -----------
dotnet build src\UnityExplorer.sln -c Release_ML_Cpp_net6
$Path = "Release\UnityExplorer.MelonLoader.IL2CPP.net6preview"
# ILRepack
lib/ILRepack.exe /target:library /lib:lib\net6 /lib:lib\unhollowed /lib:$Path /internalize /out:$Path\UnityExplorer.ML.IL2CPP.net6preview.dll $Path\UnityExplorer.ML.IL2CPP.net6preview.dll $Path\mcs.dll 
# (cleanup and move files)
Remove-Item $Path\UnityExplorer.ML.IL2CPP.net6preview.deps.json
Remove-Item $Path\Tomlet.dll
Remove-Item $Path\mcs.dll
Remove-Item $Path\Iced.dll
Remove-Item $Path\UnhollowerBaseLib.dll
New-Item -Path "$Path" -Name "Mods" -ItemType "directory" -Force
Move-Item -Path $Path\UnityExplorer.ML.IL2CPP.net6preview.dll -Destination $Path\Mods -Force
New-Item -Path "$Path" -Name "UserLibs" -ItemType "directory" -Force
Move-Item -Path $Path\UniverseLib.IL2CPP.dll -Destination $Path\UserLibs -Force
# (create zip archive)
Compress-Archive -Path $Path\* -CompressionLevel Fastest -DestinationPath $Path\..\UnityExplorer.MelonLoader.IL2CPP.net6preview.zip -Force

# ----------- MelonLoader IL2CPP (net472) -----------
dotnet build src\UnityExplorer.sln -c Release_ML_Cpp_net472
$Path = "Release\UnityExplorer.MelonLoader.IL2CPP"
# ILRepack
lib/ILRepack.exe /target:library /lib:lib\net472 /lib:lib\net35 /lib:lib\unhollowed /lib:$Path /internalize /out:$Path\UnityExplorer.ML.IL2CPP.dll $Path\UnityExplorer.ML.IL2CPP.dll $Path\mcs.dll 
# (cleanup and move files)
Remove-Item $Path\Tomlet.dll
Remove-Item $Path\mcs.dll
Remove-Item $Path\Iced.dll
Remove-Item $Path\UnhollowerBaseLib.dll
New-Item -Path "$Path" -Name "Mods" -ItemType "directory" -Force
Move-Item -Path $Path\UnityExplorer.ML.IL2CPP.dll -Destination $Path\Mods -Force
New-Item -Path "$Path" -Name "UserLibs" -ItemType "directory" -Force
Move-Item -Path $Path\UniverseLib.IL2CPP.dll -Destination $Path\UserLibs -Force
# (create zip archive)
Compress-Archive -Path $Path\* -CompressionLevel Fastest -DestinationPath $Path\..\UnityExplorer.MelonLoader.IL2CPP.zip -Force

# ----------- MelonLoader Mono -----------
dotnet build src\UnityExplorer.sln -c Release_ML_Mono
$Path = "Release\UnityExplorer.MelonLoader.Mono"
# ILRepack
lib/ILRepack.exe /target:library /lib:lib\net35 /lib:$Path /internalize /out:$Path\UnityExplorer.ML.Mono.dll $Path\UnityExplorer.ML.Mono.dll $Path\mcs.dll 
# (cleanup and move files)
Remove-Item $Path\Tomlet.dll
Remove-Item $Path\mcs.dll
New-Item -Path "$Path" -Name "Mods" -ItemType "directory" -Force
Move-Item -Path $Path\UnityExplorer.ML.Mono.dll -Destination $Path\Mods -Force
New-Item -Path "$Path" -Name "UserLibs" -ItemType "directory" -Force
Move-Item -Path $Path\UniverseLib.Mono.dll -Destination $Path\UserLibs -Force
# (create zip archive)
Compress-Archive -Path $Path\* -CompressionLevel Fastest -DestinationPath $Path\..\UnityExplorer.MelonLoader.Mono.zip -Force

# ----------- BepInEx IL2CPP -----------
dotnet build src\UnityExplorer.sln -c Release_BIE_Cpp
$Path = "Release\UnityExplorer.BepInEx.IL2CPP"
# ILRepack
lib/ILRepack.exe /target:library /lib:lib\net472 /lib:lib\unhollowed /lib:$Path /internalize /out:$Path\UnityExplorer.BIE.IL2CPP.dll $Path\UnityExplorer.BIE.IL2CPP.dll $Path\mcs.dll $Path\Tomlet.dll
# (cleanup and move files)
Remove-Item $Path\Tomlet.dll
Remove-Item $Path\mcs.dll
Remove-Item $Path\Iced.dll
Remove-Item $Path\UnhollowerBaseLib.dll
New-Item -Path "$Path" -Name "plugins" -ItemType "directory" -Force
New-Item -Path "$Path" -Name "plugins\sinai-dev-UnityExplorer" -ItemType "directory" -Force
Move-Item -Path $Path\UnityExplorer.BIE.IL2CPP.dll -Destination $Path\plugins\sinai-dev-UnityExplorer -Force
Move-Item -Path $Path\UniverseLib.IL2CPP.dll -Destination $Path\plugins\sinai-dev-UnityExplorer -Force
# (create zip archive)
Compress-Archive -Path $Path\* -CompressionLevel Fastest -DestinationPath $Path\..\UnityExplorer.BepInEx.IL2CPP.zip -Force

# ----------- BepInEx 5 Mono -----------
dotnet build src\UnityExplorer.sln -c Release_BIE5_Mono
$Path = "Release\UnityExplorer.BepInEx5.Mono"
# ILRepack
lib/ILRepack.exe /target:library /lib:lib\net35 /lib:$Path /internalize /out:$Path\UnityExplorer.BIE5.Mono.dll $Path\UnityExplorer.BIE5.Mono.dll $Path\mcs.dll $Path\Tomlet.dll
# (cleanup and move files)
Remove-Item $Path\Tomlet.dll
Remove-Item $Path\mcs.dll
New-Item -Path "$Path" -Name "plugins" -ItemType "directory" -Force
New-Item -Path "$Path" -Name "plugins\sinai-dev-UnityExplorer" -ItemType "directory" -Force
Move-Item -Path $Path\UnityExplorer.BIE5.Mono.dll -Destination $Path\plugins\sinai-dev-UnityExplorer -Force
Move-Item -Path $Path\UniverseLib.Mono.dll -Destination $Path\plugins\sinai-dev-UnityExplorer -Force
# (create zip archive)
Compress-Archive -Path $Path\* -CompressionLevel Fastest -DestinationPath $Path\..\UnityExplorer.BepInEx5.Mono.zip -Force

# ----------- BepInEx 6 Mono -----------
dotnet build src\UnityExplorer.sln -c Release_BIE6_Mono
$Path = "Release\UnityExplorer.BepInEx6.Mono"
# ILRepack
lib/ILRepack.exe /target:library /lib:lib\net35 /lib:$Path /internalize /out:$Path\UnityExplorer.BIE6.Mono.dll $Path\UnityExplorer.BIE6.Mono.dll $Path\mcs.dll $Path\Tomlet.dll
# (cleanup and move files)
Remove-Item $Path\Tomlet.dll
Remove-Item $Path\mcs.dll
New-Item -Path "$Path" -Name "plugins" -ItemType "directory" -Force
New-Item -Path "$Path" -Name "plugins\sinai-dev-UnityExplorer" -ItemType "directory" -Force
Move-Item -Path $Path\UnityExplorer.BIE6.Mono.dll -Destination $Path\plugins\sinai-dev-UnityExplorer -Force
Move-Item -Path $Path\UniverseLib.Mono.dll -Destination $Path\plugins\sinai-dev-UnityExplorer -Force
# (create zip archive)
Compress-Archive -Path $Path\* -CompressionLevel Fastest -DestinationPath $Path\..\UnityExplorer.BepInEx6.Mono.zip -Force

# ----------- Standalone Mono -----------
dotnet build src\UnityExplorer.sln -c Release_STANDALONE_Mono
$Path = "Release\UnityExplorer.Standalone.Mono"
# ILRepack
lib/ILRepack.exe /target:library /lib:lib\net35 /lib:$Path /internalize /out:$Path\UnityExplorer.Standalone.Mono.dll $Path\UnityExplorer.Standalone.Mono.dll $Path\mcs.dll $Path\Tomlet.dll
# (cleanup and move files)
Remove-Item $Path\Tomlet.dll
Remove-Item $Path\mcs.dll
Compress-Archive -Path $Path\* -CompressionLevel Fastest -DestinationPath $Path\..\UnityExplorer.Standalone.Mono.zip -Force

# ----------- Standalone IL2CPP -----------
dotnet build src\UnityExplorer.sln -c Release_STANDALONE_Cpp
$Path = "Release\UnityExplorer.Standalone.IL2CPP"
# ILRepack
lib/ILRepack.exe /target:library /lib:lib\net472 /lib:lib\unhollowed /lib:$Path /internalize /out:$Path\UnityExplorer.Standalone.IL2CPP.dll $Path\UnityExplorer.Standalone.IL2CPP.dll $Path\mcs.dll $Path\Tomlet.dll
# (cleanup and move files)
Remove-Item $Path\Tomlet.dll
Remove-Item $Path\mcs.dll
Remove-Item $Path\Iced.dll
Remove-Item $Path\UnhollowerBaseLib.dll
Compress-Archive -Path $Path\* -CompressionLevel Fastest -DestinationPath $Path\..\UnityExplorer.Standalone.IL2CPP.zip -Force

# ----------- Editor (mono) -----------
$Path1 = "Release\UnityExplorer.Standalone.Mono"
$Path2 = "UnityEditorPackage\Runtime"
Copy-Item $Path1\UnityExplorer.STANDALONE.Mono.dll -Destination $Path2
Copy-Item $Path1\UniverseLib.Mono.dll -Destination $Path2
Compress-Archive -Path UnityEditorPackage\* -CompressionLevel Fastest -DestinationPath Release\UnityExplorer.Editor.zip -Force