using Godot;
using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Cosmosity.Pathfinders;
public class PathfindingUtilities
{
    public struct PathNode
    {
        public int X, Y;
        public int G;
        public int ParentIndex;
        public int Version;
        public PathNode(Vector2I pos)
        {
            X = pos.X;
            Y = pos.Y;
            G = int.MaxValue;
            ParentIndex = -1;
        }
    }
    public class PathfindingContext
    {
        // Stores the index of the node in Grid[]. Dequeue it to get the PathNode from Grid[].
        public PriorityQueue<int, int> OpenList = new();
        public bool[] IsClosed = [];
        public PathNode[] Grid = [];
        public int CurrentVersion = 1;
        public PathfindingContext(int width, int height)
        {
            int size = width * height;
            IsClosed = new bool[size];
            Grid = new PathNode[size];
        }
        public void Reset()
        {
            OpenList.Clear();
            CurrentVersion++;
        }
    }
    public class PathfindingContextHandle : IDisposable
    {
        private ConcurrentBag<PathfindingContext> _pool;
        private PathfindingContext _context;
        public PathfindingContext GetContext(int width, int height)
        {
            if (!_pool.TryTake(out PathfindingContext context))
                context = new PathfindingContext(width, height);
            return _context = context;
        }
        public PathfindingContextHandle(ConcurrentBag<PathfindingContext> pool)
        {
            _pool = pool;
        }
        public void Dispose()
        {
            _context.Reset();
            _pool.Add(_context);
        }
    }
    // S, N, E, W, SE, NE, SW, NW
    public static readonly int[] DxArray = { 0, 0, 1, -1, 1, 1, -1, -1 };
    public static readonly int[] DyArray = { 1, -1, 0, 0, 1, -1, 1, -1 };
    private static readonly byte[] _dirIndexMap = [
        7, 3, 6, // dx = -1, dy = -1, 0, 1
        1, 8, 0, // dx = 0,  dy = -1, 0, 1
        5, 2, 4 // dx = 1,  dy = -1, 0, 1
    ];
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte GetDirIndex(int dx, int dy) => _dirIndexMap[(dx + 1) * 3 + dy + 1];
}
