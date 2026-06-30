using UnityEngine;
using System.Collections.Generic;

public class WaterSource : MonoBehaviour, IInteractable
{
    public List<ParticleSystem> SprayEffects;

    public GameObject DroplingPrefab;
    public float spawnRadius = 3f;
    public float spawnInterval = 2f;
    public int MaxDriplings = 6;
    [SerializeField] bool spawnOnStart;

    int activeDriplings;

    public string Prompt => "Activate";

    bool isActivated;
    float spawnTimer;

    void Start()
    {
        if (spawnOnStart)
            ActivateSpawning();
    }

    public void Interact(GameObject interactor)
    {
        ActivateSpawning();
    }

    void ActivateSpawning()
    {
        if (isActivated)
            return;

        isActivated = true;

        foreach (ParticleSystem ps in SprayEffects)
            ps.Play();
    }

    void Update()
    {
        if (!isActivated)
            return;

        if (activeDriplings >= MaxDriplings)
            return;

        spawnTimer += Time.deltaTime;

        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            SpawnDripling();
        }
    }

    void SpawnDripling()
    {
        activeDriplings++;
        Vector2 offset = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPos = transform.position + new Vector3(offset.x, 0f, offset.y);

        Dripling dripling = Instantiate(DroplingPrefab, spawnPos, Quaternion.identity).GetComponent<Dripling>();
        dripling.OnConsumed += OnDriplingConsumed;

        NpcWander wander = dripling.GetComponent<NpcWander>();
        if (wander != null)
        {
            wander.SetWanderCenter(transform.position);
            wander.SetWanderRadius(spawnRadius);
            wander.Initialize();
            wander.BeginWandering();
        }
    }

    void OnDriplingConsumed()
    {
        activeDriplings--;
    }

    public void InteractReleased(GameObject interactor)
    {
    }
}
