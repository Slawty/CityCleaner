using UnityEngine;
using UnityEngine.Events;
using Cysharp.Threading.Tasks;

public class SpawnManager : MonoBehaviour
{
    [SerializeField] GameObject coinPrefab;
    [SerializeField] ParticleSystem coinParticles;
    [SerializeField] ParticleSystem pickupChunkParticles;
    [SerializeField] ParticleSystem tempChunkParticles;
    [SerializeField] ParticleSystem splitableDestroyParticles;
    [SerializeField] ParticleSystem splitableDestroySmokeParticles;
    [SerializeField] float splitableDestroyBaseSize = 20f;
    [SerializeField] float splitableDestroyReferenceExtent = 0.5f;
    [SerializeField] int splitableDestroyBaseCount = 5;
    [SerializeField] float multipleSpawnDelay = 0.2f;

    CoinParticleFlight coinFlight;
    Vector3 splitableSmokeDefaultScale;

    public UnityAction OnCoinSpawned;

    void Awake()
    {
        coinFlight = coinParticles.GetComponent<CoinParticleFlight>();
        if (coinFlight == null)
            throw new System.InvalidOperationException($"{nameof(SpawnManager)} on {name}: {nameof(CoinParticleFlight)} is missing on {coinParticles.name}.");

        if (splitableDestroySmokeParticles != null)
            splitableSmokeDefaultScale = splitableDestroySmokeParticles.transform.localScale;
    }

    public void SpawnCoin(Vector3 spawnPos)
    {
        Vector3 landingPos = coinFlight.FindLandingSpot(spawnPos);
        coinFlight.EmitFlight(spawnPos, landingPos);
        OnCoinSpawned?.Invoke();
    }

    public async UniTask SpawnCoins(int amount, Vector3 spawnPos)
    {
        for (int index = 0; index < amount; index++)
        {
            SpawnCoin(spawnPos);
            await UniTask.Delay(System.TimeSpan.FromSeconds(multipleSpawnDelay), cancellationToken: destroyCancellationToken);
        }
    }

    public void SpawnPickupChunk(Vector3 spawnPos, Vector3 spawnDirection)
    {
        ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams();
        emit.position = spawnPos;
        emit.velocity = spawnDirection * Random.Range(1f, 2f);
        pickupChunkParticles.Emit(emit, 1);
        Debug.Log("Spawn chunk");
    }

    public async UniTask SpawnPickupChunks(int amount, Vector3 spawnPos, Vector3 spawnDirection, float spawnDelay = -1f)
    {
        for (int index = 0; index < amount; index++)
        {
            Vector3 rngDir = Quaternion.AngleAxis(Random.Range(-90f, 90f), Random.onUnitSphere) * spawnDirection * Random.Range(1f, 1.5f);
            SpawnPickupChunk(spawnPos, rngDir);
            float delay = spawnDelay == -1f ? multipleSpawnDelay : spawnDelay;
            await UniTask.Delay(System.TimeSpan.FromSeconds(delay), cancellationToken: destroyCancellationToken);
        }
    }

    void SpawnTempChunk(Vector3 spawnPos, Vector3 spawnDirection)
    {
        ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams();
        emit.position = spawnPos;
        emit.velocity = spawnDirection * Random.Range(1f, 2f);
        tempChunkParticles.Emit(emit, 1);
    }

    public async UniTask SpawnTempChunks(int amount, Vector3 spawnPos, Vector3 spawnDirection, float spawnDelay = -1f)
    {
        for (int index = 0; index < amount; index++)
        {
            Vector3 rngDir = Quaternion.AngleAxis(Random.Range(-90f, 90f), Random.onUnitSphere) * spawnDirection * Random.Range(1f, 1.5f);
            SpawnTempChunk(spawnPos, rngDir);
            float delay = spawnDelay == -1f ? multipleSpawnDelay : spawnDelay;
            await UniTask.Delay(System.TimeSpan.FromSeconds(delay), cancellationToken: destroyCancellationToken);
        }
    }

    public void SpawnSplitableDestroyVfx(Bounds bounds, float scaleMultiplier = 1f)
    {
        float sizeFactor = bounds.extents.magnitude / splitableDestroyReferenceExtent * scaleMultiplier;
        Vector3 spawnPos = bounds.center;

        EmitScaledParticles(splitableDestroyParticles, spawnPos, splitableDestroyBaseSize, splitableDestroyBaseCount, sizeFactor);
        SpawnSplitableSmoke(splitableDestroySmokeParticles, spawnPos, sizeFactor);
    }

    void SpawnSplitableSmoke(ParticleSystem smoke, Vector3 spawnPos, float sizeFactor)
    {
        if (smoke == null)
            return;

        Transform smokeTransform = smoke.transform;
        smokeTransform.position = spawnPos;
        smokeTransform.localScale = splitableSmokeDefaultScale * sizeFactor;

        if (smoke.isPlaying)
            smoke.Stop(false, ParticleSystemStopBehavior.StopEmitting);

        smoke.Play();
    }

    void EmitScaledParticles(ParticleSystem particles, Vector3 position, float baseSize, int baseCount, float sizeFactor)
    {
        if (particles == null)
            return;

        float particleSize = baseSize * sizeFactor;
        int particleCount = Mathf.Max(1, Mathf.RoundToInt(baseCount * sizeFactor));

        ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams
        {
            position = position,
            applyShapeToPosition = false,
            startSize = particleSize,
        };
        particles.Emit(emit, particleCount);
    }
}
