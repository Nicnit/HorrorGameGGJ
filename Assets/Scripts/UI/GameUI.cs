using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameUI : MonoBehaviour
{
    public GameObject pauseMenu;
    private bool isPaused = false;
    public Animator Mask;
    public bool isMaskOn = false;

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Debug.Log("Escape Pressed");
            TogglePause();
        }
    }

    public void TogglePause() {
        isPaused = !isPaused;

        Cursor.visible = isPaused;
        if (isPaused) Cursor.lockState = CursorLockMode.None;
        Debug.Log("Pressed");

        pauseMenu.SetActive(isPaused);
        Time.timeScale = isPaused ? 0f : 1f;
    }

    public void PlayButtonPress()
    {
        AudioManager.Instance.PlaySoundEffect(E_SoundEffect.ButtonPressed);
    }

    public void Exit() {
        Debug.Log("Pressed");
        SceneManager.LoadScene("MainMenu");
    }

    public void MaskOn() {
        Mask.SetTrigger("PutOnMask");           // Play("Mask On");
    }

    public void MaskOff()
    {
        Mask.SetTrigger("TakeOffMask"); //Play("Mask Off");
    }
}
