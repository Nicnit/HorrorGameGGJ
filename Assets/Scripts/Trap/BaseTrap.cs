using Unity.VisualScripting;
using UnityEngine;

public abstract class BaseTrap : MonoBehaviour
{
    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            DoTrapEffect(other.gameObject);
        }
    }

    protected virtual void DoTrapEffect(GameObject player)
    {
        // Default Behavior here
    }
    
    
    
    
}