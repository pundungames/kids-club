//using GameAnalyticsSDK;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameEventListener : MonoBehaviour
{
    private void Awake()
    {
       // GameAnalytics.Initialize();

        DontDestroyOnLoad(gameObject);
    }
    private void Start()
    {
    }
    private void OnEnable()
    {
        EventManager.levelStart += LevelStart;
        EventManager.levelComplete += LevelComplete;
        EventManager.levelFail += LevelFail;
    }
    private void OnDisable()
    {
        EventManager.levelStart -= LevelStart;
        EventManager.levelComplete -= LevelComplete;
        EventManager.levelFail -= LevelFail;
    }
    void LevelStart()
    {
      //  GameAnalytics.NewProgressionEvent(GAProgressionStatus.Start, PlayerPrefs.GetInt("_level", 0).ToString(), "Stage_01", "Level_Progress");
        DebugEvent("LevelStart");
    }
    void LevelComplete()
    {
       // GameAnalytics.NewProgressionEvent(GAProgressionStatus.Complete, PlayerPrefs.GetInt("_level", 0).ToString(), "Stage_01", "Level_Progress");
        DebugEvent("LevelComplete");
    }
    void LevelFail()
    {
     //   GameAnalytics.NewProgressionEvent(GAProgressionStatus.Fail, PlayerPrefs.GetInt("_level", 0).ToString(), "Stage_01", "Level_Progress");
        DebugEvent("LevelComplete");
    }
    void DebugEvent(string eventType)
    {
        Debug.Log($"{eventType} event, level:{PlayerPrefs.GetInt("_level", 0)}");
    }
}