using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AIDefinition;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

public class NPCAbilityDefinitionTests
{
    [Test]
    public void NPCAbilities_MatchBibleRecastAndResourceCosts()
    {
        var abilities = BuildNPCAbilities();
        var expected = ExpectedAbilities();

        abilities.Keys.Should().BeEquivalentTo(expected.Keys);

        foreach (var (feat, expectedAbility) in expected)
        {
            abilities.Should().ContainKey(feat);

            var ability = abilities[feat];
            ability.Name.Should().Be(expectedAbility.Name);
            ability.RecastGroup.Should().Be(expectedAbility.RecastGroup);
            ability.RecastDelay.Should().NotBeNull();
            ability.RecastDelay(0).Should().Be(expectedAbility.RecastSeconds);

            ability.Requirements
                .OfType<AbilityRequirementStamina>()
                .Should()
                .ContainSingle()
                .Which
                .RequiredSTM
                .Should()
                .Be(expectedAbility.StaminaCost);

            if (expectedAbility.FPCost.HasValue)
            {
                ability.Requirements
                    .OfType<AbilityRequirementFP>()
                    .Should()
                    .ContainSingle()
                    .Which
                    .RequiredFP
                    .Should()
                    .Be(expectedAbility.FPCost.Value);
            }
            else
            {
                ability.Requirements.OfType<AbilityRequirementFP>().Should().BeEmpty();
            }
        }
    }

    [Test]
    public void NPCAbilities_HaveMatchingFeat2daRows()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2daRows(root / "SWLOR_Haks" / "swlor2_2da" / "feat.2da");

