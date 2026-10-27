
using UnityEngine;

public class BearCameraFollow : MonoBehaviour
{
    [Header("Bear POV")]
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private Transform centerEyeAnchor;

    [Tooltip("Position offset in world space, in Unity units.")]
    [SerializeField] private Vector3 eyeOffset =
        new Vector3(0f, 2.5f, 0f);

    private Vector3 trackingOffset;

    private void Start()
    {
        if (cameraTarget == null || centerEyeAnchor == null)
        {
            Debug.LogError(
                "BearCameraFollow: Assign Camera Target and Center Eye Anchor.",
                this
            );
            enabled = false;
            return;
        }

        // Preserve the headset's local tracking offset.
        trackingOffset = centerEyeAnchor.localPosition;
    }

    private void LateUpdate()
    {
        if (cameraTarget == null || centerEyeAnchor == null)
            return;

        // Move the rig with the bear.
        // Do not overwrite headset rotation.
        Vector3 desiredEyePosition =
            cameraTarget.position +
            cameraTarget.TransformDirection(eyeOffset);

        transform.position +=
            desiredEyePosition - centerEyeAnchor.position;
    }
}
