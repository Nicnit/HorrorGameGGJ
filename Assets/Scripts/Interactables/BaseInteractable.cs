using System;
using UnityEngine;
using UnityEngine.InputSystem;

public abstract class BaseInteractable : MonoBehaviour
{
    private bool isFinished = false;
    protected void FinishInteractable() => isFinished = true;
    public bool IsFinished() => isFinished;
    [SerializeField] protected float interactDistance;

    protected GameObject player;
    protected InputAction isInteracting;
    public string InteractableText;
    public bool isInteractable = true;

    protected void Awake()
    {
        isInteracting = InputSystem.actions.FindAction("ItemInteract");
        isInteracting.Enable();
    }

    protected virtual void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
    }

    protected virtual void Update()
    {
        if (isInteracting != null && isInteracting.triggered && isInteractable && playerInRange)
        {
            OnInteractionTrigger();
        }
    }

    // --- Trigger-based detection ---
    private bool playerInRange = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && isInteractable)
        {
            playerInRange = true;
            ToggleUINotice(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            ToggleUINotice(false);
        }
    }

    protected virtual void ToggleUINotice(bool show) {
        NoticeUI notice = FindFirstObjectByType<NoticeUI>();
        notice.ToggleNotice(show, InteractableText);
    }

    /// <summary>
    /// When player performs interaction
    /// </summary>
    protected abstract void OnInteractionTrigger();
}
