using System;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Random = UnityEngine.Random;

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
    [Header("Monster Aggro Settings")]
    // Upfront minimum aggro duration when putting on the mask
    [SerializeField] private float aggroDurationPuttingMaskOn;
    [Tooltip("If true, uses aggroDurationPutting as max")]
    [SerializeField] private bool randomizeAggroDuration;
    [SerializeField] private float minAggroDuration;
    [SerializeField] private float timeWithoutMaskUntilAggro; // Sets Aggro for randomly 
    
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


    private float maskOffTime;
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

        if (!IsMaskOn)
        {
            maskOffTime += Time.deltaTime;
            if (maskOffTime > timeWithoutMaskUntilAggro)
            {
                maskOffTime = 0;
                SetAggroDuration();
            }
        }
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
            // TODO audio mask on effect
            PutOnMask();
        }
        else
        {
            SetMaskOff(); 
            
            curTimer = 0f;
            changingMaskState = true;
            isPuttingOn = false;
            gameui.MaskOff();
            // TODO audio mask off effect
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
        AudioManager.Instance.PlaySoundEffect(E_SoundEffect.MaskOn);
        // Aggro set high monster aggro
        SetMonsterAggro();
        
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
        AudioManager.Instance.PlaySoundEffect(E_SoundEffect.MaskOff);
        // Aggro change Monster Aggro
        SetMonsterAggro();
        
        
        // Activate Fog
        SetFogLevel(0);
        
        // Hides traps via IsMaskOn 
        
        // Additional SFX / VFX
    }

    private void SetMonsterAggro()
    {
        // TODO keep aggro decay after mask off? Accumulates aggro from traps over time
        GridChaser.Instance?.ToggleAggroOverride(maskOn, true);
    }

    private void SetFogLevel(float val)
    {
        fogMat.SetFloat(IntensityID, val);
    }

    private void SetAggroDuration()
    {
        float duration = randomizeAggroDuration ? 
            Random.Range(minAggroDuration, aggroDurationPuttingMaskOn) :  aggroDurationPuttingMaskOn;
        // Make monster chase. After toggling off,continues chasing until timer runs out. in meantime pauses that timer.
        GridChaser.Instance?.Aggro(duration);
    }
    
    
    // If long enough without mask, Aggro monster?
}