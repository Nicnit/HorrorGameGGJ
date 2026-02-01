using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class NoteUI : MonoBehaviour
{
    public GameObject noteObject;
    protected InputAction isInteracting;

    protected void Awake()
    {
        isInteracting = InputSystem.actions.FindAction("ItemInteract");
        isInteracting.Enable();
    }

    protected virtual void Update()
    {
        // If UI showing and player uses Interact button, do interaction
        // Also if no other UI is showing
        if (isInteracting != null && isInteracting.triggered) // TODO check if MAIN UI is activated via UI manager
        {
            noteObject.SetActive(false);
        }
    }

    public void ShowNote(string noteText) {
        noteObject.SetActive(true);
        noteObject.GetComponentInChildren<TMP_Text>().SetText(noteText);
    }
}
