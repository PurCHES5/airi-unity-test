using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Interactable : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField]
    AnimationClip enterAnimationClip;

    [Header("Snap Point")]
    [SerializeField]
    Transform snapPoint;   // where character should sit

    public AnimationClip GetEnterAnimationClip()
        => enterAnimationClip;

    public Transform GetSnapPoint()
        => snapPoint;

    CharacterController characterController;

    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out characterController))
        {
            characterController.OnEnterInteractable(this);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out characterController))
        {
            characterController.OnExitInteractable(this);
            characterController = null;
        }
    }
}