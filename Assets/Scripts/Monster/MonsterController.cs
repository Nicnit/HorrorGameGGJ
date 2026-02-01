using System;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class GridChaser : MonoBehaviour
{
    public static GridChaser Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private Controller_LevelGeneration gen;
    [SerializeField] private GridDistanceField field;

    [Header("Steering")]
    [SerializeField] private float arriveRadius = 0.2f;
    [SerializeField] private float repathCooldown = 0.05f;
    [SerializeField] private float turnSpeed = 10f;

    [Header("Aggro")]
    [Range(0f, 1f)]
    [SerializeField] private float aggressionLevel = 0.0f;
    [SerializeField] private float aggressionDecaySeconds = 15f;
    [SerializeField] private float aggroSpeedMax = 5f;
    [SerializeField] private float aggroSpeedMin = 3f;
    [SerializeField] private float closeChaseSpeed = 3f;
    [SerializeField] private int closeChaseTiles = 3;

    [Header("Wander (Grid Path)")]
    [SerializeField] private float wanderSpeed = 4f;
    [SerializeField] private float wanderPickCooldown = 1.0f;
    [SerializeField] private int wanderPickAttempts = 200;
    [SerializeField] private int wanderMinManhattanDist = 6;
    [SerializeField] private float wanderGoalArriveTiles = 0.0f;

    [Header("Player")]
    [SerializeField] private bool playerHidden = false;

    [Header("Line of Sight (Override)")]
    [SerializeField] private Transform player;
    [SerializeField] private LayerMask obstacleMask = ~0;
    [SerializeField] private float losEyeHeight = 0.7f;
    [SerializeField] private float losPlayerHeight = 0.7f;
    [SerializeField] private float losMaxDistance = 999f;
    [SerializeField] private float losChaseSpeed = 5f;

    [Header("LoS Debug")]
    [SerializeField] private bool drawLosGizmos = true;
    [SerializeField] private bool drawLosEveryFrame = true;
    [SerializeField] private bool verboseLosLogging = false;
    [SerializeField] private float losLogCooldown = 0.5f;

    [Header("Debug Gizmos")]
    [SerializeField] private bool drawDebugGizmos = true;
    [SerializeField] private float gizmoGoalSphereRadius = 0.35f;
    [SerializeField] private float gizmoLineHeight = 0.15f;
    [SerializeField] private float gizmoDirLength = 2.0f;

    private CharacterController _cc;

    private enum Mode { Chase, Wander }
    [SerializeField] private Mode _mode = Mode.Wander;

    private float _aggroTimeRemaining;

    private float _chaseRepathT;
    private Vector3Int _chaseTargetCell;
    private bool _hasChaseTarget;

    private float _wanderPickT;
    private float _wanderRepathT;
    private Vector3Int _wanderGoalCell;
    private Vector3Int _wanderTargetCell;
    private bool _hasWanderGoal;
    private bool _hasWanderTarget;

    private Vector3 _lastSteerDir = Vector3.forward;

    private bool _lastLosClear;
    private Vector3 _lastLosOrigin;
    private Vector3 _lastLosTarget;
    private RaycastHit _lastLosHit;
    private bool _lastLosHadHit;
    private float _losLogT;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _cc = GetComponent<CharacterController>();
        if (gen == null) gen = FindFirstObjectByType<Controller_LevelGeneration>();
        if (field == null) field = FindFirstObjectByType<GridDistanceField>();
        if (player == null) player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (gen == null) gen = FindFirstObjectByType<Controller_LevelGeneration>();
        if (field == null) field = FindFirstObjectByType<GridDistanceField>();
        if (gen == null || field == null) return;

        if (player == null) player = GameObject.FindGameObjectWithTag("Player")?.transform;

        TickAggro(Time.deltaTime);

        Vector3Int myCell = gen.WorldToCell(transform.position);
        if (!gen.IsWalkableCell(myCell))
        {
            _hasChaseTarget = false;
            _hasWanderGoal = false;
            _hasWanderTarget = false;
            return;
        }

        bool los = HasLineOfSightToPlayer(out Vector3 playerWorld, out RaycastHit hit, out bool hadHit);

        if (verboseLosLogging)
        {
            _losLogT += Time.deltaTime;
            if (_losLogT >= losLogCooldown)
            {
                _losLogT = 0f;
                if (playerHidden)
                {
                    Debug.Log($"[GridChaser LoS] playerHidden=true => LoS forced false");
                }
                else if (player == null)
                {
                    Debug.Log($"[GridChaser LoS] player=null => LoS false");
                }
                else
                {
                    string maskBits = Convert.ToString(obstacleMask.value, 2);
                    if (hadHit)
                    {
                        Debug.Log($"[GridChaser LoS] blocked={(!los)} hit={hit.collider.name} layer={LayerMask.LayerToName(hit.collider.gameObject.layer)} dist={hit.distance:0.00} mask={obstacleMask.value}({maskBits})");
                    }
                    else
                    {
                        Debug.Log($"[GridChaser LoS] blocked={(!los)} hit=NONE distToPlayer={(Vector3.Distance(_lastLosOrigin, _lastLosTarget)):0.00} mask={obstacleMask.value}({maskBits})");
                    }
                }
            }
        }

        if (drawLosEveryFrame && player != null && drawLosGizmos)
        {
            Color c = los ? Color.green : Color.red;
            Debug.DrawLine(_lastLosOrigin, _lastLosTarget, c);
            if (_lastLosHadHit)
                Debug.DrawLine(_lastLosOrigin, _lastLosHit.point, Color.magenta);
        }

        if (los)
        {
            Vector3 to = playerWorld - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > 0.0001f) _lastSteerDir = to.normalized;
            SteerToward(playerWorld, losChaseSpeed);
            return;
        }

        bool closeToPlayer = false;
        if (!playerHidden && field.TryGetPlayerDistance(myCell, out int myD))
            closeToPlayer = myD <= closeChaseTiles;

        _mode = (!playerHidden && (aggressionLevel > 0f || closeToPlayer)) ? Mode.Chase : Mode.Wander;

        float speed = GetCurrentMoveSpeed(closeToPlayer);

        if (_mode == Mode.Chase)
        {
            _chaseRepathT += Time.deltaTime;
            if (_chaseRepathT >= repathCooldown)
            {
                _chaseRepathT = 0f;
                PickNextCell_DownhillToPlayer(myCell, out _chaseTargetCell, out _hasChaseTarget);
            }

            if (_hasChaseTarget)
            {
                Vector3 targetWorld = gen.CellCenterWorld(_chaseTargetCell);
                Vector3 d = targetWorld - transform.position;
                d.y = 0f;
                if (d.sqrMagnitude > 0.0001f) _lastSteerDir = d.normalized;
                SteerToward(targetWorld, speed);
            }

            _hasWanderTarget = false;
            return;
        }

        _wanderPickT += Time.deltaTime;
        if (!_hasWanderGoal || _wanderPickT >= wanderPickCooldown || ReachedWanderGoal(myCell))
        {
            TryPickNewWanderGoal(myCell);
            _wanderPickT = 0f;
            _hasWanderTarget = false;
            _wanderRepathT = repathCooldown;
        }

        if (!_hasWanderGoal) return;

        field.BuildFromTargetCell(_wanderGoalCell);

        _wanderRepathT += Time.deltaTime;
        if (_wanderRepathT >= repathCooldown)
        {
            _wanderRepathT = 0f;
            PickNextCell_DownhillToGoal(myCell, out _wanderTargetCell, out _hasWanderTarget);
        }

        if (_hasWanderTarget)
        {
            Vector3 targetWorld = gen.CellCenterWorld(_wanderTargetCell);
            Vector3 d = targetWorld - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude > 0.0001f) _lastSteerDir = d.normalized;
            SteerToward(targetWorld, speed);
        }
        else
        {
            _hasWanderGoal = false;
        }
    }

    public void Aggro(float? decaySeconds = null)
    {
        float s = decaySeconds ?? aggressionDecaySeconds;
        aggressionDecaySeconds = Mathf.Max(0.01f, s);

        aggressionLevel = 1.0f;
        _aggroTimeRemaining = aggressionDecaySeconds;

        _mode = Mode.Chase;
        _chaseRepathT = 999f;
    }

    public void SetPlayerHidden(bool hidden)
    {
        playerHidden = hidden;
        if (hidden)
        {
            aggressionLevel = 0f;
            _aggroTimeRemaining = 0f;
        }
    }

    private void TickAggro(float dt)
    {
        if (_aggroTimeRemaining > 0f)
        {
            _aggroTimeRemaining -= dt;
            float t = Mathf.Clamp01(_aggroTimeRemaining / aggressionDecaySeconds);
            aggressionLevel = t;
        }
        else
        {
            aggressionLevel = Mathf.Clamp01(aggressionLevel);
        }
    }

    private float GetCurrentMoveSpeed(bool closeToPlayer)
    {
        if (closeToPlayer) return closeChaseSpeed;
        if (_mode == Mode.Wander) return wanderSpeed;
        return Mathf.Lerp(aggroSpeedMin, aggroSpeedMax, aggressionLevel);
    }

    private void PickNextCell_DownhillToPlayer(Vector3Int myCell, out Vector3Int next, out bool hasNext)
    {
        next = myCell;
        hasNext = false;

        if (!field.TryGetPlayerDistance(myCell, out int myD)) return;
        if (myD == 0) return;

        int bestD = myD;
        Vector3Int best = myCell;

        foreach (var n in gen.GetNeighbors4(myCell))
        {
            if (!gen.IsWalkableCell(n)) continue;
            if (!field.TryGetPlayerDistance(n, out int nd)) continue;
            if (nd < bestD)
            {
                bestD = nd;
                best = n;
            }
        }

        if (best == myCell) return;

        next = best;
        hasNext = true;
    }

    private void PickNextCell_DownhillToGoal(Vector3Int myCell, out Vector3Int next, out bool hasNext)
    {
        next = myCell;
        hasNext = false;

        if (!field.TryGetTargetDistance(myCell, out int myD)) return;
        if (myD == 0) return;

        int bestD = myD;
        Vector3Int best = myCell;

        foreach (var n in gen.GetNeighbors4(myCell))
        {
            if (!gen.IsWalkableCell(n)) continue;
            if (!field.TryGetTargetDistance(n, out int nd)) continue;
            if (nd < bestD)
            {
                bestD = nd;
                best = n;
            }
        }

        if (best == myCell) return;

        next = best;
        hasNext = true;
    }

    private void TryPickNewWanderGoal(Vector3Int myCell)
    {
        if (gen.TryGetRandomWalkableCell(out var goal, attempts: wanderPickAttempts, avoid: myCell, minManhattanDist: wanderMinManhattanDist))
        {
            _wanderGoalCell = goal;
            _hasWanderGoal = true;
            return;
        }

        _hasWanderGoal = false;
    }

    private bool ReachedWanderGoal(Vector3Int myCell)
    {
        if (!_hasWanderGoal) return true;

        if (wanderGoalArriveTiles <= 0f)
            return myCell == _wanderGoalCell;

        int dx = Mathf.Abs(myCell.x - _wanderGoalCell.x);
        int dz = Mathf.Abs(myCell.z - _wanderGoalCell.z);
        return (dx + dz) <= Mathf.CeilToInt(wanderGoalArriveTiles);
    }

    private void SteerToward(Vector3 targetWorld, float speed)
    {
        Vector3 to = targetWorld - transform.position;
        to.y = 0f;

        if (to.magnitude <= arriveRadius)
            return;

        Vector3 desired = to.normalized * speed;

        if (desired.sqrMagnitude > 0.0001f)
        {
            Quaternion look = Quaternion.LookRotation(desired.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.deltaTime * turnSpeed);
        }

        Vector3 move = desired * Time.deltaTime;
        _cc.Move(move);
    }

    private bool HasLineOfSightToPlayer(out Vector3 playerWorld, out RaycastHit hit, out bool hadHit)
    {
        playerWorld = default;
        hit = default;
        hadHit = false;

        _lastLosClear = false;
        _lastLosHadHit = false;

        if (playerHidden) return false;
        if (player == null) return false;

        Vector3 origin = transform.position + Vector3.up * losEyeHeight;
        Vector3 target = player.position + Vector3.up * losPlayerHeight;

        _lastLosOrigin = origin;
        _lastLosTarget = target;

        Vector3 to = target - origin;
        float dist = to.magnitude;

        if (dist < 0.001f)
        {
            playerWorld = target;
            _lastLosClear = true;
            return true;
        }

        if (dist > losMaxDistance) return false;

        Vector3 dir = to / dist;

        if (Physics.Raycast(origin, dir, out RaycastHit rh, dist, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            hadHit = true;
            hit = rh;

            _lastLosHadHit = true;
            _lastLosHit = rh;

            _lastLosClear = false;
            return false;
        }

        playerWorld = target;
        _lastLosClear = true;
        return true;
    }

    private void OnDrawGizmos()
    {
        if (drawLosGizmos)
        {
            Gizmos.color = _lastLosClear ? Color.green : Color.red;
            Gizmos.DrawLine(_lastLosOrigin, _lastLosTarget);

            if (_lastLosHadHit)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawSphere(_lastLosHit.point, 0.08f);
            }
        }

        if (!drawDebugGizmos) return;

        Vector3 origin = transform.position + Vector3.up * gizmoLineHeight;

        Vector3 dir = _lastSteerDir;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;
        dir.Normalize();

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(origin, origin + dir * gizmoDirLength);

        if (_hasWanderGoal && gen != null)
        {
            Vector3 goal = gen.CellCenterWorld(_wanderGoalCell);
            goal.y = origin.y;

            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(goal, gizmoGoalSphereRadius);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(origin, goal);
        }
    }
}
