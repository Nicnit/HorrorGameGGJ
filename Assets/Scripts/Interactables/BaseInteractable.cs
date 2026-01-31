using System;
using UnityEngine;
using UnityEngine.InputSystem;

public abstract class BaseInteractable : MonoBehaviour
{
    private bool isFinished = false;
    protected void FinishInteractable() => isFinished = true;
    public bool IsFinished() => isFinished;
    [SerializeField] protected GameObject noticePopup;
    [SerializeField] protected float interactDistance;
 
    protected GameObject player;
    protected InputAction isInteracting;

    protected void Awake()
    {
        isInteracting = InputSystem.actions.FindAction("ItemInteract");
        isInteracting.Enable();
    }

    protected virtual void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
    }
    
    /// <summary>
    /// Manage the UI popup and player input
    /// </summary>
    protected virtual void Update()
    {
        // If player close enough, popup notice ui
        float playerDistance = Vector3.Distance(transform.position, player.transform.position);
        if (playerDistance < interactDistance)
            ShowUINotice();
        else
            HideUINotice();
        
        // If UI showing and player uses Interact button, do interaction
        // Also if no other UI is showing
        if (isInteracting != null && isInteracting.triggered
                && noticePopup.activeSelf) // TODO check if MAIN UI is activated via UI manager
        {
            OnInteractionTrigger();
        }
    }
    
    
    protected virtual void ShowUINotice()
    {
        noticePopup.SetActive(true);
    }
    
    protected virtual void HideUINotice()
    {
        noticePopup.SetActive(false);
    }


    /// <summary>
    /// When player performs interaction
    /// </summary>
    protected abstract void OnInteractionTrigger();
}
