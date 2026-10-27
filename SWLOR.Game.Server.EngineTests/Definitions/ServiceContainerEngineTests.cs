using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.AchievementService;
using SWLOR.Game.Server.Service.CurrencyService;
using SWLOR.Game.Server.Service.FactionService;
using SWLOR.Game.Server.Service.PlayerMarketService;
using SWLOR.Game.Server.Service.TaxiService;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// Proves, on a booted server, that the service container was built before the handlers ran and that the converted
    /// services (Achievement, Faction, Taxi, Bank, Currency, Guild, PlayerMarket, Music, Weather) work through their static facades
    /// against the real database.
    /// </summary>
    public static class ServiceContainerEngineTests
    {
        /// <summary>
        /// The module-load and module-cache handlers of the converted facades forward to the container, so if any of
        /// them ran before the container existed it would have thrown and left its cache empty.
        /// </summary>
        [EngineTest("Service container exists and the converted services' boot handlers filled their caches", Category = "Composition")]
        public static async Task ContainerExistsAndBootHandlersRan(EngineTestContext ctx)
        {
            await ctx.WaitFrameAsync();

            ctx.Assert(CompositionRoot.IsBuilt, "The server's service container should be built.");

            var reservation = Gui.GetSystemReservation("Achievement");
            ctx.AssertEqual(6, reservation.Count, "Achievement GUI ids reserved by the module-load handler");

            var expectedAchievements = Enum.GetValues(typeof(AchievementType)).Cast<AchievementType>()
                .Count(type => type.GetAttribute<AchievementType, AchievementAttribute>().IsActive);
            ctx.AssertEqual(expectedAchievements, Achievement.GetActiveAchievements().Count, "Active achievements cached by the module-cache handler");

            var expectedFactions = Enum.GetValues(typeof(FactionType)).Length - 1;
            ctx.AssertEqual(expectedFactions, Faction.GetAllFactions().Count, "Factions cached by the module-cache handler");

            ctx.Assert(Taxi.GetDestinationsByRegionId(1).ContainsKey(TaxiDestinationType.VelesEntrance),
                "Taxi destinations cached by the module-cache handler should include Veles Entrance.");

            ctx.AssertEqual("Rebuild Tokens", Currency.GetCurrencyDetail(CurrencyType.RebuildToken).Name, "Currencies cached by the module-cache handler");
            ctx.AssertEqual(5, Guild.MaxRank, "Guild data loaded by the module-cache handler");
            ctx.AssertEqual("Hunter's Guild", Guild.GetGuild(GuildType.HuntersGuild).Name, "Guilds cached by the module-cache handler");
            ctx.Assert(Guild.DateTasksLoaded != null, "Guild tasks refreshed by the quests-registered handler");
            ctx.AssertEqual("MARKET_GLOBAL", PlayerMarket.GetMarketRegion(MarketRegionType.Global).MarketId, "Market regions cached by the module-cache handler");
            ctx.Assert(PlayerMarket.GetActiveCategories().Count > 0, "Market categories cached by the module-cache handler");

            ctx.Assert(Area.GetAreas().Count > 0, "Areas cached by the module-cache handler");
            ctx.Assert(Area.GetAreaByResref(Area.GetAreas().Keys.First()) != OBJECT_INVALID, "Areas answer by resref from the cache");
            ctx.Assert(Planet.GetAllPlanets().ContainsKey(PlanetType.Viscara), "Planets cached by the module-cache handler should include Viscara.");
            ctx.AssertEqual("3 / 25 Categories", Notes.GetCategoryUsageText(3), "Notes answer through the container");
            ctx.Assert(!HoloCom.IsHologram(OBJECT_INVALID), "HoloCom answers through the container");
            ctx.Assert(Music.GetAllSongs().Count > 0, "Songs cached by the module-cache handler");
            ctx.Assert(SWLOR.Game.Server.Service.Weather.GetWeather(OBJECT_INVALID) == SWLOR.NWN.API.NWScript.Enum.Weather.Invalid,
                "Weather answers through the container");
        }

        /// <summary>
        /// A player's faction standing, faction points and registered taxi destination are written through the facades,
        /// the container's instances and the database adapter, and read back from Redis on a later frame.
        /// </summary>
        [EngineTest("Converted facades persist through the container to the database", Category = "Composition", TimeoutSeconds = 45f)]
        public static async Task FacadesPersistThroughTheContainer(EngineTestContext ctx)
        {
            using var player = await PlayerAbilityFixture.CreateAsync(ctx);

            Faction.AdjustPlayerFactionStanding(player.Creature, FactionType.Jedi, 120);
            Faction.AdjustPlayerFactionPoints(player.Creature, FactionType.Jedi, 30);
            Taxi.RegisterTaxiDestination(player.Creature, TaxiDestinationType.VelesEntrance);

            // The database serves a stored copy again on the next frame.
            await ctx.WaitFrameAsync();
            var stored = DB.Get<Player>(player.Id);

            ctx.AssertEqual(120, stored.Factions[FactionType.Jedi].Standing, "Stored Jedi standing");
            ctx.AssertEqual(30, stored.Factions[FactionType.Jedi].Points, "Stored Jedi points");
            ctx.Assert(stored.TaxiDestinations[1].Contains(TaxiDestinationType.VelesEntrance),
                "Veles Entrance should be registered in the stored taxi destinations.");
        }

        /// <summary>
        /// A player's currency balance and guild points are written through the Currency and Guild facades, the
        /// container's instances and the database adapter, and read back from Redis on a later frame. The bank answers
        /// from the same container.
        /// </summary>
        [EngineTest("Economy facades persist through the container to the database", Category = "Composition", TimeoutSeconds = 45f)]
        public static async Task EconomyFacadesPersistThroughTheContainer(EngineTestContext ctx)
        {
            using var player = await PlayerAbilityFixture.CreateAsync(ctx);

            Currency.GiveCurrency(player.Creature, CurrencyType.RebuildToken, 5);
            Currency.TakeCurrency(player.Creature, CurrencyType.RebuildToken, 2);
            ctx.AssertEqual(3, Currency.GetCurrency(player.Creature, CurrencyType.RebuildToken), "Balance read back through the Currency facade");
            Guild.GiveGuildPoints(player.Creature, GuildType.HuntersGuild, 150);

            // The database serves a stored copy again on the next frame.
            await ctx.WaitFrameAsync();
            var stored = DB.Get<Player>(player.Id);

            ctx.AssertEqual(3, stored.Currencies[CurrencyType.RebuildToken], "Stored Rebuild Token balance");
            ctx.AssertEqual(150, stored.Guilds[GuildType.HuntersGuild].Points, "Stored Hunter's Guild points");
            ctx.AssertEqual(0L, Bank.GetItemCount(player.Id), "A new player has nothing in the bank");
        }
    }
}
