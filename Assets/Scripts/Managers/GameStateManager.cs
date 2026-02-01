using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles the state of the game
/// Including
/// Progress in Story
/// 
/// </summary>
public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    private GameObject monsterSlashEffect; 
    Animator slashAnimator;

    private bool killingPlayer = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return; 
        }
        Instance = this;
    }

    private void Start()
    {
        monsterSlashEffect = GameObject.Find("MonsterSlashEffect");
        if (monsterSlashEffect != null)
            slashAnimator = monsterSlashEffect.GetComponent<Animator>();
        
        Application.targetFrameRate = 60; 
        QualitySettings.vSyncCount = 1;
    }


    [Tooltip("Player must reach these items to reach endstate")]
    List<BaseInteractable> objectiveInteractables = new List<BaseInteractable>();

    [SerializeField] private GameObject SlashEffect;

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

    public IEnumerator KillPlayer()
    {
        killingPlayer = true;
        // start animation
        DeathCam camDeath = GameObject.FindGameObjectWithTag("Player").GetComponentInChildren<DeathCam>();
        if (camDeath == null) Debug.Log("Couldn't find Death Cam");
        if (camDeath.isActive)
        {
            // already running onDeath
            yield return new WaitUntil(() => camDeath.isFinished());
            yield break;
        }
        Debug.Log("Calling on death.");
        camDeath.OnDeath();
        yield return new WaitUntil(() => camDeath.isFinished());

        // Show monster slashes
        slashAnimator.SetTrigger("Slash");
        
        // Reload Main Menu ?
        StartCoroutine(LoadAfterDelay(4f));
    }
    
    
    private IEnumerator LoadAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(0);
    }

    private void ReachEndState()
    {
        // TODO End game / unlock final door
        throw new System.NotImplementedException();
    }

    public bool isKillingPlayer()
    {
        return killingPlayer;
    }
}
