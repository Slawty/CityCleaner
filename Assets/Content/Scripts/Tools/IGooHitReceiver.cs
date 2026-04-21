using UnityEngine;

public interface IGooHitReceiver
{
    void OnGooHit(Vector3 hitPoint, GameObject source);
}
