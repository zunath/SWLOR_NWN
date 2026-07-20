namespace SWLOR.Toolset.Domain.Workspace
{
    /// <summary>
    /// The module resource kinds a <see cref="ModuleWorkspace"/> knows how to enumerate and load:
    /// areas (.are, paired with .git/.gic) and every blueprint type this package supports.
    /// </summary>
    public enum ResourceType
    {
        Area,
        Utc,
        Uti,
        Utp,
        Utd,
        Utm,
        Utt,
        Uts,
        Utw
    }

    /// <summary>
    /// File-naming conventions for <see cref="ResourceType"/>: the module subfolder name and the
    /// blueprint/area file extension are identical for every type this package supports (e.g.
    /// "utc" folder holds "*.utc.json" files), so one string covers both.
    /// </summary>
    public static class ResourceTypeExtensions
    {
        public static string Extension(this ResourceType type)
        {
            return type switch
            {
                ResourceType.Area => "are",
                ResourceType.Utc => "utc",
                ResourceType.Uti => "uti",
                ResourceType.Utp => "utp",
                ResourceType.Utd => "utd",
                ResourceType.Utm => "utm",
                ResourceType.Utt => "utt",
                ResourceType.Uts => "uts",
                ResourceType.Utw => "utw",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown resource type.")
            };
        }
    }
}
