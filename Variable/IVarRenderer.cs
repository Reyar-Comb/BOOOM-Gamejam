public interface IVarRenderer
{
    Var RenderedVar { get; set; }
    bool RenderVarBody { get; set; }
    bool RenderAttackRange { get; set; }
    bool RenderDetectRange { get; set; }
    bool RenderDirection { get; set; }
    bool InterpolateRenderPosition { get; set; }

    void SetVar(Var var);
    void ClearVar();
}
