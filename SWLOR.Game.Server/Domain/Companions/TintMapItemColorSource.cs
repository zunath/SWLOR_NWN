namespace SWLOR.Game.Server.Domain.Companions
{
    public readonly record struct TintMapItemColorSource(
        string MaterialResref,
        string VariableName,
        int? StoredValue);
}
