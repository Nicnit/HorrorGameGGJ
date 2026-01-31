using System;
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
    [SerializeField] private float maskPutOnTimer;
    [SerializeField] private float maskTakeOffTimer;

    private InputAction toggleMaskAction;
    private bool maskOn;
    private bool changingMaskState = false;
    
    private void Start()
    {
        toggleMaskAction = InputSystem.actions.FindAction("ToggleMask");
    }


    private void Update()
    {
        // When mask put on/off, override current animation / cooldown and replace

        if (changingMaskState) // Update timers
        {
            if (maskOn)
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
            // TODO put on mask animation here
            PutOnMask();
        }
        else
        {
            curTimer = 0f;
            // TODO take off mask animation here
            TakeOffMask();
        }
    }
    

    private float curTimer;

    public void PutOnMask()
    {
        if (maskOn)
        {
            changingMaskState = false;
            return;
        }
        
        // Handle Timer until Animation finishes
        if (curTimer < maskPutOnTimer)
        {
            curTimer += Time.deltaTime;
            return;
        }
        
        SetMaskOn();
    }

    public void TakeOffMask()
    {
        if (!maskOn)
        {
            changingMaskState = false;
            return;
        }
        
        // Handle Timer until Animation finishes
        if (curTimer < maskTakeOffTimer)
        {
            curTimer += Time.deltaTime;
            return;
        }
        
        SetMaskOff();
    }

    private void SetMaskOn()
    {
        maskOn = true;
        
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
