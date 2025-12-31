using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SoundButton : MonoBehaviour
{
    [SerializeField] private AudioMixerGroup mixerGroup;
    [SerializeField] private Sprite muteSprite;
    private Sprite startSprite;
    private Image image;

    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnButton);
        image = transform.GetComponent<Image>();
        startSprite = image.sprite;
        Check();
    }

    public void Check()
    {
        var isActive = PlayerPrefs.GetInt(mixerGroup.name + "Setting", 1) == 1;
        if (isActive)
        {
            image.sprite = startSprite;
        }
        else
        {
            image.sprite = muteSprite;
        }
        if (mixerGroup.name == "Music")
            mixerGroup.audioMixer.SetFloat(mixerGroup.name + " Volume", isActive ? -5f : -80f);
        else
            mixerGroup.audioMixer.SetFloat(mixerGroup.name + " Volume", isActive ? 0f : -80f);
    }

    private void OnButton()
    {
        var isActive = PlayerPrefs.GetInt(mixerGroup.name + "Setting", 1) == 1;
        if (isActive)
        {
            image.sprite = muteSprite;
        }
        else
        {
            image.sprite = startSprite;
        }
        if (mixerGroup.name == "Music")
            mixerGroup.audioMixer.SetFloat(mixerGroup.name + " Volume", isActive ? -80f : -5f);
        else
            mixerGroup.audioMixer.SetFloat(mixerGroup.name + " Volume", isActive ? -80f : 0f);
        PlayerPrefs.SetInt(mixerGroup.name + "Setting", isActive ? 0 : 1);
    }
}
