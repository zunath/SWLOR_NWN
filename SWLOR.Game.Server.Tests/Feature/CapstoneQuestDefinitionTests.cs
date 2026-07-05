using FluentAssertions;
using NUnit.Framework;
using System.Reflection;
using System.Text.Json;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.LootTableDefinition;
using SWLOR.Game.Server.Feature.QuestDefinition;
using SWLOR.Game.Server.Feature.SpawnDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AchievementService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.QuestService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpawnService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

public class CapstoneQuestDefinitionTests
{
    private const int SignatureAbilityFeatStart = 2719;
    private const int SignatureAbilityFeatEnd = 2753;

    private static readonly int[] ResistanceSubtypes =
    {
        (int)ResistanceType.Fire,
        (int)ResistanceType.Poison,
        (int)ResistanceType.Electrical,
        (int)ResistanceType.Ice,
        (int)ResistanceType.Mind,
        (int)ResistanceType.Mobility,
        (int)ResistanceType.Trauma,
        (int)ResistanceType.Disruption,
    };

    private static readonly int[] NpcAbilityFeatIds =
        new[]
        {
        FeatType.RendingBite,
        FeatType.CripplingTalons,
        FeatType.PiercingQuills,
        FeatType.ToxicSpit,
        FeatType.ScorchingBreath,
        FeatType.InfernoBlast,
        FeatType.SeismicSlam,
        FeatType.RupturingQuake,
        FeatType.TerrifyingBellow,
        FeatType.DisorientingScreech,
        FeatType.MaulingBite,
        FeatType.BonecrusherBite,
        FeatType.RakingClaws,
        FeatType.PouncingStrike,
        FeatType.TailSweep,
        FeatType.GoringCharge,
        FeatType.BarbedVolley,
        FeatType.VenomSpray,
        FeatType.ToxicCloud,
        FeatType.FrostSpit,
        FeatType.StaticBurst,
        FeatType.SavageRoar,
        FeatType.SonicShriek,
        FeatType.ChitinGuard,
        FeatType.IronCarapace,
        FeatType.PrecisionShot,
        FeatType.SuppressingShot,
        FeatType.GrenadeBurst,
        FeatType.SerratedSlash,
        FeatType.BrutalBash,
        FeatType.TacticalMark,
        FeatType.OverloadShot,
        FeatType.ArcPulse,
        FeatType.IonBurst,
        FeatType.TargetLock,
        FeatType.ShrapnelBurst,
        FeatType.ForceRend,
        FeatType.MindSpike,
        FeatType.DarkShock,
        FeatType.DreadWave,
        FeatType.GlacialSlime,
        FeatType.HoarfrostGlob,
        FeatType.PermafrostRupture,
        FeatType.RimePounce,
        FeatType.CryoBile,
        FeatType.CapacitorSurge,
        FeatType.StaticWeb,
        FeatType.ForceSunder,
        FeatType.NullShock,
        }.Select(feat => (int)feat).ToArray();

    private static readonly int[] CreatureOnlyNpcAbilityFeatIds =
        new[]
        {
        FeatType.RendingBite,
        FeatType.CripplingTalons,
        FeatType.PiercingQuills,
        FeatType.ToxicSpit,
        FeatType.ScorchingBreath,
        FeatType.InfernoBlast,
        FeatType.SeismicSlam,
        FeatType.RupturingQuake,
        FeatType.TerrifyingBellow,
        FeatType.MaulingBite,
        FeatType.BonecrusherBite,
        FeatType.RakingClaws,
        FeatType.PouncingStrike,
        FeatType.TailSweep,
        FeatType.GoringCharge,
        FeatType.BarbedVolley,
        FeatType.VenomSpray,
        FeatType.ToxicCloud,
        FeatType.FrostSpit,
        FeatType.SavageRoar,
        FeatType.ChitinGuard,
        FeatType.IronCarapace,
        FeatType.GlacialSlime,
        FeatType.HoarfrostGlob,
        FeatType.PermafrostRupture,
        FeatType.RimePounce,
        FeatType.CryoBile,
        }.Select(feat => (int)feat).ToArray();

    [Test]
    public void CapstoneQuestDefinitions_DefineRemainingThirtyNineLinesAndReuseAreaGroupsAtMostThreeTimes()
    {
        CapstoneQuestDefinitionTestData.Lines.Should().HaveCount(39);
        CapstoneQuestDefinitionTestData.AreaGroups.Should().HaveCount(13);

        CapstoneQuestDefinitionTestData.Lines
            .GroupBy(line => line.AreaGroup)
            .Should()
            .AllSatisfy(group => group.Should().HaveCountLessThanOrEqualTo(3));

        CapstoneQuestDefinitionTestData.Lines
            .Select(line => line.PerkType)
            .Should()
            .OnlyHaveUniqueItems()
            .And
            .NotContain(PerkType.BloodFrenzy);
    }

