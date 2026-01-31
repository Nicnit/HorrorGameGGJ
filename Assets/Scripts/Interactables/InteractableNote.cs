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
            noteVFX.SetActive(false);
    }
    
    
    
    protected override void OnInteractionTrigger()
    {
        // update progress manager to include this note as done/read
        FinishNote();
        
        // Show Note UI
        canShowNote = !canShowNote;
        
        // Do note sound effect
        ReadSFX();
    }
    
    private void ReadSFX()
    {
        // Play audio TODO audio manager involvement
        throw new System.NotImplementedException();
    }

    protected override void HideUINotice()
    {
        base.HideUINotice();
        // Hide Note so doesn't display when too far
        canShowNote = false;
    }
}
