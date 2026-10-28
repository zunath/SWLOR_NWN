using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.ShipModuleDefinition;
using SWLOR.Game.Server.Feature.SpaceObjectDefinition;
using SWLOR.Game.Server.Feature.SpawnDefinition;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The role ships of every faction tier: their presets, support modules, spawn tables and the DM tools' squad picker.
/// </summary>
public class SpaceNpcRoleShipTests
{
    private static readonly string[] RoleHulls = { "ewar", "sniper", "minelayer", "logistics", "carrier" };

    private static readonly Dictionary<int, string[]> OrbitsByTier = new()
    {
        [1] = new[] { "SPACE_VISCARA_ORBIT", "SPACE_NARSHADDAA_ORBIT", "SPACE_VISCARA_BELT" },
        [2] = new[] { "SPACE_MONCALA_ORBIT", "SPACE_VISCARA_BELT" },
        [3] = new[] { "SPACE_HUTLAR_ORBIT", "SPACE_MIDRIM_NEBULA" },
        [4] = new[] { "SPACE_TATOOINE_ORBIT", "SPACE_MIDRIM_NEBULA" },
        [5] = new[] { "SPACE_DATHOMIR_ORBIT", "SPACE_DANTOOINE_ORBIT", "SPACE_MIDRIM_1", "SPACE_KORRIBAN_DEBRIS" },
        [6] = new[] { "SPACE_KORRIBAN_ORBIT", "SPACE_MIDRIM_2" },
    };

    private static ShipNpcEntry Entry(string tag) => RevampNpcShipCatalog.Ships.Single(x => x.CreatureTag == tag);

    [Test]
    public void EveryTier_HasEveryRole()
    {
        for (var tier = 1; tier <= 6; tier++)
        {
            var roles = RevampNpcShipCatalog.Ships
                .Where(x => x.CreatureTag.StartsWith($"t{tier}"))
                .Select(x => x.Role)
                .ToHashSet();

            roles.Should().Contain(new[] { "Fighter", "Interceptor", "Bomber", "Sniper", "Electronic Warfare", "Gunship",
                "Logistics", "Carrier", "Minelayer" }, $"tier {tier}");
        }
    }

    [TestCase("ewar", "Electronic Warfare", "Normal", ShipSize.Small)]
    [TestCase("sniper", "Sniper", "Normal", ShipSize.Small)]
    [TestCase("minelayer", "Minelayer", "Normal", ShipSize.Small)]
    [TestCase("logistics", "Logistics", "Normal", ShipSize.Medium)]
    [TestCase("carrier", "Carrier", "Boss", ShipSize.Large)]
    public void RoleShips_HaveTheirRoleDifficultyAndSize(string hull, string role, string difficulty, ShipSize size)
    {
        for (var tier = 1; tier <= 6; tier++)
        {
            var entry = Entry($"t{tier}{hull}");
            (entry.Role, entry.Difficulty, entry.Size).Should().Be((role, difficulty, size), entry.CreatureTag);
        }
    }

    [Test]
    public void SupportModules_AreMidOrLowStepsAtTheShipsRank()
    {
        foreach (var entry in RevampNpcShipCatalog.Ships)
        {
            foreach (var tag in entry.Support)
            {
                var step = RevampModuleCatalog.Steps.SingleOrDefault(x => x.Tag == tag);
                step.Should().NotBeNull($"{entry.CreatureTag} fits {tag}");
                step!.Row.Should().NotBe(ShipModulePowerType.High, tag);
                step.Rank.Should().Be(Math.Min(45, entry.Level / 5 * 5), $"{entry.CreatureTag} {tag}");
            }
        }
    }

    [Test]
    public void Gunships_CarryAHardenerAndGiveUpAQuarterOfItsResist()
    {
        foreach (var entry in RevampNpcShipCatalog.Ships.Where(x => x.CreatureTag.EndsWith("gunship")))
        {
            entry.Support.Should().ContainSingle(x => x.StartsWith("sm_hard_"), entry.CreatureTag);
            var hardener = RevampModuleCatalog.Steps.Single(x => x.Tag == entry.Support.Single());
            entry.ResistAll.Should().Be(10 - (int)Math.Round(hardener.ResistAll * 0.25, MidpointRounding.ToEven), entry.CreatureTag);
        }
    }

    [Test]
    public void LogisticsShips_CarryRepairers_CargoHaulersDoNot()
    {
        for (var tier = 1; tier <= 6; tier++)
        {
            var families = Entry($"t{tier}logistics").Support
                .Select(tag => RevampModuleCatalog.Steps.Single(x => x.Tag == tag).Family)
                .ToList();
            families.Should().BeEquivalentTo(new[] { ShipModuleFamily.HullRepairer, ShipModuleFamily.ShieldRepairer });
            Entry($"t{tier}cargo").Support.Should().BeEmpty();
        }
    }

