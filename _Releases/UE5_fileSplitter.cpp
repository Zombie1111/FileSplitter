﻿//By David Westberg, github.com/Zombie1111/FileSplitter
#if WITH_EDITOR
#include "UE5_fileSplitter.h"
#include "Modules/ModuleManager.h"
#include "Editor.h"
#include "CoreMinimal.h"

IMPLEMENT_MODULE(FUE5FileSplitterModule, UE5FileSplitter)

const FString splitBasePath = "";//Should be relative to project root directory

void FUE5FileSplitterModule::StartupModule()
{
    FString Msg = FPaths::ConvertRelativePathToFull(FPaths::ProjectDir() + splitBasePath + "xMergeFiles.exe");
    UE_LOG(LogTemp, Warning, TEXT("Merging splitted files: %s"), *Msg);

    FProcHandle Handle = FPlatformProcess::CreateProc(*Msg, nullptr, true, false, false, nullptr, 0, nullptr, nullptr);

    if (Handle.IsValid())
    {
        FPlatformProcess::WaitForProc(Handle);
    }
    else
    {
        UE_LOG(LogTemp, Error, TEXT("Merging failed: %s"), *Msg);
        throw;//Dont allow starting without merging
    }
}

void FUE5FileSplitterModule::ShutdownModule()
{
    FString Msg = FPaths::ConvertRelativePathToFull(FPaths::ProjectDir() + splitBasePath + "xSplitFiles.exe");
    UE_LOG(LogTemp, Warning, TEXT("Running file splitter: %s"), *Msg);

    FProcHandle Handle = FPlatformProcess::CreateProc(*Msg, nullptr, true, false, false, nullptr, 0, nullptr, nullptr);

    if (Handle.IsValid())
    {
        FPlatformProcess::WaitForProc(Handle);
    }
    else
    {
        UE_LOG(LogTemp, Error, TEXT("Splitting failed: %s"), *Msg);
    }
}
#endif
