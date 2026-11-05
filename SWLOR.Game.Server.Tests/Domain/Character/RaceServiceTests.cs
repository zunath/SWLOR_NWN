using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Service.RaceService;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>Unit tests of the race instance: the appearance tables, the scaling and the class prerequisites.</summary>
[TestFixture]
public class RaceServiceTests
{
    private const uint Player = 1001;

    private static readonly RacialType[] Races =
    {
        RacialType.Human, RacialType.Bothan, RacialType.Chiss, RacialType.Zabrak, RacialType.Twilek,
        RacialType.Mirialan, RacialType.Echani, RacialType.Cathar, RacialType.Togruta, RacialType.Trandoshan,
        RacialType.Wookiee, RacialType.MonCalamari, RacialType.Ugnaught, RacialType.Rodian, RacialType.KelDor,
        RacialType.Droid, RacialType.Nautolan, RacialType.Ewok
    };

    private FakeNWScript _engine = null!;
    private RaceService _service = null!;
    private IDisposable _scope = null!;

    [SetUp]
    public void SetUp()
    {
        _engine = new FakeNWScript();
        _service = new RaceService();
        _scope = NWScriptRuntime.Override(_engine);
    }

    [TearDown]
    public void TearDown() => _scope.Dispose();

    private void PlayerIs(RacialType race, Gender gender)
    {
        _engine.SetReturn("GetRacialType", race);
        _engine.SetReturn("GetGender", gender);
    }

    private void PlayerEntering(RacialType race, Gender gender, bool isPlayer = true, bool isDM = false)
    {
        _engine.SetReturn("GetEnteringObject", Player);
        _engine.SetReturn("GetIsPC", isPlayer);
        _engine.SetReturn("GetIsDM", isDM);
        PlayerIs(race, gender);
    }

    // ---- the tables

    [TestCaseSource(nameof(Races))]
    public void Every_playable_race_has_a_male_and_a_female_default_after_the_module_loads(RacialType race)
    {
        _service.LoadRaces();

        _service.GetDefaultAppearance(race, Gender.Male).Should().NotBeNull();
        _service.GetDefaultAppearance(race, Gender.Female).Should().NotBeNull();
    }

    [Test]
    public void No_default_exists_before_the_module_loads()
    {
        var before = () => _service.GetDefaultAppearance(RacialType.Human, Gender.Male);

        before.Should().Throw<KeyNotFoundException>();
    }

    [Test]
    public void Each_instance_keeps_its_own_tables()
    {
        _service.LoadRaces();

        var other = () => new RaceService().GetDefaultAppearance(RacialType.Human, Gender.Male);

        other.Should().Throw<KeyNotFoundException>();
    }

    [Test]
    public void Loading_twice_rebuilds_the_same_values()
    {
        _service.LoadRaces();
        var first = _service.GetDefaultAppearance(RacialType.Cathar, Gender.Male);

        _service.LoadRaces();

        _service.GetDefaultAppearance(RacialType.Cathar, Gender.Male).Should().NotBeSameAs(first)
            .And.BeEquivalentTo(first);
    }

    [Test]
    public void Human_uses_the_defaults_of_the_appearance_class()
    {
        _service.LoadRaces();

        var human = _service.GetDefaultAppearance(RacialType.Human, Gender.Male);

        human.Should().BeEquivalentTo(new RacialAppearance());
        human.AppearanceType.Should().Be(AppearanceType.Human);
    }