    [Test]
    public void Minelayers_MineAsTheirTiersBomberTorpedo()
    {
        foreach (var entry in RevampNpcShipCatalog.Ships)
        {
            if (entry.Role != "Minelayer")
            {
                entry.MineDmg.Should().Be(0, entry.CreatureTag);
                continue;
            }

            // An ace mines as the Elite Bomber of its level, so only its tier's bomber row exists to compare with.
            var ace = ShipAceRules.Get(entry.CreatureTag);
            var tier = ace != null ? $"t{ace.Tier}" : entry.CreatureTag.Substring(0, 2);
            var bomber = Entry(tier + "bomber");
            var torpedo = bomber.Weapons.Single(x => x.ModuleTag.StartsWith("sm_torp_"));
            entry.MineDmg.Should().BePositive(entry.CreatureTag);
            if (bomber.Level == entry.Level)
                entry.MineDmg.Should().Be(torpedo.Dmg, entry.CreatureTag);
        }
    }

    [Test]
    public void BuildStatus_FitsSupportModulesInTheirRows()
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var logistics = ShipNpcRules.BuildStatus(Entry("t3logistics"), "NPC_Gunship3", false, now);
        logistics.HighPowerModules.Values.Select(x => x.ItemTag).Should().Equal("sm_ion_00");
        logistics.LowPowerModules.Values.Select(x => x.ItemTag).Should().Equal("sm_hrep_30");
        logistics.MidPowerModules.Values.Select(x => x.ItemTag).Should().Equal("sm_shrep_30");
        logistics.MidPowerModules.Keys.Should().Equal(1);

        var gunship = ShipNpcRules.BuildStatus(Entry("t3gunship"), "NPC_Gunship3", false, now);
        gunship.MidPowerModules[1].ItemTag.Should().Be("sm_hard_30");
        gunship.MidPowerModules[1].NpcShot.Should().BeNull();
    }

    [Test]
    public void RoleShips_SpawnInTheirTiersOrbits()
    {
        var tables = new SpaceSpawnDefinition().BuildSpawnTables();
        var registered = new NPCSpaceObjectDefinition().BuildSpaceObjects();

        foreach (var (tier, orbits) in OrbitsByTier)
        {
            foreach (var orbit in orbits)
            {
                var spawns = tables[orbit].Spawns;
                var fighters = spawns.Single(x => x.Resref == $"t{tier}fighter").Weight;
                foreach (var hull in RoleHulls)
                {
                    var spawn = spawns.SingleOrDefault(x => x.Resref == $"t{tier}{hull}");
                    spawn.Should().NotBeNull($"{orbit} spawns t{tier}{hull}");
                    spawn!.Type.Should().Be(ObjectType.Creature);
                    spawn.Weight.Should().BeInRange(1, fighters / 10, $"{orbit} t{tier}{hull} spawns at a modest weight");
                    registered.Should().ContainKey(spawn.Resref);
                }
            }
        }
    }

    [Test]
    public void SquadCatalog_NamesARegisteredShipForEveryPick()
    {
        // The six faction tiers, then the Rift Raiders (Wild Space).
        SpaceSquadCatalog.Factions.Should().HaveCount(7);
        for (var faction = 0; faction < 6; faction++)
        {
            for (var role = 0; role < SpaceSquadCatalog.Roles.Count; role++)
            {
                var tag = SpaceSquadCatalog.CreatureTag(faction, role);
                var expected = faction == 5 && SpaceSquadCatalog.Roles[role].Hull == "capital"
                    ? string.Empty
                    : $"t{faction + 1}{SpaceSquadCatalog.Roles[role].Hull}";
                tag.Should().Be(expected);
            }
        }

        var raiders = SpaceSquadCatalog.Roles.Select((x, i) => SpaceSquadCatalog.CreatureTag(6, i)).Where(x => x.Length > 0).ToList();
        raiders.Should().OnlyContain(x => WildSpaceTuning.RiftRaiderTags.Contains(x));
        raiders.Distinct().Should().BeEquivalentTo(WildSpaceTuning.RiftRaiderTags, "every Rift Raider can be spawned");
        SpaceSquadCatalog.CreatureTag(6, SpaceSquadCatalog.Roles.ToList().FindIndex(x => x.Hull == "pirint")).Should().Be("rrinterceptor");
        SpaceSquadCatalog.CreatureTag(6, SpaceSquadCatalog.Roles.ToList().FindIndex(x => x.Hull == "capital")).Should().BeEmpty();
        SpaceSquadCatalog.ConvoyTier(6).Should().Be(0, "the Rift Raiders run no convoys");
        SpaceSquadCatalog.ConvoyTier(2).Should().Be(3);

        SpaceSquadCatalog.CreatureTag(-1, 0).Should().BeEmpty();
        SpaceSquadCatalog.CreatureTag(0, SpaceSquadCatalog.Roles.Count).Should().BeEmpty();
    }

    [Test]
    public void SquadCatalog_PlacesShipsOnARing()
    {
        SpaceSquadCatalog.SquadSize(-3).Should().Be(1);
        SpaceSquadCatalog.SquadSize(2).Should().Be(3);
        SpaceSquadCatalog.SquadSize(99).Should().Be(SpaceSquadCatalog.MaxSquadSize);

        var offsets = Enumerable.Range(0, 4).Select(i => SpaceSquadCatalog.Offset(i, 4)).ToList();
        offsets.Should().OnlyHaveUniqueItems();
        foreach (var (x, y) in offsets)
            Math.Sqrt(x * x + y * y).Should().BeApproximately(SpaceSquadCatalog.SpawnRingMeters, 0.01);
    }
}