using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class GPUPainter : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] Camera cam;
    [SerializeField] int textureResolution = 1024;
    [SerializeField] LayerMask paintMask;

    [Header("Brush")]
    [SerializeField] Texture2D brushTexture;
    [SerializeField] float brushWorldSize = 0.5f;
    [SerializeField] float cleanSpeed = 4f;

    public Material paintMaterial;
    float nextUpdateTime;
    bool isPainting;

    void Awake()
    {
        paintMaterial = new Material(Shader.Find("Hidden/GPUPaintBrush"));
    }

    void Update()
    {
        if (!isPainting)
            return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));

        if (!Physics.Raycast(ray, out RaycastHit hit, 10f, paintMask, QueryTriggerInteraction.Ignore))
            return;

        var paintable = hit.collider.GetComponent<GPUPaintableObject>();

        if (paintable == null)
            return;

        Paint(paintable, hit.point, cleanSpeed * Time.deltaTime);
        // Debug.Log($"Paint: {hit.collider.name}");
    }

    public void StartPainting()
    {
        isPainting = true;
    }

    public void StopPainting()
    {
        isPainting = false;
    }

    public void Paint(GPUPaintableObject paintable, Vector3 hitPos, float cleanStrength)
    {
        if (paintable.isClean)
            return;

        if (paintable.maskTexture == null)
        {
            paintable.Initialize(textureResolution);
        }

        Renderer renderer = paintable.GetComponent<Renderer>();
        MeshFilter mf = paintable.GetComponent<MeshFilter>();
        if (renderer == null || mf == null) return;

        paintMaterial.SetVector("_BrushWorldPos", hitPos);
        paintMaterial.SetFloat("_BrushSize", brushWorldSize);
        paintMaterial.SetFloat("_Strength", cleanStrength);
        paintMaterial.SetTexture("_BrushTex", brushTexture);

        RenderTexture temp = RenderTexture.GetTemporary(paintable.maskTexture.width, paintable.maskTexture.height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(paintable.maskTexture, temp);
        paintMaterial.SetTexture("_MainTex", temp);

        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "GPUPaint";
        cmd.SetRenderTarget(paintable.maskTexture);
        cmd.DrawRenderer(renderer, paintMaterial);

        Graphics.ExecuteCommandBuffer(cmd);
        cmd.Release();
        RenderTexture.ReleaseTemporary(temp);

        if (Time.time > nextUpdateTime)
        {
            paintable.UpdateTracking();
            nextUpdateTime = Time.time + 0.1f;
            Managers.UI.SetCleanProgressBarPercent(paintable.GetCleanPercent() * 100f);

            if (paintable.isClean)
            {
                Vector3 dirToPlayer = (transform.position - hitPos).normalized;
                dirToPlayer.y = 1f;
                Managers.Spawning.SpawnCoins(paintable.winCoins, hitPos + dirToPlayer * 0.25f, dirToPlayer).Forget();
            }
        }
    }

}
