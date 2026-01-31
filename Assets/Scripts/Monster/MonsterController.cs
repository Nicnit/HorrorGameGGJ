using UnityEngine;

public class MonsterController : MonoBehaviour
{
    public enum MonsterState
    {
        Wander,
        Pursuit,
        Distancing
    }
    
    [SerializeField] float aggressionLevel = 0.5f; // 0 to 1 
        
        
    public void SetAggressionLevel(float level)
    {
        aggressionLevel = Mathf.Clamp01(level);
    }
    
    public float GetAggressionLevel()
    {
        return aggressionLevel;
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
