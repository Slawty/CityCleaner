using UnityEngine;

public class AreaManager : MonoBehaviour
{
    QuestInstance currentQuest;

    public void EnterArea(QuestInstance quest)
    {
        currentQuest = quest;

        if (!currentQuest.WasStarted)
            currentQuest.StartQuest();
    }

    public void ExitArea(QuestInstance quest)
    {

    }
}
