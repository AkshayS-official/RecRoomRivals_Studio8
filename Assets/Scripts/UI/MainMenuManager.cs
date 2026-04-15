using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public void OnStartGame()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void OnTutorial()
    {
        SceneManager.LoadScene("Tutorial");
    }

    public void OnQuit()
    {
        Debug.Log("[MainMenu] Quit");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
