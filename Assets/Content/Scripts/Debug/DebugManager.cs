using UnityEngine;
using UnityEngine.InputSystem;

public class DebugManager : MonoBehaviour
{
    [InlineScriptableObject]
    [SerializeField] QuestData testQuest_01;
    [SerializeField] InputActionReference interactAction;

    void Start()
    {
        interactAction.action.performed += OnDebugButton01Pressed;
    }

    void OnDebugButton01Pressed(InputAction.CallbackContext context)
    {
        Managers.Quests.StartQuest(testQuest_01);
    }

}
