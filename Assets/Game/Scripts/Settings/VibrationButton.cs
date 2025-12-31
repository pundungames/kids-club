using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class VibrationButton : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private GameObject buttonOn;
    [SerializeField] private GameObject buttonOff;
    [SerializeField] private Sprite buttonOnSprite;
    [SerializeField] private Sprite buttonOffSprite;


    private bool vibration;

    private void Awake()
    {
        InitVibration();
    }
    public void InitVibration()
    {
        int vibSetting = PlayerPrefs.GetInt("vibration", 1);
        vibration = vibSetting == 1;
        Taptic.tapticOn = vibration;
        ApplyUI(vibration, instant: true);
    }

    public void OnVibrationClick()
    {
        vibration = !vibration;
        PlayerPrefs.SetInt("vibration", vibration ? 1 : 0);
        Taptic.tapticOn = vibration;

        ApplyUI(vibration);
    }

    private void ApplyUI(bool isOn, bool instant = false)
    {
        buttonOn.SetActive(isOn);
        buttonOff.SetActive(!isOn);

    }
}
