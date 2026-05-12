using Godot;

internal sealed class VarRenderState
{
    public Vector2 DisplayPosition;
    public Vector2 LastObservedPosition;
    public Vector2 InterpolationStartPosition;
    public Vector2 InterpolationTargetPosition;
    public double InterpolationElapsed;
    public double InterpolationDuration;
    public double TimeSinceLastPositionChange;
    public double TimeSinceInterpolationFinished;
    public bool HasInterpolationState;
}
