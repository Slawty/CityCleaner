using UnityEngine;
using UnityEngine.Events;
using Cysharp.Threading.Tasks;

public class SpawnManager : MonoBehaviour
{
    [SerializeField] GameObject coinPrefab;
    [SerializeField] ParticleSystem coinParticles;
    [SerializeField] ParticleSystem pickupChunkParticles;
    [SerializeField] ParticleSystem tempChunkParticles;
    [SerializeField] float multipleSpawnDelay = 0.2f;

    CoinParticleFlight coinFlight;

    public UnityAction OnCoinSpawned;

    void Awake()
    {
        coinFlight = coinParticles.GetComponent<CoinParticleFlight>();
        if (coinFlight == null)
            throw new System.InvalidOperationException($"{nameof(SpawnManager)} on {name}: {nameof(CoinParticleFlight)} is missing on {coinParticles.name}.");
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
}
