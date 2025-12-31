using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] GameObject menuPanel;
    [SerializeField] GameObject endGamePanel;
    [SerializeField] GameObject failPanel;
    private void Awake()
    {
    }
    void Start()
    {
    }

    private void OnEnable()
    {
        EventManager.gameModeChange += GameModeChange;
        EventManager.levelFail += Fail;
    }
    private void OnDisable()
    {
        EventManager.gameModeChange -= GameModeChange;
        EventManager.levelFail -= Fail;
    }
    void GameModeChange(GameMode mode)
    {
        
    }
    void EndGamePanel()
    {
        endGamePanel.SetActive(true);
    }

    public void GameStart()
    {
        EventManager.OnGameModeChange(GameMode.game);
        menuPanel.SetActive(false);
    }
    void Fail()
    {
        failPanel.SetActive(true);
        Invoke("Retry", 2f);
    }
    void Retry()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
public enum GameMode
{
    menu,
    game,
    endGame
}