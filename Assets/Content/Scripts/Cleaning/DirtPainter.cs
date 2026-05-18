using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class DirtPainter : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference shootAction;

    [Header("References")]
    [SerializeField] private Camera playerCamera;

    [Header("Mask Template")]
    [SerializeField] private RenderTexture maskTemplate;

    [Header("Brush")]
    [SerializeField] private Texture2D brushTexture;
    [SerializeField][Range(0f, 1f)] private float brushSize = 0.05f;
    [SerializeField] private float maxDistance = 10f;

    [Header("Cleaning")]
    [SerializeField][Range(0f, 100f)] private float cleanSpeed = 10f;

    // runtime masks per renderer
    private Dictionary<Renderer, RenderTexture> runtimeMasks = new();

    Material brushMaterial;

    void Awake()
    {
        brushMaterial = new Material(Shader.Find("Hidden/BrushStrength"));
    }

    void Update()
    {
        if (shootAction.action.WasPressedThisFrame() || shootAction.action.IsPressed())
        {
            Paint();
        }
    }

    // =====================================================
    // MAIN PAINT FUNCTION
    // =====================================================

    void Paint()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));

        if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance))
            return;

        Renderer renderer = hit.collider.GetComponent<Renderer>();

        if (renderer == null)
            return;

        // Ensure runtime mask exists
        if (!runtimeMasks.TryGetValue(renderer, out RenderTexture runtimeMask))
        {
            runtimeMask = CreateRuntimeMask(renderer);
            runtimeMasks.Add(renderer, runtimeMask);
        }

        Vector2 uv = hit.textureCoord2;

        RenderTexture active = RenderTexture.active;
        RenderTexture.active = runtimeMask;

        GL.PushMatrix();
        GL.LoadOrtho();

        Rect rect = new Rect(uv.x - brushSize * 0.5f, uv.y - brushSize * 0.5f, brushSize, brushSize);

        float strength = Mathf.Clamp01(Time.deltaTime * cleanSpeed);
        brushMaterial.SetFloat("_Strength", strength);

        Graphics.DrawTexture(rect, brushTexture, brushMaterial);

        GL.PopMatrix();

        RenderTexture.active = active;

        // --------------------------------------------------
        // NEW: update cleaning progress tracking
        // --------------------------------------------------

        UpdateCleanProgress(renderer, uv);
    }

    // =====================================================
    // CREATE RUNTIME MASK
    // =====================================================

    RenderTexture CreateRuntimeMask(Renderer renderer)
    {
        Texture existingMask = renderer.material.GetTexture("_CleanMask");

        RenderTexture rt = new RenderTexture(maskTemplate.descriptor);
        rt.Create();

        if (existingMask != null)
        {
            Graphics.Blit(existingMask, rt);
        }
        else
        {
            RenderTexture active = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(true, true, Color.white);
            RenderTexture.active = active;
        }

        renderer.material.SetTexture("_CleanMask", rt);

        // IMPORTANT: link mask to PaintableObject
        PaintableObject paintable = renderer.GetComponent<PaintableObject>();

        if (paintable != null)
        {
            paintable.Initialize(rt);
        }

        return rt;
    }

    // =====================================================
    // NEW FUNCTION: UPDATE CLEAN PROGRESS
    // =====================================================

    void UpdateCleanProgress(Renderer renderer, Vector2 uv)
    {
        PaintableObject paintable = renderer.GetComponent<PaintableObject>();

        if (paintable == null)
            return;

        // NEW: pass UV and brush size directly
        paintable.UpdateCleanPixels(uv, brushSize);

        // Debug output
        float percent = paintable.GetCleanPercent();

        // Debug.Log(
        //     $"{renderer.name} Cleaned: {percent:F1}%");

        Managers.UI.SetCleanProgressBarPercent(percent);
    }

}
