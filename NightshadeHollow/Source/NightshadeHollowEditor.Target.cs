// Copyright 2024 NightshadeHollow. All Rights Reserved.
using UnrealBuildTool;
using System.Collections.Generic;

public class NightshadeHollowEditorTarget : TargetRules
{
    public NightshadeHollowEditorTarget(TargetInfo Target) : base(Target)
    {
        Type = TargetType.Editor;
        DefaultBuildSettings = BuildSettingsVersion.V5;
        IncludeOrderVersion = EngineIncludeOrderVersion.Unreal5_3;
        ExtraModuleNames.Add("NightshadeHollow");
    }
}
