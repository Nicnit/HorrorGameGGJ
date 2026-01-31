using System;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

public class MaskManager : MonoBehaviour
{
    /* Mask
     See traps (Set visibility of trap)
    Amplify Sound
    Monster Breathing Louder
        Monster Footstep Louder
    Affect Vision
    OFF : See environment
    ON: See Fog. Before fog distance, see traps, monster. Loud directional sound of monster.
        Amp Up Monster Aggression and Speed
    */
    
    [SerializeField] private GameObject vignetteVFX;
    [Tooltip("Canvas objects that represent the mask when worn")]
    [SerializeField] private GameObject maskVFX;
    
    // TODO can replace this with animaiton state machine, only user normal maskvfx
    [Tooltip("Canvas objects that represent the mask when being taken off / put on")]
    [SerializeField] private GameObject maskMoveVFX;
    
    [Tooltip("Match to time of animation if animation available")]
    [SerializeField] private float maskPutOnTimer;
    [SerializeField] private float maskTakeOffTimer;

    private InputAction toggleMaskAction;
    private bool maskOn;
    private bool changingMaskState = false;
    private bool isPuttingOn = false; // Track direction to decouple from maskOn state
    
    private void Start()
    {
        toggleMaskAction = InputSystem.actions.FindAction("ToggleMask"); // ToggleMask
        toggleMaskAction.Enable();
        maskVFX.SetActive(false);
        maskMoveVFX.SetActive(false);
    }


    private void Update()
    {
        // When mask put on/off, override current animation / cooldown and replace
        
        if (changingMaskState) // Update timers
        {
            if (!isPuttingOn)
                TakeOffMask();
            else
                PutOnMask();
        }
        
        bool toggleMask = toggleMaskAction.triggered;
        if (toggleMask)
            OnMaskTrigger();
    }

    private void OnMaskTrigger()
    {
        bool newMaskState = !maskOn;
        if (newMaskState)
        {
            curTimer = 0f;
            changingMaskState = true;
            maskMoveVFX.SetActive(true);
            isPuttingOn = true;
            // TODO put on mask animation here
            PutOnMask();
        }
        else
        {
            SetMaskOff(); 
            
            curTimer = 0f;
            maskMoveVFX.SetActive(true); // Re-enable because SetMaskOff disabled it
            changingMaskState = true;
            isPuttingOn = false;
            // TODO take off mask animation here
            TakeOffMask();
        }
    }
    

    private float curTimer;

    public void PutOnMask()
    {
        // Handle timer until anim finishes
        if (curTimer < maskPutOnTimer)
        {
            curTimer += Time.deltaTime;
            return;
        }

        changingMaskState = false;
        SetMaskOn();
    }

    public void TakeOffMask()
    {
        // Handle timer until anim finishes
        if (curTimer < maskTakeOffTimer)
        {
            curTimer += Time.deltaTime;
            return;
        }
        
        changingMaskState = false;
        maskMoveVFX.SetActive(false);
    }

    private void SetMaskOn()
    {
        maskOn = true;
        maskVFX.SetActive(true);
        maskMoveVFX.SetActive(false);
        Debug.Log("SetMaskOn");
        
        // Mask is officially on, effects start immediately
        
        // Enable Vignette
        SetVignette(false);
        // TODO Enhance Monster Audio
        
        // TODO Set Monster Aggro
        
        // TODO Activate Fog
        
        // TODO Show traps
        
        // Additional SFX / VFX
    }
    
    private void SetMaskOff()
    {
        maskOn = false;
        maskVFX.SetActive(false);
        maskMoveVFX.SetActive(false);
        Debug.Log("SetMaskOff");
        
        // Mask is officially off, effects start immediately
        
        // Enable Vignette
        SetVignette(true);
        // Enhance Monster Audio
        
        // Set Monster Aggro
        
        // Activate Fog
        
        // Show traps
        
        // Additional SFX / VFX
    }
    

    private void SetVignette(bool setOn)
    {
        // For now just turn on static/animated vignette
        // Disable on taking off mask. Layer Vignette behind UI
        vignetteVFX.SetActive(setOn);
    }
    
    
}