using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

public class PistolProjectilePresentationTests
{
    [TestCase(0, BaseItem.Sling, true)]
    [TestCase(5, BaseItem.Sling, true)]
    [TestCase(6, BaseItem.Sling, false)]
    [TestCase(7, BaseItem.Sling, false)]
    [TestCase(0, BaseItem.Pistol, false)]
    [TestCase(0, BaseItem.LegacyPistol, false)]
    public void OnlyCanonicalPistolWeaponProjectiles_AreReplaced(
        int projectileType,
        BaseItem rightHandBaseItem,
        bool expected)
    {
        var method = typeof(PistolProjectilePresentation).GetMethod(
            "ShouldReplaceProjectile",
            BindingFlags.NonPublic | BindingFlags.Static)!;

        var result = (bool)method.Invoke(null, new object[] { projectileType, rightHandBaseItem })!;

        result.Should().Be(expected);
    }

    [Test]
    public void Presentation_UsesTheExistingArrowBlasterWithoutAnImpactScript()
    {
        var projectileSpell = typeof(PistolProjectilePresentation).GetField(
            "ProjectileSpell",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        var impactScript = typeof(PistolProjectilePresentation).GetField(
            "NoImpactScript",
            BindingFlags.NonPublic | BindingFlags.Static)!;

        ((Spell)(int)projectileSpell.GetRawConstantValue()!).Should().Be(Spell.Trap_Arrow);
        impactScript.GetRawConstantValue().Should().Be("****");
    }

    [Test]
    public void TrapArrowProjectile_IsTheSingleStraightBlasterBolt()
    {
        var root = FindRepositoryRoot();
        var row = Read2daRow(
            Path.Combine(root.FullName, "SWLOR_Haks", "sw_2da", "spells.2da"),
            (int)Spell.Trap_Arrow);

        row["Proj"].Should().Be("1");
        row["ProjModel"].Should().Be("wamar_001");
        row["ProjType"].Should().Be("linked");
        row["ProjSpwnPoint"].Should().Be("hand");
        row["HasProjectile"].Should().Be("1");
    }

    private static Dictionary<string, string> Read2daRow(string path, int requestedRow)
    {
        var lines = File.ReadAllLines(path)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();
        var header = lines[1].Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines.Skip(2))
        {
            var cells = line.Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
            if (!int.TryParse(cells[0], out var row) || row != requestedRow)
                continue;

            var values = new Dictionary<string, string>();
            for (var index = 0; index < header.Length && index + 1 < cells.Length; index++)
            {
                values[header[index]] = cells[index + 1];
            }

            return values;
        }

        throw new InvalidOperationException($"Could not find row {requestedRow} in {path}.");
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")) &&
                File.Exists(Path.Combine(directory.FullName, "SWLOR_Haks", "sw_2da", "spells.2da")))
            {
                return directory;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }
}
