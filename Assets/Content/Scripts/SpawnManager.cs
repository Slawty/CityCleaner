using UnityEngine;
using UnityEngine.Events;
using Cysharp.Threading.Tasks;
using System.Threading;

public class SpawnManager : MonoBehaviour
{
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private ParticleSystem coinParticles;
    [SerializeField] private ParticleSystem pickupChunkParticles;
    [SerializeField] private ParticleSystem tempChunkParticles;

    [SerializeField] private Vector2 spawnForceMinMax = new Vector2(2f, 5f);
    [SerializeField] private float directionRandomness = 0.3f;
    [SerializeField] private float multipleSpawnDelay = 0.2f;
    [SerializeField] private Vector2 spinForceMinMax = new Vector2(1f, 3f);

    public UnityAction OnCoinSpawned;

    public void SpawnCoin(Vector3 spawnPos, Vector3 spawnDirection)
    {
        ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams();

        emit.position = spawnPos;
        emit.velocity = spawnDirection * Random.Range(1.5f, 3f);

        coinParticles.Emit(emit, 1);
        OnCoinSpawned?.Invoke();
    }

    public async UniTask SpawnCoins(int amount, Vector3 spawnPos, Vector3 spawnDirection)
    {
        for (int i = 0; i < amount; i++)
        {
            Vector3 rngDir = Quaternion.AngleAxis(Random.Range(-25f, 25f), Random.onUnitSphere) * spawnDirection;
            SpawnCoin(spawnPos, rngDir);
            await UniTask.Delay(System.TimeSpan.FromSeconds(multipleSpawnDelay), cancellationToken: destroyCancellationToken);
        }
    }

    void SpawnPickupChunk(Vector3 spawnPos, Vector3 spawnDirection)
    {
        ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams();
        emit.position = spawnPos;
        emit.velocity = spawnDirection * Random.Range(1f, 2f);
        pickupChunkParticles.Emit(emit, 1);
    }

    public async UniTask SpawnPickupChunks(int amount, Vector3 spawnPos, Vector3 spawnDirection, float spawnDelay = -1f)
    {
        for (int i = 0; i < amount; i++)
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
        for (int i = 0; i < amount; i++)
        {
            Vector3 rngDir = Quaternion.AngleAxis(Random.Range(-90f, 90f), Random.onUnitSphere) * spawnDirection * Random.Range(1f, 1.5f);
            SpawnTempChunk(spawnPos, rngDir);
            float delay = spawnDelay == -1f ? multipleSpawnDelay : spawnDelay;
            await UniTask.Delay(System.TimeSpan.FromSeconds(delay), cancellationToken: destroyCancellationToken);
        }
    }
}