    [Test]
    public void AreaGroups_DefinePlanetAndBuildExpectations()
    {
        CapstoneQuestDefinitionTestData.AreaGroups
            .Should()
            .AllSatisfy(areaGroup =>
            {
                areaGroup.PlanetType.Should().NotBe(PlanetType.Invalid);
                areaGroup.AreaExpectation.Should().NotBeNullOrWhiteSpace();
            });

        CapstoneQuestDefinitionTestData.AreaGroups
            .Select(areaGroup => areaGroup.Name)
            .Should()
            .Contain(new[]
            {
                "Veles Militia Annex",
                "Dantooine Jedi Enclave Trial Halls",
                "Korriban Forge Caverns",
                "Smuggler's Moon Fight Club Backrooms",
                "CZ-220 Breaker Yard",
                "Anchorhead Canyon Range",
                "Czerka Arms Test Range",
                "Hutlar Qion Test Site",
                "Korriban Sith Crypt Depths",
                "Viscara Republic Engineering Bunker",
                "Dantooine Medical Sublevel",
                "Dathomir Tarn Jungle Preserve",
                "Dathomir Grotto Apex Den",
            });
    }

    [Test]
    public void NPCGroups_UseAreaGroupPlanetPrefix()
    {
        foreach (var line in CapstoneQuestDefinitionTestData.Lines)
        {
            var expectedPrefix = $"{line.AreaGroup.PlanetType}_";

            foreach (var npcGroup in line.NpcGroups)
            {
                var groupName = npcGroup.ToString();

                groupName.Should().StartWith(expectedPrefix);
                groupName.Should().NotStartWith("Capstone_");
            }
        }
    }

    [Test]
    public void QuestGivers_AreDedicatedPerLineAndHaveBlueprintDialogueAndPaletteEntries()
    {
        var root = FindRepositoryRoot();
        var modulePath = Path.Combine(root.FullName, "Module");
        var palettePath = Path.Combine(modulePath, "itp", "creaturepalcus.itp.json");
        using var paletteJson = JsonDocument.Parse(File.ReadAllText(palettePath));
        var paletteResrefs = FindTypedValues(paletteJson.RootElement, "RESREF").ToArray();

        CapstoneQuestDefinitionTestData.QuestGivers.Should().HaveCount(CapstoneQuestDefinitionTestData.Lines.Count);
        CapstoneQuestDefinitionTestData.Lines.Select(line => line.QuestGiver.Name).Should().OnlyHaveUniqueItems();
        CapstoneQuestDefinitionTestData.Lines.Select(line => line.QuestGiver.Resref).Should().OnlyHaveUniqueItems();

        foreach (var line in CapstoneQuestDefinitionTestData.Lines)
        {
            var giver = line.QuestGiver;
            giver.Resref.Length.Should().BeLessThanOrEqualTo(16);

            var utcPath = Path.Combine(modulePath, "utc", $"{giver.Resref}.utc.json");
            var dlgPath = Path.Combine(modulePath, "dlg", $"{giver.DialogueResref}.dlg.json");

            File.Exists(utcPath).Should().BeTrue($"{giver.Name} needs a quest giver UTC blueprint");
            File.Exists(dlgPath).Should().BeTrue($"{giver.Name} needs a quest giver dialogue");
            paletteResrefs.Should().Contain(giver.Resref, $"{giver.Name} must be placeable from the creature palette");

            using var utcJson = JsonDocument.Parse(File.ReadAllText(utcPath));
            GetTypedValue(utcJson.RootElement, "Conversation").Should().Be(giver.DialogueResref);
            GetTypedValue(utcJson.RootElement, "Tag").Should().Be(giver.Resref);
            GetTypedValue(utcJson.RootElement, "TemplateResRef").Should().Be(giver.Resref);

            var dialogue = File.ReadAllText(dlgPath);
            dialogue.Should().Contain("condition-can-accept-quest");
            dialogue.Should().NotContain("condition-any-skill");

            foreach (var questId in line.GetQuestIds())
            {
                dialogue.Should().Contain(questId);
                dialogue.Should().Contain("\"value\": \"action-accept-quest\"");
                dialogue.Should().Contain("\"value\": \"action-advance-quest\"");
            }
        }
    }

    [Test]
    public void BuildQuests_CreatesFiveStepChainForEachCapstoneLine()
    {
        var quests = BuildCapstoneQuests();

        quests.Should().HaveCount(CapstoneQuestDefinitionTestData.Lines.Count * 5);

        foreach (var line in CapstoneQuestDefinitionTestData.Lines)
        {
            var questIds = line.GetQuestIds().ToArray();

            questIds.Should().OnlyHaveUniqueItems();
            quests.Keys.Should().Contain(questIds);
            line.FinalQuestId.Should().Be(questIds[4]);

            for (var step = 1; step < questIds.Length; step++)
            {
                quests[questIds[step]].Prerequisites
                    .OfType<RequiredQuestPrerequisite>()
                    .Should()
                    .ContainSingle()
                    .Which
                    .QuestId
                    .Should()
                    .Be(questIds[step - 1]);
            }
        }
    }

