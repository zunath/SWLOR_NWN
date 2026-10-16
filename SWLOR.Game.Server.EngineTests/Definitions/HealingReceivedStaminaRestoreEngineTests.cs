using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class HealingReceivedStaminaRestoreEngineTests
    {
        private const int RestoredStamina = 8;
        private const int CooldownSeconds = 6;

        [EngineTest("Healing-received STM restore triggers only on instant ability heals, once per cooldown", Category = "CombatHealing", TimeoutSeconds = 30f)]
        public static async Task InstantAbilityHealsOnlyWithCooldown(EngineTestContext ctx)
        {
            var healer = ctx.SpawnCreature("nw_bandit001");
            var recipient = ctx.SpawnCreature("nw_bandit001", 1f);
            await ctx.DelaySecondsAsync(1f);
            ctx.SetNPCResources(recipient, 100, 100);
            ctx.SuppressNPCNaturalRegen(recipient);
            Stat.SetNPCMaxHitPoints(recipient, 1000, true);
            ObjectPlugin.SetCurrentHitPoints(recipient, 100);
            Stat.ReduceStamina(recipient, Stat.GetCurrentStamina(recipient));
            TemporaryStatModifier.Add(recipient, StatType.HealingReceivedStaminaRestoreChance, 100, 60f);
            TemporaryStatModifier.Add(recipient, StatType.HealingReceivedStaminaRestore, RestoredStamina, 60f);
            TemporaryStatModifier.Add(recipient, StatType.HealingReceivedStaminaRestoreCooldownSeconds, CooldownSeconds, 60f);

            Stat.ApplyHealingReceivedAdjustment(recipient, 50);
            ctx.AssertEqual(0, Stat.GetCurrentStamina(recipient), "regeneration, lifesteal and other non-instant heals never restore STM");

            AbilityEffectScaling.ApplyActivatedScaledHeal(healer, recipient, 5);
            ctx.AssertEqual(RestoredStamina, Stat.GetCurrentStamina(recipient), "an instant ability heal restores STM");

            AbilityEffectScaling.ApplyActivatedScaledHeal(healer, recipient, 5);
            ctx.AssertEqual(RestoredStamina, Stat.GetCurrentStamina(recipient), "a second instant heal inside the cooldown restores nothing");

            await ctx.DelaySecondsAsync(CooldownSeconds + 0.5f);
            AbilityEffectScaling.ApplyActivatedScaledHeal(healer, recipient, 5);
            ctx.AssertEqual(RestoredStamina * 2, Stat.GetCurrentStamina(recipient), "the restore is available again after the cooldown");
        }
    }
}
