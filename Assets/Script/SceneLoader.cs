using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadGame()
    {
        SceneManager.LoadScene("TutorialMap");
    }

     public void QuitGame()
    {
        Debug.Log("Game Ditutup");
        Application.Quit();
    }
}