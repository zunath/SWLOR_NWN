using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Domain.Economy;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;
using Random = SWLOR.Game.Server.Service.Random;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>
/// Unit tests of the language instance: the engine is a fake with a small world (a speaker and a listener), the
/// database is in memory, and the unconverted skill and stat services are the instance's own seams.
/// </summary>
[TestFixture]
public class LanguageServiceTests
{
    private const uint Speaker = 1001;
    private const uint Listener = 1002;
    private const uint Npc = 1003;
    private const uint Dm = 1004;
    private const uint Possessed = 1005;

    private FakeNWScript _engine = null!;
    private LocalVariables _locals = null!;
    private InMemoryDBService _db = null!;
    private BenchLanguageService _service = null!;
    private readonly List<IDisposable> _scopes = new();
    private int _socialModifier;

    [SetUp]
    public void SetUp()
    {
        _socialModifier = 0;
        _engine = new FakeNWScript();
        _engine.SetReturnHandler("GetIsPC", arguments => (uint)arguments[0]! is Speaker or Listener or Dm);
        _engine.SetReturnHandler("GetIsDM", arguments => (uint)arguments[0]! == Dm);
        _engine.SetReturnHandler("GetIsDMPossessed", arguments => (uint)arguments[0]! == Possessed);
        _engine.SetReturnHandler("GetObjectUUID", arguments => "id-" + arguments[0]);
        _engine.SetReturnHandler("GetAbilityModifier", _ => _socialModifier);
        _locals = new LocalVariables(_engine);
        _scopes.Add(NWScriptRuntime.Override(_engine));

        _db = new InMemoryDBService();
        _service = new BenchLanguageService(_db);
        _service.LoadTranslators();
    }

    [TearDown]
    public void TearDown()
    {
        Random.ResetSeed();
        foreach (var scope in Enumerable.Reverse(_scopes))
            scope.Dispose();
        _scopes.Clear();
    }

    private sealed class BenchLanguageService : LanguageService
    {
        public BenchLanguageService(IDBService db)
            : base(db, FacadeForwarder.For<ISkillService>(), FacadeForwarder.For<IStatService>())
        {
        }

        public int MaxRank { get; set; } = 10;
        public int StatAdjustment { get; set; }
        public List<(uint Player, SkillType Skill, int Xp)> XpAwards { get; } = new();
        public Dictionary<SkillType, SkillAttribute> Languages2 { get; } = new();

        protected override SkillAttribute GetSkillDetails(SkillType skill) =>
            new(SkillCategoryType.Languages, "Language", MaxRank, true, "d", false, false, false);

        protected override int GiveSkillXP(uint player, SkillType skill, int xp, bool ignoreBonuses, bool applyHenchmanPenalty)
        {
            XpAwards.Add((player, skill, xp));
            return xp;
        }

        protected override int GetRequiredXP(int rank) => rank * 100;

        protected override Dictionary<SkillType, SkillAttribute> GetActiveSkillsByCategoryForDisplay(SkillCategoryType category) =>
            Languages2;

        protected override int GetStatAdjustment(uint creature, StatType stat) => StatAdjustment;
    }

    private Player AddPlayer(uint objectId, SkillType language, int rank)
    {
        var player = new Player("id-" + objectId);
        player.Skills[language] = new PlayerSkill { Rank = rank };
        _db.With(player);
        return player;
    }

    // ---- translating

    [Test]
    public void A_listener_who_is_not_a_player_always_understands_the_text()
    {
        _service.TranslateSnippetForListener(Speaker, Npc, SkillType.Dosh, "hello there").Should().Be("hello there");
        _service.TranslateSnippetForListener(Speaker, Dm, SkillType.Dosh, "hello there").Should().Be("hello there");
        _service.TranslateSnippetForListener(Speaker, Possessed, SkillType.Dosh, "hello there").Should().Be("hello there");
        _service.XpAwards.Should().BeEmpty();
    }

    [Test]
    public void A_listener_at_the_maximum_rank_or_the_speaker_themselves_gets_the_original()
    {
        AddPlayer(Listener, SkillType.Dosh, 10);
        _service.TranslateSnippetForListener(Npc, Listener, SkillType.Dosh, "hello there").Should().Be("hello there");

        AddPlayer(Speaker, SkillType.Dosh, 10);
        _service.TranslateSnippetForListener(Speaker, Speaker, SkillType.Dosh, "hello there").Should().Be("hello there");
        _service.XpAwards.Should().BeEmpty();
    }