    [Test]
    public void CapstoneQuests_RequireTheirLevelFiftySkillOnEveryStep()
    {
        var quests = BuildCapstoneQuests();

        foreach (var line in CapstoneQuestDefinitionTestData.Lines)
        {
            foreach (var questId in line.GetQuestIds())
            {
                var requirement = quests[questId].Prerequisites
                    .OfType<RequiredSkillRankPrerequisite>()
                    .Should()
                    .ContainSingle()
                    .Which;

                requirement.SkillType.Should().Be(line.RequiredSkillType);
                requirement.RequiredRank.Should().Be(50);
            }
        }
    }

    [Test]
    public void BeastCapstoneQuests_RequireBeastMasterySkillOnEveryStep()
    {
        var quests = BuildCapstoneQuests();

        foreach (var line in CapstoneQuestDefinitionTestData.Lines.Where(line => line.RequiredBeastRoleType.HasValue))
        {
            line.RequiredSkillType.Should().Be(SkillType.BeastMastery);

            foreach (var questId in line.GetQuestIds())
            {
                var requirement = quests[questId].Prerequisites
                    .OfType<RequiredSkillRankPrerequisite>()
                    .Should()
                    .ContainSingle()
                    .Which;

                requirement.SkillType.Should().Be(SkillType.BeastMastery);
                requirement.RequiredRank.Should().Be(50);
            }
        }
    }

    [Test]
    public void OpeningQuests_GrantAreaAccessKeys()
    {
        var quests = BuildCapstoneQuests();

        foreach (var line in CapstoneQuestDefinitionTestData.Lines)
        {
            var openingQuest = quests[line.GetQuestId(0)];

            openingQuest.OnAcceptActions.Should().ContainSingle();
            openingQuest.OnAbandonActions.Should().ContainSingle();
        }
    }

    [Test]
    public void ProofKeyItems_AreGrantedFromQuestCreditInsteadOfCollectObjectives()
    {
        var quests = BuildCapstoneQuests();

        foreach (var line in CapstoneQuestDefinitionTestData.Lines)
        {
            for (var step = 0; step < 4; step++)
            {
                var quest = quests[line.GetQuestId(step)];
                var state = quest.States[1];

                state.GetObjectives().OfType<CollectItemObjective>().Should().BeEmpty();
                state.GetObjectives()
                    .OfType<KillTargetObjective>()
                    .Should()
                    .ContainSingle(objective =>
                        objective.Group == line.NpcGroups[step] &&
                        objective.Amount > 0);

                state.KeyItemsGrantedOnAdvance.Should().ContainSingle().Which.Should().Be(line.ProofKeyItems[step]);
                quest.KeyItemsRemovedOnAbandon.Should().Contain(line.ProofKeyItems[step]);
                quest.KeyItemsRemovedOnComplete.Should().Contain(line.ProofKeyItems[step]);

                var proofName = GetKeyItemAttribute(line.ProofKeyItems[step]).Name;
                quest.States[1].JournalText.Should().Contain(proofName);
                quest.States[2].JournalText.Should().Contain(proofName);
            }

            quests[line.FinalQuestId]
                .States[1]
                .GetObjectives()
                .OfType<CollectItemObjective>()
                .Should()
                .BeEmpty();
        }
    }

    [Test]
    public void FinalQuests_UseFinalBossGroupAndGrantLineAchievement()
    {
        var quests = BuildCapstoneQuests();

        foreach (var line in CapstoneQuestDefinitionTestData.Lines)
        {
            var finalQuest = quests[line.FinalQuestId];

            finalQuest.States[1]
                .GetObjectives()
                .OfType<KillTargetObjective>()
                .Should()
                .ContainSingle(objective =>
                    objective.Group == line.NpcGroups[4] &&
                    objective.Amount == 1);

            finalQuest.OnCompleteActions.Should().ContainSingle();
        }

        var questSource = GetCapstoneQuestDefinitionSource();

        foreach (var line in CapstoneQuestDefinitionTestData.Lines)
        {
            questSource.Should()
                .Contain($"Achievement.GiveAchievement(player, AchievementType.{line.AchievementType})");
        }
    }

