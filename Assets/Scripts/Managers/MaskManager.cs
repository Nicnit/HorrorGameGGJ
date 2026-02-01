using System;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

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
    
    [Tooltip("Match to time of animation if animation available")]
    [SerializeField] private float maskPutOnTimer;
    [SerializeField] private float maskTakeOffTimer;
    
    // Toggling fog
    [SerializeField] private Material fogMat;
    private static readonly int IntensityID = Shader.PropertyToID("_Intensity");
    
    private InputAction toggleMaskAction;
    private bool maskOn;
    public bool IsMaskOn => maskOn;
    private bool changingMaskState = false;
    private bool isPuttingOn = false; // Track direction to decouple from maskOn state
    
    private void Start()
    {
        toggleMaskAction = InputSystem.actions.FindAction("ToggleMask"); // ToggleMask
        toggleMaskAction.Enable();
        //if (pcRendererData != null)
          //  fogRendererFeature = pcRendererData.GetComponent<FullScreenPassRendererFeature>();
        //fogRendererFeature.SetActive(false);
        SetFogLevel(0);
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
        GameUI gameui = FindFirstObjectByType<GameUI>();
        bool newMaskState = !maskOn;
        if (newMaskState)
        {
            curTimer = 0f;
            changingMaskState = true;
            isPuttingOn = true;      
            gameui.MaskOn();
            PutOnMask();
        }
        else
        {
            SetMaskOff(); 
            
            curTimer = 0f;
            changingMaskState = true;
            isPuttingOn = false;
            gameui.MaskOff();
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
    }

    private void SetMaskOn()
    {
        maskOn = true;
        Debug.Log("SetMaskOn");
        
        // Mask is officially on, effects start immediately
        
        // TODO SFX Enhance Monster Audio SFX
        
        // TODO Agrro set high monster aggro
        
        // Activate Fog
        SetFogLevel(1);

        // Shows traps via IsMaskOn

        // Additional SFX / VFX
    }
    
    private void SetMaskOff()
    {
        maskOn = false;
        Debug.Log("SetMaskOff");
        
        // Mask is officially off, effects start immediately
        
        
        // TODO SFX change Monster Audio
        
        // TODO Aggro change Monster Aggro
        
        // Activate Fog
        SetFogLevel(0);
        
        // Hides traps via IsMaskOn 
        
        // Additional SFX / VFX
    }
    

    private void SetFogLevel(float val)
    {
        fogMat.SetFloat(IntensityID, val);
    }
    
    
}