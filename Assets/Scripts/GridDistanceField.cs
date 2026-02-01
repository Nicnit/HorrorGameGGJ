using System.Collections.Generic;
using UnityEngine;

public class GridDistanceField : MonoBehaviour
{
    [SerializeField] private Controller_LevelGeneration gen;
    [SerializeField] private Transform player;

    // distances[x,z] = steps to player. -1 = unreachable.
    private int[,] _dist;
    private Vector3Int _lastPlayerCell;
    private bool _hasBuilt;

    public int[,] Distances => _dist;

    private void Awake()
    {
        if (gen == null) gen = FindFirstObjectByType<Controller_LevelGeneration>();

        // DO NOT do .transform on a potentially-null result
        if (player == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) player = go.transform;
        }

        // Allocate here so TryGetDistance can't NRE due to Start order.
        TryAllocate();
    }

    private void Start()
    {
        // In case gen wasn’t ready in Awake (rare), try again.
        TryAllocate();

        // Don't rebuild until we actually have a player + map
        if (CanRebuildNow())
            Rebuild();
    }

    private void Update()
    {
        // Late-bind player if it spawns after we start
        if (player == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) player = go.transform;
            else return;
        }

        // Ensure we have gen + dist
        if (gen == null)
        {
            gen = FindFirstObjectByType<Controller_LevelGeneration>();
            if (gen == null) return;
        }

        if (_dist == null)
        {
            TryAllocate();
            if (_dist == null) return;
        }

        // If your map gets generated in Start(), gen.MapData may be null in early frames
        if (gen.MapData == null)
            return;

        var pc = gen.WorldToCell(player.position);
        if (!_hasBuilt || pc != _lastPlayerCell)
        {
            Rebuild();
        }
    }

    private bool CanRebuildNow()
    {
        return gen != null && gen.MapData != null && player != null && _dist != null;
    }

    private void TryAllocate()
    {
        if (gen == null) return;

        // If you ever change map size dynamically, reallocate if mismatch.
        if (_dist == null || _dist.GetLength(0) != gen.MapWidth || _dist.GetLength(1) != gen.MapHeight)
        {
            _dist = new int[gen.MapWidth, gen.MapHeight];
            _hasBuilt = false;
        }
    }

    public void Rebuild()
    {
        if (!CanRebuildNow())
            return;

        _hasBuilt = true;
        _lastPlayerCell = gen.WorldToCell(player.position);

        // init to unreachable
        for (int x = 0; x < gen.MapWidth; x++)
        for (int z = 0; z < gen.MapHeight; z++)
            _dist[x, z] = -1;

        if (!gen.IsWalkableCell(_lastPlayerCell))
            return;

        var q = new Queue<Vector3Int>();
        _dist[_lastPlayerCell.x, _lastPlayerCell.z] = 0;
        q.Enqueue(_lastPlayerCell);

        while (q.Count > 0)
        {
            var c = q.Dequeue();
            int cd = _dist[c.x, c.z];

            foreach (var n in gen.GetNeighbors4(c))
            {
                if (!gen.InBounds(n)) continue;
                if (!gen.IsWalkableCell(n)) continue;
                if (_dist[n.x, n.z] != -1) continue;

                _dist[n.x, n.z] = cd + 1;
                q.Enqueue(n);
            }
        }
    }

    public bool TryGetDistance(Vector3Int cell, out int d)
    {
        d = -1;

        // SUPER IMPORTANT: protect against initialization order
        if (gen == null || _dist == null)
            return false;

        if (!gen.InBounds(cell))
            return false;

        d = _dist[cell.x, cell.z];
        return d >= 0;
    }
}
