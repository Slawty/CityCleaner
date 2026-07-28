using UnityEngine;

public class ScreenWaypointMarker : MonoBehaviour
{
    [SerializeField] RectTransform markerRoot;
    [SerializeField] RectTransform arrow;
    [SerializeField] Canvas canvas;
    [SerializeField] float edgePadding = 0.06f;
    [SerializeField] float worldYOffset = 1.5f;
    [SerializeField] float defaultHideDistance = 4f;

    Transform target;
    float hideDistance = -1f;

    void Awake()
    {
        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        if (markerRoot != null)
            markerRoot.gameObject.SetActive(false);
    }

    public void SetTarget(Transform waypointTarget, float distanceToHide = -1f)
    {
        target = waypointTarget;
        hideDistance = distanceToHide;

        if (target == null && markerRoot != null)
            markerRoot.gameObject.SetActive(false);
    }

    public void ClearTarget()
    {
        SetTarget(null);
    }

    void LateUpdate()
    {
        if (target == null || markerRoot == null || canvas == null)
            return;

        Camera cam = Managers.MainCam;
        if (cam == null)
            return;

        Vector3 worldPosition = target.position + Vector3.up * worldYOffset;
        float distanceToHideAt = hideDistance >= 0f ? hideDistance : defaultHideDistance;
        if (distanceToHideAt > 0f)
        {
            Transform playerTransform = Managers.Player != null ? Managers.Player.transform : null;
            if (playerTransform != null && Vector3.Distance(playerTransform.position, target.position) <= distanceToHideAt)
            {
                markerRoot.gameObject.SetActive(false);
                return;
            }
        }

        Vector3 viewportPosition = cam.WorldToViewportPoint(worldPosition);
        bool behindCamera = viewportPosition.z < 0f;
        if (behindCamera)
        {
            viewportPosition.x = 1f - viewportPosition.x;
            viewportPosition.y = 1f - viewportPosition.y;
        }

        bool onScreen = !behindCamera
            && viewportPosition.x > edgePadding && viewportPosition.x < 1f - edgePadding
            && viewportPosition.y > edgePadding && viewportPosition.y < 1f - edgePadding;

        Vector2 directionFromCenter = new Vector2(viewportPosition.x - 0.5f, viewportPosition.y - 0.5f);
        if (!onScreen)
        {
            float maxX = 0.5f - edgePadding;
            float maxY = 0.5f - edgePadding;
            float scale = Mathf.Min(
                maxX / Mathf.Max(Mathf.Abs(directionFromCenter.x), 0.0001f),
                maxY / Mathf.Max(Mathf.Abs(directionFromCenter.y), 0.0001f));
            directionFromCenter *= scale;
            viewportPosition.x = directionFromCenter.x + 0.5f;
            viewportPosition.y = directionFromCenter.y + 0.5f;
        }

        Vector2 screenPoint = new Vector2(viewportPosition.x * Screen.width, viewportPosition.y * Screen.height);
        RectTransform canvasRect = canvas.transform as RectTransform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out Vector2 localPoint);
        markerRoot.anchoredPosition = localPoint;
        markerRoot.gameObject.SetActive(true);

        if (arrow != null)
        {
            arrow.gameObject.SetActive(!onScreen);
            if (!onScreen)
            {
                float angle = Mathf.Atan2(directionFromCenter.x, directionFromCenter.y) * Mathf.Rad2Deg;
                arrow.localRotation = Quaternion.Euler(0f, 0f, -angle);
            }
        }
    }
}
