using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SoundSlider : MonoBehaviour
{
    [SerializeField] private AudioMixerGroup mixerGroup;
    [SerializeField] private Slider volumeSlider;
    private string VolumeParam => mixerGroup.name + " Volume";
    private string PrefKey => mixerGroup.name + "VolumeValue";

    private void Start()
    {
        volumeSlider.onValueChanged.AddListener(OnSliderValueChanged);

        InitVolume();
    }
    public void InitVolume()
    {
        float savedVolume = PlayerPrefs.GetFloat(PrefKey, 0.75f);
        volumeSlider.value = savedVolume;
        ApplyVolume(savedVolume);
    }

    private void OnSliderValueChanged(float value)
    {
        ApplyVolume(value);
        PlayerPrefs.SetFloat(PrefKey, value);
    }

    private void ApplyVolume(float sliderValue)
    {
        // Linear (0.0001 - 1) → dB (-80 to 0)
        float dB = Mathf.Log10(Mathf.Clamp(sliderValue, 0.0001f, 1f)) * 20f;
        mixerGroup.audioMixer.SetFloat(VolumeParam, dB);
    }
}