    [Test]
    public void A_listener_without_a_saved_record_understands_everything()
    {
        _service.TranslateSnippetForListener(Npc, Listener, SkillType.Dosh, "hello there").Should().Be("hello there");
    }

    [Test]
    public void A_listener_with_no_rank_hears_the_translator_output_and_earns_skill_xp()
    {
        AddPlayer(Listener, SkillType.Dosh, 0);

        var result = _service.TranslateSnippetForListener(Npc, Listener, SkillType.Dosh, "hello there");

        result.Should().Be(new TranslatorDosh().Translate("hello there"));
        result.Should().NotBe("hello there");
        _service.XpAwards.Should().Equal((Listener, SkillType.Dosh, 10));
    }

    [Test]
    public void A_language_without_a_translator_passes_the_text_through_unchanged()
    {
        AddPlayer(Listener, SkillType.Basic, 0);

        _service.TranslateSnippetForListener(Npc, Listener, SkillType.Basic, "hello there").Should().Be("hello there");
    }

    [Test]
    public void A_partly_trained_listener_gets_a_mix_of_both_texts_word_for_word()
    {
        AddPlayer(Listener, SkillType.Dosh, 5);
        Random.SetSeed(12345);
        var original = "one two three four five six seven eight";
        var foreign = new TranslatorDosh().Translate(original).Split(' ');

        var result = _service.TranslateSnippetForListener(Npc, Listener, SkillType.Dosh, original);

        var words = result.TrimEnd().Split(' ');
        words.Should().HaveCount(8);
        var originalWords = original.Split(' ');
        for (var i = 0; i < words.Length; i++)
            words[i].Should().BeOneOf(originalWords[i], foreign[i]);
        result.Should().EndWith(" ");
    }

    [Test]
    public void The_stat_bonus_adds_rank_grants_force_xp_and_is_capped_at_the_maximum()
    {
        AddPlayer(Listener, SkillType.Dosh, 4);
        _service.StatAdjustment = 3;

        _service.TranslateSnippetForListener(Npc, Listener, SkillType.Dosh, "a b c");
        _service.XpAwards.Should().Equal((Listener, SkillType.Dosh, 10), (Listener, SkillType.Force, 100));

        // A bonus that lifts the rank past the maximum means a guaranteed success: the original text, no award.
        _service.XpAwards.Clear();
        AddPlayer(Listener, SkillType.Dosh, 9);
        _service.StatAdjustment = 5;
        _service.TranslateSnippetForListener(Npc, Listener, SkillType.Dosh, "a b c").Should().Be("a b c");
        _service.XpAwards.Should().BeEmpty();
    }

    [Test]
    public void The_xp_scales_with_the_text_length_and_the_social_modifier_and_waits_two_minutes()
    {
        AddPlayer(Listener, SkillType.Dosh, 0);
        _socialModifier = 2;
        var text = new string('x', 120);

        _service.TranslateSnippetForListener(Npc, Listener, SkillType.Dosh, text);
        _service.XpAwards.Should().Equal((Listener, SkillType.Dosh, 120 / 3 + 20));
        _locals.Int(Listener, "LAST_LANGUAGE_SKILL_INCREASE_LOW").Should().NotBe(0);

        // A second line right away earns nothing.
        _service.TranslateSnippetForListener(Npc, Listener, SkillType.Dosh, text);
        _service.XpAwards.Should().HaveCount(1);

        // A line 3 minutes after the last award earns again.
        var threeMinutesAgo = DateTime.Now.Ticks - TimeSpan.FromMinutes(3).Ticks;
        _engine.SetReturnHandler("GetLocalInt", arguments => (string)arguments[1]! switch
        {
            "LAST_LANGUAGE_SKILL_INCREASE_LOW" => (int)(threeMinutesAgo & 0xFFFFFFFF),
            "LAST_LANGUAGE_SKILL_INCREASE_HIGH" => (int)((threeMinutesAgo >> 32) & 0xFFFFFFFF),
            _ => 0
        });
        _service.TranslateSnippetForListener(Npc, Listener, SkillType.Dosh, text);
        _service.XpAwards.Should().HaveCount(2);
    }

    [Test]
    public void A_speaker_below_the_maximum_rank_garbles_the_letters_of_their_words()
    {
        AddPlayer(Speaker, SkillType.Dosh, 0);
        Random.SetSeed(7);

        var result = _service.TranslateSnippetForListener(Speaker, Npc, SkillType.Dosh, "hello world again");

        var words = result.Split(' ');
        words.Should().HaveCount(3);
        var originals = new[] { "hello", "world", "again" };
        for (var i = 0; i < 3; i++)
            words[i].OrderBy(c => c).Should().Equal(originals[i].OrderBy(c => c));
    }

