using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AstromechService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Service;

public class AstromechSpeciesTests
{
    private static readonly SkillType[] HumanoidOnlySkills =
    {
        SkillType.Force, SkillType.Armor, SkillType.FirstAid, SkillType.BeastMastery, SkillType.Vibroblade,
        SkillType.Vibroknife, SkillType.Lightsaber, SkillType.HeavyVibroblade, SkillType.Spear, SkillType.TwinBlade,
        SkillType.Saberstaff, SkillType.Katar, SkillType.Staff, SkillType.Pistol, SkillType.Rifle, SkillType.Throwing,
        SkillType.Mimicry, SkillType.Unarmed, SkillType.TwinPistol, SkillType.Performance
    };

    private static readonly SkillType[] SharedSkills =
    {
        SkillType.Piloting, SkillType.Engineering, SkillType.Smithery, SkillType.Fabrication, SkillType.Gathering,
        SkillType.Agriculture, SkillType.Espionage, SkillType.Devices, SkillType.Leadership, SkillType.Basic,
        SkillType.Droidspeak
    };

    private static SkillAttribute Details(SkillType skill) => skill.GetAttribute<SkillType, SkillAttribute>();

    private static Player MaxedAstromech()
    {
        var player = new Player { BodyFrame = BodyFrame.Astromech, CharacterType = CharacterType.Standard };
        foreach (var skill in Enum.GetValues<SkillType>())
            player.Skills[skill] = new PlayerSkill { Rank = 50 };
        return player;
    }

    [Test]
    public void OnlyTheAstromechHasTheAstromechBodyFrame()
    {
        Race.GetBodyFrame(RacialType.Astromech).Should().Be(BodyFrame.Astromech);
        foreach (var race in Enum.GetValues<RacialType>().Where(race => race != RacialType.Astromech))
            Race.GetBodyFrame(race).Should().Be(BodyFrame.Humanoid, $"{race} has the humanoid body");
    }

    [Test]
    public void ExistingCharactersDefaultToTheHumanoidFrame()
    {
        // Saved records carry no BodyFrame value, so the default must be the body every existing character has.
        new Player().BodyFrame.Should().Be(BodyFrame.Humanoid);
        ((int)BodyFrame.Humanoid).Should().Be(0);
    }

    [Test]
    public void MachinesAreBothDroidSpeciesAndTheMachineCreatureTypes()
    {
        Race.IsMechanical(RacialType.Droid).Should().BeTrue();
        Race.IsMechanical(RacialType.Astromech).Should().BeTrue();
        Race.IsMechanical(RacialType.Robot).Should().BeTrue();
        Race.IsMechanical(RacialType.Construct).Should().BeTrue();
        Race.IsMechanical(RacialType.Human).Should().BeFalse();
        Race.IsMechanical(RacialType.Wookiee).Should().BeFalse();
    }

    [Test]
    public void SkillsThatNeedAHumanoidBodyAreClosedToAstromechs()
    {
        var closed = Enum.GetValues<SkillType>()
            .Where(skill => skill != SkillType.Invalid)
            .Where(skill => Details(skill).BodyFrameRestriction == BodyFrameRestriction.HumanoidOnly);

        closed.Should().BeEquivalentTo(HumanoidOnlySkills);
        foreach (var skill in HumanoidOnlySkills)
        {
            Details(skill).IsAvailableTo(CharacterType.Standard, BodyFrame.Astromech).Should().BeFalse($"{skill} is closed");
            Details(skill).BodyFrameRestriction.Allows(BodyFrame.Humanoid).Should().BeTrue($"{skill} stays open to humanoids");
        }
    }

    [Test]
    public void SharedSkillsStayOpenToAstromechs()
    {
        foreach (var skill in SharedSkills)
            Details(skill).IsAvailableTo(CharacterType.Standard, BodyFrame.Astromech).Should().BeTrue($"{skill} is shared");
    }

    [Test]
    public void AStandardHumanoidKeepsTheSkillsItHadBeforeBodyFrames()
    {
        foreach (var skill in Enum.GetValues<SkillType>().Where(skill => skill != SkillType.Invalid))
        {
            Details(skill).IsAvailableTo(CharacterType.Standard, BodyFrame.Humanoid)
                .Should().Be(Details(skill).IsAvailableToCharacterType(CharacterType.Standard), $"{skill} is unchanged");
        }
    }

    [Test]
    public void APerkBehindAClosedSkillCannotBeBoughtAtAnyRank()
    {
        var astromech = MaxedAstromech();

        new PerkRequirementSkill(SkillType.Pistol, 0).CheckRequirements(astromech, null)
            .Should().Contain("cannot be trained by your species");
        new PerkRequirementSkill(SkillType.Piloting, 50).CheckRequirements(astromech, null).Should().BeEmpty();

        var humanoid = MaxedAstromech();
        humanoid.BodyFrame = BodyFrame.Humanoid;
        new PerkRequirementSkill(SkillType.Pistol, 50).CheckRequirements(humanoid, null).Should().BeEmpty();
    }

