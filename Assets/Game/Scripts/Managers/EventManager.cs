using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public static class EventManager
{

    public static event UnityAction<GameMode> gameModeChange;
    public static void OnGameModeChange(GameMode mode) => gameModeChange?.Invoke(mode);



    public static event UnityAction DailyRewardClaimed;
    public static void OnDailyRewardClaimed() => DailyRewardClaimed?.Invoke();



    public static event UnityAction levelStart;
    public static void OnLevelStart() => levelStart?.Invoke();

    public static event UnityAction levelComplete;
    public static void OnLevelComplete() => levelComplete?.Invoke();

    public static event UnityAction levelFail;
    public static void OnLevelFail() => levelFail?.Invoke();

}