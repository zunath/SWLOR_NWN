using System.Reflection;
using NUnit.Framework;

namespace SWLOR.Game.Server.CharacterizationTests.Support;

/// <summary>
/// Keeps a "public constants" snapshot written as an explicit list complete. The lists replaced a reflection over the
/// facade (P2-ZA): once a facade is removed its constants live on their owners (<c>CombatRules</c>, ...), and the facade
/// removal tool respells each read in the list, both ways, so the same test compiles on the branch and, reversed, on
/// master. While the static class still exists (on master, and on the branch until its facade is removed) it is looked
/// up by name and the list must name every public constant it has, so a constant master adds shows up in the parity run.
/// </summary>
public static class FacadeConstants
{
    public static void AssertListsEveryConstant(string facadeFullName, IEnumerable<string> listed)
    {
        var facade = Type.GetType(facadeFullName + ", SWLOR.Game.Server");
        if (facade == null)
            return;

        var constants = facade.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && !field.IsInitOnly)
            .Select(field => field.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.That(listed.ToList(), Is.EqualTo(constants),
            $"The constants snapshot of {facadeFullName} must list every public constant of the class, sorted by name.");
    }
}
