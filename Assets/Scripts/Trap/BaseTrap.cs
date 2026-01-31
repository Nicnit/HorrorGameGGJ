using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public abstract class BaseTrap : MonoBehaviour
{
    [SerializeField] protected MeshRenderer trapVFX;
    [SerializeField] protected GameObject player;
    protected PlayerController playerController;
    private MaskManager maskManager;

    protected virtual void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");
        playerController = player.GetComponent<PlayerController>();
        // maskManager = player.GetComponent<MaskManager>();
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
        // Default Behavior here
    }
    
    // If mask set on, show traps
    // If mask set off, hide traps
    protected void Update()
    {
        // TODO trapVFX.SetActive(maskManager.IsMaskOn);
    }
}