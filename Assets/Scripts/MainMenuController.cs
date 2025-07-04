using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public void LoadLevelsMenu()
    {
        SceneManager.LoadScene("Levels menu"); 
    }

    public void GoToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void GoToLevelsMenu()
    {
        SceneManager.LoadScene("Levels menu");
    }
}
