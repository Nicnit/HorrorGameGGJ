using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles the state of the game
/// Including
/// Progress in Story
/// 
/// </summary>
public class GameStateManager : MonoBehaviour
{
    [Tooltip("Player must reach these items to reach endstate")]
    List<BaseInteractable> objectiveInteractables = new List<BaseInteractable>();

    private void Update()
    {
        // If notes are found
        // TODO Increase intensity with more notes found?

        foreach (BaseInteractable interactable in objectiveInteractables)
        {
            
        }
    }

    private void ReachEndState()
    {
        throw new System.NotImplementedException();
    }
}
