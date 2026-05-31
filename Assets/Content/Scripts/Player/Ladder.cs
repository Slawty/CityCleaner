using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Ladder : MonoBehaviour
{
    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        SimplePlayerMovement movement = other.GetComponentInParent<SimplePlayerMovement>();
        if (movement == null)
            return;

        movement.EnterLadder(this);
    }

    void OnTriggerExit(Collider other)
    {
        SimplePlayerMovement movement = other.GetComponentInParent<SimplePlayerMovement>();
        if (movement == null)
            return;

        movement.ExitLadder(this);
    }
}
