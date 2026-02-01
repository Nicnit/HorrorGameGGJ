using System.Collections;
using UnityEngine;

public class InteractableDoor1 : BaseInteractable
{
    private bool canOpen = false;
    public Vector3 rotationAmount = new Vector3(0, -90, 0);
    public GameObject door;

    protected override void Update()
    {
        base.Update();
    }


    protected override void OnInteractionTrigger(bool isMonster = false)
    {
        // update progress manager to include this note as done/read
        FinishInteractable();

        if ((isMonster && FindFirstObjectByType<GridChaser>().DistanceToPlayer < 10f) || !isMonster) {
            // Do note sound effect
            AudioManager.Instance.PlaySoundEffect(E_SoundEffect.DoorOpen);
        }


        StartCoroutine(RotateCoroutine(rotationAmount, 3f, door));

        this.isInteractable = false;
    }

    private IEnumerator RotateCoroutine(Vector3 byAngles, float duration, GameObject gameObject)
    {
        Quaternion startRotation = gameObject.transform.rotation;
        Quaternion endRotation = startRotation * Quaternion.Euler(byAngles);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            gameObject.transform.rotation = Quaternion.Slerp(startRotation, endRotation, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null; // wait for next frame
        }

        gameObject.transform.rotation = endRotation; // ensure exact final rotation
    }

}
