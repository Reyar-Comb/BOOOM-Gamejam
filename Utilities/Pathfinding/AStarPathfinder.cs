using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using static Cosmosity.Pathfinders.PathfindingUtilities;

namespace Cosmosity.Pathfinders;
public class AStarPathfinder : Pathfinder
{
    public static AStarPathfinderBuilder CreateBuilder() => new AStarPathfinderBuilder();
    public class AStarPathfinderBuilder : PathfinderBuilder<AStarPathfinderBuilder, AStarPathfinder>
    {
        private int _numOfNeighborsToCheck = 4;
        public override AStarPathfinderBuilder UseDiagonal(DiagonalType type)
        {
            _numOfNeighborsToCheck = type == DiagonalType.Never ? 4 : 8;
            return base.UseDiagonal(type);
        }

        public override AStarPathfinder Build()
        {
            return new AStarPathfinder(_usedRect, _heuristicType, _customHeuristic, _numOfNeighborsToCheck, _diagonalType);
        }
    }
    private readonly int _numOfNeighborsToCheck = 4;
    private AStarPathfinder(Rect2I usedRect, HeuristicType heuristicType, Func<int, int, int, int, int> customHeuristic, int numOfNeighborsToCheck, DiagonalType diagonalType)
        : base(usedRect, heuristicType, customHeuristic, diagonalType)
    {
        _numOfNeighborsToCheck = numOfNeighborsToCheck;
    }
    protected override List<Vector2I> MainLoop(PathfindingContext context, PathNode targetNode, float heuristicScale)
    {
        var openList = context.OpenList;
        var isClosed = context.IsClosed;
        var grid = context.Grid;

        while (openList.Count != 0)
        {
            int curIndex = openList.Dequeue();
            ref PathNode cur = ref grid[curIndex];

            if (isClosed[curIndex]) continue;

            isClosed[curIndex] = true;
            if (cur.X == targetNode.X && cur.Y == targetNode.Y) return RetracePath(cur, context);

            for (int i = 0; i < _numOfNeighborsToCheck; i++)
            {
                int nx = cur.X + DxArray[i];
                int ny = cur.Y + DyArray[i];
                if (!IsWalkable(nx, ny)) continue;
                if (i >= 4 && !CheckDiagonal(IsWalkable(nx, cur.Y), IsWalkable(cur.X, ny))) continue;

                var neighborIdx = GetIndex(nx, ny);
                ref var neighbor = ref grid[neighborIdx];
                neighbor.X = nx;
                neighbor.Y = ny;

                UpdateNeighbor(context, cur, targetNode, neighborIdx, heuristicScale);
            }
        }
        return null;
    }
}