    [TestCase(RacialType.Bothan, AppearanceType.Bothan, 40, 109)]
    [TestCase(RacialType.Chiss, AppearanceType.Chiss, 33, 191)]
    [TestCase(RacialType.Zabrak, AppearanceType.Zabrak, 103, 120)]
    [TestCase(RacialType.Twilek, AppearanceType.Twilek, 115, 145)]
    [TestCase(RacialType.Echani, AppearanceType.Echani, 182, 45)]
    [TestCase(RacialType.Cathar, AppearanceType.Cathar, 27, 18)]
    [TestCase(RacialType.Togruta, AppearanceType.Togruta, 212, 200)]
    [TestCase(RacialType.Trandoshan, AppearanceType.Trandoshan, 162, 135)]
    [TestCase(RacialType.Wookiee, AppearanceType.Wookiee, 192, 110)]
    [TestCase(RacialType.Rodian, AppearanceType.Rodian, 54, 10)]
    [TestCase(RacialType.KelDor, AppearanceType.KelDor, 223, 228)]
    public void The_heads_differ_by_gender_and_the_appearance_type_follows_the_race(
        RacialType race, AppearanceType appearance, int maleHead, int femaleHead)
    {
        _service.LoadRaces();

        var male = _service.GetDefaultAppearance(race, Gender.Male);
        var female = _service.GetDefaultAppearance(race, Gender.Female);

        male.AppearanceType.Should().Be(appearance);
        female.AppearanceType.Should().Be(appearance);
        male.HeadId.Should().Be(maleHead);
        female.HeadId.Should().Be(femaleHead);
    }

    [Test]
    public void Any_gender_other_than_female_reads_the_male_table()
    {
        _service.LoadRaces();

        _service.GetDefaultAppearance(RacialType.Bothan, Gender.Both)
            .Should().BeSameAs(_service.GetDefaultAppearance(RacialType.Bothan, Gender.Male));
    }

    [Test]
    public void Ewoks_are_scaled_down_and_wear_the_wookiee_style_parts()
    {
        _service.LoadRaces();

        var ewok = _service.GetDefaultAppearance(RacialType.Ewok, Gender.Female);

        ewok.Scale.Should().BeApproximately(0.86f, 0.0001f);
        ewok.TorsoId.Should().Be(208);
        ewok.RightHandId.Should().Be(209);
        ewok.LeftHandId.Should().Be(209);
    }

    // ---- scaling on entry

    [Test]
    public void A_player_entering_is_scaled_by_the_race_default()
    {
        _service.LoadRaces();
        PlayerEntering(RacialType.Ewok, Gender.Male);

        _service.ApplyWookieeScaling();

        var call = _engine.CallsTo("SetObjectVisualTransform").Should().ContainSingle().Subject;
        call.Arguments[0].Should().Be(Player);
        call.Arguments[1].Should().Be(ObjectVisualTransform.Scale);
        ((float)call.Arguments[2]!).Should().BeApproximately(0.86f, 0.0001f);
    }

    [Test]
    public void A_female_player_entering_reads_the_female_table()
    {
        _service.LoadRaces();
        PlayerEntering(RacialType.Wookiee, Gender.Female);

        _service.ApplyWookieeScaling();

        _engine.CallsTo("SetObjectVisualTransform").Should().ContainSingle();
    }

    [TestCase(false, false)]
    [TestCase(true, true)]
    public void Non_players_and_DMs_are_not_scaled(bool isPlayer, bool isDM)
    {
        _service.LoadRaces();
        PlayerEntering(RacialType.Ewok, Gender.Male, isPlayer, isDM);

        _service.ApplyWookieeScaling();

        _engine.CallsTo("SetObjectVisualTransform").Should().BeEmpty();
    }

    [TestCase(Gender.Male)]
    [TestCase(Gender.Female)]
    public void A_race_without_a_table_entry_is_left_alone(Gender gender)
    {
        _service.LoadRaces();
        PlayerEntering(RacialType.Invalid, gender);

        _service.ApplyWookieeScaling();

        _engine.CallsTo("SetObjectVisualTransform").Should().BeEmpty();
    }

    // ---- first appearance

