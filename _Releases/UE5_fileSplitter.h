#pragma once

#include "CoreMinimal.h"
#include "Modules/ModuleManager.h"

/**
 * Editor Module to handle editor startup and shutdown.
 */
class SHOOTER_API FUE5FileSplitterModule : public IModuleInterface
{
public:
    /** Called when the module is loaded */
    virtual void StartupModule() override;

    /** Called when the module is unloaded */
    virtual void ShutdownModule() override;

private:

};
