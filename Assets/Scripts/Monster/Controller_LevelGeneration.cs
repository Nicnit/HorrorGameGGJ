using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public class Controller_LevelGeneration : MonoBehaviour
{
    [Header("Map Prefabs")]
    [SerializeField] private GameObject floorPrefab;
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject grassPrefab;

    [SerializeField] private GameObject playerPrefab;

    [SerializeField] private GameObject worldLightPrefab;
    //Variation Pieces
    [SerializeField] private GameObject[] wallVariants;

    [Header("Grid Settings")]
    [SerializeField] private int tileSize = 5;
    [SerializeField] private int mapWidth = 30;
    [SerializeField] private int mapHeight = 30;
    [SerializeField] private int mapDepth = 1;

    [Header("Wall Offsets (temporary)")]
    [SerializeField] private float wallVerticalOffset = 1.5f;

    [Header("Randomness Sliders")]
    [Range(0f, 1f)]
    [SerializeField] private float directionalStickiness = 0.7f;

    [Header("Paint Params")]
    [SerializeField] private int paintBudget = 500;
    [Range(0f, 1f)]
    [SerializeField] private float jumpChancePerStep = 0.02f;
    [SerializeField] private int maxJumps = 200;

    [Header("Room Params")]
    [SerializeField] private int minRooms = 6;
    [SerializeField] private int maxRooms = 12;
    [Range(0f, 1f)]
    [SerializeField] private float roomAttemptChancePerPaintStep = 0.03f;
    [SerializeField] private Vector2Int roomWidthRange = new Vector2Int(3, 7);
    [SerializeField] private Vector2Int roomHeightRange = new Vector2Int(3, 7);
    [SerializeField] private int roomPlacementAttempts = 200;
    [SerializeField] private float variantWallChance = 0.3f;

    [Header("Debug")]
    [SerializeField] private bool verboseLogs = false;

    public enum MapElement
    {
        Empty,
        Hall,
        Room
    }

    private enum Direction
    {
        North,
        South,
        East,
        West,
        None
    }

    [Serializable]
    public struct MapPosition
    {
        public Vector3Int Position;
        public MapElement Element;
    }

    public MapPosition[,,] MapData;

    // Track placed tiles by type (makes trunk/room placement reliable)
    private readonly List<Vector3Int> _hallCells = new List<Vector3Int>();
    private readonly List<Vector3Int> _roomCells = new List<Vector3Int>();
    
    private GameObject _wallsParent;
    private readonly HashSet<string> _spawnedWallEdges = new HashSet<string>();

    private GameObject _mapParent;
    private readonly Random _rng = new Random();

    // --------------------------
    // Unity Entry
    // --------------------------
    private void Start()
    {
        _mapParent = new GameObject("Map Container");
        MapData = new MapPosition[mapWidth, mapDepth, mapHeight];

        InitializeMapData();

        Vector3Int center = new Vector3Int(mapWidth / 2, 0, mapHeight / 2);

        // Starter room
        StampRect(center.y, center.x - 1, center.x + 1, center.z - 1, center.z + 1, MapElement.Room);

        // Starter hall stubs so painting has valid trunks immediately
        SeedHallStubsFromStartRoom(center);

        RunPaint(paintBudget);
        BuildWallsFromMap(0);
        
        Vector3 worldPosCenter = MapPositionToWorld(center);

        Instantiate(playerPrefab, new Vector3(worldPosCenter.x, worldPosCenter.y+wallVerticalOffset+.5f, worldPosCenter.z), Quaternion.identity);
        Instantiate(worldLightPrefab, new Vector3(worldPosCenter.x, worldPosCenter.y+wallVerticalOffset+.5f, worldPosCenter.z), Quaternion.identity);
    }

    // --------------------------
    // Initialization
    // --------------------------
    private void InitializeMapData()
    {
        for (int x = 0; x < mapWidth; x++)
        for (int y = 0; y < mapDepth; y++)
        for (int z = 0; z < mapHeight; z++)
        {
            if (verboseLogs)
                Debug.Log($"Init Position: {x},{y},{z}");

            MapData[x, y, z] = new MapPosition
            {
                Position = new Vector3Int(x, y, z),
                Element = MapElement.Empty
            };
        }
    }

    // --------------------------
    // High-level Generation
    // --------------------------
    private void RunPaint(int budget)
    {
        int jumps = 0;

        // Count the starter room toward your min/max target
        int targetRooms = _rng.Next(minRooms, maxRooms + 1);
        int roomsPlaced = _roomCells.Count > 0 ? 1 : 0;

        if (!TryPickRandomTrunk(out Vector3Int pos, out Direction dir))
            return;

        while (budget > 0 && jumps < maxJumps)
        {
            // Occasionally try to attach a room to the current hall tile
            if (roomsPlaced < targetRooms && _rng.NextDouble() < roomAttemptChancePerPaintStep)
            {
                if (MapData[pos.x, pos.y, pos.z].Element == MapElement.Hall)
                {
                    if (TryStampRoomFromHall(pos, pos.y, out _))
                    {
                        roomsPlaced++;

                        // Jump away after stamping a room so we don't clump
                        if (TryPickRandomTrunk(out pos, out dir))
                        {
                            jumps++;
                            continue;
                        }
                    }
                }
            }

            // Random jump sometimes for variety + coverage
            if (_rng.NextDouble() < jumpChancePerStep)
            {
                if (TryPickRandomTrunk(out pos, out dir))
                {
                    jumps++;
                    continue;
                }
                break;
            }

            // Try to place a hall step
            Direction nextDir = AttemptHallStep(pos, dir, out Vector3Int nextPos);

            if (nextDir == Direction.None)
            {
                // stuck => jump to a new trunk
                if (TryPickRandomTrunk(out pos, out dir))
                {
                    jumps++;
                    continue;
                }
                break;
            }

            budget--;
            pos = nextPos;
            dir = nextDir;
        }

        // Guarantee min rooms if RNG / constraints didn’t hit it
        int minTarget = Mathf.Max(minRooms, 1); // at least the starter
        int deficit = minTarget - roomsPlaced;
        if (deficit > 0)
        {
            int forced = ForcePlaceRooms(deficit);
            roomsPlaced += forced;
        }

        Debug.Log($"Rooms placed: {roomsPlaced}/{targetRooms} (min={minRooms})");
    }

    // --------------------------
    // Tile Placement
    // --------------------------
    private void InstantiateTile(Vector3Int position, MapElement element, bool useRoof = true)
    {
        if (!InBounds(position)) return;
        if (MapData[position.x, position.y, position.z].Element != MapElement.Empty) return;

        GameObject prefab = floorPrefab;

        if (element == MapElement.Empty)
            prefab = grassPrefab;
        
        Vector3 worldPos = MapPositionToWorld(position);
        GameObject tile = Instantiate(prefab, worldPos, Quaternion.identity);
        
        MapData[position.x, position.y, position.z].Element = element;

        if (element == MapElement.Hall) _hallCells.Add(position);
        if (element == MapElement.Room) _roomCells.Add(position);

        tile.transform.parent = _mapParent.transform;
        tile.transform.name = $"{element} ({position.x},{position.z})";

        Vector3 roofPos = new Vector3(worldPos.x, worldPos.y + wallVerticalOffset * 2, worldPos.z);
        
        //Do a roof - currently just a floor on the ceiling
        if (useRoof)
        {
            GameObject roof = Instantiate(floorPrefab, roofPos, Quaternion.identity);
            roof.transform.SetParent(tile.transform);
            roof.transform.name = $"Roof ({position.x},{position.z})";
        }


    }

    private void StampRect(int y, int x0, int x1, int z0, int z1, MapElement element)
    {
        for (int x = x0; x <= x1; x++)
        for (int z = z0; z <= z1; z++)
            InstantiateTile(new Vector3Int(x, y, z), element);
    }

    private void SeedHallStubsFromStartRoom(Vector3Int center)
    {
        // 3x3 room occupies center +/-1.
        // Stubs go 1 tile beyond that => +/-2.
        var stubs = new[]
        {
            new Vector3Int(center.x,     center.y, center.z + 2),
            new Vector3Int(center.x,     center.y, center.z - 2),
            new Vector3Int(center.x + 2, center.y, center.z),
            new Vector3Int(center.x - 2, center.y, center.z),
        };

        foreach (var p in stubs)
        {
            if (InBounds(p) && MapData[p.x, p.y, p.z].Element == MapElement.Empty)
                InstantiateTile(p, MapElement.Hall);
        }
    }

    // --------------------------
    // Trunk Selection
    // --------------------------
    private bool TryPickRandomTrunk(out Vector3Int pos, out Direction dir)
    {
        pos = default;
        dir = Direction.None;

        if (_hallCells.Count == 0)
            return false;

        for (int attempt = 0; attempt < 200; attempt++)
        {
            Vector3Int candidate = _hallCells[_rng.Next(_hallCells.Count)];

            var dirs = new List<Direction> { Direction.North, Direction.East, Direction.South, Direction.West };
            ShuffleDirections(dirs);

            foreach (var d in dirs)
            {
                Vector3Int next = candidate + DirectionToOffset(d);
                if (InBounds(next) && MapData[next.x, next.y, next.z].Element == MapElement.Empty)
                {
                    pos = candidate;
                    dir = d;
                    return true;
                }
            }
        }

        return false;
    }

    // --------------------------
    // Hall Painting
    // --------------------------
    private Direction AttemptHallStep(Vector3Int position, Direction dir, out Vector3Int newPos)
    {
        newPos = position;

        var availableDirections = new List<Direction>
        {
            Direction.North,
            Direction.East,
            Direction.South,
            Direction.West
        };

        // Stickiness: prefer continuing the same direction sometimes
        if (_rng.NextDouble() < directionalStickiness && dir != Direction.None)
        {
            if (TryPlaceHall(position, dir, out newPos))
                return dir;

            availableDirections.Remove(dir);
        }

        ShuffleDirections(availableDirections);

        while (availableDirections.Count > 0)
        {
            Direction attempted = availableDirections[0];
            availableDirections.RemoveAt(0);

            if (TryPlaceHall(position, attempted, out newPos))
                return attempted;
        }

        return Direction.None;
    }

    private bool TryPlaceHall(Vector3Int from, Direction dir, out Vector3Int newPos)
    {
        newPos = from + DirectionToOffset(dir);

        if (!InBounds(newPos)) return false;
        if (MapData[newPos.x, newPos.y, newPos.z].Element != MapElement.Empty) return false;

        // Prevent 2x2 HALL blocks (rooms are allowed to be chunky; halls stay thin)
        if (WouldCreate2x2HallBlock(newPos)) return false;

        // Keep halls tree-ish: don't connect into multiple hall neighbors
        int adjacentHalls = CountAdjacentHallsXZ(newPos);
        if (adjacentHalls > 1) return false;

        InstantiateTile(newPos, MapElement.Hall);
        return true;
    }

    private int CountAdjacentHallsXZ(Vector3Int p)
    {
        int count = 0;

        Vector3Int n = p + new Vector3Int(0, 0, 1);
        Vector3Int s = p + new Vector3Int(0, 0, -1);
        Vector3Int e = p + new Vector3Int(1, 0, 0);
        Vector3Int w = p + new Vector3Int(-1, 0, 0);

        if (InBounds(n) && MapData[n.x, n.y, n.z].Element == MapElement.Hall) count++;
        if (InBounds(s) && MapData[s.x, s.y, s.z].Element == MapElement.Hall) count++;
        if (InBounds(e) && MapData[e.x, e.y, e.z].Element == MapElement.Hall) count++;
        if (InBounds(w) && MapData[w.x, w.y, w.z].Element == MapElement.Hall) count++;

        return count;
    }

    private bool WouldCreate2x2HallBlock(Vector3Int p)
    {
        int[] dx = { 0, -1 };
        int[] dz = { 0, -1 };

        foreach (int ox in dx)
        foreach (int oz in dz)
        {
            Vector3Int bl = new Vector3Int(p.x + ox, p.y, p.z + oz);
            Vector3Int br = new Vector3Int(p.x + ox + 1, p.y, p.z + oz);
            Vector3Int tl = new Vector3Int(p.x + ox, p.y, p.z + oz + 1);
            Vector3Int tr = new Vector3Int(p.x + ox + 1, p.y, p.z + oz + 1);

            if (!InBounds(bl) || !InBounds(br) || !InBounds(tl) || !InBounds(tr))
                continue;

            bool blH = (bl == p) || MapData[bl.x, bl.y, bl.z].Element == MapElement.Hall;
            bool brH = (br == p) || MapData[br.x, br.y, br.z].Element == MapElement.Hall;
            bool tlH = (tl == p) || MapData[tl.x, tl.y, tl.z].Element == MapElement.Hall;
            bool trH = (tr == p) || MapData[tr.x, tr.y, tr.z].Element == MapElement.Hall;

            if (blH && brH && tlH && trH)
                return true;
        }

        return false;
    }

    // --------------------------
    // Room Placement (fixed)
    // --------------------------
    private bool TryStampRoomFromHall(Vector3Int hallAnchor, int y, out (int x0, int x1, int z0, int z1) rect)
    {
        rect = default;

        // Try a few directions; we need an empty "door cell" adjacent to the hall
        var dirs = new List<Direction> { Direction.North, Direction.East, Direction.South, Direction.West };
        ShuffleDirections(dirs);

        foreach (var d in dirs)
        {
            Vector3Int door = hallAnchor + DirectionToOffset(d);
            if (!InBounds(door)) continue;
            if (MapData[door.x, door.y, door.z].Element != MapElement.Empty) continue;

            int w = _rng.Next(roomWidthRange.x, roomWidthRange.y + 1);
            int h = _rng.Next(roomHeightRange.x, roomHeightRange.y + 1);

            // Build a rectangle that starts at the door and extends outward in direction d
            // Randomize lateral offset so the door isn't always centered
            int lateral = (d == Direction.North || d == Direction.South) ? w : h;
            int lateralOffset = _rng.Next(0, lateral); // 0..(lateral-1)

            int x0, x1, z0, z1;

            switch (d)
            {
                case Direction.North:
                    z0 = door.z;
                    z1 = z0 + h - 1;
                    x0 = door.x - lateralOffset;
                    x1 = x0 + w - 1;
                    break;

                case Direction.South:
                    z1 = door.z;
                    z0 = z1 - (h - 1);
                    x0 = door.x - lateralOffset;
                    x1 = x0 + w - 1;
                    break;

                case Direction.East:
                    x0 = door.x;
                    x1 = x0 + w - 1;
                    z0 = door.z - lateralOffset;
                    z1 = z0 + h - 1;
                    break;

                case Direction.West:
                    x1 = door.x;
                    x0 = x1 - (w - 1);
                    z0 = door.z - lateralOffset;
                    z1 = z0 + h - 1;
                    break;

                default:
                    continue;
            }

            if (!IsRectInBounds(y, x0, x1, z0, z1)) continue;
            if (!IsRectEmpty(y, x0, x1, z0, z1)) continue;

            // Stamp room
            for (int x = x0; x <= x1; x++)
            for (int z = z0; z <= z1; z++)
                InstantiateTile(new Vector3Int(x, y, z), MapElement.Room);

            rect = (x0, x1, z0, z1);
            return true;
        }

        return false;
    }

    private int ForcePlaceRooms(int countToPlace)
    {
        int placed = 0;

        for (int attempt = 0; attempt < roomPlacementAttempts && placed < countToPlace; attempt++)
        {
            if (_hallCells.Count == 0) break;

            Vector3Int anchor = _hallCells[_rng.Next(_hallCells.Count)];

            if (TryStampRoomFromHall(anchor, anchor.y, out _))
                placed++;
        }

        if (placed < countToPlace)
            Debug.LogWarning($"ForcePlaceRooms: Only placed {placed}/{countToPlace} rooms. Consider more paint, more attempts, or looser room size constraints.");

        return placed;
    }

    private bool IsRectInBounds(int y, int x0, int x1, int z0, int z1)
    {
        return x0 >= 0 && x1 < mapWidth && z0 >= 0 && z1 < mapHeight && y >= 0 && y < mapDepth;
    }

    private bool IsRectEmpty(int y, int x0, int x1, int z0, int z1)
    {
        for (int x = x0; x <= x1; x++)
        for (int z = z0; z <= z1; z++)
        {
            if (MapData[x, y, z].Element != MapElement.Empty)
                return false;
        }

        return true;
    }

    // --------------------------
    // Utility
    // --------------------------
    private Vector3 MapPositionToWorld(Vector3Int mapPosition)
    {
        return new Vector3(
            mapPosition.x * tileSize,
            mapPosition.y * tileSize,
            mapPosition.z * tileSize
        );
    }

    private bool InBounds(Vector3Int p)
    {
        return p.x >= 0 && p.x < mapWidth
            && p.y >= 0 && p.y < mapDepth
            && p.z >= 0 && p.z < mapHeight;
    }

    private static Vector3Int DirectionToOffset(Direction dir)
    {
        switch (dir)
        {
            case Direction.North: return new Vector3Int(0, 0, 1);
            case Direction.South: return new Vector3Int(0, 0, -1);
            case Direction.East:  return new Vector3Int(1, 0, 0);
            case Direction.West:  return new Vector3Int(-1, 0, 0);
            default:              return Vector3Int.zero;
        }
    }

    private void ShuffleDirections(List<Direction> directions)
    {
        for (int i = directions.Count - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1);
            (directions[i], directions[j]) = (directions[j], directions[i]);
        }
    }
    
    //----------------
    //Wall Painting
    //---------------
    private bool IsFloorLike(MapElement e) => e == MapElement.Hall || e == MapElement.Room;