    [Test]
    public void KeyItemsAndAchievements_ArePlayerFacingAndActive()
    {
        var genericProofTerms = new[]
        {
            "Field Report",
            "Calibration Core",
            "Broken Seal",
            "Command Mark"
        };
        var genericProofEnumTerms = new[]
        {
            "FieldReport",
            "CalibrationCore",
            "BrokenSeal",
            "CommandMark"
        };

        foreach (var areaGroup in CapstoneQuestDefinitionTestData.AreaGroups)
        {
            var attribute = GetKeyItemAttribute(areaGroup.AccessKeyItem);

            attribute.Category.Should().Be(KeyItemCategoryType.Keys);
            attribute.IsActive.Should().BeTrue();
            attribute.Name.Should().Contain(areaGroup.Name);
        }

        foreach (var line in CapstoneQuestDefinitionTestData.Lines)
        {
            GetAchievementAttribute(line.AchievementType).IsActive.Should().BeTrue();

            foreach (var proof in line.ProofKeyItems)
            {
                var attribute = GetKeyItemAttribute(proof);

                attribute.Category.Should().Be(KeyItemCategoryType.QuestItems);
                attribute.IsActive.Should().BeTrue();
                attribute.Name.Should().Contain(line.DisplayName);
                attribute.Description.Should().Contain(line.DisplayName);
                attribute.Description.Should().Contain(line.AreaGroup.Name);
                attribute.Name.Should().NotContain(" Mark");

                foreach (var genericProofTerm in genericProofTerms)
                {
                    attribute.Name.Should().NotContain(genericProofTerm);
                }

                foreach (var genericProofEnumTerm in genericProofEnumTerms)
                {
                    proof.ToString().Should().NotContain(genericProofEnumTerm);
                }
            }
        }
    }

    [Test]
    public void CapstonePerks_RequireTheirFinalQuest()
    {
        var perks = BuildPerks();

        foreach (var line in CapstoneQuestDefinitionTestData.Lines)
        {
            var questRequirements = perks[line.PerkType]
                .PerkLevels
                .Values
                .SelectMany(level => level.Requirements)
                .OfType<PerkRequirementQuest>()
                .ToArray();

            questRequirements
                .Should()
                .ContainSingle(requirement => requirement.QuestId == line.FinalQuestId);
        }
    }

    [Test]
    public void CapstoneQuestDefinitions_FollowConcreteQuestBuilderPattern()
    {
        var questSource = GetCapstoneQuestDefinitionSource();

        questSource.Should().NotContain("BuildQuest(");
        questSource.Should().NotContain("CapstoneQuestDefinitionTestData.Line");

        foreach (var file in GetCapstoneQuestDefinitionFiles())
        {
            var source = File.ReadAllText(file);

            source.Should().Contain(": IQuestListDefinition");
            source.Should().Contain("private readonly QuestBuilder _builder = new();");
            source.Should().Contain("public Dictionary<string, QuestDetail> BuildQuests()");
        }
    }

    [Test]
    public void EnemyBlueprints_MatchQuestGroupsLootTablesAndFinalBossCapstoneFeats()
    {
        var root = FindRepositoryRoot();
        var modulePath = Path.Combine(root.FullName, "Module");
        var bloodFrenzyOnlyFeats = new[]
        {
            FeatType.BloodFrenzyTrait,
            FeatType.RendingCarve,
            FeatType.StimCanister,
            FeatType.BloodFrenzyFlurry,
            FeatType.ConcussiveChallenge,
        }.Select(feat => (int)feat);

        foreach (var line in CapstoneQuestDefinitionTestData.Lines)
        {
            for (var step = 0; step < 5; step++)
            {
                var enemyResref = line.EnemyResrefs[step];
                var skinResref = line.SkinResrefs[step];
                var weaponResref = line.WeaponResrefs[step];

                enemyResref.Length.Should().BeLessThanOrEqualTo(16);
                skinResref.Length.Should().BeLessThanOrEqualTo(16);
                weaponResref.Length.Should().BeLessThanOrEqualTo(16);

                var utcPath = Path.Combine(modulePath, "utc", $"{enemyResref}.utc.json");
                var skinPath = Path.Combine(modulePath, "uti", $"{skinResref}.uti.json");
                var weaponPath = Path.Combine(modulePath, "uti", $"{weaponResref}.uti.json");

                File.Exists(utcPath).Should().BeTrue($"{enemyResref} must exist for {line.DisplayName} step {step}");
                File.Exists(skinPath).Should().BeTrue($"{skinResref} must exist for {line.DisplayName} step {step}");
                File.Exists(weaponPath).Should().BeTrue($"{weaponResref} must exist for {line.DisplayName} step {step}");

                using var utcJson = JsonDocument.Parse(File.ReadAllText(utcPath));
                using var skinJson = JsonDocument.Parse(File.ReadAllText(skinPath));
                using var weaponJson = JsonDocument.Parse(File.ReadAllText(weaponPath));
                var rootElement = utcJson.RootElement;
                GetTypedValue(rootElement, "Tag").Should().Be(enemyResref);
                GetTypedValue(rootElement, "TemplateResRef").Should().Be(enemyResref);
                GetLocalInt(rootElement, "QUEST_NPC_GROUP_ID").Should().Be((int)line.NpcGroups[step]);
                GetLocalizedString(skinJson.RootElement, "LocalizedName").Should().NotContain(line.DisplayName);
                GetLocalizedString(weaponJson.RootElement, "LocalizedName").Should().NotContain(line.DisplayName);

                var expectedLootTable = CapstoneQuestDefinitionTestData.GeneralSpawnSteps.Contains(step)
                    ? line.AreaGroup.LessonLootTableId
                    : line.AreaGroup.BossLootTableId;
                GetLocalString(rootElement, "LOOT_TABLE_1").Should().Be($"{expectedLootTable},100,1");

                var featValues = GetFeatValues(rootElement).ToArray();
                featValues.Should().NotContain(bloodFrenzyOnlyFeats);
                var signatureFeat = GetSignatureFeat(featValues, enemyResref);
                Enum.GetName(typeof(FeatType), signatureFeat)
                    .Should()
                    .NotStartWith("Capstone", "generated NPC signature ability labels should use reusable ability names");
                Enum.GetName(typeof(FeatType), signatureFeat)
                    .Should()
                    .NotContain(ToIdentifier(line.DisplayName), "generated NPC signature abilities must not be branded to the capstone line");

                if (step == 4)
                {
                    GetLocalInt(rootElement, $"PERK_LEVEL_{(int)line.PerkType}").Should().Be(1);
                    featValues.Should().Contain((int)line.BossFeat);
                }
                else
                {
                    HasLocal(rootElement, $"PERK_LEVEL_{(int)line.PerkType}").Should().BeFalse();
                }
            }
        }
    }

