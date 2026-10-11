using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class AllySupportCombatPointEngineTests
    {
        private const string CreatureResref = "nw_bandit001";

        // Over-time support (Kolto Mist pulses, Emergency Bunker refreshes) settles across two
        // pulses so the test proves each enemy is credited once per cast, not once per pulse.
        // Each runs after the other cases for its skill because it keeps pulsing into later cases.
        private static readonly (string Name, FeatType Feat, SkillType Skill, int Amount, bool AllyCasts, float SettleSeconds)[] Cases =
        {
            ("Med Kit", FeatType.MedKit1, SkillType.FirstAid, 1, false, 0f),
            ("Emergency Triage", FeatType.EmergencyTriage1, SkillType.FirstAid, 1, false, 0f),
            ("Kolto Mist", FeatType.KoltoMist1, SkillType.FirstAid, 1, false, 7f),
            ("Deflector Shield", FeatType.DeflectorShield1, SkillType.Devices, 1, false, 0f),
            ("Group Deflector", FeatType.GroupDeflector1, SkillType.Devices, 1, false, 0f),
            ("Power Cell", FeatType.PowerCell1, SkillType.Devices, 1, false, 0f),
            ("Emergency Bunker", FeatType.EmergencyBunker1, SkillType.Devices, 1, false, 7f),
            ("Press the Attack", FeatType.PressTheAttack1, SkillType.Leadership, 2, false, 0f),
            ("Rousing Shout", FeatType.RousingShout1, SkillType.Leadership, 2, false, 0f),
            ("Benevolence", FeatType.Benevolence1, SkillType.Force, 1, false, 0f),
            ("Renewal", FeatType.Renewal1, SkillType.Force, 1, false, 0f),
            ("Innervate", FeatType.Innervate1, SkillType.BeastMastery, 1, true, 0f),
        };

        /// <summary>
        /// A supporter who never attacked must be credited for the enemies fighting the ally they
        /// supported, and only those: aggro-proximity enmity and bystanders do not count.
        /// </summary>
        [EngineTest("Ground support abilities credit the enemies fighting the allies they support", Category = "SupportXP", TimeoutSeconds = 180f)]
        public static async Task SupportCreditsAllyEnemies(EngineTestContext ctx)
        {
            using var supporter = await PlayerAbilityFixture.CreateAsync(ctx);
            var ally = await SpawnPartyAllyAsync(ctx, supporter.Creature);

            foreach (var (name, feat, skill, amount, allyCasts, settleSeconds) in Cases)
            {
                var fighting = await SpawnHeldCreatureAsync(ctx, 6f, 0f);
                var aggroOnly = await SpawnHeldCreatureAsync(ctx, 6f, 3f);
                var idle = await SpawnHeldCreatureAsync(ctx, 6f, -3f);
                Enmity.ModifyEnmity(ally, fighting, 100);
                Enmity.ModifyProximityEnmity(ally, aggroOnly, 1);

                await UseAbilityAsync(ctx, allyCasts ? ally : supporter.Creature, feat, ally);
                if (settleSeconds > 0f)
                    await ctx.DelaySecondsAsync(settleSeconds);

                ctx.AssertEqual(amount, Points(supporter.Creature, fighting, skill),
                    $"{name} credits {skill} once for the enemy fighting the supported ally");
                ctx.Assert(!CombatPoint.HasCombatPoints(supporter.Creature, aggroOnly),
                    $"{name} does not credit an enemy holding only aggro-proximity enmity");
                ctx.Assert(!CombatPoint.HasCombatPoints(supporter.Creature, idle),
                    $"{name} does not credit an enemy outside the fight");

                await DespawnAsync(ctx, fighting, aggroOnly, idle);
                ClearCombatPoints(supporter.Creature);
            }
        }

        /// <summary>
        /// Leadership aura toggles support every party member inside the aura, so enemies
        /// fighting any of them are credited.
        /// </summary>
        [EngineTest("Leadership aura toggles credit the enemies fighting party members in the aura", Category = "SupportXP", TimeoutSeconds = 60f)]
        public static async Task AuraToggleCreditsPartyEnemies(EngineTestContext ctx)
        {
            using var leader = await PlayerAbilityFixture.CreateAsync(ctx);
            var ally = await SpawnPartyAllyAsync(ctx, leader.Creature);
            Ability.ReapplyPlayerAuraAOE(leader.Creature);
            await ctx.WaitUntilAsync(() => Ability.GetSelfAndPartyAuraRecipients(leader.Creature).Contains(ally), 10f,
                "the party ally to enter the leader's aura");

            TemporaryStatModifier.Add(leader.Creature, StatType.ChargeOrderAuraLevel, 1, 60f);
            var fighting = await SpawnHeldCreatureAsync(ctx, 6f, 0f);
            Enmity.ModifyEnmity(ally, fighting, 100);
            try
            {
                await UseAbilityAsync(ctx, leader.Creature, FeatType.ChargeOrder1, leader.Creature);
                ctx.AssertEqual(1, Points(leader.Creature, fighting, SkillType.Leadership),
                    "toggling Charge Order on credits the enemy fighting a party member inside the aura");
            }
            finally
            {
                Ability.RemoveAura(leader.Creature, typeof(ChargeOrder1StatusEffect));
                ClearCombatPoints(leader.Creature);
            }
        }

        /// <summary>
        /// The credit pays out through the normal kill distribution: a medic who only healed
        /// earns First Aid XP when the enemy fighting their patient dies.
        /// </summary>
        [EngineTest("A medic who only heals earns First Aid XP when the patient's enemy dies", Category = "SupportXP", TimeoutSeconds = 60f)]
        public static async Task HealingMedicEarnsKillXP(EngineTestContext ctx)
        {
            using var medic = await PlayerAbilityFixture.CreateAsync(ctx);
            medic.Update(record =>
            {
                record.Skills[SkillType.FirstAid].Rank = 0;
                record.Skills[SkillType.FirstAid].XP = 0;
                record.XPDebt = 0;
                record.DMXPBonus = 0;
            });
            var ally = await SpawnPartyAllyAsync(ctx, medic.Creature);
            var fighting = await SpawnHeldCreatureAsync(ctx, 6f, 0f);
            Enmity.ModifyEnmity(ally, fighting, 100);

            await UseAbilityAsync(ctx, medic.Creature, FeatType.MedKit1, ally);
            await ctx.ExecuteInCreatureContextAsync(fighting, () => ApplyEffectToObject(DurationType.Instant, EffectDeath(), fighting));
            await ctx.WaitUntilAsync(() => !CombatPoint.HasCombatPoints(medic.Creature, fighting), 5f, "kill XP payout and cleanup");

            var firstAid = DB.Get<Player>(medic.Id).Skills[SkillType.FirstAid];
            ctx.Assert(firstAid.Rank > 0 || firstAid.XP > 0, "the medic earns First Aid XP from the patient's enemy");
        }

        private static async Task<uint> SpawnPartyAllyAsync(EngineTestContext ctx, uint supporter)
        {
            var ally = await SpawnHeldCreatureAsync(ctx, 2f, 0f);
            AssignCommand(supporter, () => AddHenchman(supporter, ally));
            await ctx.WaitUntilAsync(() => Party.IsInParty(supporter, ally), 5f, "the ally to join the supporter's party");
            return ally;
        }

        /// <summary>
        /// Paralyzed creatures keep their enmity but cannot attack, so no creature turns an
        /// aggro-only or idle enemy into a real combatant during a case.
        /// </summary>
        private static async Task<uint> SpawnHeldCreatureAsync(EngineTestContext ctx, float xOffset, float yOffset)
        {
            var creature = ctx.SpawnCreature(CreatureResref, xOffset, yOffset);
            await ctx.WaitFrameAsync();
            ctx.SuppressNPCNaturalRegen(creature);
            Stat.SetNPCMaxHitPoints(creature, 1000, true);
            ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), creature, 600f);
            return creature;
        }

        private static async Task UseAbilityAsync(EngineTestContext ctx, uint caster, FeatType feat, uint target)
        {
            var ability = Ability.GetAbilityDetail(feat);
            await ctx.ExecuteInCreatureContextAsync(caster, () =>
            {
                Ability.BeginAbilityImpact(caster, ability);
                try
                {
                    ability.ImpactAction(caster, target, 1, GetLocation(target));
                }
                finally
                {
                    Ability.EndAbilityImpact(caster);
                }
            });
        }

        private static async Task DespawnAsync(EngineTestContext ctx, params uint[] creatures)
        {
            foreach (var creature in creatures)
            {
                Enmity.ClearEnmityTable(creature);
                DestroyObject(creature);
            }

            await ctx.WaitUntilAsync(() => Array.TrueForAll(creatures, creature => !GetIsObjectValid(creature)), 5f,
                "case creatures to despawn");
        }

        private static int Points(uint player, uint creature, SkillType skill)
        {
            var tracker = (Dictionary<uint, Dictionary<uint, Dictionary<SkillType, int>>>)typeof(CombatPoint)
                .GetField("_creatureCombatPointTracker", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            return tracker.TryGetValue(creature, out var byPlayer) &&
                   byPlayer.TryGetValue(player, out var bySkill) &&
                   bySkill.TryGetValue(skill, out var points)
                ? points
                : 0;
        }

        private static void ClearCombatPoints(uint player)
        {
            typeof(CombatPoint).GetMethod("ClearPlayerCombatPoints", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { player });
        }
    }
}
