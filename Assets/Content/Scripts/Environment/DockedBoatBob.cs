using UnityEngine;

public class DockedBoatBob : MonoBehaviour
{
    [Header("Vertical")]
    [SerializeField] float bobHeight = 0.08f;
    [SerializeField] float bobSpeed = 0.6f;

    [Header("Rotation (degrees)")]
    [SerializeField] float rollAmount = 1.5f;
    [SerializeField] float rollSpeed = 0.45f;
    [SerializeField] float pitchAmount = 0.8f;
    [SerializeField] float pitchSpeed = 0.35f;
    [SerializeField] float yawAmount = 0.4f;
    [SerializeField] float yawSpeed = 0.25f;

    [Header("Horizontal drift")]
    [SerializeField] float driftAmount = 0.02f;
    [SerializeField] float driftSpeed = 0.3f;

    Vector3 restLocalPosition;
    Quaternion restLocalRotation;
    float phaseOffset;

    void Awake()
    {
        restLocalPosition = transform.localPosition;
        restLocalRotation = transform.localRotation;
        phaseOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        float time = Time.time + phaseOffset;

        Vector3 positionOffset = new Vector3(
            Mathf.Sin(time * driftSpeed) * driftAmount,
            Mathf.Sin(time * bobSpeed) * bobHeight,
            Mathf.Cos(time * driftSpeed * 0.85f) * driftAmount * 0.6f);

        float roll = Mathf.Sin(time * rollSpeed) * rollAmount;
        float pitch = Mathf.Cos(time * pitchSpeed) * pitchAmount;
        float yaw = Mathf.Sin(time * yawSpeed) * yawAmount;

        transform.localPosition = restLocalPosition + positionOffset;
        transform.localRotation = restLocalRotation * Quaternion.Euler(pitch, yaw, roll);
    }
}
