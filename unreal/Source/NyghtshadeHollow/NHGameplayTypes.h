#pragma once

#include "CoreMinimal.h"
#include "NHGameplayTypes.generated.h"

UENUM(BlueprintType)
enum class ENHFaction : uint8
{
    AdminCore    UMETA(DisplayName = "Admin Core"),
    Medical      UMETA(DisplayName = "Medical"),
    Inmates      UMETA(DisplayName = "Inmates"),
    GRAVE        UMETA(DisplayName = "G.R.A.V.E."),
    Syndicates   UMETA(DisplayName = "Syndicates"),
    Outsiders    UMETA(DisplayName = "Outsiders"),
    Neutral      UMETA(DisplayName = "Neutral"),
};

UENUM(BlueprintType)
enum class ENHAlertLevel : uint8
{
    Green    UMETA(DisplayName = "Green"),
    Yellow   UMETA(DisplayName = "Yellow"),
    Orange   UMETA(DisplayName = "Orange"),
    Red      UMETA(DisplayName = "Red"),
    Lockdown UMETA(DisplayName = "Lockdown"),
};

UENUM(BlueprintType)
enum class ENHAIState : uint8
{
    Idle        UMETA(DisplayName = "Idle"),
    Patrol      UMETA(DisplayName = "Patrol"),
    Investigate UMETA(DisplayName = "Investigate"),
    Alert       UMETA(DisplayName = "Alert"),
    Arrest      UMETA(DisplayName = "Arrest"),
    Fleeing     UMETA(DisplayName = "Fleeing"),
    Dialogue    UMETA(DisplayName = "Dialogue"),
    Dead        UMETA(DisplayName = "Dead"),
};

UENUM(BlueprintType)
enum class ENHInteractionType : uint8
{
    Talk     UMETA(DisplayName = "Talk"),
    Trade    UMETA(DisplayName = "Trade"),
    Threaten UMETA(DisplayName = "Threaten"),
    Bribe    UMETA(DisplayName = "Bribe"),
    Examine  UMETA(DisplayName = "Examine"),
};

UENUM(BlueprintType)
enum class ENHQuestStatus : uint8
{
    Locked    UMETA(DisplayName = "Locked"),
    Available UMETA(DisplayName = "Available"),
    Active    UMETA(DisplayName = "Active"),
    Completed UMETA(DisplayName = "Completed"),
    Failed    UMETA(DisplayName = "Failed"),
};

USTRUCT(BlueprintType)
struct NYGHTSHADEHOLLOW_API FNHItemData
{
    GENERATED_BODY()

    UPROPERTY(EditAnywhere, BlueprintReadWrite) FString ID;
    UPROPERTY(EditAnywhere, BlueprintReadWrite) FString DisplayName;
    UPROPERTY(EditAnywhere, BlueprintReadWrite) FString Description;
    UPROPERTY(EditAnywhere, BlueprintReadWrite) bool bIsContraband = false;
    UPROPERTY(EditAnywhere, BlueprintReadWrite) int32 ContrabandSeverity = 0;
    UPROPERTY(EditAnywhere, BlueprintReadWrite) int32 Quantity = 1;
};

USTRUCT(BlueprintType)
struct NYGHTSHADEHOLLOW_API FNHQuestObjective
{
    GENERATED_BODY()

    UPROPERTY(EditAnywhere, BlueprintReadWrite) FString ID;
    UPROPERTY(EditAnywhere, BlueprintReadWrite) FString Description;
    UPROPERTY(EditAnywhere, BlueprintReadWrite) bool bIsCompleted = false;
    UPROPERTY(EditAnywhere, BlueprintReadWrite) bool bIsOptional = false;
    UPROPERTY(EditAnywhere, BlueprintReadWrite) FString RequiredFlag;
};
