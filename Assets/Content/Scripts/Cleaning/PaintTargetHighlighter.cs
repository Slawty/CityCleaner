using UnityEngine;

public class PaintTargetHighlighter : MonoBehaviour
{
    [SerializeField] GPUPainterWorld painter;
    [SerializeField] float rayDistance = 12f;
    [SerializeField] float sphereCastRadius = 0.15f;

    Camera cam;
    GPUPaintableObject currentTarget;

    void Awake()
    {
        cam = Managers.MainCam;
    }

    void LateUpdate()
    {
        if (!painter.IsPainting || Managers.Input.InteractionBlocked())
        {
            ClearTarget();
            return;
        }

        UpdateTarget();
    }

    void OnDisable()
    {
        ClearTarget();
    }

    void UpdateTarget()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        Vector3 castOrigin = ray.origin + ray.direction * sphereCastRadius;
        float castDistance = rayDistance - sphereCastRadius;

        GPUPaintableObject hitPaintable = null;
        if (castDistance > 0f && Physics.SphereCast(castOrigin, sphereCastRadius, ray.direction, out RaycastHit hit, castDistance, painter.PaintMask, QueryTriggerInteraction.Ignore))
            hitPaintable = hit.collider.GetComponentInParent<GPUPaintableObject>();

        if (currentTarget == hitPaintable)
            return;

        if (currentTarget != null)
            currentTarget.SetAimOutline(false);

        currentTarget = hitPaintable;

        if (currentTarget != null)
            currentTarget.SetAimOutline(true);
    }

    void ClearTarget()
    {
        if (currentTarget == null)
            return;

        currentTarget.SetAimOutline(false);
        currentTarget = null;
    }
}
