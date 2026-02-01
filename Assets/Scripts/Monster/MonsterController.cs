using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class GridChaser : MonoBehaviour
{
    [SerializeField] private Controller_LevelGeneration gen;
    [SerializeField] private GridDistanceField field;

    [Header("Steering")]
    [SerializeField] private float moveSpeed = 3.0f;
    [SerializeField] private float arriveRadius = 0.2f;
    [SerializeField] private float repathCooldown = 0.05f; // how often we pick a new cell
    [SerializeField] private float turnSpeed = 10f;

    private CharacterController _cc;
    private float _t;

    private Vector3Int _targetCell;
    private bool _hasTarget;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
        if (gen == null) gen = FindFirstObjectByType<Controller_LevelGeneration>();
        if (field == null) field = FindFirstObjectByType<GridDistanceField>();
    }

    private void Update()
    {
        _t += Time.deltaTime;
        if (_t >= repathCooldown)
        {
            _t = 0f;
            PickNextCell();
        }

        if (_hasTarget)
        {
            Vector3 targetWorld = gen.CellCenterWorld(_targetCell);
            SteerToward(targetWorld);
        }
    }

    private void PickNextCell()
    {
        if(field == null || gen == null)
            return;
        
        var myCell = gen.WorldToCell(transform.position);

        if (!gen.IsWalkableCell(myCell))
        {
            _hasTarget = false;
            return;
        }

        // If unreachable, bail
        if (!field.TryGetDistance(myCell, out int myD))
        {
            _hasTarget = false;
            return;
        }

        // If we’re already basically at the player cell (distance 0), no move needed
        if (myD == 0)
        {
            _hasTarget = false;
            return;
        }

        // Choose the walkable neighbor with smallest distance
        int bestD = myD;
        Vector3Int best = myCell;

        foreach (var n in gen.GetNeighbors4(myCell))
        {
            if (!gen.IsWalkableCell(n)) continue;
            if (!field.TryGetDistance(n, out int nd)) continue;
            if (nd < bestD)
            {
                bestD = nd;
                best = n;
            }
        }

        if (best == myCell)
        {
            _hasTarget = false; // stuck (maybe local minimum if dist map missing)
            return;
        }

        _targetCell = best;
        _hasTarget = true;
    }

    private void SteerToward(Vector3 targetWorld)
    {
        Vector3 to = targetWorld - transform.position;
        to.y = 0f;

        if (to.magnitude <= arriveRadius)
            return;

        Vector3 desired = to.normalized * moveSpeed;

        // Smooth facing
        if (desired.sqrMagnitude > 0.0001f)
        {
            Quaternion look = Quaternion.LookRotation(desired.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.deltaTime * turnSpeed);
        }

        // CharacterController move
        Vector3 move = desired * Time.deltaTime;
        _cc.Move(move);
    }
}
