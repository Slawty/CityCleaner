using UnityEngine;

public class HitFlashObject : MonoBehaviour
{
    [SerializeField] private float hitScaleStrength = 0.1f;
    [SerializeField] private float hitScaleFrequency = 40f;
    [SerializeField] float maxFlashPerSeond = 0.1f;
    [SerializeField] AnimationCurve scaleCurve;
    public Transform ForceDirection;
    public string Prompt => "Mine Chunk";
    private Vector3 baseScale;
    private float hitTimer;
    Material flashMaterial;
    float flashTimeCounter;

    void Start()
    {
        baseScale = transform.localScale;
        flashMaterial = GetComponent<MeshRenderer>().material;
    }

    public void UpdateLaserHit(float damagePerSecond)
    {
        // Debug.Log("Health: " + Health);

        hitTimer += Time.deltaTime * hitScaleFrequency;
        float curveTime = hitTimer % 1f;

        float scaleOffset = scaleCurve.Evaluate(curveTime) * hitScaleStrength;
        transform.localScale = baseScale * (1f + scaleOffset);
        flashTimeCounter += Time.deltaTime;
        if (flashTimeCounter > maxFlashPerSeond)
        {
            flashMaterial.SetFloat("_FlashStartTime", Time.time);
            flashTimeCounter = 0f;
        }
    }
}
