using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CharacterController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float wanderRadius    = 5f;
    [SerializeField] float minDecisionTime = 2f;
    [SerializeField] float maxDecisionTime = 5f;

    [Header("Animations")]
    [SerializeField] AnimationClip idleAnimation;
    [SerializeField] AnimationClip walkAnimation;

    AnimationSystem animationSystem;
    Animator        animator;
    NavMeshAgent    agent;
    public NavMeshAgent Agent => agent;

    enum CharacterState { Idle, Wander, Action }
    CharacterState currentState;
    float          stateTimer;

    List<CharacterAction> actionPool    = new();
    CharacterAction       currentAction;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    void Start()
    {
        animator = GetComponent<Animator>();
        agent    = GetComponent<NavMeshAgent>();

        animationSystem = new AnimationSystem(this, animator, idleAnimation, walkAnimation);

        if (actionPool.Count == 0)
        {
            var interactables = FindObjectsByType<Interactable>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            foreach (var i in interactables)
                actionPool.Add(new SitAction { Name = "Sit", Target = i, Duration = 5f });
        }

        DecideNextState();
    }

    void Update()
    {
        stateTimer -= Time.deltaTime;

        UpdateAnimationFromAgent();

        switch (currentState)
        {
            case CharacterState.Idle:   HandleIdle();   break;
            case CharacterState.Wander: HandleWander(); break;
            case CharacterState.Action: HandleAction(); break;
        }

        // Timer-based decisions are suppressed while an action is running.
        // The action calls OnActionComplete() itself when it is fully done.
        if (stateTimer <= 0f && currentState != CharacterState.Action)
            DecideNextState();
    }

    void OnDestroy() => animationSystem.Destroy();

    // ── State decision ────────────────────────────────────────────────────────

    void DecideNextState()
    {
        float roll = UnityEngine.Random.value;
        if      (roll < 0.4f) EnterIdle();
        else if (roll < 0.8f) EnterWander();
        else                  EnterAction();
    }

    // ── Idle ──────────────────────────────────────────────────────────────────

    void EnterIdle()
    {
        currentState = CharacterState.Idle;
        stateTimer   = UnityEngine.Random.Range(minDecisionTime, maxDecisionTime);
        agent.isStopped = true;
    }

    void HandleIdle() { }

    // ── Wander ────────────────────────────────────────────────────────────────

    void EnterWander()
    {
        currentState = CharacterState.Wander;
        stateTimer   = UnityEngine.Random.Range(4f, 8f);
        agent.isStopped = false;
        agent.SetDestination(GetRandomNavMeshLocation());
    }

    void HandleWander()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            DecideNextState();
    }

    Vector3 GetRandomNavMeshLocation()
    {
        Vector3 dir = UnityEngine.Random.insideUnitSphere * wanderRadius + transform.position;
        return NavMesh.SamplePosition(dir, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas)
            ? hit.position
            : transform.position;
    }

    // ── Action ────────────────────────────────────────────────────────────────

    void EnterAction()
    {
        if (actionPool.Count == 0) { EnterIdle(); return; }

        currentState = CharacterState.Action;
        stateTimer   = float.MaxValue;  // ← block all timer-based interruptions

        currentAction           = actionPool[UnityEngine.Random.Range(0, actionPool.Count)];
        currentAction.OnComplete = OnActionComplete;
        currentAction.Execute(this);
    }

    void HandleAction() { }   // action manages itself via coroutines + OnComplete

    // Called by the action when it has fully finished (including exit animation).
    void OnActionComplete()
    {
        currentAction = null;
        DecideNextState();
    }

    // ── Animation public API ──────────────────────────────────────────────────

    /// Play <paramref name="clip"/> once; <paramref name="onFinished"/> fires when done.
    public void PlayOneShotAnimation(AnimationClip clip, Action onFinished)
        => animationSystem.PlayOneShot(clip, onFinished);

    /// Blend <paramref name="clip"/> in on the action layer and keep it looping.
    public void PlayLoopingAnimation(AnimationClip clip)
        => animationSystem.PlayLooping(clip);

    /// Blend the action layer back to locomotion; <paramref name="onFinished"/> fires when done.
    public void StopActionAnimation(Action onFinished = null)
        => animationSystem.StopActionLayer(onFinished: onFinished);

    // ── Movement public API ───────────────────────────────────────────────────

    public void MoveTo(Vector3 position)
    {
        agent.isStopped = false;
        agent.SetDestination(position);
    }

    // ── Interactable callbacks ────────────────────────────────────────────────

    public void OnEnterInteractable(Interactable interactable)
        => currentAction?.OnEnterInteractable(this, interactable);

    public void OnExitInteractable(Interactable interactable) { }

    public void SetRootMotion(bool enabled)
        => animator.applyRootMotion = enabled;

    // ── Internal ─────────────────────────────────────────────────────────────

    void UpdateAnimationFromAgent()
        => animationSystem.UpdateLocomotion(new Vector3(0, 0, agent.velocity.magnitude), 1f);
}