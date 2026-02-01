using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
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
    MonsterCloseBy,
    MonsterFootstep,
    MonsterGrowling,
    MaskOn,
    MaskOff,
    AmbientNoise1,
    AmbientNoise2,
    DoorOpen,
    PlayerFootstep,
    Note,
}

public class AudioManager : MonoBehaviour
{
    [SerializeField] public static AudioManager Instance;

    [Header("Current Audio Playing")]
    public AudioSource BackgroundMusic;
    public AudioSource SoundEffect;

    [Header("Sound Effect Reference")]
    public AudioSource ButtonPressed;
    public AudioSource MonsterCloseBy;
    public AudioSource MonsterFootstep;
    public AudioSource MonsterGrowling;
    public AudioSource MaskOn;
    public AudioSource MaskOff;
    public AudioSource AmbientNoise1;
    public AudioSource AmbientNoise2;
    public AudioSource DoorOpen;
    public AudioSource PlayerFootstep;
    public AudioSource Note;


    [Header("Background Reference")]
    public AudioSource MainMenu;
    public AudioSource Game;

    [Header("Volume")]
    public float masterVolume = -15;
    public float soundEffectVolume = -15;
    public float backgroundSliderVolume = -15;

    [Header("Continuous Track")]
    public bool PlayerIsMoving = false;

    [Header("Ambident Track")]
    public float minDelay = 20f;
    public float maxDelay = 50f;

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

    private void Update()
    {
        if (PlayerIsMoving)
        {
            if(!PlayerFootstep.isPlaying)
                PlayerFootstep.Play();
        }
        else {
            if (PlayerFootstep.isPlaying)
                PlayerFootstep.Stop();
        }


    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        Debug.Log(scene.name);
        switch (scene.name)
        {
            case "MainMenu":
                Instance.ChangeBackground(E_BackGroundMusic.MainMenu);
                StopCoroutine(PlaySoundRandomly());
                break;
            case "Level Generation":
                Instance.ChangeBackground(E_BackGroundMusic.Game);
                StartCoroutine(PlaySoundRandomly());
                break;
        }
    }

    public void ChangeBackground(E_BackGroundMusic background)
    {

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
            case E_SoundEffect.Note:
                Note.Play();
                break;
            case E_SoundEffect.DoorOpen:
                DoorOpen.Play();
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
            case E_SoundEffect.Note:
                Note.Play();
                break;
            case E_SoundEffect.DoorOpen:
                DoorOpen.Play();
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
            case E_SoundEffect.Note:
                Note.Stop();
                break;
            case E_SoundEffect.DoorOpen:
                DoorOpen.Stop();
                break;
        }
    }

    private IEnumerator PlaySoundRandomly()
    {
        while (true)
        {
            float delay = UnityEngine.Random.Range(minDelay, maxDelay);
            Debug.Log(delay);
            yield return new WaitForSeconds(delay);

            // Play the sound
            int choice = UnityEngine.Random.Range(0, 2);

            if (choice == 0)
                AmbientNoise1.Play();
            else
                AmbientNoise2.Play();

        }
    }
}