    [Test]
    public void EnemyBlueprints_UseUniqueSpecialAbilityPackages()
    {
        var root = FindRepositoryRoot();
        var modulePath = Path.Combine(root.FullName, "Module");
        var abilityPackageKeys = new List<string>();

        foreach (var line in CapstoneQuestDefinitionTestData.Lines)
        {
            for (var step = 0; step < 5; step++)
            {
                var enemyResref = line.EnemyResrefs[step];
                var utcPath = Path.Combine(modulePath, "utc", $"{enemyResref}.utc.json");
                using var utcJson = JsonDocument.Parse(File.ReadAllText(utcPath));

                var package = GetFeatValues(utcJson.RootElement)
                    .Intersect(NpcAbilityFeatIds)
                    .OrderBy(feat => feat)
                    .ToArray();

                package.Should().HaveCountGreaterThanOrEqualTo(2, $"{enemyResref} needs a real special ability package");
                abilityPackageKeys.Add(string.Join(",", package));

                if (line.RequiredBeastRoleType.HasValue)
                {
                    package.Should()
                        .Contain(feat => CreatureOnlyNpcAbilityFeatIds.Contains(feat), $"{enemyResref} is a beast capstone enemy");
                }
                else
                {
                    package.Should()
                        .NotContain(CreatureOnlyNpcAbilityFeatIds, $"{enemyResref} is humanoid and should not use creature-only attacks");
                }
            }
        }

        abilityPackageKeys.Should().OnlyHaveUniqueItems("each generated capstone enemy should feel mechanically distinct");
    }

    [Test]
    public void GeneratedNPCSignatureAbilities_AreFirstClassNpcAbilityDefinitions()
    {
        var root = FindRepositoryRoot();
        var abilityPath = Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "AbilityDefinition",
            "NPC");

        var observedSignatureFeats = new HashSet<FeatType>();

        foreach (var line in CapstoneQuestDefinitionTestData.Lines)
        {
            for (var step = 0; step < 5; step++)
            {
                var enemyResref = line.EnemyResrefs[step];
                var utcPath = Path.Combine(root.FullName, "Module", "utc", $"{enemyResref}.utc.json");
                using var utcJson = JsonDocument.Parse(File.ReadAllText(utcPath));
                var feat = GetSignatureFeat(GetFeatValues(utcJson.RootElement), enemyResref);
                var featName = Enum.GetName(typeof(FeatType), feat)!;
                var filePath = Path.Combine(abilityPath, $"{feat}AbilityDefinition.cs");

                observedSignatureFeats.Add(feat);
                featName.Should().NotStartWith("Capstone");
                File.Exists(filePath).Should().BeTrue($"{feat} must be a real NPC ability definition");

                var source = File.ReadAllText(filePath);
                source.Should().Contain($"FeatType.{featName}");
                source.Should().Contain("NPCSignatureAbility.");
            }
        }

        observedSignatureFeats.Should().HaveCount(SignatureAbilityFeatEnd - SignatureAbilityFeatStart + 1);

        var helperPath = Path.Combine(abilityPath, "NPCSignatureAbility.cs");
        var helperSource = File.ReadAllText(helperPath);
        helperSource.Should().Contain("RecastGroup.Capstone");
        helperSource.Should().NotContain("RecastGroup.CapstoneSignature");
        helperSource.Should().NotContain("RecastGroup.NPCSignature");

