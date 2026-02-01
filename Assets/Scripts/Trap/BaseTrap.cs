using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public abstract class BaseTrap : MonoBehaviour
{
    [SerializeField] protected float aggressionDuration = 4f;
    [SerializeField] protected MeshRenderer trapVFX;
    [SerializeField] protected GameObject player;
    protected PlayerController playerController;
    [SerializeField] protected MaskManager maskManager;
    [SerializeField] protected bool selfDestructs = true;

    protected virtual void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");
        playerController = player.GetComponent<PlayerController>();
    }

    // If mask set on, show traps
    // If mask set off, hide traps
    protected void Update()
    {
        trapVFX.enabled = maskManager.IsMaskOn;
    }
    

    protected virtual void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger Enter");
        if (other.gameObject.tag.Equals("Player"))
        {
            DoTrapEffect();
        }
    }

    protected virtual void DoTrapEffect()
    {
        // TODO (if doing one SFX for all traps) traps SFX + monster scream SFX
        
        // Aggro monster
        GridChaser.Instance?.Aggro(aggressionDuration, false);
    }

    protected virtual void EndTrapLifecycle()
    {
        if (selfDestructs)
            this.gameObject.SetActive(false);
    }
}