private void BuildWallsFromMap(int y)
{
    if (_wallsParent == null)
        _wallsParent = new GameObject("Walls Container");

    _spawnedWallEdges.Clear();

    for (int x = 0; x < mapWidth; x++)
    for (int z = 0; z < mapHeight; z++)
    {
        var p = new Vector3Int(x, y, z);
        if (!InBounds(p)) continue;

        MapElement here = MapData[x, y, z].Element;
        if (!IsFloorLike(here))
        {
            InstantiateTile(p, MapElement.Empty, false);
            continue;
        }

        // North edge
        TryPlaceWallOnEdge(p, Direction.North);

        // South edge
        TryPlaceWallOnEdge(p, Direction.South);

        // East edge
        TryPlaceWallOnEdge(p, Direction.East);

        // West edge
        TryPlaceWallOnEdge(p, Direction.West);
    }
}

private void TryPlaceWallOnEdge(Vector3Int floorCell, Direction edgeDir)
{
    Vector3Int neighbor = floorCell + DirectionToOffset(edgeDir);

    bool neighborIsFloor =
        InBounds(neighbor) &&
        IsFloorLike(MapData[neighbor.x, neighbor.y, neighbor.z].Element);

    // If neighbor is floor-like, no wall between them
    if (neighborIsFloor)
        return;

    // Dedupe key per *edge* so we never place the same edge twice
    // Canonicalize by ordering the two cells (even if neighbor is out of bounds, it’s still fine)
    string edgeKey = MakeEdgeKey(floorCell, neighbor);

    if (_spawnedWallEdges.Contains(edgeKey))
        return;

    _spawnedWallEdges.Add(edgeKey);

    SpawnWallForEdge(floorCell, edgeDir);
}

