using UnityEngine;
using UnityEngine.SceneManagement;

public class BackgroundMusic : MonoBehaviour
{
    private static BackgroundMusic instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            // Subscribe to scene change event
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // If the new scene is a level, destroy this GameObject to stop the music
        if (scene.name == "Level 1" || scene.name == "Level 2") // add more as needed
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        // Clean up event subscription when object is destroyed
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
