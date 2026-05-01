using Godot;
using System;
public static class Vector2Extensions
{
    public static Vector2I ToFacingDirection(this Vector2 direction)
    {
        if (direction.LengthSquared() <= MathConstants.EpsilonSquared)
        {
            return Vector2I.Down;
        }

        Vector2 normalizedDirection = direction.Normalized();
        return Mathf.Abs(normalizedDirection.X) >= Mathf.Abs(normalizedDirection.Y)
            ? new Vector2I(Mathf.Sign(normalizedDirection.X) > 0.0f ? 1 : -1, 0)
            : new Vector2I(0, Mathf.Sign(normalizedDirection.Y) > 0.0f ? 1 : -1);
    }
}
