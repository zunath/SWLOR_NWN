using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.AbilityBehaviors;

public sealed class TwinPistolAbilityBehaviors : IAbilityBehaviorSource
{
    [EngineTest("Twin Pistol ability behaviors", Category = "AbilityBehavior", TimeoutSeconds = 900f)]
    public static Task Run(EngineTestContext ctx) => AbilityBehaviorExecutor.RunAsync(ctx, new TwinPistolAbilityBehaviors().BuildCases());

    public List<AbilityBehaviorCase> BuildCases()
    {
        var cases = new List<AbilityBehaviorCase>();
        foreach (var feat in new[] { FeatType.PairedVolley1, FeatType.PairedVolley2, FeatType.PairedVolley3, FeatType.PairedVolley4,
                     FeatType.FocusedBurst1, FeatType.FocusedBurst2, FeatType.FocusedBurst3, FeatType.FinalSalvo })
            cases.Add(Attack(feat));

        foreach (var feat in new[] { FeatType.ControlledBurst1, FeatType.ControlledBurst2 })
        {
            var attack = Attack(feat);
            attack.ImpactRefundsCosts = true;
            attack.Notes = "The successful activation restores 2/3 STM once, within its actual paid cost.";
            cases.Add(attack);
        }

        var twinSuns = Attack(FeatType.TwinSuns);
        twinSuns.ExpectedActivatorStatusEffects = new[] { typeof(TwinSunsStatusEffect) };
        cases.Add(twinSuns);

        // Lead Shot and Tempo Shot grant their own Cadence on a landed hit, without needing a chain.
        foreach (var feat in new[] { FeatType.LeadShot1, FeatType.LeadShot2, FeatType.LeadShot3, FeatType.LeadShot4,
                     FeatType.TempoShot1, FeatType.TempoShot2 })
        {
            var attack = Attack(feat);
            attack.SetupNPCPerkLevels[PerkType.Cadence] = 1;
            attack.ExpectedActivatorStatusEffects = new[] { typeof(CadenceStatusEffect) };
            cases.Add(attack);
        }

        foreach (var feat in new[] { FeatType.ClosingShot1, FeatType.ClosingShot2, FeatType.ClosingShot3 })
            cases.Add(Attack(feat));

        cases.Add(new AbilityBehaviorCase
        {
            Feat = FeatType.AkimboStance,
            Target = AbilityTargetKind.Self,
            ExpectedActivatorStatusEffects = new[] { typeof(AkimboStanceStatusEffect) },
            ExpectedActivatorStatAdjustments = new()
            {
                [StatType.AttackDelayReductionPercent] = 10,
                [StatType.OrdinaryAttackDamagePercentAdjustment] = -30,
            },
            ExpectsRecast = true,
            VerifiesImmediateRecastRejection = true,
        });
        cases.Add(new AbilityBehaviorCase
        {
            Feat = FeatType.CrossfireStance,
            Target = AbilityTargetKind.Self,
            ExpectedActivatorStatusEffects = new[] { typeof(CrossfireStanceStatusEffect) },
            ExpectedActivatorStatAdjustments = new()
            {
                [StatType.AttackDelayReductionPercent] = 10,
                [StatType.OrdinaryAttackDamagePercentAdjustment] = -20,
                [StatType.MovementSpeedPercentAdjustment] = 10,
            },
            ExpectsRecast = true,
            VerifiesImmediateRecastRejection = true,
        });
        return cases;
    }

    private static AbilityBehaviorCase Attack(FeatType feat) => new()
    {
        Feat = feat,
        Target = AbilityTargetKind.HostileCreature,
        EquipMainHandResref = "b_twinpistol",
        ExpectsTargetDamage = true,
        ExpectsSTMCost = true,
        ExpectsRecast = true,
        VerifiesImmediateRecastRejection = true,
    };
}
