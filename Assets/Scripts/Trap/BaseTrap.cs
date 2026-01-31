using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public abstract class BaseTrap : MonoBehaviour
{
    protected GameObject player;
    protected PlayerController playerController;

    protected virtual void Start()
    {
        player = GameObject.FindWithTag("Player");
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
    
    
    
    
}