    [Test]
    public void Setting_the_default_appearance_sets_the_look_colors_and_every_body_part()
    {
        _service.LoadRaces();
        PlayerIs(RacialType.Cathar, Gender.Male);

        _service.SetDefaultRaceAppearance(Player);

        var cathar = _service.GetDefaultAppearance(RacialType.Cathar, Gender.Male);
        _engine.CallsTo("SetCreatureAppearanceType").Should().ContainSingle()
            .Which.Arguments.Should().Equal(Player, cathar.AppearanceType);
        _engine.CallsTo("SetColor").Select(call => (call.Arguments[1], call.Arguments[2])).Should().Equal(
            ((object?)ColorChannel.Skin, (object?)cathar.SkinColorId),
            ((object?)ColorChannel.Hair, (object?)cathar.HairColorId));

        var parts = _engine.CallsTo("SetCreatureBodyPart")
            .ToDictionary(call => (CreaturePart)call.Arguments[0]!, call => (int)call.Arguments[1]!);
        parts.Should().HaveCount(16);
        parts[CreaturePart.Head].Should().Be(27);
        parts[CreaturePart.Torso].Should().Be(201);
        parts[CreaturePart.LeftFoot].Should().Be(201);
        _engine.CallsTo("SetCreatureBodyPart").Should().OnlyContain(call => (uint)call.Arguments[2]! == Player);
    }

    [Test]
    public void Setting_the_default_appearance_of_a_female_uses_her_head()
    {
        _service.LoadRaces();
        PlayerIs(RacialType.Rodian, Gender.Female);

        _service.SetDefaultRaceAppearance(Player);

        _engine.CallsTo("SetCreatureBodyPart")
            .Single(call => (CreaturePart)call.Arguments[0]! == CreaturePart.Head).Arguments[1].Should().Be(10);
        _engine.CallsTo("SetCreatureBodyPart")
            .Single(call => (CreaturePart)call.Arguments[0]! == CreaturePart.RightHand).Arguments[1].Should().Be(44);
    }

    // ---- class prerequisites

    private void ForceSensitiveRows(params string[] requiredRaces)
    {
        _engine.SetReturnHandler("Get2DAString", arguments =>
        {
            var row = (int)arguments[2]!;
            return row < requiredRaces.Length ? requiredRaces[row] : "****";
        });
    }

    [Test]
    public void An_invalid_class_is_available_to_no_race()
    {
        _service.IsClassAvailableToRace(ClassType.Invalid, RacialType.Human).Should().BeFalse();
        _engine.CallsTo("Get2DAString").Should().BeEmpty();
    }

    [Test]
    public void Any_other_class_is_available_to_every_race_without_reading_the_table()
    {
        _service.IsClassAvailableToRace(ClassType.Standard, RacialType.Droid).Should().BeTrue();

        _engine.CallsTo("Get2DAString").Should().BeEmpty();
    }

    [Test]
    public void Force_sensitive_is_available_to_the_races_the_prerequisite_table_names()
    {
        ForceSensitiveRows(((int)RacialType.Human).ToString(), "****", ((int)RacialType.Zabrak).ToString());

        _service.IsClassAvailableToRace(ClassType.ForceSensitive, RacialType.Human).Should().BeTrue();
        _service.IsClassAvailableToRace(ClassType.ForceSensitive, RacialType.Zabrak).Should().BeTrue();
        _service.IsClassAvailableToRace(ClassType.ForceSensitive, RacialType.Droid).Should().BeFalse();
    }

    [Test]
    public void The_prerequisite_table_is_read_once_per_instance()
    {
        ForceSensitiveRows(((int)RacialType.Human).ToString());

        _service.IsClassAvailableToRace(ClassType.ForceSensitive, RacialType.Human);
        var reads = _engine.CountOf("Get2DAString");
        _service.IsClassAvailableToRace(ClassType.ForceSensitive, RacialType.Droid);

        reads.Should().Be(255);
        _engine.CountOf("Get2DAString").Should().Be(reads);
        _engine.CallsTo("Get2DAString")
            .Should().OnlyContain(call => (string)call.Arguments[0]! == "cls_pres_force" && (string)call.Arguments[1]! == "ReqParam1");
    }
}
