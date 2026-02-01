using System.Collections;
using System.Collections.Specialized;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class DeathCam : MonoBehaviour
{

    public bool isActive = false;
    [SerializeField] private Transform neck;
    [SerializeField] private float radius = 1f;
    [SerializeField] private int numSwoops = 3;
    [SerializeField] private float swoopDuration;

    [SerializeField] private PlayerController pc;
    [SerializeField] private GameObject monsterPrefab;
    [SerializeField] private float distanceFromPlayer = 3.0f;
    [SerializeField] private LayerMask obstacleMask;

    private GameObject spawnedMonster;

    public void OnDeath()
    {
        if (isActive) return;
        isActive = true;
        // TODO: sfx signaling smtn spooky

        GameObject oldMonster = GameObject.FindGameObjectWithTag("Monster");
        Vector3 monsterPos = oldMonster != null ? oldMonster.transform.position : (neck.position + neck.forward * 5f); // get monster pos when it got player

        // remove old monster
        if (oldMonster != null)
            oldMonster.SetActive(false);

        // TODO: lock all Input / freeze game

        Vector3 finalScarePos = GetFinalPos(monsterPos);
        StartCoroutine(SwoopsSequence(finalScarePos));
    }

    private IEnumerator SwoopsSequence(Vector3 finalScarePos)
    {
        // draw disk in front of camera position by half its radius such that circle is completely in front of camera

        // pick random points within a sphere
        Vector3[] points = GetRandomPoints();

        // for each, swoop look towards it, then repeat for next point
        int spawnIndex = numSwoops / 2;
        for (int i = 0; i < numSwoops; i++)
        {
            yield return StartCoroutine(SwoopTo(points[i]));

            if (i == spawnIndex)
            {
                // halfway through swoops, new monster prefab spawns at new monster pos 
                if (monsterPrefab != null)
                    spawnedMonster = Instantiate(monsterPrefab, finalScarePos, Quaternion.identity);
            }
        }
        yield return StartCoroutine(SwoopTo(finalScarePos)); // center onto monster position
        yield return StartCoroutine(HandleEnd()); // monster disappear and reappear
    }

    private Vector3 GetFinalPos(Vector3 oldMonsterPos)
    {
        Vector3 dirToMonster = (oldMonsterPos - neck.position).normalized;

        // checking for valid spots along direction to monster for higher success
        float originalDistance = Vector3.Distance(neck.position, oldMonsterPos);
        RaycastHit hit;
        if (Physics.Raycast(neck.position, dirToMonster, out hit, distanceFromPlayer, obstacleMask))
        {
            if (hit.distance < 1.5f)
            {
                // check opposite dir for a better solution
                if (Physics.Raycast(neck.position, -dirToMonster, out hit, distanceFromPlayer, obstacleMask))
                    return hit.point + hit.normal * 0.5f;
                return neck.position - dirToMonster * distanceFromPlayer;
            }
            return hit.point + hit.normal * 0.5f; // hit a wall, so go in front
        }
        else {
            return neck.position + (dirToMonster * distanceFromPlayer);
        }
    }

    private IEnumerator HandleEnd()
    {
        bool showMonster = Random.value > 0.1f; // 90% monster shows, else player is spooked in the dark and gets jumpscared (optional)
        if (showMonster && spawnedMonster != null)
        {   
            // make monster appear for a random length 0.5 - 4
            float randomLength = Random.Range(0.5f, 4f);
            yield return new WaitForSeconds(randomLength);
        }

        if (spawnedMonster != null) Destroy(spawnedMonster);

        // wait in only darkness for a random bit
        float randTime = Random.Range(1f, 6f);
        yield return new WaitForSeconds(randTime);
        
        PlayDeath();
    }


    private IEnumerator SwoopTo(Vector3 point)
    {
        // should ease rotate to point
        Quaternion startRot = neck.rotation;
        Vector3 dir = (point - neck.position).normalized;
        if (dir.sqrMagnitude < 1e-6f) dir = neck.forward;

        Quaternion endRot = Quaternion.LookRotation(dir);

        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.001f, swoopDuration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            float ease = t * t * (3f - 2f * t);
            neck.rotation = Quaternion.Slerp(startRot, endRot, ease);
            yield return null;
        }
        neck.rotation = endRot;
    }

    private Vector3[] GetRandomPoints()
    {
        // create sphere in front of camera position, so the circle is mostly in front of camera
        Vector3 center = neck.position + neck.forward * Mathf.Max(radius * 0.5f, 0.5f);
        Vector3[] points = new Vector3[numSwoops];
        for (int i = 0; i < numSwoops; i++)
        {
            Vector2 rand = Random.insideUnitCircle * radius;
            points[i] = center + neck.right * rand.x + neck.up * rand.y;
        }
        return points;
    }
 
    private void PlayDeath()
    { 
        // lock input
        // TODO: animator and sfx
        // game over screen
    }
}
