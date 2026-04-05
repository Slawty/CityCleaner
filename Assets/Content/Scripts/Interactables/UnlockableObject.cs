using UnityEngine;

public class UnlockableObject : MonoBehaviour
{
    public CoinReceiver CoinReceiver;
    public GameObject PreviewObject;
    public GameObject RealObject;


    void Start()
    {
        CoinReceiver.OnCompleted += OnConinsPayed;
    }

    void OnDestroy()
    {
        CoinReceiver.OnCompleted -= OnConinsPayed;
    }

    void OnConinsPayed()
    {
        PreviewObject.gameObject.SetActive(false);
        RealObject.gameObject.SetActive(true);
        CoinReceiver.gameObject.SetActive(false);
    }
}
