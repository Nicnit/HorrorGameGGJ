using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.PlayerLoop;
using Update = Unity.VisualScripting.Update;

public class InteractableNote : BaseInteractable
{
    public List<Sprite> sprites;

    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (sprites.Count > 0)
        {
            int randomIndex = Random.Range(0, sprites.Count);
            spriteRenderer.sprite = sprites[randomIndex];
        }
    }


    protected override void OnInteractionTrigger(bool isMonster = false)
    {
        // update progress manager to include this note as done/read
        FinishInteractable();

        if (isMonster == false)
        {

            NoteUI note = FindFirstObjectByType<NoteUI>();

            // Show Note UI
            note.ShowNote();

            // Do note sound effect
            AudioManager.Instance.PlaySoundEffect(E_SoundEffect.Note);

            GameStateManager.Instance.numFoundNotes++;
            this.gameObject.SetActive(false);
        }
    }
}
