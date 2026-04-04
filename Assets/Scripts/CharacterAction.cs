using System;
using UnityEngine;

[Serializable]
public abstract class CharacterAction
{
    public string      Name;
    public Interactable Target;
    public float       Duration = 3f;

    // Wired up by CharacterController before Execute() is called.
    // Must be invoked by the action when it has fully completed (including exit anim).
    public Action OnComplete;

    protected bool started;

    public virtual void Execute(CharacterController controller)
    {
        started = false;

        if (Target == null)
        {
            OnComplete?.Invoke();   // nothing to do; let the AI move on immediately
            return;
        }

        controller.MoveTo(Target.transform.position);
    }

    public virtual void OnEnterInteractable(
        CharacterController controller,
        Interactable interactable) { }
}