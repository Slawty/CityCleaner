using UnityEngine;

/// <summary>
/// Keeps a world-space transform facing the main camera. Enable only while the hosted UI should be visible.
/// </summary>
public class CameraBillboard : MonoBehaviour
{
    [Tooltip("Applied after LookRotation e.g. (0,180,0) if the canvas faces the wrong way.")]
    [SerializeField] Vector3 eulerFacingOffset = new Vector3(0f, 180f, 0f);
    [SerializeField] bool yawOnly;

    Transform cameraTransform;

    void LateUpdate()
    {
        if (cameraTransform == null)
        {
            Camera camera = Managers.MainCam;
            if (camera == null)
                return;

            cameraTransform = camera.transform;
        }

        Vector3 toCamera = cameraTransform.position - transform.position;
        if (yawOnly)
            toCamera.y = 0f;

        if (toCamera.sqrMagnitude < 1e-8f)
            return;

        Quaternion face = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
        if (eulerFacingOffset != Vector3.zero)
            face *= Quaternion.Euler(eulerFacingOffset);

        transform.rotation = face;
    }
}
