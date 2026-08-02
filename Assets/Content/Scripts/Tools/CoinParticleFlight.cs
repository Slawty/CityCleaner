using System.Collections.Generic;
using UnityEngine;

public class CoinParticleFlight : MonoBehaviour
{
    struct FlightState
    {
        public Vector3 Start;
        public Vector3 End;
        public float ArcHeight;
        public float Elapsed;
        public float Duration;
    }

    [SerializeField] ParticleSystem ps;
    [SerializeField] LayerMask groundMask = ~0;
    [SerializeField] float landingDistanceMin = 0.4f;
    [SerializeField] float landingDistanceMax = 1.2f;
    [SerializeField] float landingSpreadRadius = 0.5f;
    [SerializeField] float flightDuration = 0.55f;
    [SerializeField] float arcHeight = 1.5f;
    [SerializeField] float verticalDeltaThreshold = 0.5f;
    [SerializeField] float wallArcHeightMultiplier = 0.3f;
    [SerializeField] bool skipWallLandings = true;
    [SerializeField] float minGroundNormalY = 0.7f;
    [SerializeField] float groundRaycastStartOffset = 0.5f;
    [SerializeField] float groundRaycastDownDistance = 10f;
    [SerializeField] float groundRaycastStepUp = 5f;
    [SerializeField] int groundRaycastUpSteps = 2;
    [SerializeField] float landingHeightOffset = 0.15f;

    readonly Dictionary<uint, FlightState> activeFlights = new();
    ParticleSystem.Particle[] particles;

    void Awake()
    {
        if (ps == null)
            ps = GetComponent<ParticleSystem>();

        ParticleSystem.CollisionModule collision = ps.collision;
        collision.enabled = false;

        ParticleSystem.MainModule main = ps.main;
        main.gravityModifier = 0f;

        particles = new ParticleSystem.Particle[ps.main.maxParticles];
    }

    void LateUpdate()
    {
        if (activeFlights.Count == 0)
            return;

        int count = ps.GetParticles(particles);
        bool changed = false;

        for (int index = 0; index < count; index++)
        {
            ParticleSystem.Particle particle = particles[index];
            if (!activeFlights.TryGetValue(particle.randomSeed, out FlightState flight))
                continue;

            flight.Elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(flight.Elapsed / flight.Duration);

            if (normalizedTime >= 1f)
            {
                particle.position = flight.End;
                activeFlights.Remove(particle.randomSeed);
            }
            else
            {
                particle.position = EvaluateArc(flight.Start, flight.End, flight.ArcHeight, normalizedTime);
                activeFlights[particle.randomSeed] = flight;
            }

            particle.velocity = Vector3.zero;
            particles[index] = particle;
            changed = true;
        }

        if (changed)
            ps.SetParticles(particles, count);
    }

    public void EmitFlight(Vector3 spawnPos, Vector3 landingPos)
    {
        uint seed = (uint)Random.Range(1, int.MaxValue);
        while (activeFlights.ContainsKey(seed))
            seed = (uint)Random.Range(1, int.MaxValue);

        ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams
        {
            position = spawnPos,
            velocity = Vector3.zero,
            randomSeed = seed
        };

        ps.Emit(emit, 1);
        activeFlights[seed] = new FlightState
        {
            Start = spawnPos,
            End = landingPos,
            ArcHeight = GetArcHeight(spawnPos, landingPos),
            Elapsed = 0f,
            Duration = flightDuration
        };
    }

    public Vector3 FindLandingSpot(Vector3 spawnPos)
    {
        Vector3 toPlayer = Managers.Player.transform.position - spawnPos;
        toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude < 0.01f)
            toPlayer = Managers.Player.transform.forward;

        toPlayer.Normalize();

        Vector3 lateral = Vector3.Cross(Vector3.up, toPlayer);
        float forwardDistance = Random.Range(landingDistanceMin, landingDistanceMax);
        float lateralDistance = Random.Range(-landingSpreadRadius, landingSpreadRadius);

        Vector3 landingPoint = spawnPos + toPlayer * forwardDistance + lateral * lateralDistance;
        return ProjectPointToGround(landingPoint, spawnPos.y);
    }

    float GetArcHeight(Vector3 spawnPos, Vector3 landingPos)
    {
        float verticalDelta = landingPos.y - spawnPos.y;
        if (verticalDelta <= verticalDeltaThreshold)
            return arcHeight;

        return arcHeight * wallArcHeightMultiplier;
    }

    Vector3 ProjectPointToGround(Vector3 worldPoint, float spawnHeight)
    {
        float startY = spawnHeight + groundRaycastStartOffset;

        for (int step = 0; step <= groundRaycastUpSteps; step++)
        {
            Vector3 origin = new Vector3(worldPoint.x, startY + step * groundRaycastStepUp, worldPoint.z);
            if (TryRaycastGround(origin, groundRaycastDownDistance, out RaycastHit hit))
                return hit.point + Vector3.up * landingHeightOffset;
        }

        worldPoint.y = spawnHeight + landingHeightOffset;
        return worldPoint;
    }

    bool TryRaycastGround(Vector3 origin, float maxDistance, out RaycastHit groundHit)
    {
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, maxDistance, groundMask, QueryTriggerInteraction.Ignore);
        int playerLayer = Managers.Player.gameObject.layer;

        groundHit = default;
        float closestDistance = float.MaxValue;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.gameObject.layer == playerLayer)
                continue;

            if (skipWallLandings && hit.normal.y < minGroundNormalY)
                continue;

            if (hit.distance >= closestDistance)
                continue;

            closestDistance = hit.distance;
            groundHit = hit;
        }

        return closestDistance < float.MaxValue;
    }

    static Vector3 EvaluateArc(Vector3 start, Vector3 end, float height, float normalizedTime)
    {
        Vector3 position = Vector3.Lerp(start, end, normalizedTime);
        position.y += height * 4f * normalizedTime * (1f - normalizedTime);
        return position;
    }
}
