namespace SWLOR.ContentInjection;

/// <summary>What a file's project is to the conversion: it decides how a creator of made content gets <c>Content</c>.</summary>
internal enum ProjectKind
{
    /// <summary>A project that neither is the server nor references it: never touched.</summary>
    Unrelated,

    /// <summary><c>SWLOR.Game.Server</c>.</summary>
    Server,

    /// <summary><c>SWLOR.Game.Server.EngineTests</c>: unconverted classes use the temporary <c>GameServices.Content</c> until they are converted.</summary>
    EngineTests,

    /// <summary>A unit-test project: a fixture declares <c>Content</c> from its own container, never a static lookup.</summary>
    UnitTests,

    /// <summary>Any other project that references the server (CombatSim, CraftSim, CLI, Admin, the toolset).</summary>
    Tool
}
