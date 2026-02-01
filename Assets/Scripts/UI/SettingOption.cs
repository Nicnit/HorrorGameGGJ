using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingOption : MonoBehaviour
{
    public AudioMixer AudioControl;
    public Slider MasterSlider;
    public Slider SoundEffectSlider;
    public Slider BackgroundSlider;

    public void SetSlider()
    {
        MasterSlider.value = AudioManager.Instance.masterVolume;
        SoundEffectSlider.value = AudioManager.Instance.soundEffectVolume;
        BackgroundSlider.value = AudioManager.Instance.backgroundSliderVolume;
    }

    public void SetMasterVolume(float volume)
    {
        AudioManager.Instance.masterVolume = volume;
        AudioControl.SetFloat("Master", volume);
        if (MasterSlider.value == -30)
        {
            AudioControl.SetFloat("Master", -80);
        }
    }

    public void SetSoundEffectsVolume(float volume)
    {
        AudioManager.Instance.soundEffectVolume = volume;

        AudioControl.SetFloat("Sound Effect", volume);
        if (SoundEffectSlider.value == -30)
        {
            AudioControl.SetFloat("Sound Effect", -80);
        }
    }
    public void SetBackgroundVolume(float volume)
    {
        AudioManager.Instance.backgroundSliderVolume = volume;

        AudioControl.SetFloat("Background", volume);
        if (BackgroundSlider.value == -30)
        {
            AudioControl.SetFloat("Background", -80);
        }
    }
}