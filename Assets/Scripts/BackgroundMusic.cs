using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class BackgroundMusic : MonoBehaviour
{
    private static BackgroundMusic instance;
    private AudioSource audioSource;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            audioSource = GetComponent<AudioSource>();

            if (audioSource.clip != null && !audioSource.isPlaying)
            {
                audioSource.loop = true;
                audioSource.Play();
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("Scene loaded: " + scene.name);

        if (IsLevelScene(scene.name))
        {
            Debug.Log("Pausing background music in: " + scene.name);
            if (audioSource.isPlaying)
                audioSource.Pause();
        }
        else
        {
            Debug.Log("Resuming background music in: " + scene.name);
            if (!audioSource.isPlaying)
                audioSource.UnPause();
        }
    }

    private bool IsLevelScene(string sceneName)
    {
        // Add all your actual game level scene names here
        return sceneName == "Level 1" || sceneName == "Level 2" || sceneName == "Level 3";
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
