using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Pistol;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class FanTheHammerTargetingTests
{
    private static readonly (FeatType Feat, Spell Spell)[] Ranks =
    {
        (FeatType.FanTheHammer1, Spell.FanTheHammer1),
        (FeatType.FanTheHammer2, Spell.FanTheHammer2)
    };

    [Test]
    public void BothRanks_DeclareTenMetreForwardConesMatchingClientTargeting()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2daRows(Path.Combine(root, "SWLOR_Haks", "sw_2da", "feat.2da"));
        var spellRows = Read2daRows(Path.Combine(root, "SWLOR_Haks", "sw_2da", "spells.2da"));
        var abilities = new FanTheHammerAbilityDefinition().BuildAbilities();

        foreach (var (feat, spell) in Ranks)
        {
            var ability = abilities[feat];
            var targeting = ability.Targeting;
            targeting.Should().NotBeNull($"{feat} must retain its client targeting metadata");
            targeting!.UpdatesClientTargeting.Should().BeTrue();
            targeting.Spell.Should().Be(spell);
            targeting.Shape.Should().Be(AbilityTargetingShapeType.Cone);
            targeting.SizeX.Should().Be(10f);
            targeting.SizeY.Should().Be(10f);
            targeting.Flags.Should().Be(
                AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf);
            ability.RequiresTarget.Should().BeFalse();
            ability.RequiresLocationTarget.Should().BeTrue();

            var featRow = featRows[(int)feat];
            featRow["TARGETSELF"].Should().Be("****", "the client must let the player aim the cone");
            featRow["HostileFeat"].Should().Be("1");
            int.Parse(featRow["SPELLID"], CultureInfo.InvariantCulture).Should().Be((int)spell);

            var spellRow = spellRows[(int)spell];
            spellRow["TargetShape"].Should().Be("cone");
            ParseSize(spellRow["TargetSizeX"]).Should().Be(10f);
            ParseSize(spellRow["TargetSizeY"]).Should().Be(10f);
            int.Parse(spellRow["TargetFlags"], CultureInfo.InvariantCulture)
                .Should().Be((int)(AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf));
            spellRow["TargetType"].Should().Be("0x3E", "the client spell row must accept a directional target");
        }
    }

    [TestCase(8f, 0f, true, TestName = "TargetEightMetresAheadIsInside")]
    [TestCase(10.01f, 0f, false, TestName = "TargetBeyondTenMetresIsOutside")]
    [TestCase(-1f, 0f, false, TestName = "TargetBehindCasterIsOutside")]
    [TestCase(5f, 3f, false, TestName = "TargetOutsideConeWidthIsOutside")]
    public void BothRanks_CombatConeIncludesOnlyTargetsInForwardRange(float x, float y, bool expected)
    {
        var abilityShape = typeof(SWLOR.Game.Server.Service.Ability)
            .GetMethod("IsPositionInCombatImpactShape", BindingFlags.NonPublic | BindingFlags.Static);
        abilityShape.Should().NotBeNull("Fan the Hammer uses the shared combat impact shape matcher");

        var abilities = new FanTheHammerAbilityDefinition().BuildAbilities();
        foreach (var (feat, _) in Ranks)
        {
            var targeting = abilities[feat].Targeting;
            targeting.Shape.Should().Be(AbilityTargetingShapeType.Cone);

            var inside = (bool)abilityShape!.Invoke(null, new object[]
            {
                new Vector3(x, y, 0f),
                Vector3.Zero,
                0f,
                CombatImpactAreaShape.Cone,
                targeting.SizeX,
                targeting.SizeY
            })!;

            inside.Should().Be(expected, $"{feat} targeting metadata defines this cone");
        }
    }

    private static float ParseSize(string value) => value == "****"
        ? 0f
        : float.Parse(value, CultureInfo.InvariantCulture);

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")) &&
                File.Exists(Path.Combine(directory.FullName, "SWLOR_Haks", "sw_2da", "feat.2da")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }

    private static Dictionary<int, Dictionary<string, string>> Read2daRows(string path)
    {
        var lines = File.ReadAllLines(path).Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
        var headers = lines[1].Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
        var result = new Dictionary<int, Dictionary<string, string>>();

        foreach (var line in lines.Skip(2))
        {
            var cells = line.Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
            if (!int.TryParse(cells[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var row))
                continue;

            var values = new Dictionary<string, string>();
            for (var index = 0; index < headers.Length && index + 1 < cells.Length; index++)
                values[headers[index]] = cells[index + 1];

            result[row] = values;
        }

        return result;
    }
}