        foreach (var feat in ExpectedAbilities().Keys)
        {
            featRows.Should().ContainKey((int)feat, $"{feat} must have a usable feat.2da row");
            featRows[(int)feat]["LABEL"].Should().Be(feat.ToString());
        }
    }

    [Test]
    public void NewResistanceThreatAbilities_UseFeatOnly2daRowsWithSharedNpcSpell()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2daRows(root / "SWLOR_Haks" / "swlor2_2da" / "feat.2da");
        var spellRows = Read2daRows(root / "SWLOR_Haks" / "swlor2_2da" / "spells.2da");

        spellRows[904]["Label"].Should().Be("Bite", "NPC innate abilities reuse the shared feat-activation spell row");

        foreach (var feat in NewResistanceThreatAbilities())
        {
            featRows[(int)feat]["LABEL"].Should().Be(feat.ToString());
            featRows[(int)feat]["SPELLID"].Should().Be("904");
            spellRows.Values.Should().NotContain(
                row => row.ContainsKey("Label") && row["Label"] == feat.ToString(),
                $"{feat} should not require a separate spells.2da row");
        }
    }

    [Test]
    public void NPCAbilities_AreAvailableToDefaultNpcAI()
    {
        Ability.CacheData();
        var profiles = new DefaultAIProfileDefinition().BuildProfiles();
        var genericAbilityFeats = profiles[AIProfileType.Generic].Actions
            .Where(action => action.Type == AIActionType.Ability)
            .Select(action => action.Feat)
            .ToHashSet();

        foreach (var feat in ExpectedAbilities().Keys)
        {
            Ability.IsFeatRegistered(feat).Should().BeTrue($"{feat} must be registered before AI can execute it");
            genericAbilityFeats.Should().Contain(feat, $"{feat} should be exposed through the Generic NPC AI profile");
        }
    }

    private static Dictionary<FeatType, AbilityDetail> BuildNPCAbilities()
    {
        var definitionType = typeof(IAbilityListDefinition);
        var npcAbilityNamespace = typeof(ArcPulseAbilityDefinition).Namespace;
        var abilities = new Dictionary<FeatType, AbilityDetail>();

        var definitions = typeof(ArcPulseAbilityDefinition)
            .Assembly
            .GetTypes()
            .Where(type =>
                type.IsClass &&
                !type.IsAbstract &&
                type.Namespace == npcAbilityNamespace &&
                definitionType.IsAssignableFrom(type))
            .Select(type => (IAbilityListDefinition)Activator.CreateInstance(type)!)
            .ToList();

        foreach (var definition in definitions)
        {
            foreach (var (feat, ability) in definition.BuildAbilities())
            {
                abilities.Add(feat, ability);
            }
        }

        return abilities;
    }

    private static Dictionary<FeatType, ExpectedNPCAbility> ExpectedAbilities()
    {
        return new Dictionary<FeatType, ExpectedNPCAbility>
        {
            [FeatType.RendingBite] = new("Rending Bite", RecastGroup.RendingBite, 14f, 3),
            [FeatType.CripplingTalons] = new("Crippling Talons", RecastGroup.CripplingTalons, 10f, 3),
            [FeatType.PiercingQuills] = new("Piercing Quills", RecastGroup.PiercingQuills, 18f, 5),
            [FeatType.ToxicSpit] = new("Toxic Spit", RecastGroup.ToxicSpit, 18f, 3),
            [FeatType.ScorchingBreath] = new("Scorching Breath", RecastGroup.ScorchingBreath, 22f, 5),
            [FeatType.InfernoBlast] = new("Inferno Blast", RecastGroup.InfernoBlast, 34f, 8),
            [FeatType.SeismicSlam] = new("Seismic Slam", RecastGroup.SeismicSlam, 28f, 6),
            [FeatType.RupturingQuake] = new("Rupturing Quake", RecastGroup.RupturingQuake, 48f, 10),
            [FeatType.TerrifyingBellow] = new("Terrifying Bellow", RecastGroup.TerrifyingBellow, 20f, 4),
            [FeatType.DisorientingScreech] = new("Disorienting Screech", RecastGroup.DisorientingScreech, 36f, 7),
            [FeatType.IronCarapace] = new("Iron Carapace", RecastGroup.IronCarapace, 32f, 4),
            [FeatType.MaulingBite] = new("Mauling Bite", RecastGroup.MaulingBite, 13f, 4),
            [FeatType.BonecrusherBite] = new("Bonecrusher Bite", RecastGroup.BonecrusherBite, 16f, 5),
            [FeatType.RakingClaws] = new("Raking Claws", RecastGroup.RakingClaws, 11f, 3),
            [FeatType.PouncingStrike] = new("Pouncing Strike", RecastGroup.PouncingStrike, 18f, 5),
            [FeatType.TailSweep] = new("Tail Sweep", RecastGroup.TailSweep, 18f, 5),
            [FeatType.GoringCharge] = new("Goring Charge", RecastGroup.GoringCharge, 22f, 6),
            [FeatType.BarbedVolley] = new("Barbed Volley", RecastGroup.BarbedVolley, 17f, 5),
            [FeatType.VenomSpray] = new("Venom Spray", RecastGroup.VenomSpray, 18f, 5),
            [FeatType.ToxicCloud] = new("Toxic Cloud", RecastGroup.ToxicCloud, 24f, 7),
            [FeatType.FrostSpit] = new("Frost Spit", RecastGroup.FrostSpit, 24f, 2),
            [FeatType.StaticBurst] = new("Static Burst", RecastGroup.StaticBurst, 20f, 6),
            [FeatType.SavageRoar] = new("Savage Roar", RecastGroup.SavageRoar, 21f, 4),
            [FeatType.SonicShriek] = new("Sonic Shriek", RecastGroup.SonicShriek, 19f, 5),
            [FeatType.ChitinGuard] = new("Chitin Guard", RecastGroup.ChitinGuard, 32f, 5),
            [FeatType.PrecisionShot] = new("Precision Shot", RecastGroup.PrecisionShot, 15f, 4),
            [FeatType.SuppressingShot] = new("Suppressing Shot", RecastGroup.SuppressingShot, 17f, 4),
            [FeatType.GrenadeBurst] = new("Grenade Burst", RecastGroup.GrenadeBurst, 22f, 6),
            [FeatType.SerratedSlash] = new("Serrated Slash", RecastGroup.SerratedSlash, 15f, 4),
            [FeatType.BrutalBash] = new("Brutal Bash", RecastGroup.BrutalBash, 16f, 4),
            [FeatType.TacticalMark] = new("Tactical Mark", RecastGroup.TacticalMark, 20f, 4),
            [FeatType.OverloadShot] = new("Overload Shot", RecastGroup.OverloadShot, 17f, 5),
            [FeatType.ArcPulse] = new("Arc Pulse", RecastGroup.ArcPulse, 20f, 6),
            [FeatType.IonBurst] = new("Ion Burst", RecastGroup.IonBurst, 18f, 5),
            [FeatType.TargetLock] = new("Target Lock", RecastGroup.TargetLock, 20f, 3),
            [FeatType.ShrapnelBurst] = new("Shrapnel Burst", RecastGroup.ShrapnelBurst, 20f, 6),
            [FeatType.ForceRend] = new("Force Rend", RecastGroup.ForceRend, 16f, 5),
            [FeatType.MindSpike] = new("Mind Spike", RecastGroup.MindSpike, 18f, 4),
            [FeatType.DarkShock] = new("Dark Shock", RecastGroup.DarkShock, 24f, 7),
            [FeatType.DreadWave] = new("Dread Wave", RecastGroup.DreadWave, 24f, 6),
            [FeatType.GlacialSlime] = new("Glacial Slime", RecastGroup.GlacialSlime, 17f, 4),
            [FeatType.HoarfrostGlob] = new("Hoarfrost Glob", RecastGroup.HoarfrostGlob, 16f, 4),
            [FeatType.PermafrostRupture] = new("Permafrost Rupture", RecastGroup.PermafrostRupture, 26f, 7),
            [FeatType.RimePounce] = new("Rime Pounce", RecastGroup.RimePounce, 15f, 5),
            [FeatType.CryoBile] = new("Cryo Bile", RecastGroup.CryoBile, 24f, 8),
            [FeatType.CapacitorSurge] = new("Capacitor Surge", RecastGroup.CapacitorSurge, 20f, 5),
            [FeatType.StaticWeb] = new("Static Web", RecastGroup.StaticWeb, 22f, 6),
            [FeatType.ForceSunder] = new("Force Sunder", RecastGroup.ForceSunder, 18f, 6),
            [FeatType.NullShock] = new("Null Shock", RecastGroup.NullShock, 24f, 7),
        };
    }

    private static FeatType[] NewResistanceThreatAbilities()
    {
        return new[]
        {
            FeatType.GlacialSlime,
            FeatType.HoarfrostGlob,
            FeatType.PermafrostRupture,
            FeatType.RimePounce,
            FeatType.CryoBile,
            FeatType.CapacitorSurge,
            FeatType.StaticWeb,
            FeatType.ForceSunder,
            FeatType.NullShock
        };
    }

    private sealed record ExpectedNPCAbility(
        string Name,
        RecastGroup RecastGroup,
        float RecastSeconds,
        int StaminaCost,
        int? FPCost = null);

    private static Dictionary<int, Dictionary<string, string>> Read2daRows(PathInfo path)
    {
        var lines = File.ReadAllLines(path.FullName)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();
        var header = lines[1].Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
        var result = new Dictionary<int, Dictionary<string, string>>();

        foreach (var line in lines.Skip(2))
        {
            var cells = line.Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
            if (!int.TryParse(cells[0], out var row))
                continue;

            var values = new Dictionary<string, string>();
            for (var index = 0; index < header.Length && index + 1 < cells.Length; index++)
            {
                values[header[index]] = cells[index + 1];
            }

            result[row] = values;
        }

        return result;
    }

    private static PathInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            var candidate = directory.FullName;
            if (File.Exists(Path.Combine(candidate, "SWLOR.Game.Server.sln")) &&
                File.Exists(Path.Combine(candidate, "SWLOR_Haks", "swlor2_2da", "feat.2da")))
            {
                return new PathInfo(candidate);
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }

    private sealed record PathInfo(string FullName)
    {
        public static PathInfo operator /(PathInfo path, string child)
        {
            return new PathInfo(Path.Combine(path.FullName, child));
        }
    }
}
