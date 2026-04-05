using UnityEngine;

public class GrassCutterTool : Tool
{
    [Header("Cutting Settings")]
    [SerializeField] private float cutDuration = 2f;
    [SerializeField] private float range = 3f;
    [SerializeField] private LayerMask grassLayer;

    [Header("Ray Origin")]
    [SerializeField] private Transform rayOrigin;

    private bool isCutting;
    private CuttableGrass currentTarget;
    private float durationCounter;

    protected override void OnShootStart()
    {
        isCutting = true;
    }

    protected override void OnShootStop()
    {
        isCutting = false;
        currentTarget = null;
        durationCounter = 0f;
    }

    private void Update()
    {
        if (!isCutting)
            return;

        CuttableGrass detected = DetectGrass();

        // If target changed, sync durationCounter to new grass progress
        if (detected != currentTarget)
        {
            currentTarget = detected;

            if (currentTarget != null)
            {
                durationCounter = currentTarget.Progress * cutDuration;
            }
            else
            {
                durationCounter = 0f;
            }
        }

        if (currentTarget != null && !currentTarget.IsCut)
        {
            durationCounter += Time.deltaTime;

            float progress = durationCounter / cutDuration;

            currentTarget.SetProgress(progress);
        }
    }

    private CuttableGrass DetectGrass()
    {
        Collider[] hits = Physics.OverlapSphere(
            rayOrigin.position,
            range,
            grassLayer,
            QueryTriggerInteraction.Collide
        );

        float closestDist = float.MaxValue;
        CuttableGrass closest = null;

        foreach (var col in hits)
        {
            CuttableGrass grass = col.GetComponent<CuttableGrass>();

            if (grass == null)
                continue;

            float dist = (col.transform.position - rayOrigin.position).sqrMagnitude;

            if (dist < closestDist)
            {
                closestDist = dist;
                closest = grass;
            }
        }

        // if (closest != null)
        //     Debug.Log($"Grass detected: {closest.name}");

        return closest;
    }
}
