namespace SWLOR.Toolset.Domain.Workspace
{
    /// <summary>One blueprint-backed object placed in an area's GIT instance list.</summary>
    public sealed record ObjectPlacement(
        ResourceType BlueprintType,
        string BlueprintResRef,
        string AreaResRef,
        int InstanceIndex,
        string Tag,
        float X,
        float Y,
        float Z);
}
