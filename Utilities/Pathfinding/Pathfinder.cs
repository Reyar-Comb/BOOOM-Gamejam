using Godot;
using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Cosmosity.Pathfinders.PathfindingUtilities;

namespace Cosmosity.Pathfinders;
public abstract class Pathfinder
{
    public enum HeuristicType
    {
        Octile,
        Manhattan,
        Euclidean,
        Chebyshev,
        Custom
    }
    public enum DiagonalType
    {
        Never,
        WhenBothEmpty,
        WhenAtLeastOneEmpty,
        Always
    }

    public abstract class PathfinderBuilder<TBuilder, TPathfinder>
        where TBuilder : PathfinderBuilder<TBuilder, TPathfinder>
        where TPathfinder : Pathfinder
    {
        protected Func<int, int, int, int, int> _customHeuristic = null;
        protected HeuristicType _heuristicType;
        protected DiagonalType _diagonalType;
        protected Rect2I _usedRect = default;

        protected PathfinderBuilder(
            HeuristicType heuristicType = HeuristicType.Octile,
            DiagonalType diagonalType = DiagonalType.WhenBothEmpty)
        {
            _heuristicType = heuristicType;
            _diagonalType = diagonalType;
        }

        public TBuilder UseCustomHeuristic(Func<int, int, int, int, int> heuristic)
        {
            _customHeuristic = heuristic;
            _heuristicType = HeuristicType.Custom;
            return (TBuilder)this;
        }

        public TBuilder UseHeuristic(HeuristicType type)
        {
            _heuristicType = type;
            return (TBuilder)this;
        }

        public TBuilder SetRect(int left, int top, int right, int bottom)
        {
            _usedRect = new Rect2I(left, top, right - left, bottom - top);
            return (TBuilder)this;
        }

        public virtual TBuilder UseDiagonal(DiagonalType type)
        {
            _diagonalType = type;
            return (TBuilder)this;
        }

        public abstract TPathfinder Build();
    }

