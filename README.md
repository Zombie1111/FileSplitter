<h1 align="center">File Splitter by David Westberg</h1>

## Overview
A fast and simple file splitter & merger, an alternative to git LFS. Splits large files into multiple smaller files, automatically adds the large files to your .gitignore and merges the splitted files back into their orginal large file.

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

Download and copy xMergeFiles.exe and xSplitFiles.exe found in _Releases into the root folder of your project (Usually where your .gitingore file is located).

Run _MergeFiles.exe before opening your project and _SplitFiles.exe after closing your project.

The _Releases folder contains example scripts to automate the splitting and merging process in Unity and UE5.

**Unity**

Follow General instruction and then place Releases/Unity_fileSplitter.cs anywhere inside your Assets folder.

**UE5**

Follow General instruction and then import _Releases/UE5_fileSplitter.h and _Releases/UE5_fileSplitter.cpp into your project using your IDE.

**Building Source**

Make sure you have Visual Studio installed with .NET desktop development workload.
Open the _Source/FileSplitter.sln solution.

Alternatively create a new project with C# Console App template and copy paste the code from _Source/FileSplitter/Program.cs into your .cs script.

## License
This project is licensed under MIT - See the `LICENSE` file for more details.
