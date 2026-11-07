using Microsoft.CodeAnalysis.CSharp.Syntax;
using SWLOR.Game.Server.Tests.Service;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// Methods of the stat instance class for the Roslyn harnesses that compile them against their own stand-ins: as static
/// methods, with each injected service spelled as the static stand-in the harness declares.
/// </summary>
internal static class StatHarnessSource
{
    private const string InstanceFile = "Domain/Character/StatService.cs";

    public static IEnumerable<string> Methods(Func<MethodDeclarationSyntax, bool> select) =>
        DMPossessionHarness.InstanceMethods(InstanceFile, select).Select(Respell);

    private static string Respell(string source) => source
        .Replace("_db.", "DB.")
        .Replace("_combatRecorder.", "CombatRecorder.")
        .Replace("_playerFeedback.", "PlayerFeedback.")
        .Replace("_beastMastery.", "BeastMastery.")
        .Replace("_powerGloveEquipment.", "PowerGloveEquipment.");
}
