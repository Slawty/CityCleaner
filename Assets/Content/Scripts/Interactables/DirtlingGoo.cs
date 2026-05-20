using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(DirtlingStateController))]
public class DirtlingGoo : MonoBehaviour, IGooHitReceiver
{
    [SerializeField] float gooPerHit = 0.12f;
    [SerializeField] float gooDecayPerSecond = 0.1f;
    [SerializeField, Range(0f, 1f)] float maxSlowPercent = 0.65f;
    [SerializeField] float waterVulnerabilityBonus = 1f;
    [SerializeField] Renderer gooTintRenderer;
    [SerializeField] Color gooTintColor = new Color(0.4f, 1f, 0.35f, 1f);

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");

    float gooAmount;
    float baseNavSpeed;
    Color baseTintColor;
    bool hasBaseTintColor;
    MaterialPropertyBlock tintBlock;
    DirtlingStateController stateController;
    NavMeshAgent navAgent;

    public float GooAmount => gooAmount;
    public bool IsGooed => gooAmount > 0.01f;
    public bool BlocksFlee => IsGooed;
    public float WaterDizzyMultiplier => 1f + gooAmount * waterVulnerabilityBonus;

    void Awake()
    {
        stateController = GetComponent<DirtlingStateController>();
        navAgent = GetComponent<NavMeshAgent>();
        baseNavSpeed = navAgent.speed;
        CacheBaseTint();
    }

    void Update()
    {
        if (gooAmount > 0f)
        {
            gooAmount = Mathf.Max(0f, gooAmount - gooDecayPerSecond * Time.deltaTime);
            ApplySlow();
            ApplyTint();
        }
        else
        {
            RestoreNavSpeed();
            ClearTint();
        }
    }

    public void OnGooHit(Vector3 hitPoint, GameObject source)
    {
        DirtlingState state = stateController.CurrentState;
        if (state == DirtlingState.Processed || state == DirtlingState.Vacuumed || state == DirtlingState.PhysicsBall)
            return;

        gooAmount = Mathf.Min(1f, gooAmount + gooPerHit);
        ApplySlow();
        ApplyTint();
        stateController.OnGooApplied();
    }

    void ApplySlow()
    {
        if (!navAgent.enabled)
            return;

        navAgent.speed = baseNavSpeed * (1f - gooAmount * maxSlowPercent);
    }

    void RestoreNavSpeed()
    {
        if (!navAgent.enabled)
            return;

        navAgent.speed = baseNavSpeed;
    }

    void CacheBaseTint()
    {
        if (gooTintRenderer == null)
            gooTintRenderer = GetComponentInChildren<Renderer>();

        if (gooTintRenderer == null)
            return;

        if (gooTintRenderer.sharedMaterial != null && gooTintRenderer.sharedMaterial.HasProperty(BaseColorId))
        {
            baseTintColor = gooTintRenderer.sharedMaterial.GetColor(BaseColorId);
            hasBaseTintColor = true;
            return;
        }

        if (gooTintRenderer.sharedMaterial != null && gooTintRenderer.sharedMaterial.HasProperty(ColorId))
        {
            baseTintColor = gooTintRenderer.sharedMaterial.GetColor(ColorId);
            hasBaseTintColor = true;
        }
    }

    void ApplyTint()
    {
        if (gooTintRenderer == null || !hasBaseTintColor)
            return;

        tintBlock ??= new MaterialPropertyBlock();
        gooTintRenderer.GetPropertyBlock(tintBlock);
        Color tinted = Color.Lerp(baseTintColor, gooTintColor, gooAmount);
        if (gooTintRenderer.sharedMaterial.HasProperty(BaseColorId))
            tintBlock.SetColor(BaseColorId, tinted);
        else
            tintBlock.SetColor(ColorId, tinted);
        gooTintRenderer.SetPropertyBlock(tintBlock);
    }

    void ClearTint()
    {
        if (gooTintRenderer == null)
            return;

        gooTintRenderer.SetPropertyBlock(null);
    }
}
