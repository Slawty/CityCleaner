using UnityEngine;
using System.Collections.Generic;

public class InputManager : MonoBehaviour
{
    List<object> interactionBlockers = new List<object>();

    public bool InteractionBlocked()
    {
        return interactionBlockers.Count > 0;
    }

    public void BlockInteraction(Object source)
    {
        if (!interactionBlockers.Contains(source))
            interactionBlockers.Add(source);
    }

    public void UnblockInteraction(Object source)
    {
        interactionBlockers.Remove(source);
    }
}
