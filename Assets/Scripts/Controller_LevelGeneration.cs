using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public class Controller_LevelGeneration : MonoBehaviour
{
    
    //Prefabs for Level Generation
    [Header ("Map Prefabs")]
    
    [SerializeField] private GameObject floorPrefab;
    
    [SerializeField] private GameObject horizontalWallPrefab;

    [SerializeField] private GameObject verticalWallPrefab;

    [SerializeField] private int tileSize = 5;
    
    [SerializeField] private float wallHorizontalOffset = 2.25f;
    [SerializeField] private float wallVerticalOffset = 1.5f;
    
    private GameObject _mapParent;
    
    [SerializeField] private int mapWidth = 30;
    [SerializeField] private int mapHeight = 30;
    [SerializeField] private int mapDepth = 3;

    [Header("Randomness Sliders")] [Range(0, 1)] [SerializeField]
    private Double directionalStickiness = .7;
    
    [Serializable]
    public enum MapElement
    {
        Empty,
        Floor
    }

    enum Direction
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

    [Serializable]
    public struct CollisionMap
    {
        public bool PositionBlocked;
        public bool NorthBlocked;
        public bool SouthBlocked;
        public bool EastBlocked;
        public bool WestBlocked;
        public bool AboveBlocked;
        public bool BelowBlocked;
    }
    private readonly Random _rng = new Random();
    
    public MapPosition[,,] MapData;
    
    
    

    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _mapParent = new GameObject("Map Container");
        
        MapData = new MapPosition[mapWidth, mapDepth, mapHeight];


    
        
        // Initialize Map Data
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapDepth; y++)
            {
                for (int z = 0; z < mapHeight; z++)
                {
                    Debug.Log("Position: " + x + "," + y + ","  +z);
             
                    MapData[x, y, z] = new MapPosition
                    {
                        Position = new Vector3Int(x, y, z),
                        Element = MapElement.Empty
                    };
                }
            }
        }
        
        Vector3Int startPos = new Vector3Int((int)mapWidth / 2, 0, (int)mapHeight / 2);
        
        // Generate Floor tiles in a 3x3 Grid centered at (0,0,0)
        //GenerateFloorRange(0, startPos.x - 1, startPos.x +1, startPos.z-1, startPos.z+1);
        
        Vector3Int pos = startPos;
        Direction dir = Direction.North;

        for (int i = 0; i < 50; i++)
        {
            Direction nextDir = AttemptStep(pos, dir, out Vector3Int nextPos);
            if (nextDir == Direction.None) break;

            pos = nextPos;
            dir = nextDir;
        }

        dir = Direction.South;
        pos = startPos;
        
        for (int i = 0; i < 50; i++)
        {
            Direction nextDir = AttemptStep(pos, dir, out Vector3Int nextPos);
            if (nextDir == Direction.None) break;

            pos = nextPos;
            dir = nextDir;
        }
        
        dir = Direction.East;
        pos = startPos;
        
        
        for (int i = 0; i < 50; i++)
        {
            Direction nextDir = AttemptStep(pos, dir, out Vector3Int nextPos);
            if (nextDir == Direction.None) break;

            pos = nextPos;
            dir = nextDir;
        }
        
        GenerateFloorRange(0, startPos.x-1, startPos.x+1, startPos.z-1, startPos.z+1);
        
        
        
        
    }
    
    private void InstantiateFloorTile(Vector3Int position)
    {
        Debug.Log("Instantiating Floor Tile at " + position + "\n");


        if (CheckPositionCollision(position).PositionBlocked)
        {
            return;
        }
        
        Vector3 worldPos = MapPositionToWorld(position);
        GameObject floorTile = Instantiate(floorPrefab, worldPos, Quaternion.identity);
        MapData[position.x, position.y, position.z].Element = MapElement.Floor;

        //Append Y Value if not at ground level
        string yLog = position.y == 0 ? "" : " - " + position.y.ToString();
        floorTile.transform.parent = _mapParent.transform;
        floorTile.transform.name = "Floor (" + position.x + "," + position.z + ")" + yLog;
    }
    
    private Vector3Int WorldToMapPosition(Vector3 worldPosition)
    {
        int x = Mathf.FloorToInt(worldPosition.x / tileSize);
        int y = Mathf.FloorToInt(worldPosition.y / tileSize);
        int z = Mathf.FloorToInt(worldPosition.z / tileSize);
        
        return new Vector3Int(x, y, z);
    }
    
    private Vector3 MapPositionToWorld(Vector3Int mapPosition)
    {
        float x = mapPosition.x * tileSize;
        float y = mapPosition.y * tileSize;
        float z = mapPosition.z * tileSize;
        
        return new Vector3(x, y, z);
    }
    
    private CollisionMap CheckPositionCollision(Vector3Int position)
    {
        CollisionMap collisionMap = new CollisionMap();

        // If the center is out of bounds, treat as blocked.
        if (!InBounds(position))
        {
            collisionMap.PositionBlocked = true;
            collisionMap.NorthBlocked = true;
            collisionMap.SouthBlocked = true;
            collisionMap.EastBlocked  = true;
            collisionMap.WestBlocked  = true;
            collisionMap.AboveBlocked = true;
            collisionMap.BelowBlocked = true;
            return collisionMap;
        }

        collisionMap.PositionBlocked = MapData[position.x, position.y, position.z].Element != MapElement.Empty;

        Vector3Int n = new Vector3Int(position.x, position.y, position.z + 1);
        Vector3Int s = new Vector3Int(position.x, position.y, position.z - 1);
        Vector3Int e = new Vector3Int(position.x + 1, position.y, position.z);
        Vector3Int w = new Vector3Int(position.x - 1, position.y, position.z);
        Vector3Int a = new Vector3Int(position.x, position.y + 1, position.z);
        Vector3Int b = new Vector3Int(position.x, position.y - 1, position.z);

        collisionMap.NorthBlocked = !InBounds(n) || MapData[n.x, n.y, n.z].Element != MapElement.Empty;
        collisionMap.SouthBlocked = !InBounds(s) || MapData[s.x, s.y, s.z].Element != MapElement.Empty;
        collisionMap.EastBlocked  = !InBounds(e) || MapData[e.x, e.y, e.z].Element != MapElement.Empty;
        collisionMap.WestBlocked  = !InBounds(w) || MapData[w.x, w.y, w.z].Element != MapElement.Empty;
        collisionMap.AboveBlocked = !InBounds(a) || MapData[a.x, a.y, a.z].Element != MapElement.Empty;
        collisionMap.BelowBlocked = !InBounds(b) || MapData[b.x, b.y, b.z].Element != MapElement.Empty;

        return collisionMap;
    }

    private void GenerateFloorRange(int y, int xStart, int xEnd, int zStart, int zEnd)
    {
        if (xStart > xEnd || zStart > zEnd)
        {
            throw new System.ArgumentException("Invalid range for floor generation: xStart must be <= xEnd and zStart must be <= zEnd.");
        }

        for (int x = xStart; x <= xEnd; x++)
        {
            for(int z = zStart; z <= zEnd; z++)
            {
                Vector3Int position = new Vector3Int(x, y, z);
                InstantiateFloorTile(position);
            }
        }

    }
    
    private bool InBounds(Vector3Int p)
    {
        return p.x >= 0 && p.x < mapWidth
                        && p.y >= 0 && p.y < mapDepth
                        && p.z >= 0 && p.z < mapHeight;
    }

    private Direction AttemptStep(Vector3Int position, Direction dir, bool randomDirection = false)
    {
        var availableDirections = new List<Direction>
        {
            Direction.North,
            Direction.East,
            Direction.South,
            Direction.West
        };

        // If we *prefer* to keep going straight, try that first (if not blocked)
        if (!randomDirection && _rng.NextDouble() < directionalStickiness && dir != Direction.None)
        {
            // Try straight ahead first
            if (TryInstantiateFloorInDirection(position, dir, out _))
            {
                return dir;
            }

            // If straight failed, remove it and try others
            availableDirections.Remove(dir);
        }

        // Randomize remaining options
        ShuffleDirections(availableDirections);

        while (availableDirections.Count > 0)
        {
            Direction attemptedDirection = availableDirections[0];
            availableDirections.RemoveAt(0);

            if (TryInstantiateFloorInDirection(position, attemptedDirection, out _))
            {
                return attemptedDirection;
            }
        }

        return Direction.None;
    }

    private Direction AttemptStep(Vector3Int position, Direction dir, out Vector3Int newPos, bool randomDirection = false)
    {
        newPos = position;

        var availableDirections = new List<Direction>
        {
            Direction.North,
            Direction.East,
            Direction.South,
            Direction.West
        };

        if (!randomDirection && _rng.NextDouble() < directionalStickiness && dir != Direction.None)
        {
            if (TryInstantiateFloorInDirection(position, dir, out newPos))
                return dir;

            availableDirections.Remove(dir);
        }

        ShuffleDirections(availableDirections);

        while (availableDirections.Count > 0)
        {
            Direction attemptedDirection = availableDirections[0];
            availableDirections.RemoveAt(0);

            if (TryInstantiateFloorInDirection(position, attemptedDirection, out newPos))
                return attemptedDirection;
        }

        return Direction.None;
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

    private static Vector3Int Step(Vector3Int position, Direction dir)
    {
        return position + DirectionToOffset(dir);
    }

    
    private bool TryInstantiateFloorInDirection(Vector3Int from, Direction dir, out Vector3Int newPos)
    {
        newPos = Step(from, dir);

        if (!InBounds(newPos))
            return false;

        if (CheckPositionCollision(newPos).PositionBlocked)
            return false;

        InstantiateFloorTile(newPos);
        return true;
    }


    private void ShuffleDirections(List<Direction> directions)
    {
        for (int i = directions.Count - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1); // 0..i inclusive
            (directions[i], directions[j]) = (directions[j], directions[i]);
        }
    }


    private bool DirectionBlocked(CollisionMap map, Direction dir)
    {
        switch (dir)
        {
            case Direction.East:
                return map.EastBlocked;
            case Direction.West:
                return map.WestBlocked;
            case Direction.North:
                return map.NorthBlocked;
            case Direction.South:
                return map.SouthBlocked;
        }

        return false;
    }
}
