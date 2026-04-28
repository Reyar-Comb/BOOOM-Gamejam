using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using static Cosmosity.Pathfinders.PathfindingUtilities;
using System.Numerics;

namespace Cosmosity.Pathfinders;
public class JPSPathfinder : Pathfinder
{
    private abstract class JPSRule
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool IsWalkableInMask(int mask, int dx, int dy)
        {
            int shift = 7 - GetDirIndex(dx, dy);
            return shift != -1 && (mask & (1 << shift)) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void AddDirection(ref byte result, int dx, int dy)
        {
            int shift = 7 - GetDirIndex(dx, dy);
            if (shift != -1) result |= (byte)(1 << shift);
        }
        public void Precompute(byte[] directionMasks)
        {
            PrecomputeBehavior(directionMasks);
        }
        protected abstract void PrecomputeBehavior(byte[] directionMasks);
        public abstract bool HasForcedNeighbor(int x, int y, int dx, int dy, JPSPathfinder finder);
    }
    private class JPSRuleWhenBothEmpty : JPSRule
    {
        protected override void PrecomputeBehavior(byte[] directionMasks)
        {
            for (int dir = 0; dir < 8; dir++)
            {
                int dx = DxArray[dir];
                int dy = DyArray[dir];

                for (int mask = 0; mask < 256; mask++)
                {
                    byte result = 0;

                    if (dx != 0 && dy != 0)
                    {
                        bool diagValid = IsWalkableInMask(mask, dx, 0) && IsWalkableInMask(mask, 0, dy);

                        if (IsWalkableInMask(mask, dx, 0)) AddDirection(ref result, dx, 0);
                        if (IsWalkableInMask(mask, 0, dy)) AddDirection(ref result, 0, dy);
                        if (diagValid && IsWalkableInMask(mask, dx, dy)) AddDirection(ref result, dx, dy);

                    }
                    else if (dx != 0 && dy == 0)
                    {
                        if (IsWalkableInMask(mask, dx, 0)) AddDirection(ref result, dx, 0);

                        if (!IsWalkableInMask(mask, -dx, -1) && IsWalkableInMask(mask, 0, -1))
                        {
                            AddDirection(ref result, 0, -1);
                            if (IsWalkableInMask(mask, dx, -1) && IsWalkableInMask(mask, dx, 0)) AddDirection(ref result, dx, -1);
                        }
                        if (!IsWalkableInMask(mask, -dx, 1) && IsWalkableInMask(mask, 0, 1))
                        {
                            AddDirection(ref result, 0, 1);
                            if (IsWalkableInMask(mask, dx, 1) && IsWalkableInMask(mask, dx, 0)) AddDirection(ref result, dx, 1);
                        }
                    }
                    else if (dx == 0 && dy != 0)
                    {
                        if (IsWalkableInMask(mask, 0, dy)) AddDirection(ref result, 0, dy);

                        if (!IsWalkableInMask(mask, -1, -dy) && IsWalkableInMask(mask, -1, 0))
                        {
                            AddDirection(ref result, -1, 0);
                            if (IsWalkableInMask(mask, -1, dy) && IsWalkableInMask(mask, 0, dy)) AddDirection(ref result, -1, dy);
                        }
                        if (!IsWalkableInMask(mask, 1, -dy) && IsWalkableInMask(mask, 1, 0))
                        {
                            AddDirection(ref result, 1, 0);
                            if (IsWalkableInMask(mask, 1, dy) && IsWalkableInMask(mask, 0, dy)) AddDirection(ref result, 1, dy);
                        }
                    }

                    directionMasks[dir * 256 + mask] = result;
                }
            }
        }

