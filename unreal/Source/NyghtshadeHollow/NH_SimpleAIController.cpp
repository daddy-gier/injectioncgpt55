#include "NH_SimpleAIController.h"
#include "BehaviorTree/BehaviorTree.h"
#include "BehaviorTree/BlackboardComponent.h"
#include "Perception/AISenseConfig_Sight.h"
#include "Perception/AISenseConfig_Hearing.h"
#include "NavigationSystem.h"
#include "NH_GameState.h"
#include "Kismet/GameplayStatics.h"

ANH_SimpleAIController::ANH_SimpleAIController()
{
    PrimaryActorTick.bCanEverTick = true;

    PerceptionComponent = CreateDefaultSubobject<UAIPerceptionComponent>(TEXT("AIPerception"));
    SetPerceptionComponent(*PerceptionComponent);

    // Sight config
    auto* SightConfig = CreateDefaultSubobject<UAISenseConfig_Sight>(TEXT("SightConfig"));
    SightConfig->SightRadius = 1200.f;
    SightConfig->LoseSightRadius = 1500.f;
    SightConfig->PeripheralVisionAngleDegrees = 60.f;
    SightConfig->SetMaxAge(5.f);
    SightConfig->DetectionByAffiliation.bDetectEnemies = true;
    SightConfig->DetectionByAffiliation.bDetectNeutrals = true;
    PerceptionComponent->ConfigureSense(*SightConfig);

    // Hearing config
    auto* HearConfig = CreateDefaultSubobject<UAISenseConfig_Hearing>(TEXT("HearConfig"));
    HearConfig->HearingRange = 600.f;
    HearConfig->SetMaxAge(3.f);
    HearConfig->DetectionByAffiliation.bDetectEnemies = true;
    HearConfig->DetectionByAffiliation.bDetectNeutrals = true;
    PerceptionComponent->ConfigureSense(*HearConfig);

    PerceptionComponent->SetDominantSense(*SightConfig->GetSenseImplementation());
}

void ANH_SimpleAIController::BeginPlay()
{
    Super::BeginPlay();
    PerceptionComponent->OnTargetPerceptionUpdated.AddDynamic(this, &ANH_SimpleAIController::OnPerceptionUpdated);

    if (BehaviorTree)
        RunBehaviorTree(BehaviorTree);
}

void ANH_SimpleAIController::OnPossess(APawn* InPawn)
{
    Super::OnPossess(InPawn);
    if (BehaviorTree)
        RunBehaviorTree(BehaviorTree);
}

void ANH_SimpleAIController::Tick(float DeltaTime)
{
    Super::Tick(DeltaTime);
    UpdateSuspicionDecay(DeltaTime);
    EvaluateStateTransition();
}

void ANH_SimpleAIController::OnPerceptionUpdated(AActor* Actor, FAIStimulus Stimulus)
{
    if (!Actor || !Actor->ActorHasTag("Player")) return;

    if (Stimulus.WasSuccessfullySensed())
    {
        AddSuspicion(Stimulus.Strength * 25.f);
        LastKnownPlayerLocation = Actor->GetActorLocation();

        if (Blackboard)
            Blackboard->SetValueAsVector("LastKnownPlayerLocation", LastKnownPlayerLocation);
    }
}

void ANH_SimpleAIController::AddSuspicion(float Amount)
{
    auto* GS = GetWorld() ? GetWorld()->GetGameState<ANH_GameState>() : nullptr;
    float Multiplier = (GS && GS->IsLockdown()) ? 2.f : 1.f;
    Suspicion = FMath::Clamp(Suspicion + Amount * Multiplier, 0.f, 100.f);
}

void ANH_SimpleAIController::UpdateSuspicionDecay(float DeltaTime)
{
    if (CurrentState == ENHAIState::Alert || CurrentState == ENHAIState::Arrest) return;
    Suspicion = FMath::Max(Suspicion - 5.f * DeltaTime, 0.f);
}

void ANH_SimpleAIController::EvaluateStateTransition()
{
    if (Suspicion >= ArrestSuspicionThreshold && CurrentState != ENHAIState::Arrest)
        SetAIState(ENHAIState::Arrest);
    else if (Suspicion >= AlertSuspicionThreshold && CurrentState == ENHAIState::Idle)
        SetAIState(ENHAIState::Alert);
    else if (Suspicion < 10.f && CurrentState == ENHAIState::Alert)
        SetAIState(ENHAIState::Patrol);
}

void ANH_SimpleAIController::SetAIState(ENHAIState NewState)
{
    CurrentState = NewState;
    if (Blackboard)
        Blackboard->SetValueAsEnum("AIState", static_cast<uint8>(NewState));
}

void ANH_SimpleAIController::SetPatrolPoints(const TArray<AActor*>& Points)
{
    PatrolPoints = Points;
    PatrolIndex = 0;
}

void ANH_SimpleAIController::MoveToNextPatrolPoint()
{
    if (PatrolPoints.Num() == 0) return;
    MoveToActor(PatrolPoints[PatrolIndex]);
    PatrolIndex = (PatrolIndex + 1) % PatrolPoints.Num();
}
