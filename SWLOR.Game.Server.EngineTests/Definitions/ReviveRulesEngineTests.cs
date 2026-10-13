using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class ReviveRulesEngineTests
    {
        private const string ActorResref = "nw_bandit001";

        [EngineTest("Resuscitation fails at no cost while a hostile player is within 20m", Category = "Revive", TimeoutSeconds = 40f)]
        public static async Task HostilePlayerNearbyBlocksRevive(EngineTestContext ctx)
        {
            using var player = await PlayerAbilityFixture.CreateAsync(ctx, 4f);
            var (caster, body) = await PrepareReviveAsync(ctx);
            try
            {
                // Both NPCs share the Hostile faction: allies to each other, enemies of the player.
                ctx.MakeHostile(caster);
                ctx.MakeHostile(body);
                ctx.Assert(GetIsReactionTypeHostile(player.Creature, caster), "the player is hostile to the caster");

                var stamina = Stat.GetCurrentStamina(caster);
                var (activated, denial) = await TryReviveAsync(ctx, caster, body);
                ctx.Assert(!activated, "the revive is refused");
                ctx.AssertEqual(ReviveRules.HostilePlayerNearbyMessage, denial, "denial reason");
                ctx.AssertEqual(stamina, Stat.GetCurrentStamina(caster), "a refused revive spends no STM");

                ChangeToStandardFaction(caster, StandardFaction.Defender);
                ChangeToStandardFaction(body, StandardFaction.Defender);
                ctx.Assert(!GetIsReactionTypeHostile(player.Creature, caster), "the player is no longer hostile to the caster");
                (activated, denial) = await TryReviveAsync(ctx, caster, body);
                ctx.Assert(activated, $"the revive works once the nearby player is not hostile: {denial}");
                await ctx.WaitUntilAsync(() => !GetIsDead(body), 8f, "the body to be revived");
            }
            finally
            {
                ReleaseBody(body);
            }
        }

        [EngineTest("After a player kill, Resuscitation waits for 15 seconds without party damage", Category = "Revive", TimeoutSeconds = 50f)]
        public static async Task RecentPartyDamageBlocksReviveAfterPlayerKill(EngineTestContext ctx)
        {
            using var player = await PlayerAbilityFixture.CreateAsync(ctx, 4f);
            var (caster, body) = await PrepareReviveAsync(ctx);
            try
            {
                ctx.Assert(!GetIsReactionTypeHostile(player.Creature, caster), "the player is not hostile to the caster");
                ReviveRules.RecordPlayerDeath(body, player.Creature);

                var attacker = ctx.SpawnCreature("nw_rat001", -3f);
                AssignCommand(attacker, () => ApplyEffectToObject(DurationType.Instant, EffectDamage(1), caster));
                await ctx.WaitUntilAsync(() => Combat.HasRecentDamageTaken(caster, 1f), 3f, "the caster to register damage");

                var (activated, denial) = await TryReviveAsync(ctx, caster, body);
                ctx.Assert(!activated, "the revive is refused right after party damage");
                ctx.AssertEqual(ReviveRules.PartyRecentlyDamagedMessage, denial, "denial reason");

                await ctx.DelaySecondsAsync(ReviveRules.PartyDamageWindowSeconds + 0.5f);
                (activated, denial) = await TryReviveAsync(ctx, caster, body);
                ctx.Assert(activated, $"the revive works after 15 seconds without damage: {denial}");
                await ctx.WaitUntilAsync(() => !GetIsDead(body), 8f, "the body to be revived");
            }
            finally
            {
                ReleaseBody(body);
            }
        }

        [EngineTest("Taking damage interrupts a Resuscitation cast before anything is spent", Category = "Revive", TimeoutSeconds = 30f)]
        public static async Task DamageInterruptsReviveCast(EngineTestContext ctx)
        {
            var (caster, body) = await PrepareReviveAsync(ctx);
            try
            {
                var attacker = ctx.SpawnCreature("nw_rat001", -3f);
                var stamina = Stat.GetCurrentStamina(caster);

                var (activated, denial) = await TryReviveAsync(ctx, caster, body);
                ctx.Assert(activated, $"the revive cast starts: {denial}");
                await ctx.DelaySecondsAsync(1f);
                AssignCommand(attacker, () => ApplyEffectToObject(DurationType.Instant, EffectDamage(1), caster));
                await ctx.WaitUntilAsync(() => !Activity.IsBusy(caster), 3f, "damage to interrupt the cast");

                await ctx.DelaySecondsAsync(4f);
                ctx.Assert(GetIsDead(body), "an interrupted revive leaves the body dead");
                ctx.AssertEqual(stamina, Stat.GetCurrentStamina(caster), "an interrupted revive spends no STM");
            }
            finally
            {
                ReleaseBody(body);
            }
        }

        private static async Task<(uint Caster, uint Body)> PrepareReviveAsync(EngineTestContext ctx)
        {
            var caster = ctx.SpawnCreature(ActorResref, -0.5f);
            var body = ctx.SpawnCreature(ActorResref, 1f);
            SetAILevel(caster, AILevel.VeryLow);
            SetAILevel(body, AILevel.VeryLow);
            await ctx.WaitFrameAsync();

            Stat.SetNPCMaxHitPoints(caster, 1000, true);
            ctx.SetResources(caster, 9999, 9999);
            ctx.SuppressNPCNaturalRegen(caster);
            ctx.SetNPCPerkLevel(caster, PerkType.Resuscitation, 1);
            CreaturePlugin.AddFeat(caster, FeatType.Resuscitation1);

            ApplyEffectToObject(DurationType.Instant, EffectDeath(), body);
            await ctx.WaitUntilAsync(() => GetIsDead(body), 5f, "the body to die");
            await ctx.WaitFrameAsync();
            DestroyCorpsePlaceable(body);
            return (caster, body);
        }

        private static async Task<(bool Activated, string Denial)> TryReviveAsync(EngineTestContext ctx, uint caster, uint body)
        {
            var activated = false;
            var denial = string.Empty;
            await ctx.ExecuteInCreatureContextAsync(caster, () =>
            {
                activated = UsePerkFeat.TryUseAbility(caster, body, FeatType.Resuscitation1, GetLocation(body));
                denial = Ability.GetLastActivationDenialReason();
            });
            return (activated, denial);
        }

        // The death pipeline marks the body non-destroyable, which would leave it behind after cleanup.
        private static void ReleaseBody(uint body)
        {
            if (GetIsObjectValid(body))
                AssignCommand(body, () => SetIsDestroyable(true, false, false));
        }

        // The death pipeline also leaves an untracked corpse placeable beside the body.
        private static void DestroyCorpsePlaceable(uint body)
        {
            for (var nth = 1; ; nth++)
            {
                var placeable = GetNearestObject(ObjectType.Placeable, body, nth);
                if (!GetIsObjectValid(placeable) || GetDistanceBetween(body, placeable) > 10f)
                    break;

                if (GetLocalObject(placeable, Loot.CorpseBodyVariable) == body)
                {
                    DestroyObject(placeable);
                    break;
                }
            }
        }
    }
}
