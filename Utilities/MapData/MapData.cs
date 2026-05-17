using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
public class MapData
{
    public const int PlayerBaseRegionId = 1;
    public const int EnemyBaseRegionId = 2;

    public enum RegionState
    {
        Occupied,
        Unoccupied,
        EnemyBase
    }

    public readonly struct BridgeConnection
    {
        public readonly Vector2I A;
        public readonly Vector2I B;

        public BridgeConnection(Vector2I a, Vector2I b)
        {
            A = a;
            B = b;
        }
    }

    private readonly struct TileExpansion
    {
        public readonly int Index;
        public readonly int RegionId;
        public readonly float Cost;

        public TileExpansion(int index, int regionId, float cost)
        {
            Index = index;
            RegionId = regionId;
            Cost = cost;
        }
    }

    private readonly struct BoundaryKey : IEquatable<BoundaryKey>
    {
        public readonly int RegionA;
        public readonly int RegionB;

        public BoundaryKey(int regionA, int regionB)
        {
            RegionA = Math.Min(regionA, regionB);
            RegionB = Math.Max(regionA, regionB);
        }

        public bool Equals(BoundaryKey other)
        {
            return RegionA == other.RegionA && RegionB == other.RegionB;
        }

        public override bool Equals(object obj)
        {
            return obj is BoundaryKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(RegionA, RegionB);
        }
    }

