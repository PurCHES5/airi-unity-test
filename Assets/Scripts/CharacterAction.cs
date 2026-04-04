using UnityEngine;
using UnityEngine.AI;
using System;

[Serializable]
public abstract class CharacterAction
{
    public string Name;

    public Interactable Target;

    public float Duration = 3f;

    protected bool started;

    public virtual void Execute(CharacterController controller)
    {
        started = false;

        if (Target == null)
            return;

        controller.MoveTo(Target.transform.position);
    }

    public virtual void OnEnterInteractable(
        CharacterController controller,
        Interactable interactable)
    {
    }
}