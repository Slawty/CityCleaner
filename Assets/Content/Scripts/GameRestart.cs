using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class GameRestart
{
    const string DefaultStartSceneName = "Start Scene";

    public static void LoadStartScene(string startSceneName = DefaultStartSceneName)
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene(startSceneName, LoadSceneMode.Single);
    }

    public static void QuitApplication()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
