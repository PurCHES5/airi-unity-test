using UnityEngine;
using UnityEngine.AI;

[System.Serializable]
public class SitAction : CharacterAction
{
    public float alignSpeed = 4f;

    bool aligning;

    Transform snapPoint;

    public override void Execute(
        CharacterController controller)
    {
        base.Execute(controller);

        Debug.Log("Going to sit");
    }

    public override void OnEnterInteractable(
        CharacterController controller,
        Interactable interactable)
    {
        if (started)
            return;

        if (interactable != Target)
            return;

        started = true;

        controller.StartCoroutine(
            AlignAndSit(controller, interactable)
        );
    }

    System.Collections.IEnumerator AlignAndSit(
        CharacterController controller,
        Interactable interactable)
    {
        var agent = controller.Agent;

        agent.isStopped = true;
        agent.updateRotation = false;

        snapPoint =
            interactable.GetSnapPoint();

        Transform t =
            controller.transform;

        while (true)
        {
            Vector3 targetPos =
                snapPoint.position;

            Quaternion targetRot =
                snapPoint.rotation;

            t.position =
                Vector3.Lerp(
                    t.position,
                    targetPos,
                    Time.deltaTime * alignSpeed
                );

            t.rotation =
                Quaternion.Slerp(
                    t.rotation,
                    targetRot,
                    Time.deltaTime * alignSpeed
                );

            Debug.Log(Vector3.Distance(
                    t.position,
                    targetPos));
            if (Vector3.Distance(
                    t.position,
                    targetPos) < 1f &&
                Quaternion.Angle(
                    t.rotation,
                    targetRot) < 1f)
            {
                break;
            }

            yield return null;
        }

        agent.updateRotation = true;

        controller.PlayOneShotAnimation(
            interactable
        );
    }
}