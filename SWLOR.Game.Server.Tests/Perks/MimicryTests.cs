using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class MimicryTests
{
    private const string MimicryTechniqueNamespace = "SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry";
    private const string NpcAbilityNamespace = "SWLOR.Game.Server.Feature.AbilityDefinition.NPC";
    private const int CustomTlkStrrefOffset = 16777216;

    // Feat, display name, tier, slot cost, source NPC feat this technique is copied from.
    // Mirrors the ORIGINAL "Technique pool" table (the first 10 techniques shipped). These rows
    // pin exact tuning for the original pool and must not be touched when the pool expands;
    // full-pool coverage is asserted separately by the reflection-driven tests below.
    private static readonly (FeatType Technique, string Name, int Tier, int SlotCost, FeatType SourceFeat)[] TechniqueTable =
    {
        (FeatType.ToxicSpitTechnique, "Toxic Spit", 1, 1, FeatType.ToxicSpit),
        (FeatType.FrostSpitTechnique, "Frost Spit", 1, 1, FeatType.FrostSpit),
        (FeatType.RakingClawsTechnique, "Raking Claws", 1, 1, FeatType.RakingClaws),
        (FeatType.SonicShriekTechnique, "Sonic Shriek", 2, 2, FeatType.SonicShriek),
        (FeatType.TailSweepTechnique, "Tail Sweep", 2, 2, FeatType.TailSweep),
        (FeatType.StaticWebTechnique, "Static Web", 2, 2, FeatType.StaticWeb),
        (FeatType.GoringChargeTechnique, "Goring Charge", 3, 2, FeatType.GoringCharge),
        (FeatType.ToxicCloudTechnique, "Toxic Cloud", 3, 3, FeatType.ToxicCloud),
        (FeatType.ScorchingBreathTechnique, "Scorching Breath", 4, 3, FeatType.ScorchingBreath),
        (FeatType.TerrifyingBellowTechnique, "Terrifying Bellow", 4, 3, FeatType.TerrifyingBellow),
    };

    [Test]
    public void MimicryTechniques_RegisterFeatsWithCommonAbilityContract()
    {
        foreach (var entry in TechniqueTable)
        {
            var abilities = BuildTechnique(entry.Technique);

            abilities.Should().ContainKey(entry.Technique, $"{entry.Technique} should register itself");
            var ability = abilities[entry.Technique];

            ((int)entry.Technique).Should().BeInRange(2773, 2782);
            ability.Name.Should().NotBeNullOrWhiteSpace();
            ability.MimicryTier.Should().BeInRange(1, 4);
            ability.MimicrySlotCost.Should().BeInRange(1, 3);
            ability.MimicrySourceFeat.Should().NotBe(FeatType.Invalid);
            ability.EffectiveLevelPerkType.Should().Be(PerkType.TechniquePotency);
            ability.SkillType.Should().Be(SkillType.Mimicry);
            ability.IsHostileAbility.Should().BeTrue($"{entry.Technique} is copied from a hostile NPC ability");
        }
    }

    [Test]
    public void MimicryTechniques_TierAndSlotCostMatchPoolTable()
    {
        foreach (var entry in TechniqueTable)
        {
            var ability = BuildTechnique(entry.Technique)[entry.Technique];

            ability.Name.Should().Be(entry.Name);
            ability.MimicryTier.Should().Be(entry.Tier, $"{entry.Technique} tier should match the pool table");
            ability.MimicrySlotCost.Should().Be(entry.SlotCost, $"{entry.Technique} slot cost should match the pool table");
            ability.MimicrySourceFeat.Should().Be(entry.SourceFeat);
        }
    }

    [Test]
    public void MimicryTechniqueSourceFeats_MatchRegisteredNpcAbilityDefinitions()
    {
        foreach (var entry in TechniqueTable)
        {
            var npcAbilities = BuildNpcSource(entry.SourceFeat);

            npcAbilities.Should().ContainKey(
                entry.SourceFeat,
                $"{entry.Technique}'s MimicrySourceFeat ({entry.SourceFeat}) should be a real, registered NPC ability");
        }
    }

    // Registry-driven contract check covering every technique in the (currently 88-strong) pool,
    // not just the original 10 pinned above. Discovers definitions by reflection so it stays valid
    // as the technique pool grows.
    [Test]
    public void MimicryTechniques_AllRegisterFeatsWithCommonAbilityContract()
    {
        var npcAbilitiesByFeat = BuildAllAbilities(NpcAbilityNamespace)
            .ToDictionary(a => a.Feat, a => a.Detail);
        var techniques = BuildAllAbilities(MimicryTechniqueNamespace);

        techniques.Should().NotBeEmpty("technique discovery should find definitions in the Mimicry ability namespace");

        foreach (var technique in techniques)
        {
            var feat = technique.Feat;
            var ability = technique.Detail;

            ability.Name.Should().NotBeNullOrWhiteSpace($"{feat} should have a display name");
            ability.Name.Should().NotContain("Technique", $"{feat}'s player-facing name should not carry the 'Technique' label");
            ability.MimicryTier.Should().BeInRange(1, 4, $"{feat}'s MimicryTier should be between 1 and 4");
            ability.MimicrySlotCost.Should().BeInRange(1, 3, $"{feat}'s MimicrySlotCost should be between 1 and 3");
            ability.EffectiveLevelPerkType.Should().Be(PerkType.TechniquePotency, $"{feat} should scale with Technique Potency");
            ability.SkillType.Should().Be(SkillType.Mimicry, $"{feat} should use the Mimicry skill");
            ability.MimicrySourceFeat.Should().NotBe(FeatType.Invalid, $"{feat} should declare a MimicrySourceFeat");

            npcAbilitiesByFeat.Should().ContainKey(ability.MimicrySourceFeat,
                $"{feat}'s MimicrySourceFeat ({ability.MimicrySourceFeat}) should be a registered NPC ability");

            var sourceAbility = npcAbilitiesByFeat[ability.MimicrySourceFeat];
            ability.Name.Should().Be(sourceAbility.Name,
                $"{feat}'s name should match the creature ability it replicates ({ability.MimicrySourceFeat})");
            ability.IsHostileAbility.Should().Be(sourceAbility.IsHostileAbility,
                $"{feat}'s IsHostileAbility should mirror its source NPC ability {ability.MimicrySourceFeat}'s hostility " +
                "(self-buff sources are not hostile, so techniques copied from them shouldn't be either)");
        }
    }

    // Every damaging technique must declare a scaling attribute so its damage tracks player stats
    // like native abilities (a technique with no CombatImpactDamageAbility gets zero stat scaling).
    // Assignment is spread across all six attributes rather than defaulting everything to one stat.
    [Test]
    public void MimicryTechniques_DeclareScalingStatsSpanningAllSixAttributes()
    {
        var techniques = BuildAllAbilities(MimicryTechniqueNamespace);
        var usedStats = new HashSet<AbilityType>();

        foreach (var technique in techniques)
        {
            technique.Detail.CombatImpactDamageAbility.Should().NotBe(AbilityType.Invalid,
                $"{technique.Feat} should declare a scaling attribute via CombatImpactDamageAbility");
            usedStats.Add(technique.Detail.CombatImpactDamageAbility);
        }

        var allAttributes = new[]
        {
            AbilityType.Might, AbilityType.Perception, AbilityType.Vitality,
            AbilityType.Agility, AbilityType.Willpower, AbilityType.Social
        };
        foreach (var attribute in allAttributes)
        {
            usedStats.Should().Contain(attribute,
                $"at least one technique should scale off {attribute} so no attribute is unused");
        }
    }

    // The core completeness requirement: every NPC ability the game registers must be learnable
    // through Mimicry, and every technique must copy a real NPC ability. Asserted bidirectionally
    // and 1:1 so the pool can never drift out of sync with the NPC ability roster.
    [Test]
    public void MimicryTechniques_EveryRegisteredNpcAbilityHasExactlyOneTechniqueTwin()
    {
        var npcAbilities = BuildAllAbilities(NpcAbilityNamespace);
        var techniques = BuildAllAbilities(MimicryTechniqueNamespace);

        npcAbilities.Should().NotBeEmpty("NPC ability discovery should find definitions in the NPC ability namespace");
        techniques.Should().NotBeEmpty("technique discovery should find definitions in the Mimicry ability namespace");

        var npcFeats = npcAbilities.Select(a => a.Feat).ToList();
        npcFeats.Should().OnlyHaveUniqueItems("each NPC ability definition should register a distinct FeatType");
        var npcFeatSet = npcFeats.ToHashSet();

        var techniqueFeats = techniques.Select(t => t.Feat).ToList();
        techniqueFeats.Should().OnlyHaveUniqueItems("each Mimicry technique should register a distinct FeatType");

        var techniquesBySourceFeat = techniques
            .GroupBy(t => t.Detail.MimicrySourceFeat)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Every technique's source feat must be a real, registered NPC ability.
        foreach (var technique in techniques)
        {
            npcFeatSet.Should().Contain(technique.Detail.MimicrySourceFeat,
                $"{technique.Feat}'s MimicrySourceFeat ({technique.Detail.MimicrySourceFeat}) should be a registered NPC ability");
        }

        // Every registered NPC ability must be learnable through exactly one technique.
        foreach (var npc in npcAbilities)
        {
            techniquesBySourceFeat.Should().ContainKey(npc.Feat,
                $"NPC ability {npc.Feat} (from {npc.DefinitionType.Name}) has no Mimicry technique that copies it");

            techniquesBySourceFeat[npc.Feat].Should().ContainSingle(
                $"NPC ability {npc.Feat} should have exactly one Mimicry technique twin, " +
                $"found {(techniquesBySourceFeat.TryGetValue(npc.Feat, out var twins) ? twins.Count : 0)}");
        }
    }

    [Test]
    public void MimicryPerkDefinition_BuildsExpectedPerksAndLevels()
    {
        var perks = BuildPerksWithout2daLookup(new MimicryPerkDefinition(),
            "CombatAnalyzer", "TechniquePotency", "AnalyzerMemory", "PatternRecognition");

        perks.Should().HaveCount(4);
        perks.Values.Should().OnlyContain(perk => perk.Category == PerkCategoryType.Mimicry);

        var combatAnalyzer = perks[PerkType.CombatAnalyzer];
        combatAnalyzer.Name.Should().Be("Combat Analyzer");
        combatAnalyzer.PerkLevels.Should().HaveCount(1);
        combatAnalyzer.PerkLevels[1].Price.Should().Be(2);
        AssertSkillRequirement(combatAnalyzer.PerkLevels[1], SkillType.Mimicry, 0);
        combatAnalyzer.PerkLevels[1].Requirements.OfType<PerkRequirementMustHavePerk>().Should().BeEmpty();
        combatAnalyzer.RefundedTriggers.Should().ContainSingle();

        var technique = perks[PerkType.TechniquePotency];
        technique.Name.Should().Be("Technique Potency");
        technique.PerkLevels.Should().HaveCount(4);
        technique.RefundedTriggers.Should().BeEmpty();
        var expectedTechniqueSkillRanks = new[] { 5, 15, 25, 35 };
        for (var level = 1; level <= 4; level++)
        {
            technique.PerkLevels[level].Price.Should().Be(3);
            AssertSkillRequirement(technique.PerkLevels[level], SkillType.Mimicry, expectedTechniqueSkillRanks[level - 1]);
            AssertMustHavePerkRequirement(technique.PerkLevels[level], PerkType.CombatAnalyzer);
        }

        var memory = perks[PerkType.AnalyzerMemory];
        memory.Name.Should().Be("Analyzer Memory");
        memory.PerkLevels.Should().HaveCount(3);
        memory.RefundedTriggers.Should().ContainSingle();
        var expectedMemoryPrices = new[] { 2, 3, 4 };
        var expectedMemorySkillRanks = new[] { 10, 25, 40 };
        for (var level = 1; level <= 3; level++)
        {
            memory.PerkLevels[level].Price.Should().Be(expectedMemoryPrices[level - 1]);
            AssertSkillRequirement(memory.PerkLevels[level], SkillType.Mimicry, expectedMemorySkillRanks[level - 1]);
            AssertMustHavePerkRequirement(memory.PerkLevels[level], PerkType.CombatAnalyzer);
        }

        var pattern = perks[PerkType.PatternRecognition];
        pattern.Name.Should().Be("Pattern Recognition");
        pattern.PerkLevels.Should().HaveCount(2);
        pattern.RefundedTriggers.Should().BeEmpty();
        var expectedPatternPrices = new[] { 2, 3 };
        var expectedPatternSkillRanks = new[] { 10, 30 };
        for (var level = 1; level <= 2; level++)
        {
            pattern.PerkLevels[level].Price.Should().Be(expectedPatternPrices[level - 1]);
            AssertSkillRequirement(pattern.PerkLevels[level], SkillType.Mimicry, expectedPatternSkillRanks[level - 1]);
            AssertMustHavePerkRequirement(pattern.PerkLevels[level], PerkType.CombatAnalyzer);
        }

        // Each perk grants exactly its passive icon feat at level 1 (perk-window icon resolution);
        // technique feats (2773-2782) are granted only by the equip system, never by perks.
        var expectedIconFeats = new Dictionary<PerkType, FeatType>
        {
            [PerkType.CombatAnalyzer] = FeatType.CombatAnalyzerTrait,
            [PerkType.TechniquePotency] = FeatType.TechniquePotencyTrait,
            [PerkType.AnalyzerMemory] = FeatType.AnalyzerMemoryTrait,
            [PerkType.PatternRecognition] = FeatType.PatternRecognitionTrait,
        };
        var techniqueFeats = TechniqueTable.Select(x => x.Technique).ToHashSet();

        foreach (var (perkType, perk) in perks)
        {
            perk.PerkLevels[1].GrantedFeats.Should().Equal(new[] { expectedIconFeats[perkType] },
                $"{perk.Name} level 1 should grant only its icon feat");

            foreach (var level in perk.PerkLevels.Values)
            {
                level.GrantedFeats.Should().NotContain(feat => techniqueFeats.Contains(feat),
                    $"{perk.Name} should never grant technique feats directly");
            }
        }
    }

    // Registry-driven 2DA linkage check covering every technique in the pool. Replaces the old
    // fixed SPELLID range assertion (1599-1608), which only fit the original 10-row pool, with a
    // check that each technique's SPELLID resolves and round-trips through spells.2da and
    // CLS_FEAT_FIGHT.2da regardless of how many techniques exist.
    [Test]
    public void MimicryTechniques_Feat2daRowsLinkToSpellsAndClassFeatTable()
    {
        var root = FindRepositoryRoot();
        var featRows = Test2daHelper.Read2da(new FileInfo(Path.Combine(
            root.FullName, "SWLOR_Haks", "sw_2da", "feat.2da")));
        var spellRows = Test2daHelper.Read2da(new FileInfo(Path.Combine(
            root.FullName, "SWLOR_Haks", "sw_2da", "spells.2da")));
        var classFeatRows = Test2daHelper.Read2da(new FileInfo(Path.Combine(
            root.FullName, "SWLOR_Haks", "sw_2da", "CLS_FEAT_FIGHT.2da")));

        var techniques = BuildAllAbilities(MimicryTechniqueNamespace);
        techniques.Should().NotBeEmpty();

        foreach (var technique in techniques)
        {
            var feat = technique.Feat;
            var featId = (int)feat;

            featRows.Should().ContainKey(featId, $"feat.2da should have a row for {feat}");
            var featRow = featRows[featId];
            featRow["LABEL"].Should().Be(feat.ToString(), $"{feat}'s feat.2da LABEL should match its enum name");
            featRow["ICON"].Should().NotBeNullOrWhiteSpace($"{feat} should have a feat.2da ICON");

            featRow.TryGetValue("SPELLID", out var spellIdText).Should().BeTrue($"{feat}'s feat.2da row should have a SPELLID column");
            int.TryParse(spellIdText, out var spellId).Should().BeTrue($"{feat}'s SPELLID should be numeric, was '{spellIdText}'");

            spellRows.Should().ContainKey(spellId, $"spells.2da should have a row for {feat} (spell id {spellId})");
            var spellRow = spellRows[spellId];
            spellRow.TryGetValue("FeatID", out var featIdText).Should().BeTrue($"{feat}'s spells.2da row should have a FeatID column");
            int.TryParse(featIdText, out var linkedFeatId).Should().BeTrue($"{feat}'s spells.2da FeatID should be numeric, was '{featIdText}'");
            linkedFeatId.Should().Be(featId, $"{feat}'s spells.2da row should point back at its feat.2da row");

            var classFeatRow = classFeatRows.Values.FirstOrDefault(row =>
                row.TryGetValue("FeatIndex", out var featIndexText) &&
                int.TryParse(featIndexText, out var featIndex) &&
                featIndex == featId);

            classFeatRow.Should().NotBeNull($"CLS_FEAT_FIGHT.2da should expose {feat} (feat id {featId})");
            classFeatRow!["List"].Should().Be("1", $"{feat} should be List=1 in CLS_FEAT_FIGHT.2da");
            classFeatRow["GrantedOnLevel"].Should().Be("99", $"{feat} should be GrantedOnLevel=99 in CLS_FEAT_FIGHT.2da");
            classFeatRow["OnMenu"].Should().Be("1", $"{feat} should be OnMenu=1 in CLS_FEAT_FIGHT.2da");
        }
    }

    // Registry-driven TLK check covering every technique in the pool. Replaces the old fixed
    // strref-id range assertion (192553-192573), which only fit the original 10-row pool, with a
    // check that every technique's FEAT/DESCRIPTION strrefs are custom TLK references that
    // resolve to non-empty text, regardless of how many techniques exist or which TLK ids they use.
    [Test]
    public void MimicryTechniques_TlkEntriesAreNonEmptyForFeatAndDescriptionStrrefs()
    {
        var root = FindRepositoryRoot();
        var featRows = Test2daHelper.Read2da(new FileInfo(Path.Combine(
            root.FullName, "SWLOR_Haks", "sw_2da", "feat.2da")));
        var tlkEntries = ReadTlkEntries(new FileInfo(Path.Combine(
            root.FullName, "SWLOR_Haks", "sw_tlk", "sw_tlk.tlk.json")));

        var techniques = BuildAllAbilities(MimicryTechniqueNamespace);
        techniques.Should().NotBeEmpty();

        foreach (var technique in techniques)
        {
            var feat = technique.Feat;
            var featId = (int)feat;

            featRows.Should().ContainKey(featId, $"feat.2da should have a row for {feat}");
            var featRow = featRows[featId];

            AssertCustomTlkStrrefIsNonEmpty(featRow, tlkEntries, feat, "FEAT", "name");
            AssertCustomTlkStrrefIsNonEmpty(featRow, tlkEntries, feat, "DESCRIPTION", "description");
        }
    }

    [Test]
    public void Mimicry_MaxSlotsScalesWithCombatAnalyzerAndAnalyzerMemoryPerkLevels()
    {
        var noAnalyzer = new Player();
        Mimicry.GetMaxSlots(noAnalyzer).Should().Be(0, "players without Combat Analyzer get no technique slots");

        var memoryWithoutAnalyzer = new Player();
        memoryWithoutAnalyzer.Perks[PerkType.AnalyzerMemory] = 3;
        Mimicry.GetMaxSlots(memoryWithoutAnalyzer).Should().Be(0, "Analyzer Memory alone should not grant slots without Combat Analyzer");

        var baseAnalyzer = new Player();
        baseAnalyzer.Perks[PerkType.CombatAnalyzer] = 1;
        Mimicry.GetMaxSlots(baseAnalyzer).Should().Be(2);

        var memoryLevel1 = new Player();
        memoryLevel1.Perks[PerkType.CombatAnalyzer] = 1;
        memoryLevel1.Perks[PerkType.AnalyzerMemory] = 1;
        Mimicry.GetMaxSlots(memoryLevel1).Should().Be(3);

        var memoryLevel2 = new Player();
        memoryLevel2.Perks[PerkType.CombatAnalyzer] = 1;
        memoryLevel2.Perks[PerkType.AnalyzerMemory] = 2;
        Mimicry.GetMaxSlots(memoryLevel2).Should().Be(4);

        var memoryLevel3 = new Player();
        memoryLevel3.Perks[PerkType.CombatAnalyzer] = 1;
        memoryLevel3.Perks[PerkType.AnalyzerMemory] = 3;
        Mimicry.GetMaxSlots(memoryLevel3).Should().Be(5);
    }

    [Test]
    public void Mimicry_UsedSlotsSumsEquippedTechniqueSlotCosts()
    {
        Ability.CacheData();
        Mimicry.CacheData();

        var empty = new Player();
        Mimicry.GetUsedSlots(empty).Should().Be(0);

        var equipped = new Player();
        equipped.EquippedTechniques.Add(FeatType.ToxicSpitTechnique); // 1 slot
        equipped.EquippedTechniques.Add(FeatType.SonicShriekTechnique); // 2 slots
        equipped.EquippedTechniques.Add(FeatType.ToxicCloudTechnique); // 3 slots

        Mimicry.GetUsedSlots(equipped).Should().Be(6);
    }

    private static Dictionary<FeatType, AbilityDetail> BuildTechnique(FeatType technique)
    {
        IAbilityListDefinition definition = technique switch
        {
            FeatType.ToxicSpitTechnique => new ToxicSpitTechniqueAbilityDefinition(),
            FeatType.FrostSpitTechnique => new FrostSpitTechniqueAbilityDefinition(),
            FeatType.RakingClawsTechnique => new RakingClawsTechniqueAbilityDefinition(),
            FeatType.SonicShriekTechnique => new SonicShriekTechniqueAbilityDefinition(),
            FeatType.TailSweepTechnique => new TailSweepTechniqueAbilityDefinition(),
            FeatType.StaticWebTechnique => new StaticWebTechniqueAbilityDefinition(),
            FeatType.GoringChargeTechnique => new GoringChargeTechniqueAbilityDefinition(),
            FeatType.ToxicCloudTechnique => new ToxicCloudTechniqueAbilityDefinition(),
            FeatType.ScorchingBreathTechnique => new ScorchingBreathTechniqueAbilityDefinition(),
            FeatType.TerrifyingBellowTechnique => new TerrifyingBellowTechniqueAbilityDefinition(),
            _ => throw new ArgumentOutOfRangeException(nameof(technique), technique, "Unknown Mimicry technique")
        };

        return definition.BuildAbilities();
    }

    private static Dictionary<FeatType, AbilityDetail> BuildNpcSource(FeatType sourceFeat)
    {
        IAbilityListDefinition definition = sourceFeat switch
        {
            FeatType.ToxicSpit => new ToxicSpitAbilityDefinition(),
            FeatType.FrostSpit => new FrostSpitAbilityDefinition(),
            FeatType.RakingClaws => new RakingClawsAbilityDefinition(),
            FeatType.SonicShriek => new SonicShriekAbilityDefinition(),
            FeatType.TailSweep => new TailSweepAbilityDefinition(),
            FeatType.StaticWeb => new StaticWebAbilityDefinition(),
            FeatType.GoringCharge => new GoringChargeAbilityDefinition(),
            FeatType.ToxicCloud => new ToxicCloudAbilityDefinition(),
            FeatType.ScorchingBreath => new ScorchingBreathAbilityDefinition(),
            FeatType.TerrifyingBellow => new TerrifyingBellowAbilityDefinition(),
            _ => throw new ArgumentOutOfRangeException(nameof(sourceFeat), sourceFeat, "Unknown Mimicry source NPC feat")
        };

        return definition.BuildAbilities();
    }

    // Reflection discovers every IAbilityListDefinition declared directly in the given namespace,
    // instantiates each one, and builds its abilities. Used to derive expectations from the actual
    // registered content instead of a hand-maintained list, so tests stay valid as the technique
    // pool (and NPC ability roster) grows or shrinks.
    private static List<DiscoveredAbility> BuildAllAbilities(string namespaceName)
    {
        var definitionTypes = typeof(IAbilityListDefinition).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } &&
                           type.Namespace == namespaceName &&
                           typeof(IAbilityListDefinition).IsAssignableFrom(type));

        var discovered = new List<DiscoveredAbility>();
        foreach (var definitionType in definitionTypes)
        {
            var definition = (IAbilityListDefinition)Activator.CreateInstance(definitionType)!;
            foreach (var (feat, detail) in definition.BuildAbilities())
            {
                discovered.Add(new DiscoveredAbility(definitionType, feat, detail));
            }
        }

        return discovered;
    }

    private readonly record struct DiscoveredAbility(Type DefinitionType, FeatType Feat, AbilityDetail Detail);

    private static void AssertCustomTlkStrrefIsNonEmpty(
        Dictionary<string, string> featRow,
        IReadOnlyDictionary<int, string> tlkEntries,
        FeatType feat,
        string column,
        string label)
    {
        featRow.TryGetValue(column, out var strrefText).Should().BeTrue($"{feat}'s feat.2da row should have a {column} column");
        int.TryParse(strrefText, out var strref).Should().BeTrue($"{feat}'s {column} strref should be numeric, was '{strrefText}'");
        strref.Should().BeGreaterThanOrEqualTo(CustomTlkStrrefOffset,
            $"{feat}'s {column} strref should reference a custom TLK entry (>= {CustomTlkStrrefOffset})");

        var tlkId = strref - CustomTlkStrrefOffset;
        tlkEntries.Should().ContainKey(tlkId, $"TLK id {tlkId} ({feat} {label}) should exist");
        tlkEntries[tlkId].Should().NotBeNullOrWhiteSpace($"TLK id {tlkId} ({feat} {label}) should have non-empty text");
    }

    private static void AssertSkillRequirement(PerkLevel level, SkillType skill, int rank)
    {
        var requirement = level.Requirements
            .OfType<PerkRequirementSkill>()
            .Should()
            .ContainSingle()
            .Which;

        requirement.Type.Should().Be(skill);
        requirement.RequiredRank.Should().Be(rank);
    }

    private static void AssertMustHavePerkRequirement(PerkLevel level, PerkType requiredPerk)
    {
        var requirement = level.Requirements
            .OfType<PerkRequirementMustHavePerk>()
            .Should()
            .ContainSingle()
            .Which;

        typeof(PerkRequirementMustHavePerk)
            .GetField("_mustHavePerkType", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(requirement)
            .Should()
            .Be(requiredPerk);
    }

    private static IReadOnlyDictionary<int, string> ReadTlkEntries(FileInfo file)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(file.FullName));
        return document.RootElement
            .GetProperty("entries")
            .EnumerateArray()
            .ToDictionary(
                entry => entry.GetProperty("id").GetInt32(),
                entry => entry.TryGetProperty("text", out var text) ? text.GetString() ?? string.Empty : string.Empty);
    }

    // Invokes the definition's private per-perk builder methods and reads the builder's perk
    // dictionary directly, skipping PerkBuilder.Build()'s feat.2da icon lookup (needs a live engine).
    // Same pattern as BeastmasterCombatUpgradeTests.BuildPerksWithout2daLookup.
    private static Dictionary<PerkType, PerkDetail> BuildPerksWithout2daLookup<T>(T definition, params string[] methodNames)
    {
        foreach (var methodName in methodNames)
        {
            typeof(T)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(definition, null);
        }

        var builder = typeof(T)
            .GetField("_builder", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(definition);

        return (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder)
            .GetField("_perks", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(builder)!;
    }

    private static PathInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            var candidate = directory.FullName;
            if (File.Exists(Path.Combine(candidate, "SWLOR.Game.Server.sln")) &&
                File.Exists(Path.Combine(candidate, "SWLOR_Haks", "sw_2da", "feat.2da")))
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
