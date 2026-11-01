using FluentAssertions;
using NUnit.Framework;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
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
                "Hutlar Jedi Trial Halls",
                "Tatooine Forge Caverns",
                "Smuggler's Moon Fight Club Backrooms",
                "CZ-220 Breaker Yard",
                "Anchorhead Canyon Range",
                "Czerka Arms Test Range",
                "Hutlar Qion Test Site",
                "Mon Cala Sith Crypt Depths",
                "Viscara Republic Engineering Bunker",
                "Mon Cala Medical Sublevel",
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
            var graphPath = Path.Combine(root.FullName, "SWLOR.Game.Server", "ConversationData", $"{giver.DialogueResref}.conversation.json");

            File.Exists(utcPath).Should().BeTrue($"{giver.Name} needs a quest giver UTC blueprint");
            File.Exists(graphPath).Should().BeTrue($"{giver.Name} needs a quest giver dialogue");
            paletteResrefs.Should().Contain(giver.Resref, $"{giver.Name} must be placeable from the creature palette");

            using var utcJson = JsonDocument.Parse(File.ReadAllText(utcPath));
            GetTypedValue(utcJson.RootElement, "Conversation").Should().Be(giver.DialogueResref);
            GetTypedValue(utcJson.RootElement, "Tag").Should().Be(giver.Resref);
            GetTypedValue(utcJson.RootElement, "TemplateResRef").Should().Be(giver.Resref);

            var dialogue = File.ReadAllText(graphPath);
            dialogue.Should().Contain("condition-can-accept-quest");
            dialogue.Should().NotContain("condition-any-skill");

            foreach (var questId in line.GetQuestIds())
            {
                dialogue.Should().Contain(questId);
                dialogue.Should().Contain("\"Key\": \"action-accept-quest\"");
                dialogue.Should().Contain("\"Key\": \"action-advance-quest\"");
            }
        }
    }

    [Test]
    public void BuildQuests_CreatesExistingFiveStepChainsAndTwoIndependentTwinPistolTrials()
    {
        var quests = BuildCapstoneQuests();

        quests.Should().HaveCount(CapstoneQuestDefinitionTestData.Lines.Count * 5 + 2);

        foreach (var questId in new[] { "twin_suns_mastery", "final_salvo_mastery" })
        {
            quests[questId].States.Should().HaveCount(2, "Twin Pistol mastery demonstrates its actual firing or movement mechanics before a trainer turn-in");
            quests[questId].States[1].GetObjectives().Should().ContainSingle()
                .Which.Should().BeOfType<TwinPistolTrialObjective>();
            quests[questId].States[2].GetObjectives().Should().BeEmpty();
            quests[questId].Prerequisites.OfType<RequiredQuestPrerequisite>().Should().BeEmpty("the two mastery trials unlock independently");
            var skill = quests[questId].Prerequisites.OfType<RequiredSkillRankPrerequisite>().Should().ContainSingle().Which;
            skill.SkillType.Should().Be(SkillType.TwinPistol);
            skill.RequiredRank.Should().Be(50);
        }

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
    public void KillSteps_HuntTheDungeonTierWithTheLinesOwnProof()
    {
        var quests = BuildCapstoneQuests();
        var proofKeys = CapstoneProofDefinitions.All.Select(proof => proof.Key).ToArray();
        proofKeys.Should().OnlyHaveUniqueItems();

        foreach (var line in CapstoneQuestDefinitionTestData.Lines)
        {
            var proofKey = ToProofKey(line.DisplayName);
            proofKeys.Should().Contain(proofKey, $"{line.DisplayName} needs a capstone proof");
            var areaLines = CapstoneQuestDefinitionTestData.Lines.Where(other => other.AreaGroup == line.AreaGroup).ToArray();

            foreach (var step in CapstoneQuestDefinitionTestData.GeneralSpawnSteps.Concat(CapstoneQuestDefinitionTestData.FixedSpawnSteps))
            {
                // Every line in the dungeon hunts the same tier, proving its own mechanic on each kill.
                var objective = quests[line.GetQuestId(step)].States[1].GetObjectives()
                    .OfType<CapstoneProofKillObjective>()
                    .Should().ContainSingle($"{line.GetQuestId(step)} is a capstone proof kill step").Subject;
                objective.Group.Should().Be(line.NpcGroups[step]);
                objective.ProofKey.Should().Be(proofKey);
                objective.CreditedGroups.Should().BeEquivalentTo(areaLines.Select(other => other.NpcGroups[step]));
            }

            foreach (var step in CapstoneQuestDefinitionTestData.OnDemandEncounterSteps)
            {
                quests[line.GetQuestId(step)].States[1].GetObjectives().OfType<CapstoneProofKillObjective>()
                    .Should().BeEmpty($"{line.GetQuestId(step)} is a boss encounter");
            }
        }
    }

    [Test]
    public void CapstoneProofs_NeverCountTheCapstoneItself()
    {
        // The quest chain unlocks the capstone, so its own ability or status can never be the proof.
        foreach (var proof in CapstoneProofDefinitions.All)
        {
            var capstone = string.Concat(proof.Key.Split('_').Select(word => char.ToUpperInvariant(word[0]) + word[1..]));
            var names = proof.EnemyStatusEffects.Concat(proof.SupportStatusEffects).Select(type => type.Name)
                .Concat(proof.AbilityHits.Select(feat => feat.ToString()));
            names.Should().NotContain(name => name.StartsWith(capstone, StringComparison.OrdinalIgnoreCase),
                $"proof '{proof.Key}' must not count its own capstone");
        }
    }

    private static string ToProofKey(string displayName) =>
        Regex.Replace(displayName.ToLowerInvariant().Replace("'", string.Empty), "[^a-z0-9]+", "_").Trim('_');

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

                var expectedLootTable = !CapstoneQuestDefinitionTestData.OnDemandEncounterSteps.Contains(step)
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
            var areaLines = CapstoneQuestDefinitionTestData.Lines.Where(line => line.AreaGroup == areaGroup).ToArray();
            foreach (var step in CapstoneQuestDefinitionTestData.GeneralSpawnSteps)
            {
                // Each general step has its own table: adepts near the entrance, specialists deeper in.
                var tableId = areaGroup.GetGeneralSpawnTableId(step);
                spawnTables.Should().ContainKey(tableId);

                spawnTables[tableId]
                    .Spawns
                    .Select(spawn => spawn.Resref)
                    .Should()
                    .BeEquivalentTo(areaLines.Select(line => line.EnemyResrefs[step]), $"{tableId} holds only step {step} enemies");
            }
        }
    }

    [Test]
    public void FixedSpawnTables_HoldOnlyTheirLinesEnemy()
    {
        var spawnTables = BuildSpawnTables();

        foreach (var line in CapstoneQuestDefinitionTestData.Lines)
        {
            foreach (var step in CapstoneQuestDefinitionTestData.FixedSpawnSteps)
            {
                var tableId = line.EncounterSpawnWaypointTags[step];
                spawnTables.Should().ContainKey(tableId, $"{line.DisplayName} step {step} needs its own spawn table");

                var spawn = spawnTables[tableId].Spawns.Should().ContainSingle().Subject;
                spawn.Resref.Should().Be(line.EnemyResrefs[step]);
                spawn.IsRare.Should().BeFalse();
            }
        }
    }

    [Test]
    public void KillObjectives_HaveEnoughSpawnsInTheirDungeon()
    {
        var quests = BuildCapstoneQuests();
        var spawnTables = BuildSpawnTables();
        var modulePath = Path.Combine(FindRepositoryRoot().FullName, "Module");
        var lineSteps = CapstoneQuestDefinitionTestData.Lines
            .SelectMany(line => line.NpcGroups.Select((group, step) => (group, line, step)))
            .ToDictionary(entry => entry.group, entry => (entry.line, entry.step));

        foreach (var quest in quests.Values)
        {
            foreach (var objective in quest.States.Values.SelectMany(state => state.GetObjectives()).OfType<KillTargetObjective>())
            {
                lineSteps.Should().ContainKey(objective.Group, $"{quest.QuestId} kills a group this spawn audit does not cover");
                var (line, step) = lineSteps[objective.Group];
                var areaResref = line.AreaGroup.DungeonAreaResref;
                using var git = JsonDocument.Parse(File.ReadAllText(Path.Combine(modulePath, "git", $"{areaResref}.git.json")));
                var waypointTags = git.RootElement.GetProperty("WaypointList").GetProperty("value")
                    .EnumerateArray()
                    .Select(waypoint => GetTypedValue(waypoint, "Tag"))
                    .ToArray();

                if (CapstoneQuestDefinitionTestData.FixedSpawnSteps.Contains(step))
                {
                    // Fixed spawns guarantee the whole kill count is standing in the dungeon at once.
                    waypointTags.Count(tag => tag == line.EncounterSpawnWaypointTags[step])
                        .Should()
                        .BeGreaterThanOrEqualTo(objective.Amount, $"{quest.QuestId} needs {objective.Amount} fixed spawns in {areaResref}");
                }
                else if (CapstoneQuestDefinitionTestData.GeneralSpawnSteps.Contains(step))
                {
                    // General steps roll from their tier's table; at least one of the group is expected at any time
                    // and later kills come from respawns.
                    var tableId = line.AreaGroup.GetGeneralSpawnTableId(step);
                    var spawnPoints = waypointTags.Count(tag => tag == tableId);
                    if (HasLocal(git.RootElement, "CREATURE_SPAWN_TABLE_ID") &&
                        GetLocalString(git.RootElement, "CREATURE_SPAWN_TABLE_ID") == tableId)
                    {
                        spawnPoints += GetAreaCreatureSpawnCount(modulePath, areaResref, git.RootElement);
                    }

                    var spawns = spawnTables[tableId].Spawns;
                    var weight = spawns.Where(spawn => spawn.Resref == line.EnemyResrefs[step]).Sum(spawn => spawn.Weight);
                    var expected = spawnPoints * weight / (double)spawns.Sum(spawn => spawn.Weight);
                    expected.Should().BeGreaterThanOrEqualTo(1, $"{quest.QuestId} needs its enemies to be present in {areaResref}");
                }
                else
                {
                    objective.Amount.Should().Be(1, $"{quest.QuestId} is an on-demand encounter that spawns a single boss");
                }
            }
        }
    }

    [Test]
    public void SpawnPoints_StandApartInTheirDungeon()
    {
        // The random spawn bake (Walkmesh.BakeArea) keeps spawn points 6 m clear of creatures, and the general
        // capstone spawn points were laid out at that spacing. Inner circle guards never wander from their fixed
        // spawns, so they keep a wider gap from every other spawn point to stay spread out.
        const double MinimumSpacingMeters = 6.0;
        const double InnerCircleSpacingMeters = 10.0;
        var spawnTables = BuildSpawnTables();
        var modulePath = Path.Combine(FindRepositoryRoot().FullName, "Module");

        foreach (var areaResref in CapstoneQuestDefinitionTestData.AreaGroups.Select(group => group.DungeonAreaResref).Distinct())
        {
            using var git = JsonDocument.Parse(File.ReadAllText(Path.Combine(modulePath, "git", $"{areaResref}.git.json")));
            var spawnPoints = git.RootElement.GetProperty("WaypointList").GetProperty("value")
                .EnumerateArray()
                .Select(waypoint => (
                    Tag: GetTypedValue(waypoint, "Tag"),
                    X: waypoint.GetProperty("XPosition").GetProperty("value").GetDouble(),
                    Y: waypoint.GetProperty("YPosition").GetProperty("value").GetDouble()))
                .Where(waypoint => spawnTables.ContainsKey(waypoint.Tag) || waypoint.Tag.EndsWith("_WD_SPAWN") || waypoint.Tag.EndsWith("_MS_SPAWN"))
                .ToArray();

            spawnPoints.Should().Contain(point => point.Tag.EndsWith("_IC_SPAWN"), $"{areaResref} has inner circle spawn points");
            for (var i = 0; i < spawnPoints.Length; i++)
            {
                for (var j = i + 1; j < spawnPoints.Length; j++)
                {
                    var (a, b) = (spawnPoints[i], spawnPoints[j]);
                    var distance = Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
                    var minimum = a.Tag.EndsWith("_IC_SPAWN") || b.Tag.EndsWith("_IC_SPAWN")
                        ? InnerCircleSpacingMeters
                        : MinimumSpacingMeters;
                    distance.Should().BeGreaterThanOrEqualTo(minimum,
                        $"{areaResref} {a.Tag} ({a.X:0.0},{a.Y:0.0}) and {b.Tag} ({b.X:0.0},{b.Y:0.0}) are too close");
                }
            }

            // Inner circle guards also keep clear of the warden altars (appearance 2174) and their activators.
            const int WardenAltarAppearance = 2174;
            var altars = git.RootElement.GetProperty("Placeable List").GetProperty("value")
                .EnumerateArray()
                .Where(placeable => placeable.GetProperty("Appearance").GetProperty("value").GetInt32() == WardenAltarAppearance ||
                                    GetTypedValue(placeable, "Tag").EndsWith("_wd_call"))
                .Select(placeable => (
                    Tag: GetTypedValue(placeable, "Tag"),
                    X: placeable.GetProperty("X").GetProperty("value").GetDouble(),
                    Y: placeable.GetProperty("Y").GetProperty("value").GetDouble()))
                .ToArray();
            altars.Should().NotBeEmpty($"{areaResref} has warden altars");
            foreach (var guard in spawnPoints.Where(point => point.Tag.EndsWith("_IC_SPAWN")))
            {
                foreach (var altar in altars)
                {
                    Math.Sqrt(Math.Pow(guard.X - altar.X, 2) + Math.Pow(guard.Y - altar.Y, 2)).Should().BeGreaterThanOrEqualTo(InnerCircleSpacingMeters,
                        $"{areaResref} {guard.Tag} ({guard.X:0.0},{guard.Y:0.0}) is too close to warden altar {altar.Tag} ({altar.X:0.0},{altar.Y:0.0})");
                }
            }
        }
    }

    [Test]
    public void GeneralSpawnPoints_KeepEachRoomToOneTier()
    {
        // Adepts hold the rooms nearest the entrance and specialists the deeper ones (the walk order is checked
        // in engine by CapstoneTierSpawnDepthEngineTests); points within 12 m share a room and never mix tiers.
        const double RoomMeters = 12.0;
        var modulePath = Path.Combine(FindRepositoryRoot().FullName, "Module");

        foreach (var areaGroup in CapstoneQuestDefinitionTestData.AreaGroups)
        {
            using var git = JsonDocument.Parse(File.ReadAllText(Path.Combine(modulePath, "git", $"{areaGroup.DungeonAreaResref}.git.json")));
            var points = git.RootElement.GetProperty("WaypointList").GetProperty("value")
                .EnumerateArray()
                .Select(waypoint => (
                    Tag: GetTypedValue(waypoint, "Tag"),
                    X: waypoint.GetProperty("XPosition").GetProperty("value").GetDouble(),
                    Y: waypoint.GetProperty("YPosition").GetProperty("value").GetDouble()))
                .ToArray();
            var adepts = points.Where(point => point.Tag == areaGroup.SpawnTableId).ToArray();
            // Inner circle guards stand deep as well, never in an adept room.
            var deepPoints = points.Where(point => point.Tag == areaGroup.DeepSpawnTableId || point.Tag.EndsWith("_IC_SPAWN")).ToArray();

            (adepts.Length + deepPoints.Count(point => point.Tag == areaGroup.DeepSpawnTableId)).Should().Be(20, $"{areaGroup.DungeonAreaResref} has twenty general spawn points");
            adepts.Should().NotBeEmpty();
            deepPoints.Should().NotBeEmpty();
            foreach (var adept in adepts)
            {
                foreach (var deepPoint in deepPoints)
                {
                    Math.Sqrt(Math.Pow(adept.X - deepPoint.X, 2) + Math.Pow(adept.Y - deepPoint.Y, 2)).Should().BeGreaterThan(RoomMeters,
                        $"{areaGroup.DungeonAreaResref} adept point ({adept.X:0.0},{adept.Y:0.0}) shares a room with {deepPoint.Tag} point ({deepPoint.X:0.0},{deepPoint.Y:0.0})");
                }
            }
        }
    }

    // Mirrors Spawn.CalculateCreatureSpawnCount for areas that register their table as an area local.
    private static int GetAreaCreatureSpawnCount(string modulePath, string areaResref, JsonElement git)
    {
        if (HasLocal(git, "CREATURE_SPAWN_COUNT") && GetLocalInt(git, "CREATURE_SPAWN_COUNT") > 0)
            return GetLocalInt(git, "CREATURE_SPAWN_COUNT");

        using var are = JsonDocument.Parse(File.ReadAllText(Path.Combine(modulePath, "are", $"{areaResref}.are.json")));
        var size = are.RootElement.GetProperty("Width").GetProperty("value").GetInt32() *
                   are.RootElement.GetProperty("Height").GetProperty("value").GetInt32();
        return size switch
        {
            <= 12 => 3,
            <= 32 => 6,
            <= 64 => 14,
            <= 256 => 20,
            <= 512 => 35,
            <= 1024 => 45,
            _ => 0,
        };
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
            foreach (var (waypointResref, tableId) in new[]
                     {
                         (areaGroup.GeneralWaypointResref, areaGroup.SpawnTableId),
                         (areaGroup.DeepWaypointResref, areaGroup.DeepSpawnTableId),
                     })
            {
                var waypointPath = Path.Combine(modulePath, "utw", $"{waypointResref}.utw.json");

                File.Exists(waypointPath).Should().BeTrue($"{areaGroup.Name} needs a {tableId} spawn waypoint blueprint");
                paletteResrefs.Should().Contain(waypointResref);

                using var waypointJson = JsonDocument.Parse(File.ReadAllText(waypointPath));
                GetTypedValue(waypointJson.RootElement, "Tag").Should().Be(tableId);
                GetTypedValue(waypointJson.RootElement, "TemplateResRef").Should().Be(waypointResref);
            }
        }

        foreach (var line in CapstoneQuestDefinitionTestData.Lines)
        {
            foreach (var step in CapstoneQuestDefinitionTestData.OnDemandEncounterSteps.Concat(CapstoneQuestDefinitionTestData.FixedSpawnSteps))
            {
                var waypointResref = line.EncounterSpawnWaypointResrefs[step];
                var waypointPath = Path.Combine(modulePath, "utw", $"{waypointResref}.utw.json");

                File.Exists(waypointPath).Should().BeTrue($"{line.DisplayName} needs a step {step} spawn waypoint blueprint");
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
