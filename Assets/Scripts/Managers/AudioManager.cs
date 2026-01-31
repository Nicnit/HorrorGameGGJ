using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.Rendering.DebugUI;


public enum E_BackGroundMusic
{
    MainMenu,
    Game,
}
public enum E_SoundEffect
{
    ButtonPressed,
}

public class AudioManager : MonoBehaviour
{
    [SerializeField] public static AudioManager Instance;

    [Header("Current Audio Playing")]
    public AudioSource BackgroundMusic;
    public AudioSource SoundEffect;

    [Header("Sound Effect Reference")]
    public AudioSource ButtonPressed;

    [Header("Background Reference")]
    public AudioSource MainMenu;
    public AudioSource Game;

    [Header("Volume")]
    public float masterVolume = -15;
    public float soundEffectVolume = -15;
    public float backgroundSliderVolume = -15;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);

        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnSceneUnloaded(Scene scene)
    {

    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        switch (scene.name)
        {
            case "Main Menu":
                Instance.ChangeBackground(E_BackGroundMusic.MainMenu);
                break;
            case "Game":
                Instance.ChangeBackground(E_BackGroundMusic.Game);
                break;
        }
    }

    public void ChangeBackground(E_BackGroundMusic background)
    {

        if (BackgroundMusic.isPlaying)
            BackgroundMusic.Stop();

        switch (background)
        {
            case E_BackGroundMusic.MainMenu:
                BackgroundMusic = MainMenu;
                break;
            case E_BackGroundMusic.Game:
                BackgroundMusic = Game;
                break;
        }

        BackgroundMusic.Play();
    }

    public void PlaySoundEffect(E_SoundEffect soundEffect)
    {

        switch (soundEffect)
        {
            case E_SoundEffect.ButtonPressed:
                ButtonPressed.Play();
                break;
        }
    }
    public void PlaySoundEffect(string soundEffect)
    {

        switch ((E_SoundEffect)Enum.Parse(typeof(E_SoundEffect), soundEffect))
        {
            case E_SoundEffect.ButtonPressed:
                ButtonPressed.Play();
                break;
        }
    }

    public void StopSoundEffect(E_SoundEffect soundEffect)
    {

        switch (soundEffect)
        {
            case E_SoundEffect.ButtonPressed:
                ButtonPressed.Stop();
                break;
        }
    }
}
