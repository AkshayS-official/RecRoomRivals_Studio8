using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialManager : MonoBehaviour
{
    public void OnBack()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