    [Test]
    public void NoPerkOfAHumanoidOnlyLineIsOpenToAnAstromech()
    {
        using var perks = new DroidPerkCacheScope();
        var astromech = MaxedAstromech();
        var closedNames = HumanoidOnlySkills.Select(skill => Details(skill).Name).ToArray();
        var open = new List<string>();

        foreach (var (perk, detail) in Perk.GetAllPerks().Where(entry => entry.Value.GroupType == PerkGroupType.Player))
        {
            var category = detail.Category.GetAttribute<PerkCategoryType, PerkCategoryAttribute>().Name;
            var family = category.Split(" - ")[0];
            var gatedBySkill = detail.PerkLevels.Values.SelectMany(level => level.Requirements)
                .OfType<PerkRequirementSkill>()
                .Any(requirement => HumanoidOnlySkills.Contains(requirement.Type));
            if (!closedNames.Contains(family) && !gatedBySkill)
                continue;

            var firstLevel = detail.PerkLevels.OrderBy(level => level.Key).First().Value;
            if (firstLevel.Requirements.All(requirement =>
                    string.IsNullOrWhiteSpace(requirement.CheckRequirements(astromech, null))))
            {
                open.Add($"{perk} ({category})");
            }
        }

        string.Join(", ", open).Should().BeEmpty("an astromech with every rank must still be refused each humanoid-only perk");
    }

    [Test]
    public void AstromechsSpeakOnlyDroidspeakAndWookieesOnlyShyriiwook()
    {
        Language.GetOnlySpokenLanguage(RacialType.Astromech).Should().Be(SkillType.Droidspeak);
        Language.GetOnlySpokenLanguage(RacialType.Wookiee).Should().Be(SkillType.Shyriiwook);
        Language.GetOnlySpokenLanguage(RacialType.Droid).Should().Be(SkillType.Invalid);
        Language.GetOnlySpokenLanguage(RacialType.Human).Should().Be(SkillType.Invalid);
        Language.GetOnlySpokenLanguageMessage(RacialType.Astromech).Should().Contain("Droidspeak");
        Language.GetOnlySpokenLanguageMessage(RacialType.Wookiee).Should().Be("Wookiees can only speak Shyriiwook.");
    }

    [Test]
    public void AnAstromechPlayerEquipsOnlyAstromechModules()
    {
        var none = Array.Empty<RacialType>();
        var astromechOnly = new[] { RacialType.Astromech };
        var droidOnly = new[] { RacialType.Droid };

        Item.GetRacialEquipError(RacialType.Astromech, true, BaseItem.Armor, none).Should().NotBeEmpty();
        Item.GetRacialEquipError(RacialType.Astromech, true, BaseItem.Pistol, none).Should().NotBeEmpty();
        Item.GetRacialEquipError(RacialType.Astromech, true, BaseItem.Armor, droidOnly).Should().NotBeEmpty();
        Item.GetRacialEquipError(RacialType.Astromech, true, BaseItem.Armor, astromechOnly).Should().BeEmpty();
        Item.GetRacialEquipError(RacialType.Astromech, false, BaseItem.Pistol, none)
            .Should().BeEmpty("astromech creatures keep their creature gear");

        Item.GetRacialEquipError(RacialType.Human, true, BaseItem.Armor, astromechOnly)
            .Should().Be("This item may only be equipped by Astromechs.");
        Item.GetRacialEquipError(RacialType.Droid, true, BaseItem.Armor, astromechOnly).Should().NotBeEmpty();
    }

    [Test]
    public void TheDroidEquipRulesAreUnchanged()
    {
        var none = Array.Empty<RacialType>();
        var droidOnly = new[] { RacialType.Droid };

        Item.GetRacialEquipError(RacialType.Human, true, BaseItem.Armor, none).Should().BeEmpty();
        Item.GetRacialEquipError(RacialType.Human, true, BaseItem.Armor, droidOnly)
            .Should().Be("This item may only be equipped by Droids.");
        Item.GetRacialEquipError(RacialType.Droid, true, BaseItem.Armor, droidOnly).Should().BeEmpty();
        Item.GetRacialEquipError(RacialType.Droid, true, BaseItem.Armor, none)
            .Should().Be("Droids may not equip that item.");
        Item.GetRacialEquipError(RacialType.Droid, true, BaseItem.Pistol, none).Should().BeEmpty();
    }

    [Test]
    public void EveryChassisIsDistinctAndTheFirstIsTheStartingOne()
    {
        var all = AstromechChassisCatalog.All;

        all.Should().HaveCount(8);
        all.Select(chassis => chassis.Appearance).Should().OnlyHaveUniqueItems();
        all.Select(chassis => chassis.Model).Should().OnlyHaveUniqueItems();
        all.Select(chassis => chassis.Name).Should().OnlyHaveUniqueItems();
        AstromechChassisCatalog.Default.Appearance.Should().Be(AppearanceType.SWLORAstromechChassis1);
        AstromechChassisCatalog.IsChassis(AppearanceType.Human).Should().BeFalse();
        AstromechChassisCatalog.Get(AppearanceType.Human).Should().BeSameAs(AstromechChassisCatalog.Default);
        AstromechChassisCatalog.Get(AppearanceType.SWLORAstromechChassis7).Model.Should().Be("c_astro_07");
    }

