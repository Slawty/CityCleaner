using UnityEngine;

public class UnlockWall : MonoBehaviour
{
    public CoinReceiver coinReceiver;

    void Start()
    {
        coinReceiver.OnCompleted += OnConinsPayed;
    }

    void OnDestroy()
    {
        coinReceiver.OnCompleted -= OnConinsPayed;
    }

    void OnConinsPayed()
    {
        Destroy(gameObject);
    }
}
