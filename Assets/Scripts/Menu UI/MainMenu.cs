using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private string gameScene = "Game";
    public void PlayButton()
    {
        LoadLevel(gameScene);
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    public void LoadLevel(string targetScene)
    {
        SceneManager.LoadScene(targetScene);
    }
    

}
