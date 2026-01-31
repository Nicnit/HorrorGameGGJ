using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public abstract class BaseTrap : MonoBehaviour
{
    [SerializeField] protected GameObject player;
    protected PlayerController playerController;

    protected virtual void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");
        playerController = player.GetComponent<PlayerController>();
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.tag.Equals("Player"))
        {
            DoTrapEffect(other.gameObject);
        }
    }

    protected virtual void DoTrapEffect(GameObject player)
    {
        // Default Behavior here
    }
    
    // If mask set on, show traps
    // If mask set off, hide traps
    
    private 
    
    
}