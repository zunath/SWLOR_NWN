using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// What the name override service carries across hot reloads (MaintainabilityRefactorCharacterCompanionsSplit.md, "What a hot reload
    /// must recheck"). Two headless players stay online across the reloads, one phase per generation like
    /// <see cref="HotReloadPlayersEngineTests"/>: the first run has one player name the other and sends the other AFK; each later run checks
    /// that the player is still AFK, that the observer still sees the known name with the AFK tag, and that a tell between the two named players still goes through this build's tell handler. The run
    /// after the last reload moves the player, so the AFK movement check the reload had to start again clears the flag, and releases both.
    /// Reload is off in the normal engine sweep, so there the scenario skips; it runs with <c>scripts/run-hot-reload-check.sh
    /// --engine-filter HotReloadPlayers --reloads 2</c>.
    /// </summary>
    public sealed class HotReloadNamesEngineTests
    {
        private const string Prefix = "SWLOR_HRN_";
        private const int ReloadsToVerify = 2;
        private const string NamedAs = "Vessa Tal";

        private static readonly string AfkSuffix = " " + ColorToken.Purple("[AFK]");

        // One per loaded build: a run whose token differs from the module's is the first run of a new generation.
        private static readonly string GenerationToken = Guid.NewGuid().ToString("N");

        public HotReloadNamesEngineTests(IPlayerNameService names, IPlayerNameOverrideService nameOverrides, IPlayerKnownNameService knownNames, IDBService db)
        {
            Names = names;
            NameOverrides = nameOverrides;
            KnownNames = knownNames;
            DB = db;
        }

        private IPlayerNameService Names { get; }
        private IPlayerNameOverrideService NameOverrides { get; }
        private IPlayerKnownNameService KnownNames { get; }
        private IDBService DB { get; }

        [EngineTest("A player who is AFK and named by another keeps both across hot reloads", Category = "HotReloadPlayers", TimeoutSeconds = 120f)]
        public async Task AfkAndNamesSurviveReloads(EngineTestContext ctx)
        {
            if (Environment.GetEnvironmentVariable("SWLOR_HOT_RELOAD")?.Trim().ToLowerInvariant() is not ("true" or "1" or "yes"))
                ctx.Skip("Hot reload is off; run scripts/run-hot-reload-check.sh --engine-filter HotReloadPlayers --reloads 2.");

            var module = GetModule();
            if (GetLocalInt(module, Prefix + "PREPARED") == 0)
            {
                await PrepareAsync(ctx);
                ctx.SetResultDetail("prepared: reload the server " + ReloadsToVerify + " times to verify");
                return;
            }

            if (GetLocalString(module, Prefix + "TOKEN") == GenerationToken)
                ctx.Skip("No reload since the last phase of this scenario ran.");

            Verify(ctx);
            var verified = GetLocalInt(module, Prefix + "VERIFIED") + 1;
            SetLocalString(module, Prefix + "TOKEN", GenerationToken);
            SetLocalInt(module, Prefix + "VERIFIED", verified);
            await VerifyTellAsync(ctx);
            ctx.SetResultDetail($"verified after reload {verified} of {ReloadsToVerify}");
            if (verified >= ReloadsToVerify)
            {
                await VerifyMovementClearsAfkAsync(ctx);
                Release(ctx);
            }
        }

        private async Task PrepareAsync(EngineTestContext ctx)
        {
            var module = GetModule();
            var afkFixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var afk = afkFixture.Creature;
            ctx.Untrack(afk);
            afkFixture.KeepAcrossReloads();
            var observerFixture = await PlayerAbilityFixture.CreateAsync(ctx, 3f);
            var observer = observerFixture.Creature;
            ctx.Untrack(observer);
            observerFixture.KeepAcrossReloads();
            SetLocalInt(module, EngineTest.KeepArenaVariable, 1);

            KnownNames.SetKnownName(observer, afk, NamedAs);
            ctx.Assert(NameOverrides.ToggleAfk(afk), "the player goes AFK");
            var expected = Names.GetDisplayName(observer, afk) + AfkSuffix;
            ctx.AssertEqual(expected, RenamePlugin.GetPCNameOverride(afk, observer), "the observer sees the known name with the AFK tag");

            SetLocalString(module, Prefix + "EXPECTED_OVERRIDE", expected);
            SetLocalObject(module, Prefix + "AFK", afk);
            SetLocalInt(module, Prefix + "AFK_CLIENT", (int)afkFixture.ClientId);
            SetLocalInt(module, Prefix + "AFK_WAS_PLAYER", afkFixture.WasPlayer);
            SetLocalObject(module, Prefix + "OBSERVER", observer);
            SetLocalInt(module, Prefix + "OBSERVER_CLIENT", (int)observerFixture.ClientId);
            SetLocalInt(module, Prefix + "OBSERVER_WAS_PLAYER", observerFixture.WasPlayer);
            SetLocalString(module, Prefix + "TOKEN", GenerationToken);
            SetLocalInt(module, Prefix + "VERIFIED", 0);
            SetLocalInt(module, Prefix + "PREPARED", 1);
        }

        private void Verify(EngineTestContext ctx)
        {
            var module = GetModule();
            var afk = GetLocalObject(module, Prefix + "AFK");
            var observer = GetLocalObject(module, Prefix + "OBSERVER");
            ctx.Assert(GetIsObjectValid(afk) && GetIsObjectValid(observer), "both players are still in the world");
            ctx.Assert(NameOverrides.IsAfk(afk), "the AFK flag is carried into the new build");
            ctx.AssertEqual(GetLocalString(module, Prefix + "EXPECTED_OVERRIDE"), RenamePlugin.GetPCNameOverride(afk, observer),
                "the observer still sees the known name with the AFK tag");
        }

        /// <summary>
        /// A tell from the observer to the player they named goes through the tell handler of this build: while the chat message is
        /// delivered, the observer sees the player's chat name (the known name alone), not the name with the descriptor beside it.
        /// </summary>
        private async Task VerifyTellAsync(EngineTestContext ctx)
        {
            var module = GetModule();
            var afk = GetLocalObject(module, Prefix + "AFK");
            var observer = GetLocalObject(module, Prefix + "OBSERVER");
            var restored = RenamePlugin.GetPCNameOverride(afk, observer);
            var duringTell = string.Empty;
            await ctx.ExecuteInCreatureContextAsync(observer, () =>
            {
                ChatPlugin.SendMessage(SWLOR.Game.Server.Core.NWNX.Enum.ChatChannel.PlayerTell, "Reload check", observer, afk);
                duringTell = RenamePlugin.GetPCNameOverride(afk, observer);
            });
            ctx.AssertEqual(NamedAs + AfkSuffix, duringTell, "the tell handler put the chat name on the player the observer named");
            await ctx.WaitUntilAsync(() => RenamePlugin.GetPCNameOverride(afk, observer) == restored, 3f, "the tell handler to restore the name again");
        }

        /// <summary>The last phase: moving the player ends AFK, which needs the movement check the reload started again.</summary>
        private async Task VerifyMovementClearsAfkAsync(EngineTestContext ctx)
        {
            var module = GetModule();
            var afk = GetLocalObject(module, Prefix + "AFK");
            var observer = GetLocalObject(module, Prefix + "OBSERVER");
            ObjectPlugin.SetPosition(afk, GetPosition(afk) + new System.Numerics.Vector3(1f, 0f, 0f));
            await ctx.WaitUntilAsync(() => !NameOverrides.IsAfk(afk), 4f, "the movement check of the new build to clear AFK");
            ctx.AssertEqual(Names.GetDisplayName(observer, afk), RenamePlugin.GetPCNameOverride(afk, observer), "the name has no AFK tag after moving");
        }

        private void Release(EngineTestContext ctx)
        {
            var module = GetModule();
            var afk = GetLocalObject(module, Prefix + "AFK");
            var observer = GetLocalObject(module, Prefix + "OBSERVER");
            var observerId = GetObjectUUID(observer);
            using (PlayerAbilityFixture.Reattach(afk, (uint)GetLocalInt(module, Prefix + "AFK_CLIENT"), GetLocalInt(module, Prefix + "AFK_WAS_PLAYER")))
            using (PlayerAbilityFixture.Reattach(observer, (uint)GetLocalInt(module, Prefix + "OBSERVER_CLIENT"), GetLocalInt(module, Prefix + "OBSERVER_WAS_PLAYER")))
            {
            }

            ctx.Track(afk);
            ctx.Track(observer);
            foreach (var names in DB.Search(new DBQuery<PlayerKnownName>()
                         .AddFieldSearch(nameof(Entity.PlayerKnownName.ObserverPlayerId), observerId, false)
                         .AddPaging(50, 0)).ToList())
                DB.Delete<PlayerKnownName>(names.Id);

            foreach (var name in new[] { "AFK", "OBSERVER" })
                DeleteLocalObject(module, Prefix + name);
            foreach (var name in new[] { "AFK_CLIENT", "AFK_WAS_PLAYER", "OBSERVER_CLIENT", "OBSERVER_WAS_PLAYER", "VERIFIED", "PREPARED" })
                DeleteLocalInt(module, Prefix + name);
            foreach (var name in new[] { "TOKEN", "EXPECTED_OVERRIDE" })
                DeleteLocalString(module, Prefix + name);
            DeleteLocalInt(module, EngineTest.KeepArenaVariable);
        }
    }
}
