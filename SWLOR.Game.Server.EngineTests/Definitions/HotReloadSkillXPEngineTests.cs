using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// What the skill XP service keeps across hot reloads (MaintainabilityRefactorCharacterCompanionsSplit.md, "What a hot reload must
    /// recheck"). One headless player stays online across the reloads, one phase per generation like <see cref="HotReloadPlayersEngineTests"/>.
    /// The first run puts a skill at its rank limit and awards it XP, which shows the rank limit warning once. Each later run, in the new build,
    /// checks that an award at the limit still adds nothing, that the build's once-per-login set holds one entry for that skill however
    /// many awards follow, and that the build's XP message queue still combines two awards of one source into one message. The set is state
    /// of the build (<c>Runtime</c>): a reload starts it empty, and the run records whether the entry was there before its first award.
    /// Reload is off in the normal engine sweep, so there the scenario skips; it runs with <c>scripts/run-hot-reload-check.sh
    /// --engine-filter HotReloadPlayers --reloads 2</c>.
    /// </summary>
    public sealed class HotReloadSkillXPEngineTests
    {
        private const string Prefix = "SWLOR_HRX_";
        private const int ReloadsToVerify = 2;
        private const SkillType LimitedSkill = SkillType.Piloting;
        private const SkillType FreeSkill = SkillType.Smithery;

        // One per loaded build: a run whose token differs from the module's is the first run of a new generation.
        private static readonly string GenerationToken = Guid.NewGuid().ToString("N");

        public HotReloadSkillXPEngineTests(ISkillXPService skillXP, ISkillRankService skillRank, IDBService db)
        {
            SkillXP = skillXP;
            SkillRank = skillRank;
            DB = db;
        }

        private ISkillXPService SkillXP { get; }
        private ISkillRankService SkillRank { get; }
        private IDBService DB { get; }

        [EngineTest("A player at a rank limit keeps one warning and the XP message queue across hot reloads", Category = "HotReloadPlayers", TimeoutSeconds = 120f)]
        public async Task LimitWarningAndMessageQueueSurviveReloads(EngineTestContext ctx)
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

            var player = GetLocalObject(module, Prefix + "PLAYER");
            ctx.Assert(GetIsObjectValid(player), "the player is still in the world");
            var carried = Warned(player);
            await AwardAtTheLimitAsync(ctx, player);
            await VerifyMessageQueueAsync(ctx, player);
            await VerifyRankRulesAsync(ctx, player);

            var verified = GetLocalInt(module, Prefix + "VERIFIED") + 1;
            SetLocalString(module, Prefix + "TOKEN", GenerationToken);
            SetLocalInt(module, Prefix + "VERIFIED", verified);
            ctx.SetResultDetail($"verified after reload {verified} of {ReloadsToVerify}; the limit warning was {(carried ? "already recorded" : "not recorded")} in the new build before its first award");
            if (verified >= ReloadsToVerify)
                Release(ctx);
        }

        private async Task PrepareAsync(EngineTestContext ctx)
        {
            var module = GetModule();
            var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            ctx.Untrack(player);
            fixture.KeepAcrossReloads();
            SetLocalInt(module, EngineTest.KeepArenaVariable, 1);

            fixture.Update(record =>
            {
                record.XPDebt = 0;
                record.DMXPBonus = 0;
                foreach (var skill in new[] { LimitedSkill, FreeSkill })
                {
                    record.Skills[skill] = new PlayerSkill { Rank = 5, XP = 0, IsLocked = false, RankLimit = skill == LimitedSkill ? 5 : null };
                }
            });

            ctx.Assert(!Warned(player), "no rank limit warning is recorded before the first award");
            await AwardAtTheLimitAsync(ctx, player);
            await VerifyRankRulesAsync(ctx, player);

            SetLocalObject(module, Prefix + "PLAYER", player);
            SetLocalString(module, Prefix + "PLAYER_ID", fixture.Id);
            SetLocalInt(module, Prefix + "PLAYER_CLIENT", (int)fixture.ClientId);
            SetLocalInt(module, Prefix + "PLAYER_WAS_PLAYER", fixture.WasPlayer);
            SetLocalString(module, Prefix + "TOKEN", GenerationToken);
            SetLocalInt(module, Prefix + "VERIFIED", 0);
            SetLocalInt(module, Prefix + "PREPARED", 1);
        }

        private async Task AwardAtTheLimitAsync(EngineTestContext ctx, uint player)
        {
            var playerId = GetObjectUUID(player);
            for (var award = 0; award < 2; award++)
            {
                var used = 0;
                await ctx.ExecuteInCreatureContextAsync(player, () => used = SkillXP.GiveSkillXP(player, LimitedSkill, 100, true, false));
                ctx.AssertEqual(0, used, "an award at the rank limit adds nothing");
                ctx.AssertEqual(5, DB.Get<Player>(playerId).Skills[LimitedSkill].Rank, "the limited skill stays at its rank");
            }

            ctx.Assert(Warned(player), "the build recorded the rank limit warning for the player");
            ctx.AssertEqual(1, WarningSet().Count(entry => entry.PlayerId == playerId), "the build holds one warning entry per limited skill however many awards follow");
        }

        private async Task VerifyMessageQueueAsync(EngineTestContext ctx, uint player)
        {
            var queue = ServiceInternals.Field<ISkillXPService, SkillXPMessageQueue>("_xpMessages");
            var sendField = typeof(SkillXPMessageQueue).GetField("_send", BindingFlags.NonPublic | BindingFlags.Instance);
            var originalSend = (Action<uint, SkillType, int>)sendField.GetValue(queue);
            var messages = new List<int>();
            sendField.SetValue(queue, new Action<uint, SkillType, int>((target, skill, xp) =>
            {
                if (target == player && skill == FreeSkill)
                    messages.Add(xp);
                originalSend(target, skill, xp);
            }));
            try
            {
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    SkillXP.GiveSkillXP(player, FreeSkill, 10, true, false, player);
                    SkillXP.GiveSkillXP(player, FreeSkill, 15, true, false, player);
                });
                await ctx.WaitUntilAsync(() => messages.Count > 0, 3f, "the combined XP message of the build's queue");
                await ctx.WaitFrameAsync();
                ctx.AssertEqual(1, messages.Count, "two awards of one source send one message");
                ctx.AssertEqual(25, messages[0], "the message carries the combined XP");
            }
            finally
            {
                sendField.SetValue(queue, originalSend);
            }
        }

        /// <summary>
        /// The rank rules of the running build: the limit, the ceiling and the XP a limited skill can still take, the skill cap block, and a skill
        /// decay at the cap, which picks the one unlocked candidate. The player's own skill records are put back afterwards.
        /// </summary>
        private async Task VerifyRankRulesAsync(EngineTestContext ctx, uint player)
        {
            var playerId = GetObjectUUID(player);
            var record = DB.Get<Player>(playerId);
            var limited = record.Skills[LimitedSkill];
            var details = ServiceInternals.Field<ISkillService, Dictionary<SkillType, SkillAttribute>>("_allSkills")[LimitedSkill];
            ctx.AssertEqual(5, SkillRank.GetRankLimit(limited, details) ?? -1, "the rank limit of the limited skill");
            ctx.AssertEqual(5, SkillRank.GetRankCeiling(limited, details), "the rank ceiling is the limit");
            ctx.AssertEqual(0, SkillRank.GetMaxDistributableXP(record, LimitedSkill), "a skill at its limit can take no RP XP");
            ctx.Assert(SkillRank.GetMaxDistributableXP(record, FreeSkill) > 0, "a skill below its ceiling can take RP XP");

            var backup = record.Skills.ToDictionary(entry => entry.Key, entry => new PlayerSkill
            {
                Rank = entry.Value.Rank, XP = entry.Value.XP, IsLocked = entry.Value.IsLocked, RankLimit = entry.Value.RankLimit
            });
            try
            {
                var fillers = new[] { SkillType.FirstAid, SkillType.Fabrication, SkillType.Gathering, SkillType.Leadership, SkillType.BeastMastery, SkillType.Force, SkillType.Armor, SkillType.Unarmed };
                foreach (var key in record.Skills.Keys.ToList())
                    record.Skills[key] = new PlayerSkill();
                foreach (var filler in fillers)
                    record.Skills[filler] = new PlayerSkill { Rank = 50, IsLocked = filler != SkillType.Leadership };
                DB.Set(record);

                ctx.AssertEqual(400, SkillRank.GetTotalContributingSkillRanks(record), "the cap total counts the contributing ranks");
                ctx.Assert(!SkillRank.IsBlockedBySkillCap(record, LimitedSkill), "the unlocked skill can still give up a rank");
                ctx.AssertEqual(SkillType.Leadership, SkillRank.GetDecayCandidates(record, LimitedSkill).Single(), "the one unlocked skill is the decay candidate");

                await ctx.ExecuteInCreatureContextAsync(player, () => SkillXP.GiveSkillXP(player, LimitedSkill, SkillXPChart.GetRequiredXP(0), true, false));
                var after = DB.Get<Player>(playerId);
                ctx.AssertEqual(1, after.Skills[LimitedSkill].Rank, "the award raised the skill by a rank");
                ctx.AssertEqual(49, after.Skills[SkillType.Leadership].Rank, "decay took a rank from the one unlocked candidate");
                ctx.AssertEqual(400, SkillRank.GetTotalContributingSkillRanks(after), "the total stays at the cap");
            }
            finally
            {
                var restore = DB.Get<Player>(playerId);
                restore.Skills.Clear();
                foreach (var entry in backup)
                    restore.Skills[entry.Key] = entry.Value;
                DB.Set(restore);
            }
        }

        private static HashSet<(string PlayerId, SkillType Skill)> WarningSet() =>
            ServiceInternals.Field<ISkillXPService, HashSet<(string PlayerId, SkillType Skill)>>("_limitWarningsSent");

        private static bool Warned(uint player) => WarningSet().Contains((GetObjectUUID(player), LimitedSkill));

        private void Release(EngineTestContext ctx)
        {
            var module = GetModule();
            var player = GetLocalObject(module, Prefix + "PLAYER");
            using (PlayerAbilityFixture.Reattach(player, (uint)GetLocalInt(module, Prefix + "PLAYER_CLIENT"), GetLocalInt(module, Prefix + "PLAYER_WAS_PLAYER")))
            {
            }

            ctx.Track(player);
            DeleteLocalObject(module, Prefix + "PLAYER");
            foreach (var name in new[] { "PLAYER_CLIENT", "PLAYER_WAS_PLAYER", "VERIFIED", "PREPARED" })
                DeleteLocalInt(module, Prefix + name);
            foreach (var name in new[] { "TOKEN", "PLAYER_ID" })
                DeleteLocalString(module, Prefix + name);
            DeleteLocalInt(module, EngineTest.KeepArenaVariable);
        }
    }
}
