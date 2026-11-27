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
    /// What the Mimicry loadout part keeps and rebuilds across hot reloads (MaintainabilityRefactorCharacterCompanionsSplit.md, "What a hot
    /// reload must recheck"). The scenario spans generations of the build, so it keeps its state on the module and runs in phases, one per
    /// generation like <see cref="HotReloadPlayersEngineTests"/>: the first run equips a technique on a headless player it leaves online;
    /// every run, in the build it ran in, checks that
    /// <list type="bullet">
    /// <item>exactly one <see cref="MimicryLoadoutService"/> handler is registered for the module enter event (the login restore) and one for
    /// the skill decay event, each bound to this build's instance;</item>
    /// <item>the technique the kept player equipped before the reload is still equipped: the saved loadout, the native feat and this build's
    /// reads (<see cref="IMimicryLoadoutService.IsTechniqueEquipped(uint, FeatType)"/>, the learned and equipped lists);</item>
    /// <item>the login restore of this build gives the technique's feat back after it is taken away;</item>
    /// <item>a Mimicry rank lost to decay at the skill cap still unequips the technique it no longer supports (the decay handler of this build).</item>
    /// </list>
    /// Reload is off in the normal engine sweep, so there the scenario skips; it runs with <c>scripts/run-hot-reload-check.sh
    /// --engine-filter HotReloadPlayers --reloads 2</c>.
    /// </summary>
    public sealed class HotReloadMimicryLoadoutEngineTests
    {
        private const string Prefix = "SWLOR_HRM_";
        private const int ReloadsToVerify = 2;
        private const FeatType Technique = FeatType.BrutalBashTechnique;
        private const SkillType GainedSkill = SkillType.Piloting;

        // One per loaded build: a run whose token differs from the module's is the first run of a new generation.
        private static readonly string GenerationToken = Guid.NewGuid().ToString("N");

        public HotReloadMimicryLoadoutEngineTests(
            IMimicryLoadoutService loadout,
            IMimicryService mimicry,
            IAbilityCatalog abilities,
            ISkillService skill,
            ISkillXPService skillXP,
            IDBService db)
        {
            Loadout = loadout;
            Mimicry = mimicry;
            Abilities = abilities;
            Skill = skill;
            SkillXP = skillXP;
            DB = db;
        }

        private IMimicryLoadoutService Loadout { get; }
        private IMimicryService Mimicry { get; }
        private IAbilityCatalog Abilities { get; }
        private ISkillService Skill { get; }
        private ISkillXPService SkillXP { get; }
        private IDBService DB { get; }

        [EngineTest("An equipped technique stays equipped and the login restore and the decay handler work across hot reloads", Category = "HotReloadPlayers", TimeoutSeconds = 180f)]
        public async Task LoadoutSurvivesReloads(EngineTestContext ctx)
        {
            if (Environment.GetEnvironmentVariable("SWLOR_HOT_RELOAD")?.Trim().ToLowerInvariant() is not ("true" or "1" or "yes"))
                ctx.Skip("Hot reload is off; run scripts/run-hot-reload-check.sh --engine-filter HotReloadPlayers --reloads 2.");

            var module = GetModule();
            if (GetLocalInt(module, Prefix + "PREPARED") == 0)
            {
                await PrepareAsync(ctx);
                ctx.SetResultDetail($"prepared {GetLocalObject(module, Prefix + "CREATURE")}: reload the server {ReloadsToVerify} times to verify");
                return;
            }

            if (GetLocalString(module, Prefix + "TOKEN") == GenerationToken)
                ctx.Skip("No reload since the last phase of this scenario ran.");

            var creature = GetLocalObject(module, Prefix + "CREATURE");
            ctx.Assert(GetIsObjectValid(creature) && GetIsPC(creature), "the kept player is still online");
            AssertStillEquipped(ctx, creature, GetLocalString(module, Prefix + "ID"));
            await Verify(ctx, creature);

            var verified = GetLocalInt(module, Prefix + "VERIFIED") + 1;
            SetLocalString(module, Prefix + "TOKEN", GenerationToken);
            SetLocalInt(module, Prefix + "VERIFIED", verified);
            ctx.SetResultDetail($"verified after reload {verified} of {ReloadsToVerify}");
            if (verified >= ReloadsToVerify)
                Release(ctx);

            await ctx.WaitFrameAsync();
        }

        private async Task PrepareAsync(EngineTestContext ctx)
        {
            var module = GetModule();
            var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var creature = fixture.Creature;
            ctx.Untrack(creature);
            fixture.KeepAcrossReloads();
            SetLocalInt(module, EngineTest.KeepArenaVariable, 1);

            var requirement = Abilities.GetAbilityDetail(Technique).MimicrySkillRequirement;
            fixture.Update(record =>
            {
                record.Perks[PerkType.CombatAnalyzer] = 2;
                record.Skills[SkillType.Mimicry] = new PlayerSkill { Rank = requirement };
                record.LearnedTechniques[Technique] = DateTime.UtcNow;
            });
            await ctx.ExecuteInCreatureContextAsync(creature, () =>
                ctx.Assert(Loadout.EquipTechnique(creature, Technique), "the learned technique equips in the boot build"));
            AssertStillEquipped(ctx, creature, fixture.Id);
            await Verify(ctx, creature);

            SetLocalObject(module, Prefix + "CREATURE", creature);
            SetLocalString(module, Prefix + "ID", fixture.Id);
            SetLocalInt(module, Prefix + "CLIENT", (int)fixture.ClientId);
            SetLocalInt(module, Prefix + "WAS_PLAYER", fixture.WasPlayer);
            SetLocalString(module, Prefix + "TOKEN", GenerationToken);
            SetLocalInt(module, Prefix + "VERIFIED", 0);
            SetLocalInt(module, Prefix + "PREPARED", 1);
        }

        private void AssertStillEquipped(EngineTestContext ctx, uint creature, string playerId)
        {
            var record = DB.Get<Player>(playerId);
            ctx.Assert(record.EquippedTechniques.Contains(Technique), "the saved loadout still holds the technique");
            ctx.Assert(CreaturePlugin.GetKnowsFeat(creature, Technique), "the technique's native feat is still known");
            ctx.Assert(Loadout.IsTechniqueEquipped(creature, Technique), "this build reads the technique as equipped");
            ctx.AssertEqual(Technique, Loadout.GetEquippedTechniques(creature).Single().Feat, "this build lists the equipped technique with its detail");
            ctx.Assert(Loadout.GetLearnedTechniques(creature).Any(pair => pair.Feat == Technique), "this build lists the learned technique");
            ctx.AssertEqual(Mimicry.GetTechniqueDetail(Technique).MimicrySlotCost, Loadout.GetUsedSlots(creature), "the used slots are the technique's cost");
            ctx.AssertEqual(Loadout.GetMaxSlots(record), Loadout.GetMaxSlots(creature), "this build reads the slot budget of the saved record");
            ctx.Assert(Loadout.GetUsedSlots(creature) <= Loadout.GetMaxSlots(creature), "the equipped technique fits the slot budget");
        }

        private async Task Verify(EngineTestContext ctx, uint creature)
        {
            AssertOneHandler(ctx, ScriptName.OnModuleEnter, nameof(MimicryLoadoutService), Loadout);
            AssertOneHandler(ctx, ScriptName.OnSwlorLoseSkill, nameof(MimicryLoadoutService), Loadout);

            // The login restore of this build: the feat is taken away, and the restore grants it again for the equipped technique.
            await ctx.ExecuteInCreatureContextAsync(creature, () => CreaturePlugin.RemoveFeat(creature, Technique));
            ctx.Assert(!CreaturePlugin.GetKnowsFeat(creature, Technique), "the feat is taken away");
            await ctx.ExecuteInCreatureContextAsync(creature, () => ServiceInternals.Invoke<IMimicryLoadoutService>("RestorePlayerTechniques", creature));
            ctx.Assert(CreaturePlugin.GetKnowsFeat(creature, Technique), "the login restore of this build gives the equipped technique's feat back");

            await VerifyDecay(ctx);
        }

        private static void AssertOneHandler(EngineTestContext ctx, string script, string service, object instance)
        {
            var own = ServerManager.Scripts.GetActionScripts(script)
                .Where(entry => entry.Name.Contains(service))
                .ToList();
            ctx.AssertEqual(1, own.Count, $"exactly one {service} handler is registered for {script}");
            ctx.Assert(ReferenceEquals(own[0].Action.Target, instance), $"the {script} handler of {service} is bound to this build's instance");
        }

        /// <summary>A rank gained at the skill cap decays the Mimicry rank, and the handler of this build unequips what it no longer supports.</summary>
        private async Task VerifyDecay(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            var requirement = Abilities.GetAbilityDetail(Technique).MimicrySkillRequirement;
            fixture.Update(record =>
            {
                AtTheCap(record, requirement);
                record.Perks[PerkType.CombatAnalyzer] = 2;
                record.UnallocatedSP = 0;
                record.LearnedTechniques[Technique] = DateTime.UtcNow;
                record.EquippedTechniques.Add(Technique);
            });
            await ctx.ExecuteInCreatureContextAsync(player, () => CreaturePlugin.AddFeat(player, Technique));
            ctx.Assert(CreaturePlugin.GetKnowsFeat(player, Technique), "the equipped technique's feat is known before the decay");

            await ctx.ExecuteInCreatureContextAsync(player, () => SkillXP.GiveSkillXP(player, GainedSkill, Skill.GetRequiredXP(0), true, false));
            await ctx.WaitFrameAsync();

            var record = DB.Get<Player>(fixture.Id);
            ctx.AssertEqual(requirement - 1, record.Skills[SkillType.Mimicry].Rank, "Mimicry is the only unlocked skill and decays by a rank");
            ctx.AssertEqual(0, record.EquippedTechniques.Count, "the decay handler of this build unequips the technique the lost rank no longer supports");
            ctx.Assert(!CreaturePlugin.GetKnowsFeat(player, Technique), "the unequipped technique's feat is removed");
        }

        /// <summary>
        /// Shapes the record so a rank gained by <see cref="GainedSkill"/> must decay Mimicry and nothing else: Mimicry holds the only unlocked
        /// rank, every other skill that counts toward the cap is locked at the ranks that bring the total to the cap, and the gained skill starts at rank 0.
        /// </summary>
        private void AtTheCap(Player record, int mimicryRank)
        {
            foreach (var skill in Enum.GetValues<SkillType>())
                record.Skills[skill] = new PlayerSkill();

            record.Skills[SkillType.Mimicry].Rank = mimicryRank;
            var remaining = SkillService.SkillCap - mimicryRank;
            foreach (var skill in Skill.GetAllContributingSkills().Keys.Where(skill => skill != GainedSkill && skill != SkillType.Mimicry).OrderBy(skill => (int)skill))
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

        /// <summary>The last phase: takes the player offline and lets the runner destroy the arena again.</summary>
        private static void Release(EngineTestContext ctx)
        {
            var module = GetModule();
            var creature = GetLocalObject(module, Prefix + "CREATURE");
            using (PlayerAbilityFixture.Reattach(creature, (uint)GetLocalInt(module, Prefix + "CLIENT"), GetLocalInt(module, Prefix + "WAS_PLAYER")))
            {
            }

            ctx.Track(creature);
            DeleteLocalObject(module, Prefix + "CREATURE");
            DeleteLocalString(module, Prefix + "ID");
            DeleteLocalString(module, Prefix + "TOKEN");
            foreach (var name in new[] { "CLIENT", "WAS_PLAYER", "VERIFIED", "PREPARED" })
                DeleteLocalInt(module, Prefix + name);
            DeleteLocalInt(module, EngineTest.KeepArenaVariable);
        }
    }
}