private string MakeEdgeKey(Vector3Int a, Vector3Int b)
{
    // Order-independent: smaller comes first
    // Works fine even when b is out of bounds, since it still has coordinates.
    if (CompareVec3Int(a, b) <= 0)
        return $"{a.x},{a.y},{a.z}|{b.x},{b.y},{b.z}";
    return $"{b.x},{b.y},{b.z}|{a.x},{a.y},{a.z}";
}

private int CompareVec3Int(Vector3Int a, Vector3Int b)
{
    if (a.x != b.x) return a.x.CompareTo(b.x);
    if (a.y != b.y) return a.y.CompareTo(b.y);
    return a.z.CompareTo(b.z);
}

private void SpawnWallForEdge(Vector3Int floorCell, Direction edgeDir)
{
    Vector3 floorCenter = MapPositionToWorld(floorCell);
    float half = tileSize * 0.5f;

    if (wallPrefab == null) return;

    GameObject prefab = wallPrefab;

    bool variantWall = _rng.NextDouble() < variantWallChance;

    if (variantWall && wallVariants.Length > 0)
    {
        prefab = wallVariants[_rng.Next(wallVariants.Length)];
    }
    
    Vector3 spawnPos = floorCenter;
    Quaternion rot = Quaternion.identity;

    // Assumption:
    // - wallPrefab's "default" orientation runs along X (left-right).
    // - Therefore North/South edges need no rotation.
    // - East/West edges need a 90° yaw.
    switch (edgeDir)
    {
        case Direction.North:
            spawnPos += new Vector3(0f, wallVerticalOffset,  half);
            rot = Quaternion.Euler(0f, 90f, 0f);
            break;

        case Direction.South:
            spawnPos += new Vector3(0f, wallVerticalOffset, -half);
            rot = Quaternion.Euler(0f, 90f, 0f);
            break;

        case Direction.East:
            spawnPos += new Vector3( half, wallVerticalOffset, 0f);
            rot = Quaternion.Euler(0f, 0f, 0f);
            break;

        case Direction.West:
            spawnPos += new Vector3(-half, wallVerticalOffset, 0f);
            rot = Quaternion.Euler(0f, 0f, 0f);
            break;

        default:
            return;
    }

    GameObject wall = Instantiate(prefab, spawnPos, rot);
    wall.transform.parent = _wallsParent.transform;
    wall.transform.name = $"Wall_{edgeDir}_({floorCell.x},{floorCell.z})";
}


}
