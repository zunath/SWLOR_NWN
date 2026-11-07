using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.IntegrationTests.Support;
using SWLOR.Game.Server.Service.CurrencyService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.IntegrationTests.Chains;

/// <summary>A new character's first entry: the server's module-enter handler against the record the engine's character creation left in Redis.</summary>
[TestFixture]
public class PlayerFirstEntryTests : IntegrationTestBase
{
    [SetUp]
    public void SetUpPlayer()
    {
        ServerCaches.EnsureSkills();
        ConfigurePlayer();
        Engine.SetReturn("GetHitDice", 40);
        Engine.SetReturn("GetRacialType", RacialType.Human);
        SWLOR.Game.Server.Service.Race.LoadRaces();
        Engine.SetReturn("GetClassByPosition", ClassType.Standard);
        GiveTheEngineItemCreation();
    }

    /// <summary>The starting items: the engine creates each one in the player's inventory, and the last one created is worn.</summary>
    private void GiveTheEngineItemCreation()
    {
        const uint ObjectInvalid = 0x7F000000;
        uint next = 2000;
        var created = new HashSet<uint>();
        Engine.SetReturnHandler("CreateItemOnObject", _ =>
        {
            created.Add(next);
            return next++;
        });
        Engine.SetReturnHandler("GetIsObjectValid", arguments => arguments[0] is uint id && (id == Hero || created.Contains(id)));
        Engine.SetReturnHandler("GetItemPossessor", arguments => arguments[0] is uint id && created.Contains(id) ? Hero : ObjectInvalid);
        Engine.SetReturnHandler("GetItemInSlot", _ => next - 1);
    }

    [Test]
    public void FirstEntry_CompletesTheRecordAndGrantsTheStartingTokens()
    {
        // Native character creation saves the record pending; only that request authorizes initialization.
        Db.Set(new Player(HeroId) { CharacterInitializationPending = true });
        Db.EndFrame();

        PlayerInitialization.InitializePlayer();

        var stored = JObject.Parse(Db.GetRawJson<Player>(HeroId));
        stored["CharacterInitializationPending"]!.Value<bool>().Should().BeFalse();
        Db.EndFrame();
        var record = Db.Get<Player>(HeroId);
        record.CharacterInitializationPending.Should().BeFalse();
        record.Currencies[CurrencyType.RebuildToken].Should().Be(1);
        record.Currencies[CurrencyType.LanguageToken].Should().Be(1);
        record.CharacterType.Should().Be(CharacterType.Standard);
        Engine.CountOf("BootPC").Should().Be(0);
        Db.SearchCount(new Service.DBService.DBQuery<Player>().AddFieldSearch(nameof(Player.Id), HeroId, false)).Should().Be(1);
    }

    [Test]
    public void SecondEntry_DoesNotInitializeAgain()
    {
        Db.Set(new Player(HeroId) { CharacterInitializationPending = true });
        Db.EndFrame();
        PlayerInitialization.InitializePlayer();
        Db.EndFrame();
        var first = Db.GetRawJson<Player>(HeroId);

        PlayerInitialization.InitializePlayer();

        Db.GetRawJson<Player>(HeroId).Should().Be(first, "a completed character keeps its record on every later entry");
        Db.EndFrame();
        Db.Get<Player>(HeroId).Currencies[CurrencyType.RebuildToken].Should().Be(1);
    }

    [Test]
    public void AnUnknownCharacter_IsRefusedAndNoRecordIsCreated()
    {
        PlayerInitialization.InitializePlayer();

        Engine.CountOf("BootPC").Should().Be(1);
        Redis.KeyExists($"Player:{HeroId}").Should().BeFalse();
    }
}
