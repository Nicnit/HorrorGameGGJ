using UnityEngine;

public class TripwireTrap : BaseTrap
{
    [SerializeField] private float stunDuration;
    
    protected override void DoTrapEffect()
    {
        base.DoTrapEffect();
        // Stun the player
        playerController.StunLock(stunDuration);
        
        // Stunlock VFX / trap animation? Out of scope
        
        // destroy
        Invoke(nameof(EndTrapLifecycle),  stunDuration);
    }
}
