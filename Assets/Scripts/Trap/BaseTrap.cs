using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public abstract class BaseTrap : MonoBehaviour
{
    [SerializeField] protected float aggressionDuration = 4f;
    [SerializeField] protected MeshRenderer trapVFX;
    [SerializeField] protected GameObject player;
    protected PlayerController playerController;
    [SerializeField] protected MaskManager maskManager;
    [SerializeField] protected bool selfDestructs = true;
    
    // Visuals
    private MeshRenderer[] childRenderers;
    private bool lastState = true;

    protected virtual void Awake()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");
        if (maskManager == null)
            maskManager = GameObject.FindGameObjectWithTag("Mask").GetComponent<MaskManager>();
        
        childRenderers = GetComponentsInChildren<MeshRenderer>();
    }

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
        if (trapVFX.enabled != maskManager.IsMaskOn)
            SetTrapVisibility(maskManager.IsMaskOn);
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger Enter");
        if (other.gameObject.tag.Equals("Player"))
        {
            DoTrapEffect();
        }
    }

    protected virtual void SetSizeRandomly(float range)
    {
        this.gameObject.transform.localScale += Vector3.one * Random.Range(-range, range);
    }
    
    public void SetTrapVisibility(bool isVisible)
    {
        foreach (MeshRenderer renderer in childRenderers)
        {
            if (renderer != null)
                renderer.enabled = isVisible;
        }
        
        // This parent object's visibility
        trapVFX.enabled = isVisible;
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