using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public class Controller_LevelGeneration : MonoBehaviour
{
    [Header("Map Prefabs")]
    [SerializeField] private GameObject floorPrefab;
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject ceilingPrefab;
    [SerializeField] private GameObject grassPrefab;
    [SerializeField] private GameObject[] randomObjects;
    [SerializeField] private GameObject[] trapPrefabs;
    
    [SerializeField] private float randomObjectSpawnChance = 0.2f;
    [SerializeField] private float trapInHallChance = 0.1f;
    [SerializeField] private float randomObjectTrapChance = 0.25f;

    [SerializeField] private GameObject playerPrefab;

    [SerializeField] private GameObject worldLightPrefab;
    [SerializeField] private GameObject[] wallVariants;
    [SerializeField] private GameObject[] ceilingVariants;
    [SerializeField] private GameObject[] floorVariants;
    
    [Header("Doors")]
    [SerializeField] private GameObject[] doorPrefabs;
    [SerializeField] private float doorChance = 1.0f;
    
    [Header("Notes")]
    [SerializeField] private GameObject notePrefab;
    [SerializeField] private float noteY = 0.5f;
    
    [SerializeField] private int noteCornerSampleRadius = 1;
    
    [SerializeField] private int noteCornerInset = 2;
    [SerializeField] private int noteAttemptsPerCornerFallback = 250;
    
    [SerializeField] private int noteMinDistanceTilesFromPlayer = 4;
    [SerializeField] private int noteGlobalFallbackAttempts = 1000;


    
    [Header("Enemy Spawning")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private int minEnemyDistanceTiles = 12;
    [SerializeField] private int enemySpawnAttempts = 300;
    [SerializeField] private float enemyY = 0.5f;

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
    [SerializeField] private float variantFloorChance = 0.3f;
    
    
    [Header("Staged Room Sets")]
    [SerializeField] private StagedFloorPiece[] stagedFloorPieces;
    [SerializeField] private int stagedPiecesTarget = 3;    
    [SerializeField] private int stagedPlacementAttempts = 600; 
    [SerializeField] private float stagedPlacementChance = 0.6f; 

    [Serializable]
    public class StagedFloorPiece
    {
        public GameObject prefab;

        [Min(1)] public int width = 1;
        [Min(1)] public int height = 1;  

        [Range(0f, 1f)] public float weight = 1f;

        public bool allowRotate90 = true;

        [Tooltip("How much to inset from tile edges when positioning (helps avoid clipping walls).")]
        public float inset = 0.6f;

        [Tooltip("If true, every tile in the footprint must NOT be adjacent to any hall (avoids blocking doorways).")]
        public bool requireNotAdjacentToHall = true;
    }
    

    [Header("Debug")]
    [SerializeField] private bool verboseLogs = false;

    public int MapHeight { get => mapHeight;}
    public int MapWidth { get => mapWidth;}
    
    public enum MapElement
    {
        Empty,
        Hall,
        Grass,
        Room,
        RoomWithObject
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
    
    private readonly List<Vector3Int> _hallCells = new List<Vector3Int>();
    private readonly List<Vector3Int> _roomCells = new List<Vector3Int>();
    
    private GameObject _wallsParent;
    private readonly HashSet<string> _spawnedWallEdges = new HashSet<string>();

    private GameObject _mapParent;
    private readonly Random _rng = new Random();
    private int _lightCounter = 0;
    private GameObject _doorsParent;
    private readonly HashSet<string> _spawnedDoorEdges = new HashSet<string>();

    // --------------------------
    // Unity Entry
    // --------------------------
    private void Start()
    {
        _mapParent = new GameObject("Map Container");
        MapData = new MapPosition[mapWidth, mapDepth, mapHeight];

        InitializeMapData();

        Vector3Int center = new Vector3Int(mapWidth / 2, 0, mapHeight / 2);
        
        StampRect(center.y, center.x - 1, center.x + 1, center.z - 1, center.z + 1, MapElement.Room);
        
        SeedHallStubsFromStartRoom(center);

        RunPaint(paintBudget);
        PlaceStagedRoomSets();
        BuildWallsFromMap(0);
        
        Vector3 worldPosCenter = MapPositionToWorld(center);

        Instantiate(playerPrefab, new Vector3(worldPosCenter.x, .5f, worldPosCenter.z), Quaternion.identity);
        Instantiate(worldLightPrefab, new Vector3(worldPosCenter.x, worldPosCenter.y+wallVerticalOffset+.5f, worldPosCenter.z), Quaternion.identity);
        SpawnEnemyFarFrom(center);
        SpawnNotesInCorners(center);
        
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
        
        int targetRooms = _rng.Next(minRooms, maxRooms + 1);
        int roomsPlaced = _roomCells.Count > 0 ? 1 : 0;

        if (!TryPickRandomTrunk(out Vector3Int pos, out Direction dir))
            return;

        while (budget > 0 && jumps < maxJumps)
        {
            if (roomsPlaced < targetRooms && _rng.NextDouble() < roomAttemptChancePerPaintStep)
            {
                if (MapData[pos.x, pos.y, pos.z].Element == MapElement.Hall)
                {
                    if (TryStampRoomFromHall(pos, pos.y, out _))
                    {
                        roomsPlaced++;

                        if (TryPickRandomTrunk(out pos, out dir))
                        {
                            jumps++;
                            continue;
                        }
                    }
                }
            }

            if (_rng.NextDouble() < jumpChancePerStep)
            {
                if (TryPickRandomTrunk(out pos, out dir))
                {
                    jumps++;
                    continue;
                }
                break;
            }

            Direction nextDir = AttemptHallStep(pos, dir, out Vector3Int nextPos);

            if (nextDir == Direction.None)
            {
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
        
        int minTarget = Mathf.Max(minRooms, 1); 
        int deficit = minTarget - roomsPlaced;
        if (deficit > 0)
        {
            int forced = ForcePlaceRooms(deficit);
            roomsPlaced += forced;
        }

        Debug.Log($"Rooms placed: {roomsPlaced}/{targetRooms} (min={minRooms})");
    }
    
    private void PlaceStagedRoomSets()
    {
        if (stagedFloorPieces == null || stagedFloorPieces.Length == 0) return;
        if (_roomCells.Count == 0) return;

        int placed = 0;

        for (int attempt = 0; attempt < stagedPlacementAttempts && placed < stagedPiecesTarget; attempt++)
        {
            if (_rng.NextDouble() > stagedPlacementChance) continue;

            if (!TryPickWeightedStagedPiece(out StagedFloorPiece piece)) continue;

            Vector3Int anchor = _roomCells[_rng.Next(_roomCells.Count)];
            
            int rotSteps = piece.allowRotate90 ? _rng.Next(0, 4) : (_rng.Next(0, 2) * 2);
            int w = piece.width;
            int h = piece.height;

            if (rotSteps % 2 == 1)
            {
                int tmp = w;
                w = h;
                h = tmp;
            }

            if (!IsRoomFootprintValid(anchor, w, h, piece.requireNotAdjacentToHall))
                continue;

            SpawnStagedPiece(piece, anchor, rotSteps);
            
            MarkFootprint(anchor, w, h, MapElement.RoomWithObject);

            placed++;
        }

        if (placed < stagedPiecesTarget)
        {
            Debug.Log($"Placed {placed}/{stagedPiecesTarget} staged sets. (More attempts or smaller pieces increases success rate.)");
        }
    }

    private bool TryPickWeightedStagedPiece(out StagedFloorPiece piece)
    {
        piece = null;

        float total = 0f;
        for (int i = 0; i < stagedFloorPieces.Length; i++)
        {
            var p = stagedFloorPieces[i];
            if (p == null || p.prefab == null) continue;
            if (p.weight <= 0f) continue;
            total += p.weight;
        }
        if (total <= 0f) return false;

        float r = (float)_rng.NextDouble() * total;
        float acc = 0f;

        for (int i = 0; i < stagedFloorPieces.Length; i++)
        {
            var p = stagedFloorPieces[i];
            if (p == null || p.prefab == null) continue;
            if (p.weight <= 0f) continue;

            acc += p.weight;
            if (r <= acc)
            {
                piece = p;
                return true;
            }
        }

        return false;
    }
    
    private bool IsRoomFootprintValid(Vector3Int anchor, int w, int h, bool requireNotAdjacentToHall)
    {
        for (int dx = 0; dx < w; dx++)
        for (int dz = 0; dz < h; dz++)
        {
            var c = new Vector3Int(anchor.x + dx, anchor.y, anchor.z + dz);
            if (!InBounds(c)) return false;

            var e = MapData[c.x, c.y, c.z].Element;
            if (e != MapElement.Room) return false;

            if (requireNotAdjacentToHall && IsAdjacentToHall(c))
                return false;
        }

        return true;
    }

    private bool IsAdjacentToHall(Vector3Int cell)
    {
        foreach (var n in GetNeighbors4(cell))
        {
            if (InBounds(n) && MapData[n.x, n.y, n.z].Element == MapElement.Hall)
                return true;
        }
        return false;
    }

    private void MarkFootprint(Vector3Int anchor, int w, int h, MapElement markAs)
    {
        for (int dx = 0; dx < w; dx++)
        for (int dz = 0; dz < h; dz++)
        {
            var c = new Vector3Int(anchor.x + dx, anchor.y, anchor.z + dz);
            if (!InBounds(c)) continue;
            MapData[c.x, c.y, c.z].Element = markAs;
        }
    }


    private void SpawnStagedPiece(StagedFloorPiece piece, Vector3Int anchor, int rotSteps)
    {
        Vector3 anchorWorld = MapPositionToWorld(anchor);
        
        Vector3 spawnPos = OffsetWorldPositionWithinTile(anchorWorld, piece.inset);

        Quaternion rot = Quaternion.Euler(0f, rotSteps * 90f, 0f);

        GameObject obj = Instantiate(piece.prefab, spawnPos, rot);
        obj.transform.SetParent(_mapParent.transform);
        obj.name = $"StagedSet_{piece.prefab.name}_({anchor.x},{anchor.z})_rot{rotSteps * 90}";
    }


    // --------------------------
    // Tile Placement
    // --------------------------
    private void InstantiateTile(Vector3Int position, MapElement element, bool useRoof = true)
    {
        if (!InBounds(position)) return;
        if (MapData[position.x, position.y, position.z].Element != MapElement.Empty) return;

        GameObject prefab = floorPrefab;

        if (element == MapElement.Grass)
            prefab = grassPrefab;
        
        if (element == MapElement.Hall || element == MapElement.Room)
        {
            if (_rng.NextDouble() < variantFloorChance && floorVariants.Length > 0)
            {
                prefab = floorVariants[_rng.Next(floorVariants.Length)];
            }
        }

        Vector3 worldPos = MapPositionToWorld(position);
        GameObject tile = Instantiate(prefab, worldPos, Quaternion.identity);

        MapData[position.x, position.y, position.z].Element = element;

        if (element == MapElement.Hall) _hallCells.Add(position);
        if (element == MapElement.Room) _roomCells.Add(position);

        tile.transform.parent = _mapParent.transform;
        tile.transform.name = $"{element} ({position.x},{position.z})";

        if (useRoof && element != MapElement.Grass)
        {
            Vector3 roofPos = new Vector3(worldPos.x, worldPos.y + wallVerticalOffset * 2, worldPos.z);
            GameObject ceiling = ceilingPrefab;
            if(_lightCounter++ % 5 == 0 && ceilingVariants.Length > 0)
            {
                ceiling = ceilingVariants[_rng.Next(ceilingVariants.Length)];
            }
            GameObject roof = Instantiate(ceiling, roofPos, Quaternion.identity);
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

        if (WouldCreate2x2HallBlock(newPos)) return false;

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
        
        var dirs = new List<Direction> { Direction.North, Direction.East, Direction.South, Direction.West };
        ShuffleDirections(dirs);

        foreach (var d in dirs)
        {
            Vector3Int door = hallAnchor + DirectionToOffset(d);
            if (!InBounds(door)) continue;
            if (MapData[door.x, door.y, door.z].Element != MapElement.Empty) continue;

            int w = _rng.Next(roomWidthRange.x, roomWidthRange.y + 1);
            int h = _rng.Next(roomHeightRange.x, roomHeightRange.y + 1);
            
            int lateral = (d == Direction.North || d == Direction.South) ? w : h;
            int lateralOffset = _rng.Next(0, lateral); 

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

    public bool InBounds(Vector3Int p)
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
    private bool IsFloorLike(MapElement e) =>
        e == MapElement.Hall || e == MapElement.Room || e == MapElement.RoomWithObject;

    private bool IsInRoom(MapElement e) =>
        e == MapElement.Room || e == MapElement.RoomWithObject;
    private void BuildWallsFromMap(int y)
    {
        if (_wallsParent == null)
            _wallsParent = new GameObject("Walls Container");
        
        if (_doorsParent == null)
            _doorsParent = new GameObject("Doors Container");
        
        _spawnedDoorEdges.Clear();


        _spawnedWallEdges.Clear();

        for (int x = 0; x < mapWidth; x++)
        for (int z = 0; z < mapHeight; z++)
        {
            var p = new Vector3Int(x, y, z);
            if (!InBounds(p)) continue;

            MapElement here = MapData[x, y, z].Element;
            if (!IsFloorLike(here))
            {
                InstantiateTile(p, MapElement.Grass, false);
                continue;
            }

            if (here == MapElement.Room)
            {
                bool adjacentToHall = false;
                foreach (var n in GetNeighbors4(p))
                {
                    if (InBounds(n) && MapData[n.x, n.y, n.z].Element == MapElement.Hall)
                    {
                        adjacentToHall = true;
                        break;
                    }
                }

                if (!adjacentToHall)
                {
                    if (_rng.NextDouble() < randomObjectSpawnChance)
                    {
                        if (trapPrefabs.Length > 0 && _rng.NextDouble() < randomObjectTrapChance)
                        {
                            GenerateTrapAt(p, p, here);
                        }
                        else if (randomObjects.Length > 0)
                        {
                            GameObject objPrefab = randomObjects[_rng.Next(randomObjects.Length)];
                            Vector3 worldPos = MapPositionToWorld(p);
                            
                            Quaternion rot = Quaternion.Euler(0f, (float)_rng.NextDouble() * 360f, 0f);
                            
                            GameObject obj = Instantiate(objPrefab, OffsetWorldPositionWithinTile(worldPos), rot);
                            
                            obj.transform.parent = _mapParent.transform;
                            obj.transform.name = $"RoomObject ({p.x},{p.z})";
                            MapData[x, y, z].Element = MapElement.RoomWithObject;
                        }
                    }
                }
            }

            
            else if (here == MapElement.Hall)
            {
                if (trapPrefabs.Length > 0 && _rng.NextDouble() < trapInHallChance)
                {
                    GenerateTrapAt(p, p, here);
                }
            }
       

            TryPlaceDoorOnBoundary(p, Direction.North, y);
            TryPlaceDoorOnBoundary(p, Direction.South, y);
            TryPlaceDoorOnBoundary(p, Direction.East,  y);
            TryPlaceDoorOnBoundary(p, Direction.West,  y);

            TryPlaceWallOnEdge(p, Direction.North);
            TryPlaceWallOnEdge(p, Direction.South);
            TryPlaceWallOnEdge(p, Direction.East);
            TryPlaceWallOnEdge(p, Direction.West);

        }
    }

    private Vector3 OffsetWorldPositionWithinTile(Vector3 baseWorldPos, float inset = 1f)
    {
        float radius = (tileSize * 0.5f) - inset;
        float offsetX = (float)(_rng.NextDouble() * 2.0 - 1.0) * radius;
        float offsetZ = (float)(_rng.NextDouble() * 2.0 - 1.0) * radius;

        return new Vector3(baseWorldPos.x + offsetX, baseWorldPos.y, baseWorldPos.z + offsetZ);
    }


    private void GenerateTrapAt(Vector3Int p, Vector3Int position, MapElement element = MapElement.Room)
    {
        GameObject trapPrefab = trapPrefabs[_rng.Next(trapPrefabs.Length)];
        Vector3 worldPosTrap = MapPositionToWorld(p);
        GameObject trapObj = Instantiate(trapPrefab, OffsetWorldPositionWithinTile(worldPosTrap), Quaternion.identity);
        trapObj.transform.parent = _mapParent.transform;
        trapObj.transform.name = $"RoomTrap ({p.x},{p.z})";


        MapElement here = MapElement.Empty;

        if (element == MapElement.Room)
        {
            here = MapElement.RoomWithObject;
        }else if (element == MapElement.Hall)
        {
            here = MapElement.Hall; 
        }
        
        MapData[position.x, position.y, position.z].Element = here;
    }

    private bool IsHall(MapElement e) => e == MapElement.Hall;

    private bool IsRoomLike(MapElement e) =>
        e == MapElement.Room || e == MapElement.RoomWithObject;

    private void TryPlaceDoorOnBoundary(Vector3Int floorCell, Direction edgeDir, int y)
    {
        if (doorPrefabs == null || doorPrefabs.Length == 0) return;
        if (_rng.NextDouble() > doorChance) return;

        Vector3Int neighbor = floorCell + DirectionToOffset(edgeDir);
        if (!InBounds(neighbor)) return;

        MapElement a = MapData[floorCell.x, y, floorCell.z].Element;
        MapElement b = MapData[neighbor.x, y, neighbor.z].Element;

        bool boundary = (IsHall(a) && IsRoomLike(b)) || (IsRoomLike(a) && IsHall(b));
        if (!boundary) return;

        string edgeKey = MakeEdgeKey(floorCell, neighbor);
        if (_spawnedDoorEdges.Contains(edgeKey)) return;

        _spawnedDoorEdges.Add(edgeKey);
        SpawnDoorForEdge(floorCell, edgeDir);
    }

    private void SpawnDoorForEdge(Vector3Int floorCell, Direction edgeDir)
    {
        if (_doorsParent == null)
            _doorsParent = new GameObject("Doors Container");

        GameObject prefab = doorPrefabs[_rng.Next(doorPrefabs.Length)];
        if (prefab == null) return;

        Vector3 floorCenter = MapPositionToWorld(floorCell);
        float half = tileSize * 0.5f;

        Vector3 spawnPos = floorCenter;
        Quaternion rot = Quaternion.identity;

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

        GameObject door = Instantiate(prefab, spawnPos, rot);
        door.transform.parent = _doorsParent.transform;
        door.transform.name = $"Door_{edgeDir}_({floorCell.x},{floorCell.z})";
    }


    private void TryPlaceWallOnEdge(Vector3Int floorCell, Direction edgeDir)
    {
        Vector3Int neighbor = floorCell + DirectionToOffset(edgeDir);

        bool neighborIsFloor =
            InBounds(neighbor) &&
            IsFloorLike(MapData[neighbor.x, neighbor.y, neighbor.z].Element);
        
        if (neighborIsFloor)
            return;
        
        string edgeKey = MakeEdgeKey(floorCell, neighbor);

        if (_spawnedWallEdges.Contains(edgeKey))
            return;

        _spawnedWallEdges.Add(edgeKey);

        SpawnWallForEdge(floorCell, edgeDir);
    }

    private string MakeEdgeKey(Vector3Int a, Vector3Int b)
    {
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

    public bool IsWalkableCell(Vector3Int cell)
    {
        if (!InBounds(cell)) return false;
        var e = MapData[cell.x, cell.y, cell.z].Element;
        return e == MapElement.Hall || e == MapElement.Room;
    }

    public Vector3 CellCenterWorld(Vector3Int cell) => MapPositionToWorld(cell);

    public Vector3Int WorldToCell(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x / tileSize);
        int y = 0;
        int z = Mathf.RoundToInt(worldPos.z / tileSize);
        return new Vector3Int(x, y, z);
    }

    public IEnumerable<Vector3Int> GetNeighbors4(Vector3Int c)
    {
        yield return c + new Vector3Int(0, 0, 1);
        yield return c + new Vector3Int(1, 0, 0);
        yield return c + new Vector3Int(0, 0, -1);
        yield return c + new Vector3Int(-1, 0, 0);
    }
    
    
    private GameObject _notesParent;

    private enum Corner
    {
        SW,
        NW,
        SE,
        NE
    }

    private void SpawnNotesInCorners(Vector3Int playerCell)
    {
        if (notePrefab == null)
        {
            Debug.LogWarning("Note prefab not assigned.");
            return;
        }

        if (_notesParent == null)
            _notesParent = new GameObject("Notes Container");

        int minDist2 = noteMinDistanceTilesFromPlayer * noteMinDistanceTilesFromPlayer;

        var usedCells = new HashSet<Vector3Int>();

        var corners = new[] { Corner.SW, Corner.NW, Corner.SE, Corner.NE };
        var failedCorners = new List<Corner>();
        
        foreach (var corner in corners)
        {
            if (TryPickCornerNoteCell(corner, playerCell, minDist2, usedCells, out Vector3Int chosen))
            {
                usedCells.Add(chosen);
                SpawnNoteAtCell(chosen, corner);
            }
            else
            {
                failedCorners.Add(corner);
            }
        }
        
        foreach (var corner in failedCorners)
        {
            if (TryPickAnyFarWalkableCell(playerCell, minDist2, usedCells, out Vector3Int chosen))
            {
                usedCells.Add(chosen);
                SpawnNoteAtCell(chosen, corner);
            }
            else
            {
                Debug.LogWarning($"Could not place note for {corner} even with global fallback. Consider lowering noteMinDistanceTilesFromPlayer or increasing noteGlobalFallbackAttempts.");
            }
        }
    }

    private void SpawnNoteAtCell(Vector3Int cell, Corner cornerTag)
    {
        Vector3 world = MapPositionToWorld(cell);
        GameObject note = Instantiate(notePrefab, new Vector3(world.x, noteY, world.z), Quaternion.identity);
        note.transform.SetParent(_notesParent.transform);
        note.transform.rotation = Quaternion.Euler(270f, _rng.Next(0, 360), 0f);
        note.name = $"Note_{cornerTag}_({cell.x},{cell.z})";
    }

    private bool TryPickAnyFarWalkableCell(
        Vector3Int playerCell,
        int minDist2,
        HashSet<Vector3Int> used,
        out Vector3Int chosen)
    {
        chosen = default;

        int attempts = Mathf.Max(50, noteGlobalFallbackAttempts);
        
        for (int i = 0; i < attempts; i++)
        {
            int x = _rng.Next(0, mapWidth);
            int z = _rng.Next(0, mapHeight);
            var c = new Vector3Int(x, 0, z);

            if (!IsWalkableCell(c)) continue;
            if (used.Contains(c)) continue;

            int dx = c.x - playerCell.x;
            int dz = c.z - playerCell.z;
            int dist2 = dx * dx + dz * dz;

            if (dist2 < minDist2) continue;

            chosen = c;
            return true;
        }
        
        bool found = false;
        int bestDist2 = -1;
        Vector3Int bestCell = default;

        void ConsiderList(List<Vector3Int> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var c = list[i];
                if (!IsWalkableCell(c)) continue;
                if (used.Contains(c)) continue;

                int dx = c.x - playerCell.x;
                int dz = c.z - playerCell.z;
                int dist2 = dx * dx + dz * dz;

                if (dist2 < minDist2) continue;

                if (!found || dist2 > bestDist2)
                {
                    found = true;
                    bestDist2 = dist2;
                    bestCell = c;
                }
            }
        }

        ConsiderList(_roomCells);
        ConsiderList(_hallCells);

        if (found)
            chosen = bestCell;

        return found;
    }



    private bool TryPickCornerNoteCell(
        Corner corner,
        Vector3Int playerCell,
        int minDist2,
        HashSet<Vector3Int> used,
        out Vector3Int chosen)
    {
        chosen = default;

        Vector3Int anchor = GetCornerAnchorCell(corner);

        Vector3Int best = default;
        int bestScore = int.MinValue;
        bool found = false;

        for (int dx = -noteCornerSampleRadius; dx <= noteCornerSampleRadius; dx++)
        for (int dz = -noteCornerSampleRadius; dz <= noteCornerSampleRadius; dz++)
        {
            var c = new Vector3Int(anchor.x + dx, 0, anchor.z + dz);
            if (!InBounds(c)) continue;
            if (used.Contains(c)) continue;
            if (!IsWalkableCell(c)) continue;

            int ddx = c.x - playerCell.x;
            int ddz = c.z - playerCell.z;
            int dist2 = ddx * ddx + ddz * ddz;

            if (dist2 < minDist2) continue;
            
            int score = dist2 + _rng.Next(0, 25);

            if (!found || score > bestScore)
            {
                found = true;
                bestScore = score;
                best = c;
            }
        }

        if (found)
        {
            chosen = best;
            return true;
        }
        
        GetQuadrantBounds(corner, out int xMin, out int xMax, out int zMin, out int zMax);

        found = false;
        bestScore = int.MinValue;

        for (int attempt = 0; attempt < noteAttemptsPerCornerFallback; attempt++)
        {
            int x = _rng.Next(xMin, xMax + 1);
            int z = _rng.Next(zMin, zMax + 1);
            var c = new Vector3Int(x, 0, z);

            if (!IsWalkableCell(c)) continue;
            if (used.Contains(c)) continue;

            int ddx = c.x - playerCell.x;
            int ddz = c.z - playerCell.z;
            int dist2 = ddx * ddx + ddz * ddz;

            if (dist2 < minDist2) continue;

            int score = dist2 + _rng.Next(0, 100);

            if (!found || score > bestScore)
            {
                found = true;
                bestScore = score;
                best = c;
            }
        }

        if (found)
        {
            chosen = best;
            return true;
        }

        return false;
    }

    private Vector3Int GetCornerAnchorCell(Corner corner)
    {
        int x0 = noteCornerInset;
        int x1 = mapWidth - 1 - noteCornerInset;
        int z0 = noteCornerInset;
        int z1 = mapHeight - 1 - noteCornerInset;
        
        x0 = Mathf.Clamp(x0, 0, mapWidth - 1);
        x1 = Mathf.Clamp(x1, 0, mapWidth - 1);
        z0 = Mathf.Clamp(z0, 0, mapHeight - 1);
        z1 = Mathf.Clamp(z1, 0, mapHeight - 1);

        return corner switch
        {
            Corner.SW => new Vector3Int(x0, 0, z0),
            Corner.NW => new Vector3Int(x0, 0, z1),
            Corner.SE => new Vector3Int(x1, 0, z0),
            Corner.NE => new Vector3Int(x1, 0, z1),
            _ => new Vector3Int(x0, 0, z0),
        };
    }

    private void GetQuadrantBounds(Corner corner, out int xMin, out int xMax, out int zMin, out int zMax)
    {
        int midX = mapWidth / 2;
        int midZ = mapHeight / 2;
        
        switch (corner)
        {
            case Corner.SW:
                xMin = 0; xMax = Mathf.Max(0, midX - 1);
                zMin = 0; zMax = Mathf.Max(0, midZ - 1);
                break;

            case Corner.NW:
                xMin = 0; xMax = Mathf.Max(0, midX - 1);
                zMin = midZ; zMax = mapHeight - 1;
                break;

            case Corner.SE:
                xMin = midX; xMax = mapWidth - 1;
                zMin = 0; zMax = Mathf.Max(0, midZ - 1);
                break;

            case Corner.NE:
            default:
                xMin = midX; xMax = mapWidth - 1;
                zMin = midZ; zMax = mapHeight - 1;
                break;
        }
    }


    private void SpawnEnemyFarFrom(Vector3Int playerCell)
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning("Enemy prefab not assigned.");
            return;
        }

        int minDist2 = minEnemyDistanceTiles * minEnemyDistanceTiles;

        bool found = false;
        Vector3Int chosen = default;
        
        for (int i = 0; i < enemySpawnAttempts; i++)
        {
            bool pickRoom = _roomCells.Count > 0 && _rng.NextDouble() < 0.7;
            var list = pickRoom ? _roomCells : _hallCells;
            if (list.Count == 0) continue;

            Vector3Int cell = list[_rng.Next(list.Count)];
            
            if (!IsWalkableCell(cell)) continue;

            int dx = cell.x - playerCell.x;
            int dz = cell.z - playerCell.z;
            int dist2 = dx * dx + dz * dz;

            if (dist2 < minDist2) continue;

            chosen = cell;
            found = true;
            break;
    }
        
    if (!found)
    {
        int bestDist2 = -1;

        void ConsiderList(List<Vector3Int> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var cell = list[i];
                if (!IsWalkableCell(cell)) continue;

                int dx = cell.x - playerCell.x;
                int dz = cell.z - playerCell.z;
                int dist2 = dx * dx + dz * dz;

                if (dist2 > bestDist2)
                {
                    bestDist2 = dist2;
                    chosen = cell;
                }
            }
        }

        ConsiderList(_roomCells);
        ConsiderList(_hallCells);

        found = bestDist2 >= 0;
    }

    if (!found)
    {
        Debug.LogWarning("Could not find a valid enemy spawn cell.");
        return;
    }

    Vector3 world = MapPositionToWorld(chosen);
    Instantiate(enemyPrefab, new Vector3(world.x, enemyY, world.z), Quaternion.identity);
    Debug.Log($"Spawned enemy at cell {chosen} (minDistTiles={minEnemyDistanceTiles})");
    }

    public bool TryGetRandomWalkableCell(
        out Vector3Int cell,
        int attempts = 200,
        Vector3Int? avoid = null,
        int minManhattanDist = 0)
    {
        for (int i = 0; i < attempts; i++)
        {
            int x = _rng.Next(0, mapWidth);
            int z = _rng.Next(0, mapHeight);
            var c = new Vector3Int(x, 0, z);

            if (!IsWalkableCell(c)) continue;

            if (avoid.HasValue && c == avoid.Value) continue;

            if (minManhattanDist > 0 && avoid.HasValue)
            {
                int dx = Mathf.Abs(c.x - avoid.Value.x);
                int dz = Mathf.Abs(c.z - avoid.Value.z);
                if (dx + dz < minManhattanDist) continue;
            }

            cell = c;
            return true;
        }

        cell = default;
        return false;
    }




}
