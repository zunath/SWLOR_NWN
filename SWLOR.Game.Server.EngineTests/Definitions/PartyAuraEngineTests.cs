using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class PartyAuraEngineTests
    {
        private const string CreatureResref = "nw_bandit001";

        /// <summary>
        /// The aura enter event fires only when a creature walks into the aura, so a creature already
        /// standing inside it when it joins the party must still be enrolled as a party recipient.
        /// </summary>
        [EngineTest("Party auras reach an ally who joins the party while already inside the aura", Category = "PartyAura", TimeoutSeconds = 60f)]
        public static async Task AllyJoiningInsideAuraReceivesPartyAuras(EngineTestContext ctx)
        {
            using var leader = await PlayerAbilityFixture.CreateAsync(ctx);
            TemporaryStatModifier.Add(leader.Creature, StatType.ChargeOrderAuraLevel, 1, 120f);
            var ally = await SpawnHeldCreatureAsync(ctx, 2f, 0f);
            await ApplyAuraAndWaitForCreatureInsideAsync(ctx, leader.Creature, ally);

            try
            {
                await ToggleChargeOrderAsync(ctx, leader.Creature);
                ctx.Assert(HasChargeOrder(leader.Creature, leader.Creature), "Charge Order is active on the leader");
                ctx.Assert(!HasChargeOrder(ally, leader.Creature), "a creature outside the party does not receive Charge Order");

                AssignCommand(leader.Creature, () => AddHenchman(leader.Creature, ally));
                await ctx.WaitUntilAsync(() => Party.IsInParty(leader.Creature, ally), 5f, "the ally to join the leader's party");
                await ctx.WaitUntilAsync(() => HasChargeOrder(ally, leader.Creature), 3f,
                    "the active Charge Order to reach the ally who joined inside the aura");
                ctx.Assert(Ability.GetSelfAndPartyAuraRecipients(leader.Creature).Contains(ally),
                    "the ally who joined inside the aura is a party aura recipient");

                await ToggleChargeOrderAsync(ctx, leader.Creature);
                ctx.Assert(!HasChargeOrder(ally, leader.Creature), "toggling Charge Order off removes it from the ally");
                await ToggleChargeOrderAsync(ctx, leader.Creature);
                ctx.Assert(HasChargeOrder(ally, leader.Creature), "toggling Charge Order on reaches the ally inside the aura");
            }
            finally
            {
                Ability.RemoveAura(leader.Creature, typeof(ChargeOrder1StatusEffect));
            }
        }

        /// <summary>
        /// A player who joins a party with a creature already standing inside the player's own aura
        /// must start supporting that creature without it having to leave and re-enter. Removing a
        /// player's henchman despawns it, so the join and leave cycles drive the party invitation
        /// handlers' own paths instead.
        /// </summary>
        [EngineTest("A player who joins a party reaches members already inside their aura", Category = "PartyAura", TimeoutSeconds = 60f)]
        public static async Task JoiningPlayerAuraReachesMembersInside(EngineTestContext ctx)
        {
            using var joiner = await PlayerAbilityFixture.CreateAsync(ctx);
            TemporaryStatModifier.Add(joiner.Creature, StatType.ChargeOrderAuraLevel, 1, 120f);
            var member = await SpawnHeldCreatureAsync(ctx, 2f, 0f);
            await ApplyAuraAndWaitForCreatureInsideAsync(ctx, joiner.Creature, member);

            try
            {
                for (var cycle = 1; cycle <= 2; cycle++)
                {
                    // The joining player accepts the member's invitation.
                    await ctx.ExecuteInCreatureContextAsync(joiner.Creature, () => InvokeParty("AddToParty", member, joiner.Creature));
                    ctx.Assert(Party.IsInParty(joiner.Creature, member), $"cycle {cycle}: the player joins the member's party");
                    ctx.Assert(Ability.GetSelfAndPartyAuraRecipients(joiner.Creature).Contains(member),
                        $"cycle {cycle}: the member already inside the joining player's aura is a party aura recipient");

                    if (cycle == 1)
                    {
                        ctx.Assert(!HasChargeOrder(member, joiner.Creature), "Charge Order is not active before it is toggled");
                        await ToggleChargeOrderAsync(ctx, joiner.Creature);
                        ctx.Assert(HasChargeOrder(member, joiner.Creature),
                            "toggling Charge Order on reaches the member already inside the joining player's aura");
                    }
                    else
                    {
                        ctx.Assert(HasChargeOrder(member, joiner.Creature),
                            $"cycle {cycle}: the active Charge Order reaches the member when the player rejoins");
                    }

                    await ctx.ExecuteInCreatureContextAsync(joiner.Creature, () => InvokeParty("RemoveCreatureFromParty", joiner.Creature));
                    ctx.Assert(!Party.IsInParty(joiner.Creature, member), $"cycle {cycle}: the player leaves the party");
                    ctx.Assert(!Ability.GetSelfAndPartyAuraRecipients(joiner.Creature).Contains(member),
                        $"cycle {cycle}: the member is no longer a party aura recipient after the player leaves");
                    ctx.Assert(!HasChargeOrder(member, joiner.Creature),
                        $"cycle {cycle}: leaving the party removes Charge Order from the member");
                }
            }
            finally
            {
                Ability.RemoveAura(joiner.Creature, typeof(ChargeOrder1StatusEffect));
                if (Party.IsInParty(joiner.Creature, member))
                    await ctx.ExecuteInCreatureContextAsync(joiner.Creature, () => InvokeParty("RemoveCreatureFromParty", joiner.Creature));
            }
        }

        /// <summary>
        /// A stronger player's aura replaces a weaker player's own copy of the same aura when they join.
        /// Once the stronger copy goes away, the weaker player's still-active aura must cover them again.
        /// </summary>
        [EngineTest("A weaker aura returns when the stronger party aura that replaced it ends", Category = "PartyAura", TimeoutSeconds = 60f)]
        public static async Task WeakerAuraReturnsWhenStrongerSourceEnds(EngineTestContext ctx)
        {
            using var strong = await PlayerAbilityFixture.CreateAsync(ctx);
            using var weak = await PlayerAbilityFixture.CreateAsync(ctx, 2f);
            CreaturePlugin.SetRawAbilityScore(strong.Creature, AbilityType.Social, 20);
            CreaturePlugin.SetRawAbilityScore(weak.Creature, AbilityType.Social, 10);
            foreach (var player in new[] { strong.Creature, weak.Creature })
            {
                TemporaryStatModifier.Add(player, StatType.ChargeOrderAuraLevel, 1, 120f);
                Ability.ReapplyPlayerAuraAOE(player);
            }

            await ctx.WaitUntilAsync(() => IsInsidePlayerAura(strong.Creature, weak.Creature) && IsInsidePlayerAura(weak.Creature, strong.Creature), 10f,
                "both players to stand inside each other's aura");
            await ctx.DelaySecondsAsync(1f);

            try
            {
                await ToggleChargeOrderAsync(ctx, strong.Creature);
                await ToggleChargeOrderAsync(ctx, weak.Creature);
                ctx.Assert(HasChargeOrder(strong.Creature, strong.Creature) && HasChargeOrder(weak.Creature, weak.Creature),
                    "each player starts with their own Charge Order");

                await JoinStrongPartyAsync(ctx, strong.Creature, weak.Creature);
                await ctx.ExecuteInCreatureContextAsync(weak.Creature, () => InvokeParty("RemoveCreatureFromParty", weak.Creature));
                ctx.Assert(HasChargeOrder(weak.Creature, weak.Creature) && !HasChargeOrder(weak.Creature, strong.Creature),
                    "leaving the party restores the weaker player's own Charge Order");

                await JoinStrongPartyAsync(ctx, strong.Creature, weak.Creature);
                await ToggleChargeOrderAsync(ctx, strong.Creature);
                ctx.Assert(HasChargeOrder(weak.Creature, weak.Creature) && !HasChargeOrder(weak.Creature, strong.Creature),
                    "toggling the stronger Charge Order off restores the weaker player's own Charge Order");
                ctx.Assert(HasChargeOrder(strong.Creature, weak.Creature),
                    "the stronger player is covered by the weaker player's Charge Order once their own ends");
            }
            finally
            {
                Ability.RemoveAura(strong.Creature, typeof(ChargeOrder1StatusEffect));
                Ability.RemoveAura(weak.Creature, typeof(ChargeOrder1StatusEffect));
                if (Party.IsInParty(strong.Creature, weak.Creature))
                    await ctx.ExecuteInCreatureContextAsync(weak.Creature, () => InvokeParty("RemoveCreatureFromParty", weak.Creature));
            }
        }

        private static async Task JoinStrongPartyAsync(EngineTestContext ctx, uint strong, uint weak)
        {
            await ctx.ExecuteInCreatureContextAsync(weak, () => InvokeParty("AddToParty", strong, weak));
            ctx.Assert(HasChargeOrder(weak, strong) && !HasChargeOrder(weak, weak),
                "the stronger Charge Order replaces the weaker player's own copy on joining");
            ctx.Assert(HasChargeOrder(strong, strong) && !HasChargeOrder(strong, weak),
                "the stronger player keeps their own Charge Order");
        }

        private static async Task ApplyAuraAndWaitForCreatureInsideAsync(EngineTestContext ctx, uint player, uint creature)
        {
            ctx.Assert(!Party.IsInParty(player, creature), "the creature starts outside the player's party");
            Ability.ReapplyPlayerAuraAOE(player);
            await ctx.WaitUntilAsync(() => IsInsidePlayerAura(player, creature), 10f,
                "the creature to stand inside the player's aura before joining the party");

            // The enter event runs as the creature is placed inside the aura; give it a moment to settle.
            await ctx.DelaySecondsAsync(1f);
            ctx.Assert(!Ability.GetSelfAndPartyAuraRecipients(player).Contains(creature),
                "a creature outside the party is not a party aura recipient");
        }

        private static bool IsInsidePlayerAura(uint player, uint creature)
        {
            foreach (var aoe in ObjectSnapshot.InArea(GetArea(player), ObjectType.AreaOfEffect))
            {
                if (GetAreaOfEffectCreator(aoe) != player ||
                    GetEventScript(aoe, EventScript.AreaOfEffect_OnObjectEnter) != ScriptName.OnAuraEnter)
                    continue;

                var inside = ObjectSnapshot.Capture(() => GetFirstInPersistentObject(aoe),
                    () => GetNextInPersistentObject(aoe), GetIsObjectValid);
                if (inside.Contains(creature))
                    return true;
            }

            return false;
        }

        private static async Task ToggleChargeOrderAsync(EngineTestContext ctx, uint caster)
        {
            var ability = Ability.GetAbilityDetail(FeatType.ChargeOrder1);
            await ctx.ExecuteInCreatureContextAsync(caster, () =>
            {
                Ability.BeginAbilityImpact(caster, ability);
                try
                {
                    ability.ImpactAction(caster, caster, 1, GetLocation(caster));
                }
                finally
                {
                    Ability.EndAbilityImpact(caster);
                }
            });
        }

        private static bool HasChargeOrder(uint creature, uint source)
        {
            return StatusEffect.HasStatusEffect(creature, typeof(ChargeOrder1StatusEffect), source);
        }

        private static void InvokeParty(string methodName, params object[] arguments)
        {
            typeof(Party).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, arguments);
        }

        private static async Task<uint> SpawnHeldCreatureAsync(EngineTestContext ctx, float xOffset, float yOffset)
        {
            var creature = ctx.SpawnCreature(CreatureResref, xOffset, yOffset);
            await ctx.WaitFrameAsync();
            ctx.SuppressNPCNaturalRegen(creature);
            ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), creature, 600f);
            return creature;
        }
    }
}
