using Unity.VisualScripting;
using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine.Events;

public class CoinReceiver : MonoBehaviour, IInteractable
{
    public RessourceType Type;
    public UnityAction OnCompleted;
    public float CoinSpawnsPerSecond = 10f;
    public int RequiredAmount = 20;
    public ParticleSystem ps;
    public Transform TargetPoint;
    public Collider TriggerCollider;
    public List<GameObject> RemoveOnComplete;
    public float suctionStrength = 12f;
    public float hopForce = 4f;
    public float collectDistance = 0.3f;
    public Image fillImage;
    List<ParticleSystem.Particle> inside = new List<ParticleSystem.Particle>();
    float initialSize;
    public string Prompt => "Pay Coins";
    bool isSpawningParticles;
    float coinSpawnTimer;
    int amountCounter;

    void Start()
    {
        initialSize = ps.main.startSize.constant;
    }

    void Update()
    {
        if (!isSpawningParticles)
            return;

        coinSpawnTimer -= Time.deltaTime;

        if (coinSpawnTimer <= 0f)
        {
            coinSpawnTimer = 1f / CoinSpawnsPerSecond;

            ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams();

            emit.position = Managers.Tools.GetCurrentTool().Tip.position;
            emit.velocity = Managers.MainCam.transform.forward * Random.Range(1.5f, 3f);

            ps.Emit(emit, 1);
            if (Type == RessourceType.Coin)
                Managers.Inventory.DecreaseCoins(1);
            else if (Type == RessourceType.Poop)
                Managers.Inventory.DecreasePoop(1);

            amountCounter++;
            fillImage.fillAmount = (float)amountCounter / (float)RequiredAmount;

            if (Type == RessourceType.Coin && !Managers.Inventory.HasEnoughCoins(1) || Type == RessourceType.Poop && !Managers.Inventory.HasEnoughPoop(1))
                InteractReleased(null);

            if (amountCounter >= RequiredAmount)
            {
                if (RemoveOnComplete.Count > 0)
                {
                    foreach (GameObject go in RemoveOnComplete)
                    {
                        go.SetActive(false);
                    }
                }

                OnCompleted?.Invoke();
                InteractReleased(null);

            }
        }
    }

    void OnParticleTrigger()
    {
        int count = ps.GetTriggerParticles(
            ParticleSystemTriggerEventType.Inside,
            inside
        );

        // Debug.Log($"OnParticleTrigger. Count: {count}");
        for (int i = 0; i < count; i++)
        {
            ParticleSystem.Particle p = inside[i];

            Vector3 dir = TargetPoint.position - p.position;
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
                OnParticleReceived();
                p.remainingLifetime = 0f;
            }

            inside[i] = p;
        }

        ps.SetTriggerParticles(ParticleSystemTriggerEventType.Inside, inside);
    }

    void OnParticleReceived()
    {

    }

    public void Interact(GameObject interactor)
    {
        if (Type == RessourceType.Coin && !Managers.Inventory.HasEnoughCoins(1) || Type == RessourceType.Poop && !Managers.Inventory.HasEnoughPoop(1))
            return;

        Debug.Log($"Interact {transform.name}");
        Managers.Input.BlockInteraction(this);
        isSpawningParticles = true;
        // TriggerCollider.enabled = true;
    }

    public void InteractReleased(GameObject interactor)
    {
        Debug.Log($"Release {transform.name}");
        Managers.Input.UnblockInteraction(this);
        isSpawningParticles = false;
        // TriggerCollider.enabled = false;
    }
}
