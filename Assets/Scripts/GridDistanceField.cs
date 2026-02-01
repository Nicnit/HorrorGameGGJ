using System.Collections.Generic;
using UnityEngine;

public class GridDistanceField : MonoBehaviour
{
    [SerializeField] private Controller_LevelGeneration gen;
    [SerializeField] private Transform player;

    private int[,] _playerDist;
    private int[,] _targetDist;

    private Vector3Int _lastPlayerCell;
    private bool _playerBuilt;

    private Vector3Int _lastTargetCell;
    private bool _targetBuilt;

    public int[,] PlayerDistances => _playerDist;
    public int[,] TargetDistances => _targetDist;

    private void Awake()
    {
        if (gen == null) gen = FindFirstObjectByType<Controller_LevelGeneration>();
        if (player == null) player = GameObject.FindGameObjectWithTag("Player")?.transform;
        TryAllocate();
    }

    private void Start()
    {
        TryAllocate();
        if (CanRebuildPlayerNow()) RebuildPlayer();
    }

    private void Update()
    {
        if (player == null) player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (gen == null) gen = FindFirstObjectByType<Controller_LevelGeneration>();
        if (gen == null || gen.MapData == null || player == null) return;
            

        TryAllocate();
        if (_playerDist == null) return;

        var pc = gen.WorldToCell(player.position);
        if (!_playerBuilt || pc != _lastPlayerCell)
            RebuildPlayer();
    }

    public bool TryGetDistance(Vector3Int cell, out int distance)
    {
        return TryGetPlayerDistance(cell, out distance);
    }

    public bool TryGetPlayerDistance(Vector3Int cell, out int distance)
    {
        distance = -1;
        if (!_playerBuilt) return false;
        if (!gen.InBounds(cell)) return false;
        distance = _playerDist[cell.x, cell.z];
        return distance >= 0;
    }

    public bool TryGetTargetDistance(Vector3Int cell, out int distance)
    {
        distance = -1;
        if (!_targetBuilt) return false;
        if (!gen.InBounds(cell)) return false;
        distance = _targetDist[cell.x, cell.z];
        return distance >= 0;
    }

    public void BuildFromTargetCell(Vector3Int targetCell)
    {
        if (gen == null || gen.MapData == null) return;

        TryAllocate();
        if (_targetDist == null) return;

        _targetBuilt = true;
        _lastTargetCell = targetCell;

        FillUnreachable(_targetDist);

        if (!gen.InBounds(targetCell)) return;
        if (!gen.IsWalkableCell(targetCell)) return;

        var q = new Queue<Vector3Int>();
        _targetDist[targetCell.x, targetCell.z] = 0;
        q.Enqueue(targetCell);

        while (q.Count > 0)
        {
            var c = q.Dequeue();
            int cd = _targetDist[c.x, c.z];

            foreach (var n in gen.GetNeighbors4(c))
            {
                if (!gen.InBounds(n)) continue;
                if (!gen.IsWalkableCell(n)) continue;
                if (_targetDist[n.x, n.z] != -1) continue;

                _targetDist[n.x, n.z] = cd + 1;
                q.Enqueue(n);
            }
        }
    }

    public void RebuildPlayer()
    {
        if (!CanRebuildPlayerNow()) return;

        _playerBuilt = true;
        _lastPlayerCell = gen.WorldToCell(player.position);

        FillUnreachable(_playerDist);

        if (!gen.InBounds(_lastPlayerCell)) return;
        if (!gen.IsWalkableCell(_lastPlayerCell)) return;

        var q = new Queue<Vector3Int>();
        _playerDist[_lastPlayerCell.x, _lastPlayerCell.z] = 0;
        q.Enqueue(_lastPlayerCell);

        while (q.Count > 0)
        {
            var c = q.Dequeue();
            int cd = _playerDist[c.x, c.z];

            foreach (var n in gen.GetNeighbors4(c))
            {
                if (!gen.InBounds(n)) continue;
                if (!gen.IsWalkableCell(n)) continue;
                if (_playerDist[n.x, n.z] != -1) continue;

                _playerDist[n.x, n.z] = cd + 1;
                q.Enqueue(n);
            }
        }
    }

    private bool CanRebuildPlayerNow()
    {
        return gen != null && gen.MapData != null && player != null && _playerDist != null;
    }

    private void TryAllocate()
    {
        if (gen == null) return;
        if (gen.MapWidth <= 0 || gen.MapHeight <= 0) return;

        bool needsAlloc =
            _playerDist == null ||
            _playerDist.GetLength(0) != gen.MapWidth ||
            _playerDist.GetLength(1) != gen.MapHeight;

        if (!needsAlloc) return;

        _playerDist = new int[gen.MapWidth, gen.MapHeight];
        _targetDist = new int[gen.MapWidth, gen.MapHeight];

        _playerBuilt = false;
        _targetBuilt = false;
    }

    private static void FillUnreachable(int[,] dist)
    {
        int w = dist.GetLength(0);
        int h = dist.GetLength(1);
        for (int x = 0; x < w; x++)
        for (int z = 0; z < h; z++)
            dist[x, z] = -1;
    }
}
