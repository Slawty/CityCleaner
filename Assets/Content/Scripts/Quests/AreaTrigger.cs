using UnityEngine;

public class AreaTrigger : MonoBehaviour
{
    public QuestData quest;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Player entered area: {quest.description}");

        Managers.Quests.StartQuest(quest);
    }

    private void OnTriggerExit(Collider other)
    {
        Managers.Quests.StopCurrentTrackedQuest();
    }
}
