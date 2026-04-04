using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Interactable : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] AnimationClip enterAnimationClip;

    // Optional: loops while the character is seated.
    // If null with no exitClip, enterClip is treated as a pure one-shot.
    [SerializeField] AnimationClip idleAnimationClip;

    // Optional: played when the character stands up.
    // Requires idleAnimationClip to be set (or is skipped).
    [SerializeField] AnimationClip exitAnimationClip;

    [Header("Snap Point")]
    [SerializeField] Transform snapPoint;

    public AnimationClip GetEnterAnimationClip() => enterAnimationClip;
    public AnimationClip GetIdleAnimationClip()  => idleAnimationClip;
    public AnimationClip GetExitAnimationClip()  => exitAnimationClip;
    public Transform     GetSnapPoint()          => snapPoint;

    CharacterController occupant;

    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out occupant))
            occupant.OnEnterInteractable(this);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out occupant))
        {
            occupant.OnExitInteractable(this);
            occupant = null;
        }
    }
}