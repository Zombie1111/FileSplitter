start /w _MergeFiles.exe

@echo off
setlocal enabledelayedexpansion

::Start the first .sln file in the current directory
for %%F in (*.sln) do (
    set "solution=%%F"
    goto :run
)

echo No .sln file found!
exit /b

:run

start /w "" "devenv" "!solution!"

start /w _SplitFiles.exe