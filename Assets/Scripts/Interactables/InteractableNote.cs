using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.PlayerLoop;
using Update = Unity.VisualScripting.Update;

public class InteractableNote : BaseInteractable
{
    private bool canShowNote = false;
    [SerializeField] private GameObject noteVFX;

    protected override void Update()
    {
        base.Update();
        
        // Turn off the Notepaper if it isn't allowed to be seen
        if (!canShowNote && noteVFX.activeInHierarchy)
        {
            noteVFX.SetActive(false);
            Debug.Log("Cant show note");
        }
    }
    
    
    
    protected override void OnInteractionTrigger()
    {
        // update progress manager to include this note as done/read
        FinishInteractable();
        
        // Show Note UI
        canShowNote = !canShowNote;
        noteVFX.SetActive(true);
        
        // Do note sound effect
        // ReadSFX();
        
        Debug.Log("Did interaction trigger");
        Debug.Log("Can show note: " + canShowNote);
        Debug.Log(noteVFX.activeInHierarchy);
    }
    
    private void ReadSFX()
    {
        // Play audio TODO audio manager involvement
        throw new System.NotImplementedException();
    }

    protected override void HideUINotice()
    {
        Debug.Log("Hiding UI Notice");
        base.HideUINotice();
        // Hide Note so doesn't display when too far
        canShowNote = false;
    }
}
