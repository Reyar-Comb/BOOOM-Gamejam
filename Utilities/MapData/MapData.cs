using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
public class MapData
{
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

    public int Width { get; init; }
    public int Height { get; init; }
    // ID starts from 1, 0 means unassigned.
    private int[] _regionId;
    private PriorityQueue<TileExpansion, float> _nextTileDecider;
    private readonly int _regionSeedTileDistance;
    private RandomNumberGenerator _rg;
    private NoiseTexture2D _noise;
    public MapData(int width, int height, int regionSeedTileDistance = 8)
    {
        Width = width;
        Height = height;
        _regionId = new int[Width * Height];
        _nextTileDecider = new();
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
    public int GetIndex(int x, int y)
    {
        return _regionId[x + y * Width];
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetIndex(int x, int y, int regionId)
    {
        _regionId[x + y * Width] = regionId;
    }
    private void ResetRegion()
    {
        for (int i = 0; i < _regionId.Length; i++)
        {
            _regionId[i] = 0;
        }
    }
    public void CreateRegions(int regionCount, float randomness = 0.5f)
    {
        ResetRegion();

        _nextTileDecider.Clear();

        Vector2I[] regionSeedPositions = new Vector2I[regionCount];
        for (int i = 1; i <= regionCount; i++)
        {
            DecideSeedPositions(regionSeedPositions, i);
        }
        Expand(randomness);
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
                SetIndex(rx, ry, currentRegionId);
                EnqueueNeighbors(rx, ry, currentRegionId, 0.0f, 0.0f);
                break;
            }
        }
    }
    private void Expand(float randomness = 0.5f)
    {
        while (_nextTileDecider.Count > 0)
        {
            TileExpansion current = _nextTileDecider.Dequeue();
            int currentIndex = current.Index;
            int x = currentIndex % Width;
            int y = currentIndex / Width;

            if (!IsValid(x, y) || GetIndex(x, y) != 0)
            {
                continue;
            }
            SetIndex(x, y, current.RegionId);
            EnqueueNeighbors(x, y, current.RegionId, current.Cost, randomness);
        }
    }

    private void EnqueueNeighbors(int x, int y, int currentRegionId, float currentCost, float randomness)
    {
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

                if (GetIndex(newX, newY) == 0)
                {
                    int nextIndex = newX + newY * Width;
                    float nextCost = currentCost + GetStepCost(newX, newY, randomness);
                    _nextTileDecider.Enqueue(new TileExpansion(nextIndex, currentRegionId, nextCost), nextCost);
                }
            }
        }
    }

    private float GetStepCost(int x, int y, float randomness)
    {
        return 1f + _noise.Noise.GetNoise2D(x, y) * randomness;
    }
}
