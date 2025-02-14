// Copyright Epic Games, Inc. All Rights Reserved.
#if WITH_EDITOR
#pragma once

#include "Modules/ModuleManager.h"

class FFileSplitterModule : public IModuleInterface
{
public:

	/** IModuleInterface implementation */
	virtual void StartupModule() override;
	virtual void ShutdownModule() override;
};
#endif

