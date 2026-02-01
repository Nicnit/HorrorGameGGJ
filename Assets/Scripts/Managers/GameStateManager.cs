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
        // Increase intensity with more notes found?

        int numFinished = 0;
        foreach (BaseInteractable interactable in objectiveInteractables)
        {
            if (interactable != null && interactable.IsFinished())
                numFinished++;
        }
        
        if (numFinished >= objectiveInteractables.Count)
            ReachEndState();
    }

    private void ReachEndState()
    {
        // TODO End game / unlock final door
        throw new System.NotImplementedException();
    }
}
