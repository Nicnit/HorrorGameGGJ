using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.PlayerLoop;
using Update = Unity.VisualScripting.Update;

public class InteractableNote : BaseInteractable
{
    public string noteText;
     
    protected override void OnInteractionTrigger()
    {
        // update progress manager to include this note as done/read
        FinishInteractable();

        NoteUI note = FindFirstObjectByType<NoteUI>();

        // Show Note UI
        note.ShowNote(noteText);

        // Do note sound effect
        AudioManager.Instance.PlaySoundEffect(E_SoundEffect.Note);
        
        this.gameObject.SetActive(false);
    }
}
