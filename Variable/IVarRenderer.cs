public interface IVarRenderer
{
    void Initialize(MapData mapData);
    void AddVar(Var var);
    void RemoveVar(Var var);
    void ClearVars();
}
