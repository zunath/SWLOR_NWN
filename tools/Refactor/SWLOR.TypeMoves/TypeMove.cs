namespace SWLOR.TypeMoves;

/// <summary>One type that moves from the namespace <see cref="From"/> to the namespace <see cref="To"/>, with its file.</summary>
/// <param name="Name">The type's simple name (<c>Log</c>).</param>
/// <param name="From">The namespace it leaves (<c>SWLOR.Game.Server.Service</c>).</param>
/// <param name="To">The namespace it joins (<c>SWLOR.Game.Server.Domain.Foundation</c>).</param>
/// <param name="File">The file that declares it (<c>Log.cs</c>), which keeps its name.</param>
internal sealed record TypeMove(string Name, string From, string To, string File)
{
    /// <summary>The server's root namespace, and the name of its project folder.</summary>
    public const string ServerRoot = "SWLOR.Game.Server";

    public string OldFullName => From + "." + Name;

    public string NewFullName => To + "." + Name;

    /// <summary>The repository path of the file before the move (<c>SWLOR.Game.Server/Service/Log.cs</c>).</summary>
    public string SourcePath => FolderOf(From) + "/" + File;

    /// <summary>The repository path of the file after the move (<c>SWLOR.Game.Server/Domain/Foundation/Log.cs</c>).</summary>
    public string TargetPath => FolderOf(To) + "/" + File;

    /// <summary>
    /// The deepest namespace that encloses both <see cref="From"/> and <see cref="To"/> and is neither of them
    /// (<c>SWLOR.Game.Server</c> for <c>Service</c> to <c>Domain.Foundation</c>, <c>SWLOR.Game.Server.Domain</c> for
    /// <c>Domain.Crafting</c> to <c>Domain.Foundation</c>): where a qualifier that spells <see cref="From"/> and one that spells
    /// <see cref="To"/> start to differ.
    /// </summary>
    public string CommonParent => CommonParentOf(From, To);

    /// <summary><see cref="From"/> below <see cref="CommonParent"/> (<c>Service</c>, <c>Crafting</c>, <c>Feature.AbilityDefinition</c>).</summary>
    public string RelativeSource => From[(CommonParent.Length + 1)..];

    /// <summary>
    /// <see cref="To"/> below <see cref="CommonParent"/> (<c>Domain.Foundation</c>, <c>Foundation</c>, <c>Domain.Combat</c>): what
    /// replaces the segments of a qualifier that spell <see cref="RelativeSource"/> (<c>Service.Log</c> becomes
    /// <c>Domain.Foundation.Log</c>, <c>Crafting.RecipeType</c> becomes <c>Foundation.RecipeType</c>).
    /// </summary>
    public string RelativeTarget => To[(CommonParent.Length + 1)..];

    /// <summary>The repository folder of a namespace of the server (<c>SWLOR.Game.Server/Domain/Foundation</c>).</summary>
    public static string FolderOf(string @namespace) =>
        @namespace == ServerRoot ? ServerRoot : ServerRoot + "/" + BelowRoot(@namespace).Replace('.', '/');

    /// <summary>A namespace of the server without the root (<c>Domain.Foundation</c>): how guides and scripts write it.</summary>
    public static string BelowRoot(string @namespace) => @namespace[(ServerRoot.Length + 1)..];

    public static string Parent(string @namespace)
    {
        var dot = @namespace.LastIndexOf('.');
        return dot < 0 ? string.Empty : @namespace[..dot];
    }

    /// <summary>The last segment of a namespace (<c>Service</c>).</summary>
    public static string Leaf(string @namespace) => @namespace[(@namespace.LastIndexOf('.') + 1)..];

    /// <summary>The deepest common enclosing namespace of two namespaces, above both (see <see cref="CommonParent"/>).</summary>
    public static string CommonParentOf(string from, string to)
    {
        var a = from.Split('.');
        var b = to.Split('.');
        var common = 0;
        while (common < a.Length && common < b.Length && a[common] == b[common])
            common++;
        common = Math.Min(common, Math.Min(a.Length, b.Length) - 1);
        return string.Join(".", a.Take(common));
    }
}
