using UnityEngine;

public class TripwireTrap : BaseTrap
{
    [SerializeField] protected float stunDuration;
    
    protected virtual void DoTrapEffect()
    {
        // Stun the player
        playerController.StunLock(stunDuration);
        
        //Stunlock VFX
        // TODO tripwire trap VFX
        // TODO tripwire trap SFX
        
        // Aggro the monster
        // TODO requires monster
        
    }
}
