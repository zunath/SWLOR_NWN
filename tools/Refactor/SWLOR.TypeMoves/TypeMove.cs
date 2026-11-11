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
    /// <see cref="To"/> relative to the namespace that encloses <see cref="From"/> (<c>Domain.Foundation</c>): what replaces the
    /// last segment of a qualifier that named <see cref="From"/> (<c>Service.Log</c> becomes <c>Domain.Foundation.Log</c>).
    /// </summary>
    public string RelativeTarget => To[(Parent(From).Length + 1)..];

    /// <summary>The repository folder of a namespace of the server (<c>SWLOR.Game.Server/Domain/Foundation</c>).</summary>
    public static string FolderOf(string @namespace) =>
        @namespace == ServerRoot ? ServerRoot : ServerRoot + "/" + @namespace[(ServerRoot.Length + 1)..].Replace('.', '/');

    public static string Parent(string @namespace)
    {
        var dot = @namespace.LastIndexOf('.');
        return dot < 0 ? string.Empty : @namespace[..dot];
    }

    /// <summary>The last segment of a namespace (<c>Service</c>).</summary>
    public static string Leaf(string @namespace) => @namespace[(@namespace.LastIndexOf('.') + 1)..];
}
