#pragma once

#include "CoreMinimal.h"
#include "GameFramework/GameStateBase.h"
#include "NHGameplayTypes.h"
#include "NH_GameState.generated.h"

DECLARE_DYNAMIC_MULTICAST_DELEGATE_OneParam(FOnAlertLevelChanged, ENHAlertLevel, NewLevel);
DECLARE_DYNAMIC_MULTICAST_DELEGATE_TwoParams(FOnWorldFlagChanged, const FString&, Key, bool, Value);

UCLASS()
class NYGHTSHADEHOLLOW_API ANH_GameState : public AGameStateBase
{
    GENERATED_BODY()

public:
    ANH_GameState();

    // ── Alert System ────────────────────────────────────────────────
    UFUNCTION(BlueprintCallable, Category = "Prison|Alert")
    void EscalateAlert();

    UFUNCTION(BlueprintCallable, Category = "Prison|Alert")
    void DeescalateAlert();

    UFUNCTION(BlueprintPure, Category = "Prison|Alert")
    ENHAlertLevel GetCurrentAlertLevel() const { return CurrentAlertLevel; }

    UFUNCTION(BlueprintPure, Category = "Prison|Alert")
    bool IsLockdown() const { return CurrentAlertLevel == ENHAlertLevel::Lockdown; }

    // ── World Flags ─────────────────────────────────────────────────
    UFUNCTION(BlueprintCallable, Category = "Prison|State")
    void SetWorldFlag(const FString& Key, bool Value);

    UFUNCTION(BlueprintPure, Category = "Prison|State")
    bool GetWorldFlag(const FString& Key) const;

    // ── Reputation ──────────────────────────────────────────────────
    UFUNCTION(BlueprintCallable, Category = "Prison|Reputation")
    void ModifyPlayerReputation(int32 Delta);

    UFUNCTION(BlueprintPure, Category = "Prison|Reputation")
    int32 GetPlayerReputation() const { return PlayerReputation; }

    // ── Faction Standing ────────────────────────────────────────────
    UFUNCTION(BlueprintCallable, Category = "Prison|Faction")
    void ModifyFactionStanding(ENHFaction Faction, int32 Delta);

    UFUNCTION(BlueprintPure, Category = "Prison|Faction")
    int32 GetFactionStanding(ENHFaction Faction) const;

    // ── Delegates ───────────────────────────────────────────────────
    UPROPERTY(BlueprintAssignable, Category = "Prison|Alert")
    FOnAlertLevelChanged OnAlertLevelChanged;

    UPROPERTY(BlueprintAssignable, Category = "Prison|State")
    FOnWorldFlagChanged OnWorldFlagChanged;

protected:
    UPROPERTY(Replicated, BlueprintReadOnly, Category = "Prison")
    ENHAlertLevel CurrentAlertLevel = ENHAlertLevel::Green;

    UPROPERTY(Replicated, BlueprintReadOnly, Category = "Prison")
    int32 PlayerReputation = 0;

    UPROPERTY(Replicated)
    TMap<FString, bool> WorldFlags;

    UPROPERTY(Replicated)
    TMap<TEnumAsByte<ENHFaction>, int32> FactionStandings;

    virtual void GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const override;
};