    [Test]
    public void A_speaker_at_the_maximum_rank_or_without_a_record_is_not_garbled()
    {
        AddPlayer(Speaker, SkillType.Dosh, 10);
        _service.TranslateSnippetForListener(Speaker, Npc, SkillType.Dosh, "hello world").Should().Be("hello world");

        _db = new InMemoryDBService();
        var fresh = new BenchLanguageService(_db);
        fresh.TranslateSnippetForListener(Speaker, Npc, SkillType.Dosh, "hello world").Should().Be("hello world");
    }

    [Test]
    public void Translators_are_not_loaded_until_the_module_loads()
    {
        var unloaded = new BenchLanguageService(_db);
        AddPlayer(Listener, SkillType.Dosh, 0);

        unloaded.TranslateSnippetForListener(Npc, Listener, SkillType.Dosh, "hello there").Should().Be("hello there");
    }

    // ---- data

    [TestCase(SkillType.Basic, 255, 255, 255)]
    [TestCase(SkillType.Arkanian, 190, 210, 245)]
    [TestCase(SkillType.Bothese, 132, 56, 18)]
    [TestCase(SkillType.Catharese, 235, 235, 199)]
    [TestCase(SkillType.Cheunh, 82, 143, 174)]
    [TestCase(SkillType.Dosh, 166, 181, 73)]
    [TestCase(SkillType.Droidspeak, 192, 192, 192)]
    [TestCase(SkillType.Huttese, 162, 74, 10)]
    [TestCase(SkillType.KelDor, 162, 162, 0)]
    [TestCase(SkillType.Mandoa, 255, 215, 0)]
    [TestCase(SkillType.Rodese, 82, 255, 82)]
    [TestCase(SkillType.Shyriiwook, 149, 125, 86)]
    [TestCase(SkillType.Togruti, 82, 82, 255)]
    [TestCase(SkillType.Twileki, 65, 105, 225)]
    [TestCase(SkillType.Zabraki, 255, 102, 102)]
    [TestCase(SkillType.Mirialan, 77, 230, 215)]
    [TestCase(SkillType.MonCalamarian, 128, 128, 192)]
    [TestCase(SkillType.Ugnaught, 255, 193, 233)]
    [TestCase(SkillType.Nautila, 76, 230, 104)]
    [TestCase(SkillType.Ewokese, 112, 28, 28)]
    [TestCase(SkillType.Force, 0, 0, 0)]
    public void Each_language_has_its_chat_color(SkillType language, byte r, byte g, byte b)
    {
        _service.GetColor(language).Should().Be((r, g, b));
    }

    [TestCase(SkillType.Arkanian, "Arkanian")]
    [TestCase(SkillType.Bothese, "Bothese")]
    [TestCase(SkillType.Catharese, "Catharese")]
    [TestCase(SkillType.Cheunh, "Cheunh")]
    [TestCase(SkillType.Dosh, "Dosh")]
    [TestCase(SkillType.Droidspeak, "Droidspeak")]
    [TestCase(SkillType.Huttese, "Huttese")]
    [TestCase(SkillType.KelDor, "KelDor")]
    [TestCase(SkillType.Mandoa, "Mandoa")]
    [TestCase(SkillType.Rodese, "Rodese")]
    [TestCase(SkillType.Shyriiwook, "Shyriiwook")]
    [TestCase(SkillType.Togruti, "Togruti")]
    [TestCase(SkillType.Twileki, "Twi'leki")]
    [TestCase(SkillType.Zabraki, "Zabraki")]
    [TestCase(SkillType.Mirialan, "Mirialan")]
    [TestCase(SkillType.MonCalamarian, "Mon Calamarian")]
    [TestCase(SkillType.Ugnaught, "Ugnaught")]
    [TestCase(SkillType.Nautila, "Nautila")]
    [TestCase(SkillType.Ewokese, "Ewokese")]
    [TestCase(SkillType.Basic, "Basic")]
    [TestCase(SkillType.Force, "Basic")]
    public void Each_language_has_its_name(SkillType language, string name)
    {
        _service.GetName(language).Should().Be(name);
    }

    [Test]
    public void The_command_list_is_built_once_and_covers_every_language_with_an_alias()
    {
        var first = _service.Languages;

        first.Should().BeSameAs(_service.Languages);
        first.Should().HaveCount(20);
        first.Select(language => language.Skill).Should().OnlyHaveUniqueItems();
        first.Single(language => language.Skill == SkillType.MonCalamarian).ChatNames.Should().Equal("moncalamarian", "moncal");
        first.Single(language => language.Skill == SkillType.Mandoa).ProperName.Should().Be("Mando'a");
    }