        public override bool HasForcedNeighbor(int x, int y, int dx, int dy, JPSPathfinder finder)
        {
            if (dx != 0 && dy != 0)
            {
                return false;
            }
            else if (dx != 0 && dy == 0)
            {
                return (!finder.IsWalkable(x - dx, y - 1) && finder.IsWalkable(x, y - 1)) ||
                       (!finder.IsWalkable(x - dx, y + 1) && finder.IsWalkable(x, y + 1));
            }
            else if (dx == 0 && dy != 0)
            {
                return (!finder.IsWalkable(x - 1, y - dy) && finder.IsWalkable(x - 1, y)) ||
                       (!finder.IsWalkable(x + 1, y - dy) && finder.IsWalkable(x + 1, y));
            }
            return false;
        }
    }
    private class JPSRuleWhenAtLeastOneEmpty : JPSRule
    {
        protected override void PrecomputeBehavior(byte[] directionMasks)
        {
            for (int dir = 0; dir < 8; dir++)
            {
                int dx = DxArray[dir];
                int dy = DyArray[dir];

                for (int mask = 0; mask < 256; mask++)
                {
                    byte result = 0;

                    if (dx != 0 && dy != 0)
                    {
                        bool diagValid = IsWalkableInMask(mask, dx, 0) || IsWalkableInMask(mask, 0, dy);

                        if (IsWalkableInMask(mask, dx, 0)) AddDirection(ref result, dx, 0);
                        if (IsWalkableInMask(mask, 0, dy)) AddDirection(ref result, 0, dy);
                        if (diagValid && IsWalkableInMask(mask, dx, dy)) AddDirection(ref result, dx, dy);

                        if (!IsWalkableInMask(mask, -dx, 0) && IsWalkableInMask(mask, -dx, dy) && IsWalkableInMask(mask, 0, dy))
                        {
                            AddDirection(ref result, -dx, dy);
                        }
                        if (!IsWalkableInMask(mask, 0, -dy) && IsWalkableInMask(mask, dx, -dy) && IsWalkableInMask(mask, dx, 0))
                        {
                            AddDirection(ref result, dx, -dy);
                        }
                    }
                    else if (dx != 0 && dy == 0)
                    {
                        if (IsWalkableInMask(mask, dx, 0)) AddDirection(ref result, dx, 0);

                        if (!IsWalkableInMask(mask, 0, -1) && IsWalkableInMask(mask, dx, -1) && IsWalkableInMask(mask, dx, 0)) AddDirection(ref result, dx, -1);
                        if (!IsWalkableInMask(mask, 0, 1) && IsWalkableInMask(mask, dx, 1) && IsWalkableInMask(mask, dx, 0)) AddDirection(ref result, dx, 1);
                    }
                    else if (dx == 0 && dy != 0)
                    {
                        if (IsWalkableInMask(mask, 0, dy)) AddDirection(ref result, 0, dy);

                        if (!IsWalkableInMask(mask, -1, 0) && IsWalkableInMask(mask, -1, dy) && IsWalkableInMask(mask, 0, dy)) AddDirection(ref result, -1, dy);
                        if (!IsWalkableInMask(mask, 1, 0) && IsWalkableInMask(mask, 1, dy) && IsWalkableInMask(mask, 0, dy)) AddDirection(ref result, 1, dy);
                    }

                    directionMasks[dir * 256 + mask] = result;
                }
            }
        }