        var recastGroupPath = Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "AbilityService",
            "RecastGroup.cs");
        var recastGroupSource = File.ReadAllText(recastGroupPath);
        recastGroupSource.Should().NotContain("CapstoneSignature");
        recastGroupSource.Should().NotContain("NPCSignature");
    }

    [Test]
    public void GeneratedSignatureAbilityFeatRows_UseAbilityNames()
    {
        var root = FindRepositoryRoot();
        var feat2daPath = Path.Combine(root.FullName, "SWLOR_Haks", "sw_2da", "feat.2da");

        var generatedRows = File.ReadLines(feat2daPath)
            .Select(line => line.Trim())
            .Where(line =>
            {
                var firstToken = line.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                return int.TryParse(firstToken, out var row) &&
                    row >= SignatureAbilityFeatStart &&
                    row <= SignatureAbilityFeatEnd;
            })
            .ToArray();

        generatedRows.Should().HaveCount(SignatureAbilityFeatEnd - SignatureAbilityFeatStart + 1);
        generatedRows.Should().NotContain(row => row.Contains("Capstone"));
        generatedRows.Should().NotContain(row => CapstoneQuestDefinitionTestData.Lines.Any(line => row.Contains(ToIdentifier(line.DisplayName))));
        generatedRows.Should().NotContain(row => row.Contains("Beast"));
        generatedRows.Should().NotContain(row => row.Contains("Blade"));
        generatedRows.Should().NotContain(row => row.Contains("Force"));
        generatedRows.Should().NotContain(row => row.Contains("Rifle"));
        generatedRows.Should().NotContain(row => row.Contains("Staff"));
        generatedRows.Should().NotContain(row => row.Contains("Tech"));
        generatedRows.Should().NotContain(row => row.Contains("Thrown"));
    }

    [Test]
    public void EnemyStatSkins_DefineVariedResistanceProfiles()
    {
        var root = FindRepositoryRoot();
        var modulePath = Path.Combine(root.FullName, "Module");
        var vulnerableFamilies = new HashSet<int>();

        foreach (var line in CapstoneQuestDefinitionTestData.Lines)
        {
            var lineProfiles = new List<string>();

            for (var step = 0; step < 5; step++)
            {
                var skinResref = line.SkinResrefs[step];
                var skinPath = Path.Combine(modulePath, "uti", $"{skinResref}.uti.json");
                using var skinJson = JsonDocument.Parse(File.ReadAllText(skinPath));
                var values = new List<int>();

                foreach (var subtype in ResistanceSubtypes)
                {
                    var rawCost = GetItemPropertyCost(skinJson.RootElement, 133, subtype);
                    rawCost.Should().NotBeNull($"{skinResref} must define every resistance family");

                    var decoded = Resistance.DecodeItemPropertyCostTableValue(rawCost!.Value);
                    decoded.Should().BeInRange(-100, 100, $"{skinResref} resistance values must be valid SWLOR ratings");
                    values.Add(decoded);

                    if (decoded < 0)
                    {
                        vulnerableFamilies.Add(subtype);
                    }
                }

                values.Should().Contain(value => value < 0, $"{skinResref} should have at least one counter-resistance");
                values.Should().Contain(value => value > 0, $"{skinResref} should have at least one strength");
                lineProfiles.Add(string.Join(",", values));
            }

            lineProfiles.Should().OnlyHaveUniqueItems($"{line.DisplayName} enemies should not share one resistance profile across the line");
        }

        vulnerableFamilies.Should().BeEquivalentTo(ResistanceSubtypes, "capstone enemies should collectively expose every resistance counter");
    }

    [Test]
    public void SpawnTables_ContainOnlyGeneralEnemySteps()
    {
        var spawnTables = BuildSpawnTables();

        foreach (var areaGroup in CapstoneQuestDefinitionTestData.AreaGroups)
        {
            var tableId = areaGroup.SpawnTableId;
            spawnTables.Should().ContainKey(tableId);

            var spawnResrefs = spawnTables[tableId]
                .Spawns
                .Select(spawn => spawn.Resref)
                .ToArray();
            var areaLines = CapstoneQuestDefinitionTestData.Lines.Where(line => line.AreaGroup == areaGroup).ToArray();
            var expectedGeneralResrefs = areaLines
                .SelectMany(line => CapstoneQuestDefinitionTestData.GeneralSpawnSteps.Select(step =>
                    line.EnemyResrefs[step]))
                .ToArray();
            var excludedOnDemandResrefs = areaLines
                .SelectMany(line => CapstoneQuestDefinitionTestData.OnDemandEncounterSteps.Select(step =>
                    line.EnemyResrefs[step]))
                .ToArray();

            spawnResrefs.Should().BeEquivalentTo(expectedGeneralResrefs);
            spawnResrefs.Should().NotContain(excludedOnDemandResrefs);
        }
    }

    private static Dictionary<string, SpawnTable> BuildSpawnTables()
    {
        var spawnTables = new Dictionary<string, SpawnTable>();
        var spawnListTypes = typeof(ViscaraSpawnDefinition).Assembly.GetTypes()
            .Where(type => typeof(ISpawnListDefinition).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract);

        foreach (var type in spawnListTypes)
        {
            var definition = (ISpawnListDefinition)Activator.CreateInstance(type)!;

            foreach (var table in definition.BuildSpawnTables())
            {
                spawnTables.Add(table.Key, table.Value);
            }
        }

        return spawnTables;
    }

    [Test]
    public void LootTables_AreCreatedForEachAreaGroupWithoutQuestProofItems()
    {
        var lootTables = new CapstoneLootTableDefinition().BuildLootTables();

        lootTables.Should().HaveCount(CapstoneQuestDefinitionTestData.AreaGroups.Count * 2);

        foreach (var areaGroup in CapstoneQuestDefinitionTestData.AreaGroups)
        {
            var lessonLoot = areaGroup.LessonLootTableId;
            var bossLoot = areaGroup.BossLootTableId;

            lootTables.Should().ContainKey(lessonLoot);
            lootTables.Should().ContainKey(bossLoot);
            lootTables[lessonLoot].Select(item => item.Resref).Should().NotContain(item => item.StartsWith("capstone"));
            lootTables[bossLoot].Select(item => item.Resref).Should().NotContain(item => item.StartsWith("capstone"));
        }
    }

    [Test]
    public void WaypointBlueprints_AreAvailableForAreaSpawnsAndOnDemandBossSpawns()
    {
        var root = FindRepositoryRoot();
        var modulePath = Path.Combine(root.FullName, "Module");
        var palettePath = Path.Combine(modulePath, "itp", "waypointpalcus.itp.json");
        using var paletteJson = JsonDocument.Parse(File.ReadAllText(palettePath));
        var paletteResrefs = FindTypedValues(paletteJson.RootElement, "RESREF").ToArray();

        foreach (var areaGroup in CapstoneQuestDefinitionTestData.AreaGroups)
        {
            var waypointResref = areaGroup.GeneralWaypointResref;
            var waypointPath = Path.Combine(modulePath, "utw", $"{waypointResref}.utw.json");

            File.Exists(waypointPath).Should().BeTrue($"{areaGroup.Name} needs a general spawn waypoint blueprint");
            paletteResrefs.Should().Contain(waypointResref);

            using var waypointJson = JsonDocument.Parse(File.ReadAllText(waypointPath));
            GetTypedValue(waypointJson.RootElement, "Tag").Should().Be(areaGroup.SpawnTableId);
            GetTypedValue(waypointJson.RootElement, "TemplateResRef").Should().Be(waypointResref);
        }

        foreach (var line in CapstoneQuestDefinitionTestData.Lines)
        {
            foreach (var step in CapstoneQuestDefinitionTestData.OnDemandEncounterSteps)
            {
                var waypointResref = line.EncounterSpawnWaypointResrefs[step];
                var waypointPath = Path.Combine(modulePath, "utw", $"{waypointResref}.utw.json");

                File.Exists(waypointPath).Should().BeTrue($"{line.DisplayName} needs an on-demand boss spawn waypoint blueprint");
                paletteResrefs.Should().Contain(waypointResref);

                using var waypointJson = JsonDocument.Parse(File.ReadAllText(waypointPath));
                GetTypedValue(waypointJson.RootElement, "Tag").Should().Be(line.EncounterSpawnWaypointTags[step]);
                GetTypedValue(waypointJson.RootElement, "TemplateResRef").Should().Be(waypointResref);
            }
        }
    }

    private static KeyItemAttribute GetKeyItemAttribute(KeyItemType keyItemType)
    {
        return typeof(KeyItemType)
            .GetMember(keyItemType.ToString())
            .Single()
            .GetCustomAttribute<KeyItemAttribute>()!;
    }

    private static AchievementAttribute GetAchievementAttribute(AchievementType achievementType)
    {
        return typeof(AchievementType)
            .GetMember(achievementType.ToString())
            .Single()
            .GetCustomAttribute<AchievementAttribute>()!;
    }

    private static Dictionary<string, QuestDetail> BuildCapstoneQuests()
    {
        var quests = new Dictionary<string, QuestDetail>();
        var questDefinitions = typeof(VibrobladeCapstoneQuestDefinition)
            .Assembly
            .GetTypes()
            .Where(type =>
                typeof(IQuestListDefinition).IsAssignableFrom(type) &&
                type.Name.EndsWith("CapstoneQuestDefinition") &&
                !type.IsAbstract);

        foreach (var questDefinitionType in questDefinitions)
        {
            var questDefinition = (IQuestListDefinition)Activator.CreateInstance(questDefinitionType)!;

            foreach (var (questId, quest) in questDefinition.BuildQuests())
            {
                quests[questId] = quest;
            }
        }

        return quests;
    }

    private static Dictionary<PerkType, PerkDetail> BuildPerks()
    {
        var perks = new Dictionary<PerkType, PerkDetail>();
        var perkDefinitions = typeof(IPerkListDefinition)
            .Assembly
            .GetTypes()
            .Where(type =>
                typeof(IPerkListDefinition).IsAssignableFrom(type) &&
                !type.IsInterface &&
                !type.IsAbstract);

        foreach (var perkDefinitionType in perkDefinitions)
        {
            var perkDefinition = Activator.CreateInstance(perkDefinitionType)!;

            foreach (var method in perkDefinitionType
                         .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                         .Where(method =>
                             method.ReturnType == typeof(void) &&
                             method.GetParameters().Length == 0 &&
                             !method.Name.Contains('<'))
                         .OrderBy(method => method.MetadataToken))
            {
                method.Invoke(perkDefinition, null);
            }

            var builder = perkDefinitionType
                .GetField("_builder", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(perkDefinition)!;

            var builtPerks = (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder)
                .GetField("_perks", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(builder)!;

            foreach (var (perkType, perk) in builtPerks)
            {
                perks[perkType] = perk;
            }
        }

        return perks;
    }

    private static FeatType GetSignatureFeat(IEnumerable<int> featValues, string enemyResref)
    {
        var signatureFeats = featValues
            .Where(feat => feat >= SignatureAbilityFeatStart && feat <= SignatureAbilityFeatEnd)
            .ToArray();

        signatureFeats.Should().ContainSingle($"{enemyResref} must carry one generated reusable signature ability");
        return (FeatType)signatureFeats[0];
    }

    private static string ToIdentifier(string value)
    {
        return string.Concat(value.Where(char.IsLetterOrDigit));
    }

    private static IEnumerable<string> GetCapstoneQuestDefinitionFiles()
    {
        var root = FindRepositoryRoot();
        var questPath = Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "QuestDefinition");

        return Directory.GetFiles(questPath, "*CapstoneQuestDefinition.cs");
    }

    private static string GetCapstoneQuestDefinitionSource()
    {
        return string.Join(
            "\n",
            GetCapstoneQuestDefinitionFiles().Select(File.ReadAllText));
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
        {
            directory = directory.Parent;
        }

        return directory ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }

    private static string GetTypedValue(JsonElement root, string propertyName)
    {
        return root.GetProperty(propertyName).GetProperty("value").GetString()!;
    }

    private static string GetLocalizedString(JsonElement root, string propertyName)
    {
        var value = root.GetProperty(propertyName).GetProperty("value");
        return value.TryGetProperty("0", out var localized)
            ? localized.GetString()!
            : value.GetString()!;
    }

    private static bool HasLocal(JsonElement root, string variableName)
    {
        return root.GetProperty("VarTable")
            .GetProperty("value")
            .EnumerateArray()
            .Any(variable => GetTypedValue(variable, "Name") == variableName);
    }

    private static int GetLocalInt(JsonElement root, string variableName)
    {
        return FindLocal(root, variableName)
            .GetProperty("Value")
            .GetProperty("value")
            .GetInt32();
    }

    private static string GetLocalString(JsonElement root, string variableName)
    {
        return FindLocal(root, variableName)
            .GetProperty("Value")
            .GetProperty("value")
            .GetString()!;
    }

    private static JsonElement FindLocal(JsonElement root, string variableName)
    {
        return root.GetProperty("VarTable")
            .GetProperty("value")
            .EnumerateArray()
            .Single(variable => GetTypedValue(variable, "Name") == variableName);
    }

    private static IEnumerable<int> GetFeatValues(JsonElement root)
    {
        return root.GetProperty("FeatList")
            .GetProperty("value")
            .EnumerateArray()
            .Select(feat => feat.GetProperty("Feat").GetProperty("value").GetInt32());
    }

    private static int? GetItemPropertyCost(JsonElement root, int propertyName, int subtype)
    {
        foreach (var itemProperty in root.GetProperty("PropertiesList").GetProperty("value").EnumerateArray())
        {
            if (itemProperty.GetProperty("PropertyName").GetProperty("value").GetInt32() != propertyName)
                continue;

            if (itemProperty.GetProperty("Subtype").GetProperty("value").GetInt32() != subtype)
                continue;

            return itemProperty.GetProperty("CostValue").GetProperty("value").GetInt32();
        }

        return null;
    }

    private static IEnumerable<string> FindTypedValues(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty(propertyName, out var typedProperty) &&
                typedProperty.ValueKind == JsonValueKind.Object &&
                typedProperty.TryGetProperty("value", out var value) &&
                value.ValueKind == JsonValueKind.String)
            {
                yield return value.GetString()!;
            }

            foreach (var property in element.EnumerateObject())
            {
                foreach (var typedValue in FindTypedValues(property.Value, propertyName))
                {
                    yield return typedValue;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                foreach (var typedValue in FindTypedValues(item, propertyName))
                {
                    yield return typedValue;
                }
            }
        }
    }
}
