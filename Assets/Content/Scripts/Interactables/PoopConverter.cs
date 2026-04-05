using Unity.VisualScripting;
using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;

public class PoopConverter : MonoBehaviour, IInteractable
{
    public float PoopSpawnsPerSecond = 10f;
    public int ConversionAmount = 20;
    public ParticleSystem ps;
    public Transform InputPoint;
    public Transform OutputPoint;
    public Collider TriggerCollider;
    public float suctionStrength = 12f;
    public float hopForce = 4f;
    public float collectDistance = 0.3f;
    public ParticleSystem PoopParticles;
    public Image fillImage;
    public GameObject GoolingPrefab;
    List<ParticleSystem.Particle> inside = new List<ParticleSystem.Particle>();
    float initialSize;
    public string Prompt => "Convert Poop";
    bool isSpawningPoop;
    float poopSpawnTimer;
    int poopCounter;

    void Start()
    {
        initialSize = ps.main.startSize.constant;
    }

    void Update()
    {
        if (!isSpawningPoop)
            return;

        poopSpawnTimer -= Time.deltaTime;

        if (poopSpawnTimer <= 0f)
        {
            poopSpawnTimer = 1f / PoopSpawnsPerSecond;

            ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams();

            emit.position = Managers.Tools.GetCurrentTool().Tip.position;
            emit.velocity = Managers.MainCam.transform.forward * Random.Range(1.5f, 3f);

            PoopParticles.Emit(emit, 1);
            Managers.Inventory.DecreasePoop(1);
            poopCounter++;
            fillImage.fillAmount = (float)poopCounter / (float)ConversionAmount;

            if (!Managers.Inventory.HasEnoughPoop(1))
                InteractCanceled(null);

            if (poopCounter >= ConversionAmount)
            {
                var gooling = Instantiate(GoolingPrefab, OutputPoint.position, OutputPoint.rotation).GetComponent<Dripling>();
                poopCounter = 0;
            }
        }
    }

    void OnParticleTrigger()
    {
        int count = ps.GetTriggerParticles(
            ParticleSystemTriggerEventType.Inside,
            inside
        );

        Debug.Log($"OnParticleTrigger. Count: {count}");
        for (int i = 0; i < count; i++)
        {
            ParticleSystem.Particle p = inside[i];

            Vector3 dir = InputPoint.position - p.position;
            float dist = dir.magnitude;

            dir.Normalize();

            // scale coin while approaching vacuum
            // p.startSize = Mathf.Lerp(initialSize * 0.15f, initialSize, dist / 2f);

            // suction force toward vacuum
            Vector3 suction = dir * suctionStrength;

            // hopping motion
            // Vector3 hop = Vector3.up * hopForce * Mathf.Sin(Time.time * 8f);

            p.velocity = suction;

            // collect coin
            if (dist < collectDistance)
            {
                OnCoinReceived();
                p.remainingLifetime = 0f;
            }

            inside[i] = p;
        }

        ps.SetTriggerParticles(ParticleSystemTriggerEventType.Inside, inside);
    }

    void OnCoinReceived()
    {

    }

    public void Interact(GameObject interactor)
    {
        if (!Managers.Inventory.HasEnoughPoop(1))
            return;

        Debug.Log("Interact Poop Converter");
        isSpawningPoop = true;
        // TriggerCollider.enabled = true;
    }

    public void InteractCanceled(GameObject interactor)
    {
        isSpawningPoop = false;
        // TriggerCollider.enabled = false;
    }

}
