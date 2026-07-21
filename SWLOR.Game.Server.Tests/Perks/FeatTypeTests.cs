using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class FeatTypeTests
{
    [Test]
    public void FeatType_HasNoDuplicateValues()
    {
        // (int)FeatType.X must equal the feat.2da row number, so two members sharing a
        // value collapse onto a single row: one of them renders the other's icon, name
        // and description. It also makes Enum.GetName non-deterministic for that value,
        // which churns generated artifacts such as
        // Readmes/CombatUpgradeBibleImplementationReview.csv.
        //
        // Enum.GetName is unusable here for the same reason it is the symptom: it returns
        // an arbitrary one of the colliding names. Map each declared name back to its
        // value instead, so both sides of a collision are visible.
        var duplicates = System.Enum.GetNames<FeatType>()
            .Select(name => (Name: name, Value: (int)System.Enum.Parse<FeatType>(name)))
            .GroupBy(x => x.Value)
            .Where(x => x.Count() > 1)
            .OrderBy(x => x.Key)
            .Select(x => $"{x.Key} = {string.Join(", ", x.Select(y => y.Name).OrderBy(y => y))}")
            .ToArray();

        duplicates.Should().BeEmpty(
            "each FeatType must map to its own feat.2da row, but these values are shared:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, duplicates));
    }
}
