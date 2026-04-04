using UnityEngine;

public class SmoothFollowCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Position")]
    public Vector3 offset = new Vector3(0f, 2.5f, -2f);
    public float smoothTime = 0.15f;

    [Header("Rotation")]
    public float rotationSmoothSpeed = 10f;

    private Vector3 velocity = Vector3.zero;

    void LateUpdate()
    {
        if (target == null) return;

        // Desired position
        Vector3 desiredPosition = target.TransformPoint(offset);

        // Smooth position
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref velocity,
            smoothTime
        );

        // Desired rotation (look same direction as character)
        Quaternion desiredRotation = Quaternion.Euler(
            30f,
            target.eulerAngles.y,
            0f
        );

        // Smooth rotation
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            desiredRotation,
            rotationSmoothSpeed * Time.deltaTime
        );
    }
}