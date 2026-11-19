using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.AppearanceDefinition.AppearancePreview;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.AppearanceDefinition.RacialAppearance;
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
            if (Details(skill).BodyFrameRestriction == BodyFrameRestriction.AstromechOnly)
            {
                Details(skill).IsAvailableTo(CharacterType.Standard, BodyFrame.Humanoid)
                    .Should().BeFalse($"{skill} belongs to astromechs");
                continue;
            }

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
            // A requirement the astromech's own equivalent skill can meet (Chassis for Armor) does not close a perk.
            var gatedBySkill = detail.PerkLevels.Values.SelectMany(level => level.Requirements)
                .OfType<PerkRequirementSkill>()
                .Any(requirement => HumanoidOnlySkills.Contains(requirement.Type) &&
                                    !(requirement.AcceptsFrameEquivalent &&
                                      SkillBodyFrames.GetFrameEquivalent(requirement.Type, BodyFrame.Astromech) != requirement.Type));
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
    public void ChassisStandsInForArmorOnAnAstromech()
    {
        Details(SkillType.Chassis).BodyFrameRestriction.Should().Be(BodyFrameRestriction.AstromechOnly);
        Details(SkillType.Chassis).ContributesToSkillCap.Should().BeTrue();
        Details(SkillType.Chassis).CombatPointCategory.Should().Be(Details(SkillType.Armor).CombatPointCategory);
        Details(SkillType.Chassis).MaxRank.Should().Be(Details(SkillType.Armor).MaxRank);

        SkillBodyFrames.GetDefenseSkill(BodyFrame.Humanoid).Should().Be(SkillType.Armor);
        SkillBodyFrames.GetDefenseSkill(BodyFrame.Astromech).Should().Be(SkillType.Chassis);
        SkillBodyFrames.GetFrameEquivalent(SkillType.Armor, BodyFrame.Humanoid).Should().Be(SkillType.Armor);
        SkillBodyFrames.GetFrameEquivalent(SkillType.Pistol, BodyFrame.Astromech).Should().Be(SkillType.Pistol);
    }

    [Test]
    public void ArmorPerksOpenToChassisRankExceptTheOnesThatNeedHands()
    {
        using var perks = new DroidPerkCacheScope();
        var astromech = MaxedAstromech();
        astromech.Skills[SkillType.Chassis] = new PlayerSkill { Rank = 4 };
        astromech.Skills[SkillType.Armor] = new PlayerSkill { Rank = 50 };

        string FirstLevel(PerkType perk) => string.Join("|", Perk.GetAllPerks()[perk].PerkLevels
            .OrderBy(level => level.Key).First().Value.Requirements
            .Select(requirement => requirement.CheckRequirements(astromech, null))
            .Where(error => !string.IsNullOrWhiteSpace(error)));

        // Armor rank on the record is ignored: the astromech's Chassis rank is what counts.
        FirstLevel(PerkType.Alertness).Should().Contain("rank is 4");
        astromech.Skills[SkillType.Chassis].Rank = 50;
        foreach (var perk in new[] { PerkType.Provoke, PerkType.Alertness, PerkType.TreasureHunter, PerkType.CreditFinder })
            FirstLevel(perk).Should().BeEmpty($"{perk} asks for Armor rank, which Chassis rank meets");
        foreach (var perk in new[] { PerkType.DualWield, PerkType.Doublehand })
            FirstLevel(perk).Should().Contain("cannot be trained by your species", $"{perk} needs hands");

        var humanoid = MaxedAstromech();
        humanoid.BodyFrame = BodyFrame.Humanoid;
        humanoid.Skills[SkillType.Chassis] = new PlayerSkill { Rank = 0 };
        Perk.GetAllPerks()[PerkType.Doublehand].PerkLevels.Values
            .SelectMany(level => level.Requirements)
            .Select(requirement => requirement.CheckRequirements(humanoid, null))
            .Should().OnlyContain(error => string.IsNullOrWhiteSpace(error));
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
        // A fishing rod is the one ordinary item an astromech holds: Agriculture is open to it.
        Item.GetRacialEquipError(RacialType.Astromech, true, Item.FishingRodBaseItem, none).Should().BeEmpty();
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
    public void EveryDesignHasADomeABarrelAndTwoLegsInTheHak()
    {
        var haks = Path.Combine(FindRepositoryRoot().FullName, "SWLOR_Haks");
        var definition = new AstromechRacialAppearanceDefinition();
        var designs = Enumerable.Range(1, AstromechBody.DesignCount).ToArray();

        definition.MaleHeads.Should().Equal(designs);
        definition.FemaleHeads.Should().Equal(designs);
        definition.Torsos.Should().Equal(designs);
        definition.RightThigh.Should().Equal(designs);
        definition.LeftThigh.Should().Equal(designs);
        foreach (var absent in new[]
                 {
                     definition.Pelvis, definition.RightBicep, definition.RightForearm, definition.RightHand,
                     definition.RightShin, definition.RightFoot, definition.LeftBicep, definition.LeftForearm,
                     definition.LeftHand, definition.LeftShin, definition.LeftFoot
                 })
        {
            absent.Should().Equal(0);
        }

        var tintModels = Rows(Path.Combine(haks, "sw_2da", "tintmap.2da"))
            .Select(row => row.Length > 1 ? row[1] : string.Empty)
            .ToHashSet();
        var parts = new[]
        {
            ("sw_pt_head", "head"), ("sw_pt_chest", "chest"), ("sw_pt_lthigh", "legl"), ("sw_pt_rthigh", "legr")
        };
        foreach (var gender in new[] { "m", "f" })
        foreach (var design in designs)
        foreach (var (folder, part) in parts)
        {
            var model = $"p{gender}r0_{part}{design:D3}";
            File.Exists(Path.Combine(haks, folder, model + ".mdl")).Should().BeTrue($"{model} is a part the editor offers");
            tintModels.Should().Contain(model, "a part without a tint row cannot be painted");
        }

        foreach (var root in new[] { "pmr0", "pfr0" })
            File.Exists(Path.Combine(haks, "sw_pt_root", root + ".mdl")).Should().BeTrue();
    }

    [Test]
    public void ANewAstromechStartsInTheFirstDesignWithNoHumanoidParts()
    {
        Race.LoadRaces();

        foreach (var gender in new[] { Gender.Male, Gender.Female })
        {
            var appearance = Race.GetDefaultAppearance(RacialType.Astromech, gender);

            appearance.AppearanceType.Should().Be(AppearanceType.Astromech);
            new[] { appearance.HeadId, appearance.TorsoId, appearance.RightThighId, appearance.LeftThighId }
                .Should().OnlyContain(part => part == AstromechBody.DefaultDesign);
            new[]
            {
                appearance.NeckId, appearance.PelvisId, appearance.RightBicepId, appearance.RightForearmId,
                appearance.RightHandId, appearance.RightShinId, appearance.RightFootId, appearance.LeftBicepId,
                appearance.LeftForearmId, appearance.LeftHandId, appearance.LeftShinId, appearance.LeftFootId
            }.Should().OnlyContain(part => part == 0);
        }
    }

    [Test]
    public void ADomeIsSeatedOnTheBarrelItIsWornWith()
    {
        for (var design = 1; design <= AstromechBody.DesignCount; design++)
            AstromechBody.GetDomeOffset(design, design).Should().Be(0f, "a matched dome and barrel already meet");

        AstromechBody.GetDomeOffset(4, 3).Should().BeApproximately(0.078f, 0.0005f, "a low dome is raised onto a tall barrel");
        AstromechBody.GetDomeOffset(3, 4).Should().BeApproximately(-0.078f, 0.0005f);
        AstromechBody.GetDomeOffset(0, 3).Should().Be(0f);
        AstromechBody.GetDomeOffset(3, 104).Should().Be(0f, "a part that is not a chassis design is left where it is");
    }

    [Test]
    public void TheDomeKeepsItsSize()
    {
        var definition = new AstromechRacialAppearanceDefinition();

        definition.MinimumHeadScale.Should().Be(1f);
        definition.MaximumHeadScale.Should().Be(1f);
        RacialAppearanceRegistry.TryGet(AppearanceType.Astromech, out var registered).Should().BeTrue();
        registered.Should().BeOfType<AstromechRacialAppearanceDefinition>();
    }

    [Test]
    public void ABareChassisIsPaintedOnItsThreePaintLayersOnly()
    {
        foreach (var layer in new[] { TintMapLayerType.Metal2, TintMapLayerType.Cloth1, TintMapLayerType.Cloth2 })
        {
            AstromechBody.TryGetBarePaint(layer, out var colorId).Should().BeTrue();
            colorId.Should().BeInRange(0, TintMapMaterialRegistry.PaletteColorCount - 1);
        }

        AstromechBody.TryGetBarePaint(TintMapLayerType.Skin, out _).Should().BeFalse();
        AstromechBody.TryGetBarePaint(TintMapLayerType.Metal1, out _).Should().BeFalse();
    }

    [Test]
    public void ModulesBringOnlyTheirDyes()
    {
        var definition = ArmorAppearanceCatalog.Definitions[AppearanceType.Astromech];

        foreach (var styles in new[]
                 {
                     definition.Helmet, definition.Cloak, definition.Neck, definition.Belt,
                     definition.Pelvis, definition.Shoulder, definition.Bicep, definition.Forearm, definition.Hand,
                     definition.Thigh, definition.Shin, definition.Foot, definition.Robe
                 })
        {
            styles.Should().Equal(0);
        }

        // The torso is the plating number: it draws nothing either, and exists for the plating's inventory icon.
        definition.Torso.Should().Equal(AstromechBody.PlatingTorso);
    }

    [Test]
    public void ThePlatingTorsoHasAnIconAndAPictureButNoModel()
    {
        var haks = Path.Combine(FindRepositoryRoot().FullName, "SWLOR_Haks");
        var number = AstromechBody.PlatingTorso.ToString("D3");

        var row = Row(Path.Combine(haks, "sw_2da", "parts_chest.2da"), AstromechBody.PlatingTorso);
        row.Skip(1).Should().OnlyContain(cell => cell != "****", "the engine reads the part row of a worn torso");

        // No body may have a model under the number, or plating would draw it in place of the wearer's own body.
        Directory.GetFiles(Path.Combine(haks, "sw_pt_chest"), $"p???_chest{number}.mdl").Should().BeEmpty();
        new GeneralArmorAppearanceDefinition().Torso.Should().NotContain(AstromechBody.PlatingTorso);
        new DroidArmorAppearanceDefinition().Torso.Should().NotContain(AstromechBody.PlatingTorso);

        foreach (var gender in new[] { "m", "f" })
        {
            File.Exists(Path.Combine(haks, "sw_pt_root", $"ip{gender}_chest{number}.plt")).Should().BeTrue(
                "an armor icon is built from its part numbers");
            foreach (var view in new[] { "f", "b", "a" })
                File.Exists(Path.Combine(haks, "sw_part_previews", $"ap{gender}r0_chest{number}{view}.dds")).Should().BeTrue();
            AppearancePartNames.Get($"p{gender}r0_chest{number}", "?").Should().Be("Plating");
        }
    }

    [Test]
    public void EveryAstromechPortraitShipsInTheHakAtEverySize()
    {
        var haks = Path.Combine(FindRepositoryRoot().FullName, "SWLOR_Haks");
        var registered = Rows(Path.Combine(haks, "sw_2da", "portraits.2da"))
            .Where(row => row.Length > 3 && row[3] == ((int)RacialType.Astromech).ToString())
            .Select(row => "po_" + row[1])
            .ToList();

        registered.Should().HaveCount(AstromechBody.DesignCount);
        registered.Should().Contain(Astromech.DefaultPortrait);
        foreach (var portrait in registered)
        {
            (portrait.Length + 1).Should().BeLessThanOrEqualTo(16, "a resource name holds 16 characters");
            foreach (var size in new[] { "l", "m", "s", "t" })
                File.Exists(Path.Combine(haks, "sw_portrait", portrait + size + ".dds")).Should().BeTrue(
                    $"{portrait}{size} is a portrait size the game asks for");
        }
    }

    [Test]
    public void EveryToolBlueprintHasAHeldModelAndAnIconOfItsOwnItemClass()
    {
        var root = FindRepositoryRoot().FullName;
        var haks = Path.Combine(root, "SWLOR_Haks");
        var baseItems = Path.Combine(haks, "sw_2da", "baseitems.2da");
        var columns = Columns(baseItems);
        var classes = new Dictionary<int, string>
        {
            [(int)BaseItem.ShockProd] = Row(baseItems, (int)BaseItem.ShockProd)[columns["ItemClass"]].ToLowerInvariant(),
            [(int)BaseItem.BlasterMount] = Row(baseItems, (int)BaseItem.BlasterMount)[columns["ItemClass"]].ToLowerInvariant()
        };

        // A held item is drawn on the dynamic body, so a tool cannot borrow the staff or rifle item class it copies.
        classes[(int)BaseItem.ShockProd].Should().Be("wampr");
        classes[(int)BaseItem.BlasterMount].Should().Be("wambl");

        var blueprints = Directory.GetFiles(Path.Combine(root, "Module", "uti"), "am_w_*.uti.json");
        blueprints.Should().NotBeEmpty();
        foreach (var blueprint in blueprints)
        {
            using var item = System.Text.Json.JsonDocument.Parse(File.ReadAllText(blueprint));
            var fields = item.RootElement;
            var itemClass = classes[fields.GetProperty("BaseItem").GetProperty("value").GetInt32()];
            foreach (var (letter, field) in new[] { ("b", "ModelPart1"), ("m", "ModelPart2"), ("t", "ModelPart3") })
            {
                var part = $"{itemClass}_{letter}_{fields.GetProperty(field).GetProperty("value").GetInt32():D3}";
                File.Exists(Path.Combine(haks, "sw_weapon", part + ".mdl")).Should().BeTrue(
                    $"{Path.GetFileName(blueprint)} is drawn from {part}");
                Directory.GetFiles(Path.Combine(haks, "sw_weapon"), $"i{part}.*").Should().NotBeEmpty(
                    $"{Path.GetFileName(blueprint)} shows the icon i{part}");
            }
        }
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
        race[raceColumns["Appearance"]].Should().Be(((int)AppearanceType.Astromech).ToString());

        Rows(Path.Combine(haks, "cls_pres_stand.2da")).Should().Contain(row => row.Contains("170"),
            "the Standard class must be offered to the species in character creation");
        Rows(Path.Combine(haks, "cls_pres_force.2da")).Should().NotContain(row => row.Contains("170"),
            "an astromech can never be Force sensitive");

        // Playable species are part-based: the body is drawn from part models named for the race letter.
        var appearanceColumns = Columns(Path.Combine(haks, "appearance.2da"));
        var appearance = Row(Path.Combine(haks, "appearance.2da"), (int)AppearanceType.Astromech);
        appearance[appearanceColumns["RACE"]].Should().Be("R");
        appearance[appearanceColumns["MODELTYPE"]].Should().Be("P");
        appearance[appearanceColumns["RACIALTYPE"]].Should().Be(((int)RacialType.Astromech).ToString());
        Rows(Path.Combine(haks, "appearance.2da")).Should().NotContain(
            row => int.Parse(row[0]) > (int)AppearanceType.Astromech,
            "the astromech is one appearance; its designs are parts");

        var soundset = Row(Path.Combine(haks, "soundset.2da"), Astromech.SoundSetId);
        soundset[1].Should().Be("Astromech");

        // The item property that asks for a skill rank picks the skill from this table.
        Row(Path.Combine(haks, "iprp_skill.2da"), (int)SkillType.Chassis)[2].Should().Be("Chassis");
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
