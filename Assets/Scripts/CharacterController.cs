using UnityEngine;
using UnityEngine.AI;
using System;
using System.Collections.Generic;

public class CharacterController : MonoBehaviour
{
    [Header("Movement")]

    [SerializeField]
    float wanderRadius = 5f;

    [SerializeField]
    float minDecisionTime = 2f;

    [SerializeField]
    float maxDecisionTime = 5f;

    [Header("Animations")]

    [SerializeField]
    AnimationClip idleAnimation;

    [SerializeField]
    AnimationClip walkAnimation;

    AnimationSystem animationSystem;
    Animator animator;
    NavMeshAgent agent;
    public NavMeshAgent Agent => agent;

    bool isPlayingOneShotAnim;

    enum CharacterState
    {
        Idle,
        Wander,
        Action
    }

    CharacterState currentState;

    float stateTimer;

    List<CharacterAction> actionPool = new List<CharacterAction>();

    CharacterAction currentAction;

    void Start()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();

        animationSystem =
            new AnimationSystem(
                animator,
                idleAnimation,
                walkAnimation
            );

        if (actionPool.Count == 0)
        {
            var sitable =
                FindObjectsByType<Interactable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            foreach (var sa in sitable)
            {
                actionPool.Add(
                    new SitAction
                    {
                        Name = "Sit",
                        Target = sa,
                        Duration = 10f
                    }
                );
            }
        }

        DecideNextState();
    }

    void Update()
    {
        stateTimer -= Time.deltaTime;

        UpdateAnimationFromAgent();

        switch (currentState)
        {
            case CharacterState.Idle:
                HandleIdle();
                break;

            case CharacterState.Wander:
                HandleWander();
                break;

            case CharacterState.Action:
                HandleAction();
                break;
        }

        if (stateTimer <= 0)
        {
            DecideNextState();
        }
    }

    void OnDestroy()
    {
        animationSystem.Destroy();
    }

    // =========================
    // STATE DECISION
    // =========================

    void DecideNextState()
    {
        float roll = UnityEngine.Random.value;

        if (roll < 0.4f)
        {
            EnterIdle();
        }
        else if (roll < 0.8f)
        {
            EnterWander();
        }
        else
        {
            EnterAction();
        }
    }

    // =========================
    // IDLE
    // =========================

    void EnterIdle()
    {
        currentState = CharacterState.Idle;

        stateTimer =
            UnityEngine.Random.Range(
                minDecisionTime,
                maxDecisionTime
            );

        agent.isStopped = true;
    }

    void HandleIdle()
    {
        // nothing needed
    }

    // =========================
    // WANDER
    // =========================

    void EnterWander()
    {
        currentState = CharacterState.Wander;

        stateTimer =
            UnityEngine.Random.Range(
                4f,
                8f
            );

        Vector3 destination =
            GetRandomNavMeshLocation();

        agent.isStopped = false;

        agent.SetDestination(destination);
    }

    void HandleWander()
    {
        if (!agent.pathPending &&
            agent.remainingDistance <= agent.stoppingDistance)
        {
            DecideNextState();
        }
    }

    Vector3 GetRandomNavMeshLocation()
    {
        Vector3 randomDirection =
            UnityEngine.Random.insideUnitSphere *
            wanderRadius;

        randomDirection += transform.position;

        NavMeshHit hit;

        if (NavMesh.SamplePosition(
                randomDirection,
                out hit,
                wanderRadius,
                NavMesh.AllAreas))
        {
            return hit.position;
        }

        return transform.position;
    }

    // =========================
    // ACTION (stub)
    // =========================

    void EnterAction()
    {
        currentState = CharacterState.Action;

        if (actionPool.Count == 0)
        {
            EnterIdle();
            return;
        }

        currentAction =
            actionPool[
                UnityEngine.Random.Range(
                    0,
                    actionPool.Count
                )
            ];

        currentAction.Execute(this);
    }

    void HandleAction()
    {
    }

    // =========================
    // ANIMATION SYNC
    // =========================

    void UpdateAnimationFromAgent()
    {
        float speed =
            agent.velocity.magnitude;

        animationSystem.UpdateLocomotion(
            new Vector3(0, 0, speed),
            1
        );
    }

    // =========================
    // ONE SHOT
    // =========================

    public void PlayOneShotAnimation(
        Interactable obj)
    {
        if (isPlayingOneShotAnim)
            return;

        Debug.Log("Playing sit anim");
        isPlayingOneShotAnim = true;

        animationSystem.PlayOneShot(
            obj.GetEnterAnimationClip(),
            () =>
            {
                isPlayingOneShotAnim = false;
            }
        );
    }

    public void MoveTo(Vector3 position)
    {
        agent.isStopped = false;
        agent.SetDestination(position);
    }

    public void OnEnterInteractable(
        Interactable interactable)
    {
        currentAction?.OnEnterInteractable(
            this,
            interactable
        );
    }

    public void OnExitInteractable(
        Interactable interactable)
    {
    }
}