        public override bool HasForcedNeighbor(int x, int y, int dx, int dy, JPSPathfinder finder)
        {
            if (dx != 0 && dy != 0)
            {
                return (!finder.IsWalkable(x - dx, y) && finder.IsWalkable(x - dx, y + dy) && finder.IsWalkable(x, y + dy)) ||
                       (!finder.IsWalkable(x, y - dy) && finder.IsWalkable(x + dx, y - dy) && finder.IsWalkable(x + dx, y));
            }
            else if (dx != 0 && dy == 0)
            {
                return ((!finder.IsWalkable(x, y - 1) && finder.IsWalkable(x + dx, y - 1)) ||
                       (!finder.IsWalkable(x, y + 1) && finder.IsWalkable(x + dx, y + 1))) &&
                       finder.IsWalkable(x + dx, y);
            }
            else if (dx == 0 && dy != 0)
            {
                return ((!finder.IsWalkable(x - 1, y) && finder.IsWalkable(x - 1, y + dy)) ||
                       (!finder.IsWalkable(x + 1, y) && finder.IsWalkable(x + 1, y + dy))) &&
                       finder.IsWalkable(x, y + dy);
            }
            return false;
        }
    }
    public static JPSPathfinderBuilder CreateBuilder() => new JPSPathfinderBuilder();
    public class JPSPathfinderBuilder : PathfinderBuilder<JPSPathfinderBuilder, JPSPathfinder>
    {
        public override JPSPathfinderBuilder UseDiagonal(DiagonalType type)
        {
            if (type is not (DiagonalType.WhenBothEmpty or DiagonalType.WhenAtLeastOneEmpty))
            {
                throw new Exception("JPSPathfinder only supports WhenBothEmpty and WhenAtLeastOneEmpty currently.");
            }
            return base.UseDiagonal(type);
        }
        public override JPSPathfinder Build()
        {
            return new JPSPathfinder(_usedRect, _heuristicType, _customHeuristic, _diagonalType);
        }
    }
    private readonly byte[] _directionMasks = new byte[8 * 256];
    private short[] _distances;
    private byte[] _validDirs;
    private readonly JPSRule _activeRule;
    private bool _usingJPSPlus = false;
    private JPSPathfinder(Rect2I usedRect, HeuristicType heuristicType, Func<int, int, int, int, int> customHeuristic, DiagonalType diagonalType)
        : base(usedRect, heuristicType, customHeuristic, diagonalType)
    {
        _activeRule = diagonalType == DiagonalType.WhenBothEmpty
                            ? new JPSRuleWhenBothEmpty()
                            : new JPSRuleWhenAtLeastOneEmpty();
        _activeRule.Precompute(_directionMasks);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool HasForcedNeighbor(int x, int y, int dx, int dy)
    {
        return _activeRule.HasForcedNeighbor(x, y, dx, dy, this);
    }
    private int Jump(int x, int y, int dx, int dy, int targetX, int targetY)
    {
        int curX = x + dx;
        int curY = y + dy;
        while (true)
        {
            if (!IsWalkable(curX, curY)) return -1;
            if (dx != 0 && dy != 0 && !CheckDiagonal(IsWalkable(curX - dx, curY), IsWalkable(curX, curY - dy))) return -1;
            if (curX == targetX && curY == targetY) return GetIndex(curX, curY);
            if (HasForcedNeighbor(curX, curY, dx, dy)) return GetIndex(curX, curY);
            if (dx != 0 && dy != 0)
            {
                int straightY = curY;
                bool found = false;
                while (!found)
                {
                    straightY += dy;
                    if (!IsWalkable(curX, straightY)) break;
                    if (curX == targetX && straightY == targetY) found = true;
                    if (HasForcedNeighbor(curX, straightY, 0, dy)) found = true;
                }

                int straightX = curX;
                while (!found)
                {
                    straightX += dx;
                    if (!IsWalkable(straightX, curY)) break;
                    if (straightX == targetX && curY == targetY) found = true;
                    if (HasForcedNeighbor(straightX, curY, dx, 0)) found = true;
                }
                if (found)
                {
                    return GetIndex(curX, curY);
                }
            }
            curX += dx;
            curY += dy;
        }
    }
    private byte GetWallMask(int x, int y)
    {
        byte result = 0;
        for (int i = 0; i < 8; i++)
        {
            if (!IsWalkable(x + DxArray[i], y + DyArray[i]))
            {
                result |= (byte)(1 << (7 - i));
            }
        }
        return result;
    }
    private bool IsInGeneralDirection(int dx, int dy, int fromX, int fromY, int toX, int toY)
    {
        int signX = Math.Sign(toX - fromX);
        int signY = Math.Sign(toY - fromY);
        return signX != -dx && signY != -dy;
    }
    protected override List<Vector2I> MainLoop(PathfindingContext context, PathNode targetNode, float heuristicScale)
    {
        var openList = context.OpenList;
        var isClosed = context.IsClosed;
        var grid = context.Grid;
        var targetIndex = GetIndex(targetNode.X, targetNode.Y);

        while (openList.Count != 0)
        {
            int curIndex = openList.Dequeue();
            ref PathNode cur = ref grid[curIndex];

            if (isClosed[curIndex]) continue;

            isClosed[curIndex] = true;
            if (cur.X == targetNode.X && cur.Y == targetNode.Y) return RetracePath(cur, context);

            byte directionMask;
            if (cur.ParentIndex == -1) directionMask = 0xFF;
            else
            {
                ref PathNode parent = ref grid[cur.ParentIndex];
                int dx = Math.Sign(cur.X - parent.X);
                int dy = Math.Sign(cur.Y - parent.Y);
                byte dir = GetDirIndex(dx, dy);
                directionMask = _usingJPSPlus
                                ? _validDirs[dir]
                                : _directionMasks[dir * 256 + GetWallMask(cur.X, cur.Y)];
            }

            for (int i = 0; i < 8 - BitOperations.TrailingZeroCount(directionMask); i++)
            {
                if ((directionMask & (1 << (7 - i))) == 0) continue;

                int jumpPoint = -1;
                int dx = DxArray[i];
                int dy = DyArray[i];
                if (!_usingJPSPlus)
                {
                    jumpPoint = Jump(cur.X, cur.Y, dx, dy, targetNode.X, targetNode.Y);
                }
                else
                {
                    int distArrayIndex = i * _width * _height + curIndex;
                    int xDiff = Math.Abs(targetNode.X - cur.X);
                    int yDiff = Math.Abs(targetNode.Y - cur.Y);
                    int xSign = Math.Sign(targetNode.X - cur.X);
                    int ySign = Math.Sign(targetNode.Y - cur.Y);
                    int dist = _distances[distArrayIndex];
                    int absDist = Math.Abs(dist);
                    if (i <= 3
                    && ((dy == 0 && dx == xSign && ySign == 0 && xDiff <= absDist)
                    || (dx == 0 && dy == ySign && xSign == 0 && yDiff <= absDist))
                    )
                    {
                        jumpPoint = targetIndex;
                    }
                    else if (i >= 4
                    && IsInGeneralDirection(dx, dy, cur.X, cur.Y, targetNode.X, targetNode.Y)
                    && (xDiff <= absDist || yDiff <= absDist))
                    {
                        int minDiff = Math.Min(xDiff, yDiff);
                        jumpPoint = curIndex + _width * minDiff * dy + minDiff * dx;
                    }
                    else if (dist > 0)
                    {
                        jumpPoint = curIndex + _width * dist * dy + dist * dx;
                    }
                }

                if (jumpPoint == -1) continue;

                UpdateNeighbor(context, cur, targetNode, jumpPoint, heuristicScale);
            }
        }
        return null;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsWalkableInGridCoord(int x, int y) => IsWalkable(x + _offset.X, y + _offset.Y);
    private byte[] CalculatePrimaryJumpPoint()
    {
        byte[] primaryJumpPointMasks = new byte[(_height * _width + 1) / 2];
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                if (!IsWalkableInGridCoord(x, y)) continue;

                int index = y * _width + x;
                ref byte mask = ref primaryJumpPointMasks[index / 2];
                // If even, use left part of the byte, otherwise use right part.
                byte shift = (byte)((1 - index % 2) * 4);
                // Scan S, N, E, W
                for (int i = 0; i < 4; i++)
                {
                    if (_activeRule.HasForcedNeighbor(x + _offset.X, y + _offset.Y, DxArray[i], DyArray[i], this))
                    {
                        mask |= (byte)(1 << (3 - i + shift));
                    }
                }
            }
        }
        return primaryJumpPointMasks;
    }
    private void CalculateStraightJumpPoint(byte[] primaryJumpPointMasks)
    {
        for (int i = 0; i < 2; i++)
        {
            int dirY = -DyArray[i];
            int startX = 0;
            int startY = dirY >= 0 ? 0 : _height - 1;
            int dx = 1;
            int dy = dirY >= 0 ? 1 : -1;

            // For vertical
            for (int x = startX; dx > 0 ? x < _width : x >= 0; x += dx)
            {
                short dist = -1;
                bool isJumpPointVisited = false;
                for (int y = startY; dy > 0 ? y < _height : y >= 0; y += dy)
                {
                    int gridIndex = y * _width + x;
                    int distArrayIndex = i * _width * _height + gridIndex;
                    if (!IsWalkableInGridCoord(x, y))
                    {
                        dist = -1;
                        isJumpPointVisited = false;
                        _distances[distArrayIndex] = 0;
                        continue;
                    }
                    dist++;
                    if (isJumpPointVisited)
                    {
                        _distances[distArrayIndex] = dist;
                    }
                    else
                    {
                        _distances[distArrayIndex] = (short)-dist;
                    }

                    byte shift = (byte)((1 - gridIndex % 2) * 4);
                    if ((primaryJumpPointMasks[gridIndex / 2] & (1 << (3 - i + shift))) != 0)
                    {

                        dist = 0;
                        isJumpPointVisited = true;
                    }
                }
            }
        }
        for (int i = 2; i < 4; i++)
        {
            int dirX = -DxArray[i];
            int startX = dirX >= 0 ? 0 : _width - 1;
            int startY = 0;
            int dx = dirX >= 0 ? 1 : -1;
            int dy = 1;

            // For horizontal
            for (int y = startY; dy > 0 ? y < _height : y >= 0; y += dy)
            {
                short dist = -1;
                bool isJumpPointVisited = false;
                for (int x = startX; dx > 0 ? x < _width : x >= 0; x += dx)
                {
                    int gridIndex = y * _width + x;
                    int distArrayIndex = i * _width * _height + gridIndex;
                    if (!IsWalkableInGridCoord(x, y))
                    {
                        dist = -1;
                        isJumpPointVisited = false;
                        _distances[distArrayIndex] = 0;
                        continue;
                    }
                    dist++;
                    if (isJumpPointVisited)
                    {
                        _distances[distArrayIndex] = dist;
                    }
                    else
                    {
                        _distances[distArrayIndex] = (short)-dist;
                    }

                    byte shift = (byte)((1 - gridIndex % 2) * 4);
                    if ((primaryJumpPointMasks[gridIndex / 2] & (1 << (3 - i + shift))) != 0)
                    {

                        dist = 0;
                        isJumpPointVisited = true;
                    }
                }
            }
        }
    }
    private void CalculateDiagonalJumpPointAndWallDistance()
    {
        (byte, byte)[] correspondingDirectionMasks = [
            (0, 2), // SE: S, E
            (1, 2),
            (0, 3),
            (1, 3)
        ];
        for (int i = 4; i < 8; i++)
        {
            int dx = -DxArray[i];
            int dy = -DyArray[i];
            int startX = dx >= 0 ? 0 : _width - 1;
            int startY = dy >= 0 ? 0 : _height - 1;
            for (int y = startY; dy > 0 ? y < _height : y >= 0; y += dy)
            {
                for (int x = startX; dx > 0 ? x < _width : x >= 0; x += dx)
                {
                    int gridIndex = y * _width + x;
                    int distArrayIndex = i * _width * _height + gridIndex;
                    if (!IsWalkableInGridCoord(x, y))
                    {
                        // _distances defaults to 0 so there's no need to assign a 0 to it.
                        continue;
                    }
                    bool sideA = IsWalkableInGridCoord(x - dx, y);
                    bool sideB = IsWalkableInGridCoord(x, y - dy);
                    if (!IsWalkableInGridCoord(x - dx, y - dy) || !CheckDiagonal(sideA, sideB))
                    {
                        _distances[distArrayIndex] = 0;
                        continue;
                    }
                    int forwardNodeGridIndex = (y - dy) * _width + (x - dx);
                    (byte dirA, byte dirB) = correspondingDirectionMasks[i - 4];
                    int distArrayIndexA = dirA * _width * _height + forwardNodeGridIndex;
                    int distArrayIndexB = dirB * _width * _height + forwardNodeGridIndex;
                    if (CheckDiagonal(sideA, sideB) && (_distances[distArrayIndexA] > 0 || _distances[distArrayIndexB] > 0))
                    {
                        _distances[distArrayIndex] = 1;
                        continue;
                    }
                    int forwardNodeDistArrayIndex = i * _width * _height + forwardNodeGridIndex;
                    short forwardNodeDist = _distances[forwardNodeDistArrayIndex];
                    short diffVal = (short)(forwardNodeDist > 0 ? 1 : -1);
                    _distances[distArrayIndex] = (short)(forwardNodeDist + diffVal);
                }
            }
        }
    }
    public void Precompute()
    {
        _usingJPSPlus = true;
        _distances = new short[_height * _width * 8];
        _validDirs = [
            0b10111010, // S: S, E, W, SE, SW
            0b01110101,
            0b11101100,
            0b11010011,
            0b10101000,
            0b01100100,
            0b10010010,
            0b01010001
        ];
        byte[] primaryJumpPointMasks = CalculatePrimaryJumpPoint();
        CalculateStraightJumpPoint(primaryJumpPointMasks);
        CalculateDiagonalJumpPointAndWallDistance();
    }
}
