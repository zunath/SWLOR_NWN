namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The source of <c>WeaponActiveAbilityDefinitionBase</c> wherever it lives, for the tests that guard a rule by reading the code. The weapon
/// ability split (<c>Readmes/MaintainabilityRefactorWeaponAbilitySplit.md</c>) moves the text of <c>Feature/AbilityDefinition/WeaponActiveAbilityDefinitionBase.cs</c>
/// into the parts of <c>Feature/AbilityDefinition/WeaponAbility/</c> and the profile into its own file; the change that moves a part adds its
/// file to <see cref="PartFiles"/> and its type to <see cref="PartInterfaces"/>, so no test names a path.
/// </summary>
public static class WeaponAbilitySource
{
    /// <summary>The files that hold the code of the base as it was before the split, repository-relative, in reading order.</summary>
    public static readonly IReadOnlyList<string> PartFiles = new[]
    {
        "SWLOR.Game.Server/Feature/AbilityDefinition/WeaponActiveAbilityDefinitionBase.cs",
        "SWLOR.Game.Server/Feature/AbilityDefinition/WeaponAbilityProfile.cs",
        "SWLOR.Game.Server/Feature/AbilityDefinition/WeaponAbility/StatusAbilityEffects.cs",
        "SWLOR.Game.Server/Feature/AbilityDefinition/WeaponAbility/WeaponAbilityTemporaryModifiers.cs",
        "SWLOR.Game.Server/Feature/AbilityDefinition/WeaponAbility/WeaponAbilityStatusRiders.cs",
        "SWLOR.Game.Server/Feature/AbilityDefinition/WeaponAbility/WeaponAbilityDamageModifiers.cs",
        "SWLOR.Game.Server/Feature/AbilityDefinition/WeaponAbility/WeaponAbilitySelfEffects.cs",
        "SWLOR.Game.Server/Feature/AbilityDefinition/WeaponAbility/WeaponAbilityHitEffects.cs",
        "SWLOR.Game.Server/Feature/AbilityDefinition/WeaponAbility/WeaponAbilityHitRiders.cs",
        "SWLOR.Game.Server/Feature/AbilityDefinition/WeaponAbility/WeaponAbilityImpactEffects.cs",
        "SWLOR.Game.Server/Feature/AbilityDefinition/WeaponAbility/WeaponAbilityFriendlyTargets.cs",
        "SWLOR.Game.Server/Feature/AbilityDefinition/WeaponAbility/WeaponAbilityActivations.cs",
        "SWLOR.Game.Server/Feature/AbilityDefinition/WeaponAbility/WeaponAbilityImpacts.cs",
    };

    /// <summary>The types of the parts: a call through a field of one of these types was a call on the same instance before the move.</summary>
    public static readonly IReadOnlySet<string> PartInterfaces = new HashSet<string>(StringComparer.Ordinal);

    private static readonly PartSourceFiles Files = new(PartFiles, PartInterfaces);

    /// <summary>The combined source of the base, with LF line ends.</summary>
    public static string Read() => Files.Read().Replace("\r\n", "\n");

    /// <summary>The full paths of the files <see cref="Read"/> combines.</summary>
    public static IReadOnlyList<string> Paths() => Files.Paths();

    /// <summary>The method of this name that holds the code.</summary>
    public static Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax Method(string name) => Files.Method(name);
}