    [Test]
    public void Every_chat_name_is_unique_and_lowercase()
    {
        var names = _service.Languages.SelectMany(language => language.ChatNames).ToList();

        names.Should().OnlyHaveUniqueItems();
        names.Should().OnlyContain(name => name == name.ToLowerInvariant());
    }

    // ---- active language

    [Test]
    public void The_active_language_is_Basic_until_one_is_set_and_basic_clears_it()
    {
        _service.GetActiveLanguage(Speaker).Should().Be(SkillType.Basic);

        _service.SetActiveLanguage(Speaker, SkillType.Huttese);
        _service.GetActiveLanguage(Speaker).Should().Be(SkillType.Huttese);

        _service.SetActiveLanguage(Speaker, SkillType.Basic);
        _service.GetActiveLanguage(Speaker).Should().Be(SkillType.Basic);
        _engine.CallsTo("DeleteLocalInt").Should().ContainSingle();
    }

    // ---- mastery

    [Test]
    public void Mastering_a_language_sets_the_maximum_rank_and_the_xp_just_below_the_next_rank()
    {
        var player = new Player("p");

        _service.Master(player, SkillType.Dosh);

        player.Skills[SkillType.Dosh].Rank.Should().Be(10);
        player.Skills[SkillType.Dosh].XP.Should().Be(999);

        player.Skills[SkillType.Dosh].Rank = 2;
        _service.Master(player, SkillType.Dosh);
        player.Skills[SkillType.Dosh].Rank.Should().Be(10);
    }

    [Test]
    public void The_unmastered_languages_are_those_below_the_maximum_in_display_order()
    {
        _service.Languages2[SkillType.Dosh] = new SkillAttribute(SkillCategoryType.Languages, "Dosh", 10, true, "d", false, false, false);
        _service.Languages2[SkillType.Huttese] = new SkillAttribute(SkillCategoryType.Languages, "Huttese", 10, true, "d", false, false, false);
        _service.Languages2[SkillType.Zabraki] = new SkillAttribute(SkillCategoryType.Languages, "Zabraki", 10, true, "d", false, false, false);
        var player = new Player("p");
        player.Skills[SkillType.Dosh] = new PlayerSkill { Rank = 10 };
        player.Skills[SkillType.Huttese] = new PlayerSkill { Rank = 9 };

        _service.GetUnmasteredLanguages(player).Should().Equal(SkillType.Huttese, SkillType.Zabraki);
    }

    [Test]
    public void A_token_masters_a_language_and_is_spent()
    {
        _service.Languages2[SkillType.Dosh] = new SkillAttribute(SkillCategoryType.Languages, "Dosh", 10, true, "d", false, false, false);
        var player = new Player("p");
        player.Currencies[CurrencyType.LanguageToken] = 2;

        _service.TryMasterWithToken(player, SkillType.Dosh).Should().BeTrue();

        player.Currencies[CurrencyType.LanguageToken].Should().Be(1);
        player.Skills[SkillType.Dosh].Rank.Should().Be(10);
    }

    [Test]
    public void No_token_or_an_already_mastered_language_spends_nothing()
    {
        _service.Languages2[SkillType.Dosh] = new SkillAttribute(SkillCategoryType.Languages, "Dosh", 10, true, "d", false, false, false);
        var player = new Player("p");

        _service.TryMasterWithToken(player, SkillType.Dosh).Should().BeFalse();

        player.Currencies[CurrencyType.LanguageToken] = 0;
        _service.TryMasterWithToken(player, SkillType.Dosh).Should().BeFalse();

        player.Currencies[CurrencyType.LanguageToken] = 1;
        player.Skills[SkillType.Dosh] = new PlayerSkill { Rank = 10 };
        _service.TryMasterWithToken(player, SkillType.Dosh).Should().BeFalse();
        player.Currencies[CurrencyType.LanguageToken].Should().Be(1);
    }

    // ---- the facade

    [Test]
    public void The_facade_forwards_to_the_container_instance()
    {
        using var container = new FacadeContainer();
        using var scope = container.Install();

        Language.GetName(SkillType.Dosh).Should().Be("Dosh");
        Language.GetColor(SkillType.Basic).Should().Be((255, 255, 255));
        Language.Languages.Should().HaveCount(20);
        Language.SetActiveLanguage(Speaker, SkillType.Dosh);
        Language.GetActiveLanguage(Speaker).Should().Be(SkillType.Dosh);
        Language.LoadTranslators();
    }
}
