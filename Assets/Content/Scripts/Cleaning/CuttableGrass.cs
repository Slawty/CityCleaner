using UnityEngine;
using UnityEngine.Events;

public class CuttableGrass : MonoBehaviour
{
    public UnityAction<CuttableGrass> OnCut;
    [SerializeField] GameObject full;
    [SerializeField] GameObject medium;
    [SerializeField] GameObject shortCut;
    [SerializeField] GameObject cut;

    [Range(0, 1)]
    [SerializeField] float progress;
    public float Progress => progress;
    public bool IsCut => progress >= 0.9f;

    public void SetProgress(float value)
    {
        progress = Mathf.Clamp01(value);
        // Debug.Log($"Progress: {progress}");
        UpdateVisual();

        if (progress >= 0.9f)
        {
            OnCut?.Invoke(this);
            Managers.Spawning.SpawnCoin(transform.position, transform.up);
        }
    }

    void UpdateVisual()
    {
        full.SetActive(progress < 0.1f);
        medium.SetActive(progress >= 0.1f && progress < 0.5f);
        shortCut.SetActive(progress >= 0.5f && progress < 0.9f);
        cut.SetActive(progress >= 0.9f);
    }
}
