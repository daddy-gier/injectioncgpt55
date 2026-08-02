#pragma once

#include "CoreMinimal.h"
#include "AIController.h"
#include "NHGameplayTypes.h"
#include "Perception/AIPerceptionComponent.h"
#include "NH_SimpleAIController.generated.h"

UCLASS()
class NYGHTSHADEHOLLOW_API ANH_SimpleAIController : public AAIController
{
    GENERATED_BODY()

public:
    ANH_SimpleAIController();

    // ── Perception ──────────────────────────────────────────────────
    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = "AI|Perception")
    UAIPerceptionComponent* PerceptionComponent;

    // ── Suspicion ───────────────────────────────────────────────────
    UFUNCTION(BlueprintCallable, Category = "AI|Suspicion")
    void AddSuspicion(float Amount);

    UFUNCTION(BlueprintPure, Category = "AI|Suspicion")
    float GetSuspicion() const { return Suspicion; }

    UFUNCTION(BlueprintCallable, Category = "AI|Suspicion")
    void ClearSuspicion() { Suspicion = 0.f; }

    // ── State ────────────────────────────────────────────────────────
    UFUNCTION(BlueprintCallable, Category = "AI|State")
    void SetAIState(ENHAIState NewState);

    UFUNCTION(BlueprintPure, Category = "AI|State")
    ENHAIState GetAIState() const { return CurrentState; }

    // ── Patrol ───────────────────────────────────────────────────────
    UFUNCTION(BlueprintCallable, Category = "AI|Patrol")
    void SetPatrolPoints(const TArray<AActor*>& Points);

    UFUNCTION(BlueprintCallable, Category = "AI|Patrol")
    void MoveToNextPatrolPoint();

    // ── Behavior Tree ────────────────────────────────────────────────
    UPROPERTY(EditDefaultsOnly, Category = "AI")
    class UBehaviorTree* BehaviorTree;

    UPROPERTY(EditDefaultsOnly, Category = "AI")
    float AlertSuspicionThreshold = 60.f;

    UPROPERTY(EditDefaultsOnly, Category = "AI")
    float ArrestSuspicionThreshold = 90.f;

protected:
    virtual void BeginPlay() override;
    virtual void Tick(float DeltaTime) override;
    virtual void OnPossess(APawn* InPawn) override;

    UFUNCTION()
    void OnPerceptionUpdated(AActor* Actor, FAIStimulus Stimulus);

private:
    float Suspicion = 0.f;
    ENHAIState CurrentState = ENHAIState::Idle;
    TArray<AActor*> PatrolPoints;
    int32 PatrolIndex = 0;
    FVector LastKnownPlayerLocation;

    void UpdateSuspicionDecay(float DeltaTime);
    void EvaluateStateTransition();
    bool CanSeePlayer() const;
};
