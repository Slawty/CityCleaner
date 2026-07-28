using UnityEngine;

public class JobClientSpokenCondition : JobCompletionCondition
{
    [SerializeField] JobClient client;

    bool listening;
    bool spokenWhileListening;

    public JobClient Client => client;

    public override bool IsMet => spokenWhileListening;

    public override Transform GetWaypointTransform() => client != null ? client.WaypointTransform : null;

    public override void StartListening()
    {
        if (listening || client == null)
            return;

        listening = true;
        spokenWhileListening = false;
        client.SpokenTo += HandleSpokenTo;
    }

    public override void StopListening()
    {
        if (!listening)
            return;

        listening = false;

        if (client != null)
            client.SpokenTo -= HandleSpokenTo;
    }

    void HandleSpokenTo()
    {
        if (!listening || spokenWhileListening)
            return;

        spokenWhileListening = true;
        NotifyChanged();
    }
}
