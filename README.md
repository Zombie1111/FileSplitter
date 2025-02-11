<h1 align="center">File Splitter by David Westberg</h1>

## Overview
A fast and simple alternative to git LFS for unity that works with any github repo. Splits large files into multiple smaller files, automatically adds the large files to your .gitignore and merges the splitted files back into their orginal large file.

<img src="https://i.postimg.cc/FKWKv9Tb/file-Splitter-Image.png" width="100%" height="100%"/>

## Key Features
<ul>
<li>Split large files into multiple smaller files</li>
<li>Merge splitted files back into their orginal large file</li>
<li>Adds splitted large files to your .gitignore</li>
<li>Multithreaded, splits and merges multiple files in parallel</li>
</ul>

## Instructions
**How To Use**
<ol>
    <li>Download and copy _MergeFiles.exe and _SplitFiles.exe into the root folder of your project (Where your .gitingore file is located)</li>
    <li>Run _MergeFiles.exe before opening your project and _SplitFiles.exe after closing your project</li>
</ol>

## Technical Details
**Splitting Files**

A file is splitted by reading all of its bytes into an array and writing every `SplitConfig.splitFilesLargerThanMB * 1000000` byte to a seperate file in a temporary folder. The path to the files is then saved.
Adds the orginal file path to the .gitignore file, a copy of the .gitignore file is created before modifying it.

**Merging Files**

Loops through all saved file paths and verifies all splitted files still exists. Reads the splitted files bytes back into a single array and overwrites the originally splitted file.
Restores the .gitignore file from the copy created before modifying it and deletes all files in the temporary folder

## License
This project is licensed under MIT - See the `LICENSE` file for more details.
