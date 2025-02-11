#include "UE5_fileSplitter.h"
#include "Modules/ModuleManager.h"
#include "Editor.h"
#include "CoreMinimal.h"

IMPLEMENT_MODULE(FUE5FileSplitterModule, UE5FileSplitter)

void FUE5FileSplitterModule::StartupModule()
{
    FString Msg = FPaths::ConvertRelativePathToFull(FPaths::ProjectDir() + "_MergeFiles.exe");
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

void FUE5FileSplitterModule::ShutdownModule()//Does it run if shift+f5 in vs? Not sure....
{
    FString Msg = FPaths::ConvertRelativePathToFull(FPaths::ProjectDir() + "_SplitFiles.exe");
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
