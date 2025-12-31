using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
    public void Load()
    {
        int level = PlayerPrefs.GetInt("_level", 1);

        //loop
        if (SceneManager.GetActiveScene().buildIndex != level)
        {
            if (level > 27)
            {
                level = Random.Range(5, 28);
            }
        }
        SceneManager.LoadScene(level);
    }
    private void OnEnable()
    {
        EventManager.levelComplete += LevelComplete;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    private void OnDisable()
    {
        EventManager.levelComplete -= LevelComplete;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    void LevelComplete()
    {
        PlayerPrefs.SetInt("_level", PlayerPrefs.GetInt("_level", 1));
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EventManager.OnLevelStart();
    }
}