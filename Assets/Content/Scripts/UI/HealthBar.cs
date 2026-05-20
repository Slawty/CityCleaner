using UnityEngine;

/// <summary>
/// For a world-space canvas (or child): keeps a <see cref="ProgressBar"/> updated and
/// rotates this transform to face the player on the horizontal plane only (yaw, no pitch/roll).
/// </summary>
public class HealthBar : MonoBehaviour
{
    [SerializeField] ProgressBar progressBar;
    [Tooltip("Applied after LookRotation e.g. (0,180,0) if the canvas faces the wrong way.")]
    [SerializeField] Vector3 eulerFacingOffset;
    Transform faceTarget;

    void Start()
    {
        faceTarget = Managers.MainCam.transform;
    }

    void LateUpdate()
    {
        FaceTargetYawOnly();
    }

    void FaceTargetYawOnly()
    {
        Vector3 flat = faceTarget.position - transform.position;
        flat.y = 0f;

        if (flat.sqrMagnitude < 1e-8f)
            return;

        Quaternion face = Quaternion.LookRotation(flat.normalized, Vector3.up);
        if (eulerFacingOffset != Vector3.zero)
            face *= Quaternion.Euler(eulerFacingOffset);

        transform.rotation = face;
    }

    /// <summary>0 = empty, 1 = full. Drives <see cref="ProgressBar"/> as 0–100%.</summary>
    public void SetNormalizedFill(float normalized01)
    {
        if (progressBar == null)
            return;

        progressBar.SetPercent(Mathf.Clamp01(normalized01) * 100f);
    }

    public void SetProgressBar(ProgressBar bar)
    {
        progressBar = bar;
    }
}
