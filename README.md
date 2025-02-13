<h1 align="center">File Splitter by David Westberg</h1>

## Overview
A fast and simple file splitter & merger, an alternative to git LFS. Splits large files into multiple smaller files, automatically adds the large files to your .gitignore and merges the splitted files back into their orginal large file.

I primary made this because some of the files in my projects exceeded 100mb and I dont like git LFS.

<img src="https://i.postimg.cc/FKWKv9Tb/file-Splitter-Image.png" width="100%" height="100%"/>

## Key Features
<ul>
<li>Split large files into multiple smaller files</li>
<li>Merge splitted files back into their orginal large file</li>
<li>Adds splitted large files to your .gitignore</li>
<li>Threading, splits and merges multiple files in parallel</li>
</ul>

## Instructions
**General**

Download and copy `_Releases/xMergeFiles.exe` and `_Releases/xSplitFiles.exe` into the root folder of your project (Usually where your .gitingore file is located).

Run the respective .exe to split/merge files. You usually wanna run `_MergeFiles.exe` before opening your project and `_SplitFiles.exe` after closing your project.

Make sure the files `xSplittedFilesData_8hlk.zombSplitData~`, `xSplittedFiles_TEMPONLY_hf4n~`, `xMergeFiles.exe` and `xSplitFiles.exe` wont be excluded by your .gitignore.

The `_Releases` folder contains example scripts to automate the splitting and merging process in Unity and UE5, read below for more info.

**Unity**

Follow `General` instruction and then place `Releases/Unity_fileSplitter.cs` anywhere inside your `Assets` folder.

**UE5**

Follow `General` instruction and then import `_Releases/UE5_fileSplitter.h` and `_Releases/UE5_fileSplitter.cpp` into your project using your IDE.

The .gitignore file for unreal excludes .exe files be defualt, fix this by adding `!xMergeFiles.exe` and `!xSplitFiles.exe` at the bottom.

**Building Source**

Make sure you have Visual Studio installed with .NET desktop development workload.
Open the `_Source/FileSplitter.sln` solution.

Alternatively create a new project with C# Console App template and copy paste the code from `_Source/FileSplitter/Program.cs` into your .cs script.

## Behaviour

All files found in the same folder as the .exe (Including sub folders) and matches the filter will be included in the splitting/merging. The filter can be modified in the `Config` region of `_Source/FileSplitter/Program.cs` (Along with other settings).

The parts of the splitted files will be placed inside `xSplittedFiles_TEMPONLY_hf4n~`, all files found in this folder will be deleted when merging

Data needed to merge files after splitting is stored in `xSplittedFilesData_8hlk.zombSplitData~`

The path to the splitted files will be added to your .gitignore, a copy of the .gitignore is created before modifying it that is used to restore it later when merging.

## License
This project is licensed under MIT - See the `LICENSE` file for more details.
