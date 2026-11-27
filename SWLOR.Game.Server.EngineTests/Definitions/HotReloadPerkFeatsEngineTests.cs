using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// What the granted feat and refund parts rebuild across hot reloads (MaintainabilityRefactorCharacterCompanionsSplit.md, "What a hot
    /// reload must recheck"). One phase per generation like <see cref="HotReloadPerksEngineTests"/>: the first run checks the parts in the
    /// boot build; each later run, in the new build, checks that
    /// <list type="bullet">
    /// <item>exactly one <see cref="PerkGrantedFeatService"/> handler is registered for the cache event that builds the lookups and exactly one
    /// <see cref="PerkRefundService"/> handler for the skill decay event, each bound to this build's instance;</item>
    /// <item>this build's feat lookups hold the active ability feats of every perk, as an independent walk of the perk and ability catalogs finds
    /// them (the handlers filled them once for the generation, not left empty);</item>
    /// <item>a purchased perk's feat reaches a player through this build's sync, and the login restore gives it back after it is taken away;</item>
    /// <item>a skill decayed at the skill cap still refunds the perk level it gates (the refund handler of this build): the level, the
    /// price and the feat.</item>
    /// </list>
    /// Reload is off in the normal engine sweep, so there the scenario skips; it runs with <c>scripts/run-hot-reload-check.sh
    /// --engine-filter HotReloadPlayers --reloads 2</c>.
    /// </summary>
    public sealed class HotReloadPerkFeatsEngineTests
    {
        private const string Prefix = "SWLOR_HRF_";
        private const int ReloadsToVerify = 2;
        private const SkillType GainedSkill = SkillType.Piloting;
        private const PerkType GatedPerk = PerkType.Provoke;
        private const FeatType GatedPerkFeat = FeatType.Provoke1;

        // One per loaded build: a run whose token differs from the module's is the first run of a new generation.
        private static readonly string GenerationToken = Guid.NewGuid().ToString("N");

        public HotReloadPerkFeatsEngineTests(
            IPerkService perks,
            IPerkGrantedFeatService grantedFeats,
            IPerkRefundService refunds,
            IAbilityCatalog abilities,
            ISkillService skill,
            ISkillXPService skillXP,
            IDBService db)
        {
            Perks = perks;
            GrantedFeats = grantedFeats;
            Refunds = refunds;
            Abilities = abilities;
            Skill = skill;
            SkillXP = skillXP;
            DB = db;
        }

        private IPerkService Perks { get; }
        private IPerkGrantedFeatService GrantedFeats { get; }
        private IPerkRefundService Refunds { get; }
        private IAbilityCatalog Abilities { get; }
        private ISkillService Skill { get; }
        private ISkillXPService SkillXP { get; }
        private IDBService DB { get; }

        [EngineTest("A perk's feats fill once per generation and a refund still works across hot reloads", Category = "HotReloadPlayers", TimeoutSeconds = 180f)]
        public async Task FeatsAndRefundsSurviveReloads(EngineTestContext ctx)
        {
            if (Environment.GetEnvironmentVariable("SWLOR_HOT_RELOAD")?.Trim().ToLowerInvariant() is not ("true" or "1" or "yes"))
                ctx.Skip("Hot reload is off; run scripts/run-hot-reload-check.sh --engine-filter HotReloadPlayers --reloads 2.");

            var module = GetModule();
            if (GetLocalInt(module, Prefix + "PREPARED") == 0)
            {
                await Verify(ctx);

                SetLocalString(module, Prefix + "TOKEN", GenerationToken);
                SetLocalInt(module, Prefix + "VERIFIED", 0);
                SetLocalInt(module, Prefix + "PREPARED", 1);
                ctx.SetResultDetail("prepared: reload the server " + ReloadsToVerify + " times to verify");
                return;
            }

            if (GetLocalString(module, Prefix + "TOKEN") == GenerationToken)
                ctx.Skip("No reload since the last phase of this scenario ran.");

            await Verify(ctx);
            var verified = GetLocalInt(module, Prefix + "VERIFIED") + 1;
            SetLocalString(module, Prefix + "TOKEN", GenerationToken);
            SetLocalInt(module, Prefix + "VERIFIED", verified);
            ctx.SetResultDetail($"verified after reload {verified} of {ReloadsToVerify}");
            if (verified >= ReloadsToVerify)
                Release();
        }

        private async Task Verify(EngineTestContext ctx)
        {
            AssertHandlers(ctx);
            AssertFeatLookups(ctx);

            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            var requirement = GetPerkSkillRequirement(ctx, out var gatingSkill);
            var price = Perks.GetPerkDetails(GatedPerk).PerkLevels[1].Price;
            fixture.Update(record =>
            {
                AtTheCap(record, gatingSkill, requirement);
                record.Perks[GatedPerk] = 1;
                record.UnallocatedSP = 0;
            });

            await ctx.ExecuteInCreatureContextAsync(player, () => GrantedFeats.SyncGrantedFeats(player, GatedPerk, 1, true));
            ctx.Assert(CreaturePlugin.GetKnowsFeat(player, GatedPerkFeat), "the purchased perk's feat reaches the player through this build's sync");

            await ctx.ExecuteInCreatureContextAsync(player, () => CreaturePlugin.RemoveFeat(player, GatedPerkFeat));
            ctx.Assert(!CreaturePlugin.GetKnowsFeat(player, GatedPerkFeat), "the feat is taken away");
            await ctx.ExecuteInCreatureContextAsync(player, () => GrantedFeats.RestorePlayerFeats(player));
            ctx.Assert(CreaturePlugin.GetKnowsFeat(player, GatedPerkFeat), "the login restore gives the purchased feat back");

            await ctx.ExecuteInCreatureContextAsync(player, () => SkillXP.GiveSkillXP(player, GainedSkill, Skill.GetRequiredXP(0), true, false));
            await ctx.WaitFrameAsync();

            var record = DB.Get<Player>(fixture.Id);
            ctx.AssertEqual(requirement - 1, record.Skills[gatingSkill].Rank, "the only unlocked skill decays by a rank");
            ctx.AssertEqual(0, record.Perks[GatedPerk], "the perk level its skill requirement no longer supports is refunded by this build's handler");
            ctx.AssertEqual(price, record.UnallocatedSP, "the refunded level returns its price");
            ctx.Assert(!CreaturePlugin.GetKnowsFeat(player, GatedPerkFeat), "the refunded level's feat is removed");
        }

        private void AssertHandlers(EngineTestContext ctx)
        {
            AssertOneHandler(ctx, ScriptName.OnModuleCacheAfter, nameof(PerkGrantedFeatService), GrantedFeats);
            AssertOneHandler(ctx, ScriptName.OnSwlorLoseSkill, nameof(PerkRefundService), Refunds);
        }

        private static void AssertOneHandler(EngineTestContext ctx, string script, string service, object instance)
        {
            var own = ServerManager.Scripts.GetActionScripts(script)
                .Where(entry => entry.Name.Contains(service))
                .ToList();
            ctx.AssertEqual(1, own.Count, $"exactly one {service} handler is registered for {script}");
            ctx.Assert(ReferenceEquals(own[0].Action.Target, instance), $"the {script} handler of {service} is bound to this build's instance");
        }

        /// <summary>
        /// The active ability feats of each perk, found without the part: the ability's perk is the perk, and the feat is granted by one of its levels.
        /// </summary>
        private void AssertFeatLookups(EngineTestContext ctx)
        {
            var abilities = Abilities.GetAllAbilityDetails();
            var withFeats = 0;
            foreach (var (perkType, detail) in Perks.EnumerateAllPerks())
            {
                var expected = new List<FeatType>();
                foreach (var (_, level) in detail.PerkLevels.OrderBy(pair => pair.Key))
                {
                    foreach (var feat in level.GrantedFeats)
                    {
                        if (perkType != PerkType.Invalid
                            && abilities.TryGetValue(feat, out var ability)
                            && ability.EffectiveLevelPerkType == perkType
                            && !expected.Contains(feat))
                            expected.Add(feat);
                    }
                }

                var found = GrantedFeats.GetAllActiveAbilityFeats(perkType);
                if (Perks.GetPerkMaxLevel(perkType) <= 0)
                    continue;

                ctx.AssertEqual(string.Join(",", expected), string.Join(",", found), $"{perkType} holds its active ability feats in this build");
                if (expected.Count > 0)
                    withFeats++;
            }

            ctx.Assert(withFeats > 0, "the catalog has perks with active ability feats to check");
        }

        private int GetPerkSkillRequirement(EngineTestContext ctx, out SkillType skill)
        {
            var requirements = Perks.GetPerkDetails(GatedPerk).PerkLevels[1].Requirements.OfType<PerkRequirementSkill>().ToArray();
            ctx.AssertEqual(1, requirements.Length, $"{GatedPerk} level 1 has one skill requirement");
            skill = requirements[0].Type;
            return requirements[0].RequiredRank;
        }

        /// <summary>
        /// Shapes the record so a rank gained by <see cref="GainedSkill"/> must decay <paramref name="candidate"/> and nothing else: the candidate
        /// holds the only unlocked rank, every other skill that counts toward the cap is locked at the ranks that bring the total to the cap, and
        /// the gained skill starts at rank 0.
        /// </summary>
        private void AtTheCap(Player record, SkillType candidate, int candidateRank)
        {
            foreach (var skill in Enum.GetValues<SkillType>())
                record.Skills[skill] = new PlayerSkill();

            record.Skills[candidate].Rank = candidateRank;
            var remaining = SkillService.SkillCap - candidateRank;
            foreach (var skill in Skill.GetAllContributingSkills().Keys.Where(skill => skill != GainedSkill && skill != candidate).OrderBy(skill => (int)skill))
            {
                var rank = Math.Min(Skill.GetSkillDetails(skill).MaxRank, remaining);
                if (rank <= 0)
                    break;

                record.Skills[skill].Rank = rank;
                record.Skills[skill].IsLocked = true;
                remaining -= rank;
            }

            if (remaining > 0)
                throw new InvalidOperationException($"The cap cannot be filled: {remaining} ranks short.");

            record.TotalSPAcquired = SkillService.SkillCap;
            record.TotalAPAcquired = SkillService.SkillCap / 10;
            record.XPDebt = 0;
        }

        private void Release()
        {
            var module = GetModule();
            foreach (var name in new[] { "VERIFIED", "PREPARED" })
                DeleteLocalInt(module, Prefix + name);
            DeleteLocalString(module, Prefix + "TOKEN");
        }
    }
}
