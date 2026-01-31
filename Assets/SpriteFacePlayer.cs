using System;
using UnityEngine;

public class SpriteFacePlayer : MonoBehaviour
{
    [SerializeField] GameObject SpriteVFX;
    private GameObject player;
    
    
    
    private void LateUpdate()
    {
        SpriteVFX.transform.LookAt(transform.position);
    }
}