    [Test]
    public void PaintVariablesFollowTheChassisAndFallBackToItsOwnPaint()
    {
        var chassis = AstromechChassisCatalog.Get(AppearanceType.SWLORAstromechChassis3);
        var chosen = new Dictionary<AstromechPaintZone, int>
        {
            [AstromechPaintZone.Primary] = new TintMapColor(10, 20, 30).ToStoredValue(),
            [AstromechPaintZone.Trim] = 12
        };

        var variables = AstromechChassisCatalog.BuildPaintVariables(chassis, chosen);

        variables.Keys.Should().BeEquivalentTo("TM_c_astro_03_5", "TM_c_astro_03_4", "TM_c_astro_03_3");
        variables["TM_c_astro_03_5"].Should().Be(new TintMapColor(10, 20, 30).ToStoredValue());
        variables["TM_c_astro_03_4"].Should().Be(chassis.DefaultSecondary.ToStoredValue());
        variables["TM_c_astro_03_3"].Should().Be(chassis.DefaultTrim.ToStoredValue(),
            "a value that is not a stored color is ignored");
        AstromechChassisCatalog.BuildPaintVariables(chassis, null).Values
            .Should().OnlyContain(value => value > 0);
    }

    [Test]
    public void ScaleStaysInsideTheChassisRange()
    {
        AstromechChassisCatalog.ClampScale(0.5f).Should().Be(AstromechChassisCatalog.MinimumScale);
        AstromechChassisCatalog.ClampScale(2f).Should().Be(AstromechChassisCatalog.MaximumScale);
        AstromechChassisCatalog.ClampScale(1.05f).Should().Be(1.05f);
        AstromechChassisCatalog.ClampScale(0f).Should().Be(1f);
        AstromechChassisCatalog.ClampScale(float.NaN).Should().Be(1f);
    }

    [Test]
    public void TheChassisPlaysOnlyTheClipsItHas()
    {
        AstromechAnimations.CanPlay(Animation.FireForgetTaunt).Should().BeTrue();
        AstromechAnimations.CanPlay(Animation.FireForgetHeadTurnLeft).Should().BeTrue();
        AstromechAnimations.CanPlay(AstromechAnimations.Rest).Should().BeTrue();
        AstromechAnimations.CanPlay(Animation.LoopingSitCross).Should().BeFalse();
        AstromechAnimations.CanPlay(Animation.FireForgetBow).Should().BeFalse();
        AstromechAnimations.CanPlay(Animation.LoopingMeditate).Should().BeFalse();
    }

    [Test]
    public void TheHakDataMatchesTheSpecies()
    {
        var haks = Path.Combine(FindRepositoryRoot().FullName, "SWLOR_Haks", "sw_2da");
        var race = Row(Path.Combine(haks, "racialtypes.2da"), (int)RacialType.Astromech);
        var raceColumns = Columns(Path.Combine(haks, "racialtypes.2da"));

        race[raceColumns["Label"]].Should().Be("Astromech");
        race[raceColumns["PlayerRace"]].Should().Be("1");
        race[raceColumns["Appearance"]].Should().Be(((int)AstromechChassisCatalog.Default.Appearance).ToString());

        Rows(Path.Combine(haks, "cls_pres_stand.2da")).Should().Contain(row => row.Contains("170"),
            "the Standard class must be offered to the species in character creation");
        Rows(Path.Combine(haks, "cls_pres_force.2da")).Should().NotContain(row => row.Contains("170"),
            "an astromech can never be Force sensitive");

        var appearanceColumns = Columns(Path.Combine(haks, "appearance.2da"));
        var tintModels = Rows(Path.Combine(haks, "tintmap.2da")).Select(row => row.Length > 1 ? row[1] : string.Empty).ToHashSet();
        foreach (var chassis in AstromechChassisCatalog.All)
        {
            var appearance = Row(Path.Combine(haks, "appearance.2da"), (int)chassis.Appearance);
            appearance[appearanceColumns["RACE"]].Should().Be(chassis.Model);
            appearance[appearanceColumns["MODELTYPE"]].Should().Be("S");
            tintModels.Should().Contain(chassis.Model);
        }

        var soundset = Row(Path.Combine(haks, "soundset.2da"), Astromech.SoundSetId);
        soundset[1].Should().Be("Astromech");
    }

    private static List<string[]> Rows(string path)
    {
        return File.ReadAllLines(path).Skip(3)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(Tokens)
            .ToList();
    }

    private static string[] Row(string path, int index)
    {
        return Rows(path).Single(row => row[0] == index.ToString());
    }

    private static Dictionary<string, int> Columns(string path)
    {
        var names = Tokens(File.ReadAllLines(path)[2]);
        return names.Select((name, index) => (name, index)).ToDictionary(entry => entry.name, entry => entry.index + 1);
    }

    private static string[] Tokens(string line)
    {
        return System.Text.RegularExpressions.Regex.Matches(line, "\"[^\"]*\"|\\S+")
            .Select(match => match.Value.Trim('"'))
            .ToArray();
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")) &&
                Directory.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server")))
            {
                return directory;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
