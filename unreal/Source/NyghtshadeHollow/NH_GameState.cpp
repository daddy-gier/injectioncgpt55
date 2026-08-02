#include "NH_GameState.h"
#include "Net/UnrealNetwork.h"

ANH_GameState::ANH_GameState()
{
    // Initialize faction standings
    for (int32 i = 0; i < static_cast<int32>(ENHFaction::Neutral); i++)
        FactionStandings.Add(static_cast<ENHFaction>(i), 0);
}

void ANH_GameState::GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const
{
    Super::GetLifetimeReplicatedProps(OutLifetimeProps);
    DOREPLIFETIME(ANH_GameState, CurrentAlertLevel);
    DOREPLIFETIME(ANH_GameState, PlayerReputation);
    DOREPLIFETIME(ANH_GameState, WorldFlags);
    DOREPLIFETIME(ANH_GameState, FactionStandings);
}

void ANH_GameState::EscalateAlert()
{
    int32 Next = FMath::Min(static_cast<int32>(CurrentAlertLevel) + 1,
                            static_cast<int32>(ENHAlertLevel::Lockdown));
    CurrentAlertLevel = static_cast<ENHAlertLevel>(Next);
    OnAlertLevelChanged.Broadcast(CurrentAlertLevel);
}

void ANH_GameState::DeescalateAlert()
{
    int32 Prev = FMath::Max(static_cast<int32>(CurrentAlertLevel) - 1, 0);
    CurrentAlertLevel = static_cast<ENHAlertLevel>(Prev);
    OnAlertLevelChanged.Broadcast(CurrentAlertLevel);
}

void ANH_GameState::SetWorldFlag(const FString& Key, bool Value)
{
    WorldFlags.Add(Key, Value);
    OnWorldFlagChanged.Broadcast(Key, Value);
}

bool ANH_GameState::GetWorldFlag(const FString& Key) const
{
    const bool* Val = WorldFlags.Find(Key);
    return Val ? *Val : false;
}

void ANH_GameState::ModifyPlayerReputation(int32 Delta)
{
    PlayerReputation = FMath::Clamp(PlayerReputation + Delta, -100, 100);
}

void ANH_GameState::ModifyFactionStanding(ENHFaction Faction, int32 Delta)
{
    int32& Standing = FactionStandings.FindOrAdd(Faction);
    Standing = FMath::Clamp(Standing + Delta, -100, 100);
}

int32 ANH_GameState::GetFactionStanding(ENHFaction Faction) const
{
    const int32* Standing = FactionStandings.Find(Faction);
    return Standing ? *Standing : 0;
}
