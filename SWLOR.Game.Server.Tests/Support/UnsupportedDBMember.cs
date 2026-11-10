namespace SWLOR.Game.Server.Tests.Support;

/// <summary>The refusal a fake <see cref="SWLOR.Game.Server.Domain.Foundation.IDBService"/> gives for a member it does not model.</summary>
public static class UnsupportedDBMember
{
    public static NotSupportedException For(string member) =>
        new($"This IDBService does not implement {member}.");
}
