namespace SWLOR.FacadeRemoval;

/// <summary>
/// A static facade of a converted service that the branch deleted (decision D1 of
/// <c>Readmes/MaintainabilityRefactorFacadeRemoval.md</c>). Static callers reach the service through the property of the
/// same name on <c>SWLOR.Game.Server.Composition.GameServices</c>.
/// </summary>
/// <param name="Facade">The facade type relative to <c>SWLOR.Game.Server.Service</c>: <c>Taxi</c>, or <c>QuestService.QuestEncounter</c> for a further facade.</param>
/// <param name="Explicit">
/// True for a name that is also a namespace segment (<c>Combat</c>, <c>Mimicry</c>, <c>Performance</c>, <c>Prospecting</c>):
/// callers write <c>GameServices.Combat.X</c>, because a namespace of the same name would hide the imported property.
/// </param>
/// <param name="Constants">The facade's public constants and the classes that now hold them.</param>
internal sealed record RemovedFacade(string Facade, bool Explicit, IReadOnlyList<RemovedConstant> Constants)
{
    public const string ServiceNamespace = "SWLOR.Game.Server.Service";

    /// <summary>The simple name callers write and the name of the <c>GameServices</c> property.</summary>
    public string Name => Facade[(Facade.LastIndexOf('.') + 1)..];

    /// <summary>The facade's full type name, which its handler names start with.</summary>
    public string FullName => ServiceNamespace + "." + Facade;

    /// <summary>The facade's source file, relative to the repository root. While it exists the facade is not removed in that tree.</summary>
    public string SourcePath => "SWLOR.Game.Server/Service/" + Facade.Replace('.', '/') + ".cs";

    public RemovedConstant? Constant(string name) => Constants.FirstOrDefault(constant => constant.Name == name);
}
