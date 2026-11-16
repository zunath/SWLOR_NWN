using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.QuestService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Perks;

/// <summary>
/// The astromech skills are copies of humanoid skill families with new names (tools/AstromechSkillMirrors.json).
/// These tests hold each copy to its source, so a change to the source family cannot leave the copy behind.
/// </summary>
public class AstromechSkillMirrorTests
{
    /// <param name="AbilitySkill">
    /// The skill the mirrored abilities act as. Armaments abilities keep their source weapon skill, because the
    /// astromech's tools follow the staff and rifle combat rules; the rank behind them is Armaments.
    /// </param>
    private sealed record PerkPair(string Skill, string SourceSkill, string AbilitySkill, PerkType Source, PerkType Mirror);

    private static string Root
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                directory = directory.Parent;
            return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
        }
    }

    private static List<PerkPair> Pairs()
    {
        using var config = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "tools", "AstromechSkillMirrors.json")));
        var pairs = new List<PerkPair>();
        foreach (var mirror in config.RootElement.GetProperty("mirrors").EnumerateArray())
        {
            foreach (var line in mirror.GetProperty("lines").EnumerateArray())
            {
                foreach (var perk in line.GetProperty("perks").EnumerateObject())
                {
                    var skill = mirror.GetProperty("skill").GetString()!;
                    pairs.Add(new PerkPair(
                        skill,
                        line.TryGetProperty("source_skill", out var lineSource)
                            ? lineSource.GetString()!
                            : mirror.GetProperty("source_skill").GetString()!,
                        line.TryGetProperty("identity", out var identity) ? identity.GetString()! : skill,
                        Enum.Parse<PerkType>(perk.Name),
                        Enum.Parse<PerkType>(perk.Value.GetProperty("id").GetString()!)));
                }
            }
        }

        return pairs;
    }

    private static Dictionary<FeatType, AbilityDetail> Abilities()
    {
        return typeof(IAbilityListDefinition).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && !type.IsInterface && typeof(IAbilityListDefinition).IsAssignableFrom(type))
            .SelectMany(type => ((IAbilityListDefinition)Activator.CreateInstance(type)!).BuildAbilities())
            .ToDictionary(pair => pair.Key, pair => pair.Value);
    }

    private static (StatType Stat, int? Amount) Bonus(PerkStatBonus bonus)
    {
        try
        {
            return (bonus.Stat, bonus.Calculate(0));
        }
        catch (Exception)
        {
            // A bonus computed from the live character cannot be read without the game engine.
            return (bonus.Stat, null);
        }
    }

    private static float? Seconds(Func<float?> read)
    {
        try
        {
            return read();
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string Requirement(IAbilityActivationRequirement requirement) => requirement switch
    {
        AbilityRequirementStamina stamina => $"STM {stamina.RequiredSTM}",
        _ => requirement.GetType().Name
    };

    [Test]
    public void EveryMirroredPerkMatchesItsSourceLevelForLevel()
    {
        using var perks = new DroidPerkCacheScope();
        var all = Perk.GetAllPerks();
        var pairs = Pairs();
        pairs.Should().NotBeEmpty();

        foreach (var pair in pairs)
        {
            var source = all[pair.Source];
            var mirror = all[pair.Mirror];
            var sourceSkill = Enum.Parse<SkillType>(pair.SourceSkill);
            var mirrorSkill = Enum.Parse<SkillType>(pair.Skill);

            mirror.PerkLevels.Keys.Should().BeEquivalentTo(source.PerkLevels.Keys, $"{pair.Mirror} has the levels of {pair.Source}");
            foreach (var (level, sourceLevel) in source.PerkLevels)
            {
                var mirrorLevel = mirror.PerkLevels[level];
                mirrorLevel.Price.Should().Be(sourceLevel.Price, $"{pair.Mirror} level {level} costs what {pair.Source} costs");
                mirrorLevel.GrantedFeats.Should().HaveCount(sourceLevel.GrantedFeats.Count);
                mirrorLevel.StatBonuses.Select(Bonus)
                    .Should().BeEquivalentTo(sourceLevel.StatBonuses.Select(Bonus));

                var sourceRanks = sourceLevel.Requirements.OfType<PerkRequirementSkill>()
                    .Select(requirement => (requirement.Type == sourceSkill ? mirrorSkill : requirement.Type, requirement.RequiredRank));
                mirrorLevel.Requirements.OfType<PerkRequirementSkill>()
                    .Select(requirement => (requirement.Type, requirement.RequiredRank))
                    .Should().BeEquivalentTo(sourceRanks, $"{pair.Mirror} level {level} asks for the rank {pair.Source} asks for");
                mirrorLevel.Requirements.OfType<PerkRequirementQuest>().Count()
                    .Should().Be(sourceLevel.Requirements.OfType<PerkRequirementQuest>().Count());
            }
        }
    }

    [Test]
    public void EveryMirroredAbilityCostsAndRecastsLikeItsSource()
    {
        using var perks = new DroidPerkCacheScope();
        var all = Perk.GetAllPerks();
        var abilities = Abilities();

        foreach (var pair in Pairs())
        {
            foreach (var (level, sourceLevel) in all[pair.Source].PerkLevels)
            {
                var mirrorFeats = all[pair.Mirror].PerkLevels[level].GrantedFeats;
                for (var index = 0; index < sourceLevel.GrantedFeats.Count; index++)
                {
                    if (!abilities.TryGetValue(sourceLevel.GrantedFeats[index], out var source))
                        continue;

                    var mirror = abilities[mirrorFeats[index]];
                    mirror.AbilityLevel.Should().Be(source.AbilityLevel);
                    Seconds(() => mirror.RecastDelay?.Invoke(0)).Should().Be(Seconds(() => source.RecastDelay?.Invoke(0)),
                        $"{mirrorFeats[index]} recasts like {sourceLevel.GrantedFeats[index]}");
                    Seconds(() => mirror.ActivationDelay?.Invoke(0, 0, mirror.AbilityLevel))
                        .Should().Be(Seconds(() => source.ActivationDelay?.Invoke(0, 0, source.AbilityLevel)));
                    mirror.Requirements.Select(Requirement)
                        .Should().BeEquivalentTo(source.Requirements.Select(Requirement));
                    mirror.SkillType.Should().Be(Enum.Parse<SkillType>(pair.AbilitySkill));
                    mirror.IsHostileAbility.Should().Be(source.IsHostileAbility);
                }
            }
        }
    }

    [Test]
    public void MirrorSkillsBelongToAstromechsAndKeepTheirSourcesShape()
    {
        using var config = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "tools", "AstromechSkillMirrors.json")));
        foreach (var mirror in config.RootElement.GetProperty("mirrors").EnumerateArray())
        {
            var skill = Enum.Parse<SkillType>(mirror.GetProperty("skill").GetString()!)
                .GetAttribute<SkillType, SkillAttribute>();
            var source = Enum.Parse<SkillType>(mirror.GetProperty("source_skill").GetString()!)
                .GetAttribute<SkillType, SkillAttribute>();

            skill.BodyFrameRestriction.Should().Be(BodyFrameRestriction.AstromechOnly);
            source.BodyFrameRestriction.Should().Be(BodyFrameRestriction.HumanoidOnly);
            skill.Category.Should().Be(source.Category);
            skill.MaxRank.Should().Be(source.MaxRank);
            skill.ContributesToSkillCap.Should().Be(source.ContributesToSkillCap);
            skill.CombatPointCategory.Should().Be(source.CombatPointCategory);
            skill.Name.Should().Be(mirror.GetProperty("skill_name").GetString());
        }
    }

    [Test]
    public void AnAstromechProvesAQuestsSkillWithTheMirrorOnly()
    {
        SkillBodyFrames.GetMirror(SkillType.FirstAid, BodyFrame.Astromech).Should().Be(SkillType.UtilitySystems);
        SkillBodyFrames.GetMirror(SkillType.Armor, BodyFrame.Astromech).Should().Be(SkillType.Chassis);
        SkillBodyFrames.GetMirror(SkillType.FirstAid, BodyFrame.Humanoid).Should().Be(SkillType.FirstAid);
        SkillBodyFrames.GetMirror(SkillType.Piloting, BodyFrame.Astromech).Should().Be(SkillType.Piloting);

        // The mirror never opens the source skill's own perks: only Armor has a frame equivalent for perk requirements.
        SkillBodyFrames.GetFrameEquivalent(SkillType.FirstAid, BodyFrame.Astromech).Should().Be(SkillType.FirstAid);
        var astromech = new Player { BodyFrame = BodyFrame.Astromech };
        foreach (var skill in Enum.GetValues<SkillType>())
            astromech.Skills[skill] = new PlayerSkill { Rank = 50 };
        new PerkRequirementSkill(SkillType.FirstAid, 0).CheckRequirements(astromech, null)
            .Should().Contain("cannot be trained by your species");
        new PerkRequirementSkill(SkillType.UtilitySystems, 50).CheckRequirements(astromech, null).Should().BeEmpty();

        var humanoid = new Player { BodyFrame = BodyFrame.Humanoid };
        foreach (var skill in Enum.GetValues<SkillType>())
            humanoid.Skills[skill] = new PlayerSkill { Rank = 50 };
        new PerkRequirementSkill(SkillType.UtilitySystems, 0).CheckRequirements(humanoid, null)
            .Should().Contain("cannot be trained by your species");
    }

    [Test]
    public void AnAstromechsToolsFightAsStaffAndRifleOnItsArmamentsRank()
    {
        // Rank reads and XP awards resolve to the skill the astromech trains.
        SkillBodyFrames.GetTrainedSkill(SkillType.Staff, BodyFrame.Astromech).Should().Be(SkillType.Armaments);
        SkillBodyFrames.GetTrainedSkill(SkillType.Rifle, BodyFrame.Astromech).Should().Be(SkillType.Armaments);
        SkillBodyFrames.GetTrainedSkill(SkillType.Armor, BodyFrame.Astromech).Should().Be(SkillType.Chassis);
        SkillBodyFrames.GetTrainedSkill(SkillType.Pistol, BodyFrame.Astromech).Should().Be(SkillType.Pistol);
        foreach (var skill in Enum.GetValues<SkillType>())
            SkillBodyFrames.GetTrainedSkill(skill, BodyFrame.Humanoid).Should().Be(skill);

        // The capstone quests of the source skills accept Armaments.
        SkillBodyFrames.GetMirror(SkillType.Staff, BodyFrame.Astromech).Should().Be(SkillType.Armaments);
        SkillBodyFrames.GetMirror(SkillType.Rifle, BodyFrame.Astromech).Should().Be(SkillType.Armaments);

        // None of this opens the Staff or Rifle perks themselves.
        SkillBodyFrames.GetFrameEquivalent(SkillType.Staff, BodyFrame.Astromech).Should().Be(SkillType.Staff);
        SkillBodyFrames.GetFrameEquivalent(SkillType.Rifle, BodyFrame.Astromech).Should().Be(SkillType.Rifle);
        var astromech = new Player { BodyFrame = BodyFrame.Astromech };
        foreach (var skill in Enum.GetValues<SkillType>())
            astromech.Skills[skill] = new PlayerSkill { Rank = 50 };
        new PerkRequirementSkill(SkillType.Staff, 0).CheckRequirements(astromech, null)
            .Should().Contain("cannot be trained by your species");
        new PerkRequirementSkill(SkillType.Rifle, 0).CheckRequirements(astromech, null)
            .Should().Contain("cannot be trained by your species");
        new PerkRequirementSkill(SkillType.Armaments, 50).CheckRequirements(astromech, null).Should().BeEmpty();

        // The tools carry every weapon rule of the staff and the rifle.
        Item.StaffBaseItemTypes.Should().Contain(BaseItem.ShockProd);
        Item.RifleBaseItemTypes.Should().Contain(BaseItem.BlasterMount);
    }

    [Test]
    public void QuestSkillPrerequisites_TakeTheMirrorWhereItsLineExists_AndNeverAClosedSkill()
    {
        var astromech = new Player { BodyFrame = BodyFrame.Astromech };
        var humanoid = new Player { BodyFrame = BodyFrame.Humanoid };
        foreach (var skill in Enum.GetValues<SkillType>())
        {
            astromech.Skills[skill] = new PlayerSkill { Rank = 50 };
            humanoid.Skills[skill] = new PlayerSkill { Rank = 50 };
        }

        // A mirrored capstone line (Worldbreaker, Kill Box) accepts Armaments.
        new RequiredSkillRankPrerequisite(SkillType.Staff, 50).MeetsPrerequisite(astromech).Should().BeTrue();
        new RequiredSkillRankPrerequisite(SkillType.Rifle, 50).MeetsPrerequisite(astromech).Should().BeTrue();

        // A line the mirror does not copy (Unmoving Center, One Shot) asks for the skill itself, which is closed.
        new RequiredSkillRankPrerequisite(SkillType.Staff, 50, false).MeetsPrerequisite(astromech).Should().BeFalse();
        new RequiredSkillRankPrerequisite(SkillType.Rifle, 50, false).MeetsPrerequisite(astromech).Should().BeFalse();
        new RequiredSkillRankPrerequisite(SkillType.Staff, 50, false).MeetsPrerequisite(humanoid).Should().BeTrue();

        // A closed skill with no mirror never qualifies, even at rank 0 and whatever an old record holds.
        new RequiredSkillRankPrerequisite(SkillType.Mimicry, 0).MeetsPrerequisite(astromech).Should().BeFalse();
        new RequiredSkillRankPrerequisite(SkillType.Pistol, 50).MeetsPrerequisite(astromech).Should().BeFalse();
        new RequiredSkillRankPrerequisite(SkillType.Mimicry, 0).MeetsPrerequisite(humanoid).Should().BeTrue();
        new RequiredSkillRankPrerequisite(SkillType.Piloting, 50).MeetsPrerequisite(astromech).Should().BeTrue();
        new RequiredSkillRankPrerequisite(SkillType.Armaments, 0).MeetsPrerequisite(humanoid).Should().BeFalse();

        // The capstone files mark exactly the unmirrored lines.
        var staff = File.ReadAllText(Path.Combine(Root, "SWLOR.Game.Server", "Feature", "QuestDefinition", "StaffCapstoneQuestDefinition.cs"));
        var rifle = File.ReadAllText(Path.Combine(Root, "SWLOR.Game.Server", "Feature", "QuestDefinition", "RifleCapstoneQuestDefinition.cs"));
        Count(staff, ".PrerequisiteOwnSkill(SkillType.Staff, 50)").Should().Be(5);
        Count(staff, ".PrerequisiteSkill(SkillType.Staff, 50)").Should().Be(5);
        Count(rifle, ".PrerequisiteOwnSkill(SkillType.Rifle, 50)").Should().Be(5);
        Count(rifle, ".PrerequisiteSkill(SkillType.Rifle, 50)").Should().Be(5);
    }

    private static int Count(string text, string value) => text.Split(value).Length - 1;

    [Test]
    public void AstromechLinesAreMarkedSoOtherBodiesNeverSeeThem()
    {
        foreach (var pair in Pairs())
        {
            using var perks = new DroidPerkCacheScope();
            var category = Perk.GetAllPerks()[pair.Mirror].Category.GetAttribute<PerkCategoryType, PerkCategoryAttribute>();
            category.BodyFrameRestriction.Should().Be(BodyFrameRestriction.AstromechOnly);
            category.Name.Should().StartWith(Enum.Parse<SkillType>(pair.Skill).GetAttribute<SkillType, SkillAttribute>().Name);

            var source = Perk.GetAllPerks()[pair.Source].Category.GetAttribute<PerkCategoryType, PerkCategoryAttribute>();
            source.BodyFrameRestriction.Should().Be(BodyFrameRestriction.HumanoidOnly);
        }
    }
}
