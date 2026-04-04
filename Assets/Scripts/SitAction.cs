using System.Collections;
using UnityEngine;

[System.Serializable]
public class SitAction : CharacterAction
{
    public float alignSpeed = 4f;
    [Tooltip("How far forward the character moves after standing up to clear the chair collider.")]
    public float exitForwardOffset = 0.6f; 

    public override void Execute(CharacterController controller)
    {
        base.Execute(controller);
        Debug.Log("Going to sit");
    }

    public override void OnEnterInteractable(CharacterController controller, Interactable interactable)
    {
        if (started || interactable != Target) return;

        started = true;
        controller.StartCoroutine(PerformSit(controller, interactable));
    }

    IEnumerator PerformSit(CharacterController controller, Interactable interactable)
    {
        var agent = controller.Agent;

        // ── 1. Align to seat ──────────────────────────────────────────────────
        agent.isStopped = true;
        agent.updateRotation = false;

        yield return controller.StartCoroutine(
            AlignToSnapPoint(controller.transform, interactable.GetSnapPoint()));

        // ── Resolve clips ─────────────────────────────────────────────────────
        var enterClip = interactable.GetEnterAnimationClip();
        var idleClip = interactable.GetIdleAnimationClip();
        var exitClip = interactable.GetExitAnimationClip();

        bool hasEnter = enterClip != null;
        bool hasIdle = idleClip != null;
        bool hasExit = exitClip != null;
        bool isPureOneShot = hasEnter && !hasIdle && !hasExit;

        // ── 2. Enter animation ────────────────────────────────────────────────
        if (hasEnter)
        {
            controller.SetRootMotion(false);

            bool done = false;
            controller.PlayOneShotAnimation(enterClip, () => done = true);
            yield return new WaitUntil(() => done);

            if (isPureOneShot)
                controller.SetRootMotion(true);
        }

        // ── 3. Stay / idle phase ──────────────────────────────────────────────
        if (hasIdle)
        {
            controller.PlayLoopingAnimation(idleClip);
            yield return new WaitForSeconds(Duration);
        }
        else if (!isPureOneShot)
        {
            yield return new WaitForSeconds(Duration);
        }

        // ── 4. Exit animation ─────────────────────────────────────────────────
        if (hasExit)
        {
            bool done = false;
            controller.PlayOneShotAnimation(exitClip, () => done = true);
            yield return new WaitUntil(() => done);

            controller.SetRootMotion(true);

            bool blendDone = false;
            controller.StopActionAnimation(() => blendDone = true);
            yield return new WaitUntil(() => blendDone);
        }
        else if (hasIdle)
        {
            bool blendDone = false;
            controller.StopActionAnimation(() => blendDone = true);
            yield return new WaitUntil(() => blendDone);
        }

        // ── 5. Displacement (Move forward to clear collider) ─────────────────
        // We do this BEFORE restoring the agent so the agent doesn't fight the move.
        if (exitForwardOffset > 0)
        {
            Vector3 targetPosition = controller.transform.position + (controller.transform.forward * exitForwardOffset);
            
            // Optional: Use Warp to ensure the NavMeshAgent stays synced with the move
            // agent.Warp(targetPosition); 
            
            // If you prefer a smooth slide rather than a "warp," you could 
            // insert a small Lerp coroutine here.
        }

        // ── 6. Restore agent ──────────────────────────────────────────────────
        agent.isStopped = false;
        agent.updateRotation = true;

        OnComplete?.Invoke();
    }

    IEnumerator AlignToSnapPoint(Transform character, Transform snapPoint)
    {
        while (true)
        {
            Vector3 targetPos = snapPoint.position;
            targetPos.y = character.position.y;

            character.position = Vector3.Lerp(character.position, targetPos, Time.deltaTime * alignSpeed);
            character.rotation = Quaternion.Slerp(character.rotation, snapPoint.rotation, Time.deltaTime * alignSpeed);

            Vector2 charXZ = new Vector2(character.position.x, character.position.z);
            Vector2 targetXZ = new Vector2(snapPoint.position.x, snapPoint.position.z);

            bool close = Vector2.Distance(charXZ, targetXZ) < 0.05f &&
                         Quaternion.Angle(character.rotation, snapPoint.rotation) < 1f;

            if (close) break;
            yield return null;
        }

        character.position = snapPoint.position;
        character.rotation = snapPoint.rotation;
    }
}