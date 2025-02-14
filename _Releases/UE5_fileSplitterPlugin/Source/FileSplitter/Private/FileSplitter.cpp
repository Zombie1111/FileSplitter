// Copyright Epic Games, Inc. All Rights Reserved.
#if WITH_EDITOR
#include "FileSplitter.h"

#define LOCTEXT_NAMESPACE "FFileSplitterModule"

const FString splitBasePath = "";//Should be relative to project root directory

void FFileSplitterModule::StartupModule()
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

void FFileSplitterModule::ShutdownModule()
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

#undef LOCTEXT_NAMESPACE
	
IMPLEMENT_MODULE(FFileSplitterModule, FileSplitter)
#endif