    public int Width { get; init; }
    public int Height { get; init; }
    // ID starts from 1, 0 means unassigned.
    private int[] _regionId;
    private bool[] _isBridge;
    private RegionState[] _regionStates;
    private bool[] _regionExplored;
    private List<BridgeConnection> _bridges;
    private List<PriorityQueue<TileExpansion, float>> _nextTileDeciders;
    private readonly int _regionSeedTileDistance;
    private RandomNumberGenerator _rg;
    private NoiseTexture2D _noise;
    private List<List<Vector2I>> _regionTiles = new();
    public MapData(int width, int height, int regionSeedTileDistance = 8)
    {
        Width = width;
        Height = height;
        _regionId = new int[Width * Height];
        _isBridge = new bool[Width * Height];
        _regionStates = Array.Empty<RegionState>();
        _regionExplored = Array.Empty<bool>();
        _bridges = new();
        _nextTileDeciders = new();
        _regionSeedTileDistance = regionSeedTileDistance;
        _rg = new();
        _rg.Randomize();
        _noise = new()
        {
            Noise = new FastNoiseLite()
        };
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsValid(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsCell(int x, int y)
    {
        return IsValid(x, y);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsCell(Vector2I cell)
    {
        return ContainsCell(cell.X, cell.Y);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetRegion(int x, int y)
    {
        return IsValid(x, y) ? _regionId[x + y * Width] : 0;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsBridge(int x, int y)
    {
        return IsValid(x, y) && _isBridge[x + y * Width];
    }

    public bool IsRegionOccupied(int regionId)
    {
        return GetRegionState(regionId) == RegionState.Occupied;
    }

    public void SetRegionOccupied(int regionId, bool isOccupied)
    {
        SetRegionState(regionId, isOccupied ? RegionState.Occupied : RegionState.Unoccupied);
    }

    public RegionState GetRegionState(int regionId)
    {
        int regionIndex = regionId - 1;
        if (regionIndex < 0 || regionIndex >= _regionStates.Length)
        {
            return RegionState.Unoccupied;
        }

        return _regionStates[regionIndex];
    }

    public void SetRegionState(int regionId, RegionState state)
    {
        int regionIndex = regionId - 1;
        if (regionIndex < 0 || regionIndex >= _regionStates.Length)
        {
            Debug.PushError("Region id must match an existing region.");
            return;
        }

        _regionStates[regionIndex] = regionId switch
        {
            PlayerBaseRegionId => RegionState.Occupied,
            EnemyBaseRegionId => RegionState.EnemyBase,
            _ => state
        };
    }

    public bool IsRegionExplored(int regionId)
    {
        int regionIndex = regionId - 1;
        return regionIndex >= 0
            && regionIndex < _regionExplored.Length
            && _regionExplored[regionIndex];
    }

    public void SetRegionExplored(int regionId, bool isExplored)
    {
        int regionIndex = regionId - 1;
        if (regionIndex < 0 || regionIndex >= _regionExplored.Length)
        {
            Debug.PushError("Region id must match an existing region.");
            return;
        }

        _regionExplored[regionIndex] = isExplored;
    }

    public void ResetDynamicRegionStates()
    {
        for (int i = 0; i < _regionStates.Length; i++)
        {
            int regionId = i + 1;
            _regionStates[i] = regionId switch
            {
                EnemyBaseRegionId => RegionState.EnemyBase,
                _ => RegionState.Occupied
            };
        }
    }

    public IReadOnlyList<BridgeConnection> GetBridges()
    {
        return _bridges;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetRegion(int x, int y, int regionId)
    {
        if (!IsValid(x, y)) return;
        _regionId[x + y * Width] = regionId;
        AddRegionTile(regionId, new Vector2I(x, y));
    }

    private void AddRegionTile(int regionId, Vector2I cell)
    {
        if (regionId <= 0) return;
        EnsureRegionTileListCount(regionId);
        _regionTiles[regionId - 1].Add(cell);
    }

    private void EnsureRegionTileListCount(int regionCount)
    {
        for (int i = _regionTiles.Count; i < regionCount; i++)
        {
            _regionTiles.Add(new List<Vector2I>());
        }
    }

    public void Reset()
    {
        for (int i = 0; i < _regionId.Length; i++)
        {
            _regionId[i] = 0;
            _isBridge[i] = false;
        }
        _bridges.Clear();
        foreach (List<Vector2I> tiles in _regionTiles)
        {
            tiles.Clear();
        }
        _regionStates = Array.Empty<RegionState>();
        _regionExplored = Array.Empty<bool>();
        foreach (PriorityQueue<TileExpansion, float> decider in _nextTileDeciders)
        {
            decider.Clear();
        }
    }
    public void CreateRegions(int regionCount, float randomness = 0.5f)
    {
        Reset();

        regionCount = Math.Max(regionCount, 2);
        EnsureDeciderCount(regionCount);
        EnsureRegionTileListCount(regionCount);
        InitializeRegionStates(regionCount);

        Vector2I[] regionSeedPositions = new Vector2I[regionCount];
        CreateSeed(regionSeedPositions, 1, new Vector2I(0, Height / 2));
        CreateSeed(regionSeedPositions, 2, new Vector2I(Width - 1, Height / 2));

        for (int i = 3; i <= regionCount; i++)
        {
            DecideSeedPositions(regionSeedPositions, i);
        }
        Expand(randomness);
        CreateBridges();
    }

    private void InitializeRegionStates(int regionCount)
    {
        _regionStates = new RegionState[regionCount];
        _regionExplored = new bool[regionCount];
        ResetDynamicRegionStates();
    }

    private void CreateSeed(Vector2I[] seedPositions, int currentRegionId, Vector2I seedPosition)
    {
        seedPositions[currentRegionId - 1] = seedPosition;
        SetRegion(seedPosition.X, seedPosition.Y, currentRegionId);
        EnqueueNeighbors(seedPosition.X, seedPosition.Y, currentRegionId, 0.0f, 0.0f);
    }

    private void DecideSeedPositions(Vector2I[] seedPositions, int currentRegionId)
    {
        while (true)
        {
            int rx = _rg.RandiRange(0, Width - 1);
            int ry = _rg.RandiRange(0, Height - 1);
            Vector2I randomPos = new Vector2I(rx, ry);
            bool canPlace = true;
            for (int i = 0; i < currentRegionId - 1; i++)
            {
                if (randomPos.DistanceTo(seedPositions[i]) < _regionSeedTileDistance)
                {
                    canPlace = false;
                    break;
                }
            }
            if (canPlace)
            {
                seedPositions[currentRegionId - 1] = randomPos;
                SetRegion(rx, ry, currentRegionId);
                EnqueueNeighbors(rx, ry, currentRegionId, 0.0f, 0.0f);
                break;
            }
        }
    }

    private void EnsureDeciderCount(int regionCount)
    {
        int deciderCount = _nextTileDeciders.Count;
        for (int i = 0; i < regionCount; i++)
        {
            if (i >= deciderCount)
            {
                _nextTileDeciders.Add(new PriorityQueue<TileExpansion, float>());
            }
            else
            {
                _nextTileDeciders[i].Clear();
            }
        }
    }

    private void Expand(float randomness = 0.5f)
    {
        bool hasTilesToExpand = true;
        while (hasTilesToExpand)
        {
            hasTilesToExpand = false;
            for (int i = 0; i < _nextTileDeciders.Count; i++)
            {
                if (TryExpandNextTile(_nextTileDeciders[i], randomness))
                {
                    hasTilesToExpand = true;
                }
            }
        }
    }

    private bool TryExpandNextTile(PriorityQueue<TileExpansion, float> decider, float randomness)
    {
        while (decider.Count > 0)
        {
            TileExpansion current = decider.Dequeue();
            int currentIndex = current.Index;
            int x = currentIndex % Width;
            int y = currentIndex / Width;

            if (!IsValid(x, y) || GetRegion(x, y) != 0)
            {
                continue;
            }

            SetRegion(x, y, current.RegionId);
            EnqueueNeighbors(x, y, current.RegionId, current.Cost, randomness);
            return true;
        }

        return false;
    }

    private void EnqueueNeighbors(int x, int y, int currentRegionId, float currentCost, float randomness)
    {
        var decider = _nextTileDeciders[currentRegionId - 1];
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                if ((i == 0 && j == 0) || (i != 0 && j != 0))
                {
                    continue;
                }

                int newX = x + i;
                int newY = y + j;
                if (!IsValid(newX, newY))
                {
                    continue;
                }

                if (GetRegion(newX, newY) == 0)
                {
                    int nextIndex = newX + newY * Width;
                    float nextCost = currentCost + GetStepCost(newX, newY, randomness);
                    decider.Enqueue(new TileExpansion(nextIndex, currentRegionId, nextCost), nextCost);
                }
            }
        }
    }

    private float GetStepCost(int x, int y, float randomness)
    {
        return 1f + _noise.Noise.GetNoise2D(x, y) * randomness;
    }

    private void CreateBridges()
    {
        Dictionary<BoundaryKey, List<BridgeConnection>> candidatesByBoundary = new();

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                CollectBoundaryCandidate(candidatesByBoundary, x, y, x + 1, y);
                CollectBoundaryCandidate(candidatesByBoundary, x, y, x, y + 1);
            }
        }

        foreach (List<BridgeConnection> candidates in candidatesByBoundary.Values)
        {
            BridgeConnection bridge = candidates[_rg.RandiRange(0, candidates.Count - 1)];
            SetBridge(bridge.A);
            SetBridge(bridge.B);
            _bridges.Add(bridge);
        }
    }

    private void CollectBoundaryCandidate(
        Dictionary<BoundaryKey, List<BridgeConnection>> candidatesByBoundary,
        int ax,
        int ay,
        int bx,
        int by)
    {
        if (!IsValid(ax, ay) || !IsValid(bx, by))
        {
            return;
        }

        int regionA = GetRegion(ax, ay);
        int regionB = GetRegion(bx, by);
        if (regionA == 0 || regionB == 0 || regionA == regionB)
        {
            return;
        }

        BoundaryKey key = new(regionA, regionB);
        if (!candidatesByBoundary.TryGetValue(key, out List<BridgeConnection> candidates))
        {
            candidates = new List<BridgeConnection>();
            candidatesByBoundary[key] = candidates;
        }

        candidates.Add(new BridgeConnection(new Vector2I(ax, ay), new Vector2I(bx, by)));
    }

    private void SetBridge(Vector2I cell)
    {
        if (!IsValid(cell.X, cell.Y)) return;
        _isBridge[cell.X + cell.Y * Width] = true;
    }
    public Vector2I GetRandomPositionInRegion(int regionId)
    {
        int regionIndex = regionId - 1;
        if (regionIndex < 0 || regionIndex >= _regionTiles.Count)
        {
            Debug.PushError("Region id must match an existing region.");
            return Vector2I.Zero;
        }

        List<Vector2I> tiles = _regionTiles[regionIndex];
        if (tiles.Count == 0)
        {
            Debug.PushError($"Region {regionId} has no tiles.");
            return Vector2I.Zero;
        }

        return tiles[_rg.RandiRange(0, tiles.Count - 1)];
    }
}