    protected Func<int, int, int, int, int> _customHeuristic = null;
    protected HeuristicType _heuristicType = HeuristicType.Octile;
    protected DiagonalType _diagonalType = DiagonalType.Never;
    protected readonly int _height = 0;
    protected readonly int _width = 0;
    protected readonly Vector2I _offset = Vector2I.Zero;
    protected bool[] _wallGrid;
    protected ConcurrentBag<PathfindingContext> _contextPool = new();
    protected Pathfinder(Rect2I usedRect, HeuristicType heuristicType, Func<int, int, int, int, int> customHeuristic, DiagonalType diagonalType)
    {
        _offset = usedRect.Position;
        _width = usedRect.Size.X + 1;
        _height = usedRect.Size.Y + 1;
        _wallGrid = new bool[_width * _height];
        _heuristicType = heuristicType;
        _customHeuristic = customHeuristic;
        _diagonalType = diagonalType;
    }
    public void SetWall(Vector2I position, bool isWall = true)
    {
        if (IsOutOfBound(position)) return;

        _wallGrid[GetIndex(position)] = isWall;
    }
    public List<Vector2I> Run(Vector2I startPos, Vector2I targetPos, float heuristicScale = 1f)
    {
        if (!IsWalkable(startPos) || !IsWalkable(targetPos)) return null;
        if (startPos == targetPos) return new List<Vector2I> { startPos };
        using var handle = new PathfindingContextHandle(_contextPool);
        var context = handle.GetContext(_width, _height);
        var openList = context.OpenList;
        var isClosed = context.IsClosed;
        var grid = context.Grid;
        var version = context.CurrentVersion;
        var startNode = new PathNode(startPos) { G = 0, Version = version };
        var targetNode = new PathNode(targetPos) { Version = version };

        int startIndex = GetIndex(startPos);
        int targetIndex = GetIndex(targetPos);
        grid[startIndex] = startNode;
        grid[targetIndex] = targetNode;
        isClosed[startIndex] = false;
        isClosed[targetIndex] = false;

        openList.Enqueue(GetIndex(startPos), 0);
        return MainLoop(context, targetNode, heuristicScale);
    }
    protected abstract List<Vector2I> MainLoop(PathfindingContext context, PathNode targetNode, float heuristicScale);
    public Task<List<Vector2I>> RunAsync(Vector2I startPos, Vector2I targetPos, float heuristicScale = 1f)
    {
        return Task.Run(() => Run(startPos, targetPos, heuristicScale));
    }
    protected List<Vector2I> RetracePath(PathNode node, PathfindingContext context)
    {
        List<Vector2I> path = new();
        while (node.ParentIndex != -1)
        {
            path.Add(new Vector2I(node.X, node.Y));
            node = context.Grid[node.ParentIndex];
        }
        path.Reverse();
        return path;
    }
    protected bool CheckDiagonal(bool isWalkable1, bool isWalkable2)
    {
        return _diagonalType switch
        {
            DiagonalType.Never => false,
            DiagonalType.WhenBothEmpty => isWalkable1 && isWalkable2,
            DiagonalType.WhenAtLeastOneEmpty => isWalkable1 || isWalkable2,
            DiagonalType.Always => true,
            _ => false
        };
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected int CalculateHeuristic(int ax, int ay, int bx, int by)
    {
        switch (_heuristicType)
        {
            case HeuristicType.Octile:
                {
                    int xDiff = ax - bx > 0 ? ax - bx : bx - ax;
                    int yDiff = ay - by > 0 ? ay - by : by - ay;
                    int minDiff = xDiff > yDiff ? yDiff : xDiff;
                    int maxDiff = xDiff > yDiff ? xDiff : yDiff;
                    return minDiff * 141 + (maxDiff - minDiff) * 100;
                }
            case HeuristicType.Manhattan:
                {
                    int xDiff = ax - bx > 0 ? ax - bx : bx - ax;
                    int yDiff = ay - by > 0 ? ay - by : by - ay;
                    return (xDiff + yDiff) * 100;
                }
            case HeuristicType.Euclidean:
                {
                    int xDiff = ax - bx > 0 ? ax - bx : bx - ax;
                    int yDiff = ay - by > 0 ? ay - by : by - ay;
                    return (int)(Math.Sqrt(xDiff * xDiff + yDiff * yDiff) * 100);
                }
            case HeuristicType.Chebyshev:
                {
                    int xDiff = ax - bx > 0 ? ax - bx : bx - ax;
                    int yDiff = ay - by > 0 ? ay - by : by - ay;
                    return xDiff > yDiff ? xDiff : yDiff;
                }
            case HeuristicType.Custom:
                return _customHeuristic(ax, ay, bx, by);
            default:
                return 0;
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected int GetIndex(int x, int y) => (y - _offset.Y) * _width + (x - _offset.X);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected int GetIndex(Vector2I pos) => GetIndex(pos.X, pos.Y);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool IsOutOfBound(int x, int y) => !(x >= _offset.X && x <= _offset.X + _width - 1 && y >= _offset.Y && y <= _offset.Y + _height - 1);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool IsOutOfBound(Vector2I pos) => IsOutOfBound(pos.X, pos.Y);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool IsWalkable(int x, int y) => IsOutOfBound(x, y) ? false : !_wallGrid[GetIndex(x, y)];
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool IsWalkable(Vector2I pos) => IsWalkable(pos.X, pos.Y);
    protected void UpdateNeighbor(PathfindingContext context, PathNode cur, PathNode targetNode, int neighborIdx, float heuristicScale)
    {
        var openList = context.OpenList;
        var isClosed = context.IsClosed;
        var grid = context.Grid;
        var version = context.CurrentVersion;
        var curIndex = GetIndex(cur.X, cur.Y);

        ref var neighbor = ref grid[neighborIdx];
        neighbor.X = neighborIdx % _width + _offset.X;
        neighbor.Y = neighborIdx / _width + _offset.Y;

        if (neighbor.Version != version)
        {
            neighbor.Version = version;
            neighbor.G = int.MaxValue;
            isClosed[neighborIdx] = false;
        }
        if (isClosed[neighborIdx]) return;

        int moveCost = CalculateHeuristic(neighbor.X, neighbor.Y, cur.X, cur.Y);
        if (neighbor.G > moveCost + cur.G)
        {
            neighbor.G = moveCost + cur.G;
            neighbor.ParentIndex = curIndex;
            int neighborH = (int)(CalculateHeuristic(neighbor.X, neighbor.Y, targetNode.X, targetNode.Y) * heuristicScale);
            openList.Enqueue(neighborIdx, neighbor.G + neighborH);
        }
    }
}
