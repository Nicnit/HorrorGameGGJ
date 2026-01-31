using System;
using UnityEngine;

public class SpriteFacePlayer : MonoBehaviour
{
    [SerializeField] GameObject spriteVFX;
    [SerializeField] private bool lockXZRotation = false;
    private GameObject player;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
    }

    private void LateUpdate()
    {
        Vector3 lookAtRotation = player.transform.position - transform.position;
        Vector3 finalRotation = transform.rotation.eulerAngles;
        if (!lockXZRotation)
        {
            finalRotation.x = lookAtRotation.x;
            finalRotation.z = lookAtRotation.z;
        }
        finalRotation.y = lookAtRotation.y;
        
        spriteVFX.transform.LookAt(finalRotation);
    }
}
