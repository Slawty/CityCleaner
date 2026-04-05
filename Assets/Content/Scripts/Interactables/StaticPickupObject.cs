using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class StaticPickupObject : MonoBehaviour, IInteractable
{
    [SerializeField] GameObject dynamicPrefab;
    bool collected;

    public string Prompt => "Pick up";

    public void Interact(GameObject interactor)
    {
        if (collected) return;

        Pickup(interactor);
    }

    public void InteractCanceled(GameObject interactor)
    {

    }

    public void Pickup(GameObject interactor)
    {
        if (collected) return;

        collected = true;

        // Hide static batched object
        gameObject.SetActive(false);

        // Spawn dynamic version
        PickupInteractable dynamic = Instantiate(dynamicPrefab, transform.position, transform.rotation).GetComponent<PickupInteractable>();
        // dynamic.SetStaticPickup(this);
        dynamic.Interact(interactor);
    }

    [ContextMenu("Setup Dynamic Object")]
    void SetupDynamicObject()
    {
#if UNITY_EDITOR
        // Instantiate temporary copy
        GameObject instance = Instantiate(gameObject, transform.position, transform.rotation);
        instance.name = gameObject.name.Replace("Static", "Dynamic");
        instance.isStatic = false;

        // Remove StaticPickupObject script
        StaticPickupObject staticScript = instance.GetComponent<StaticPickupObject>();
        if (staticScript != null)
            DestroyImmediate(staticScript);

        // Add Rigidbody if missing
        if (instance.GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb = instance.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        }

        // Add PickupInteractable if missing
        if (instance.GetComponent<PickupInteractable>() == null)
        {
            instance.AddComponent<PickupInteractable>();
        }

        // Selection.activeObject = instance;
        // EditorGUIUtility.PingObject(instance);

        // Ask where to save prefab
        string path = EditorUtility.SaveFilePanelInProject(
            "Save Dynamic Pickup Prefab",
            instance.name,
            "prefab",
            "Select location for dynamic prefab"
        );

        if (!string.IsNullOrEmpty(path))
        {
            // Save prefab asset
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);

            // Assign automatically
            dynamicPrefab = prefab;

            Undo.RecordObject(this, "Assign Dynamic Prefab");
            EditorUtility.SetDirty(this);
            PrefabUtility.RecordPrefabInstancePropertyModifications(this);

            // Apply override to prefab asset
            GameObject root = PrefabUtility.GetNearestPrefabInstanceRoot(gameObject);
            if (root != null)
            {
                PrefabUtility.ApplyPrefabInstance(root, InteractionMode.UserAction);
            }

            Debug.Log($"Dynamic prefab created at: {path}", prefab);
        }

        // Cleanup temporary instance
        DestroyImmediate(instance);
#endif
    }
}
