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

        foreach (var feat in new[] { FeatType.BlasterSlide1, FeatType.BlasterSlide2, FeatType.BlasterSlide3, FeatType.BlasterSlide4 })
        {
            var slide = MovementAttack(feat);
            slide.MinimumActivatorDistanceToTargetAfterImpact = 3.5f;
            slide.MinimumActivatorTravelDistanceAfterImpact = 2f;
            slide.SetupNPCPerkLevels[PerkType.TwinPistolMomentum] = 1;
            slide.ExpectedActivatorStatusEffects = new[] { typeof(MomentumStatusEffect) };
            cases.Add(slide);
        }

        foreach (var feat in new[] { FeatType.GrappleShot1, FeatType.GrappleShot2, FeatType.GrappleShot3 })
        {
            var grapple = MovementAttack(feat);
            grapple.TargetDistanceMeters = 6f;
            grapple.MaximumActivatorDistanceToTargetAfterImpact = 2f;
            grapple.MinimumActivatorTravelDistanceAfterImpact = 3f;
            cases.Add(grapple);
        }

        // Rank I uses a player-style ground cursor beside the enemy.
        var rush = MovementAttack(FeatType.PistolRush1);
        rush.TargetDistanceMeters = 2.5f;
        // The enemy stays within the four-meter firing corridor, off the travel path.
        rush.GroundTargetOffsetMeters = new System.Numerics.Vector2(4.2f, 2f);
        rush.MinimumActivatorTravelDistanceAfterImpact = 4f;
        rush.MaximumActivatorDistanceToGroundTargetAfterImpact = .75f;
        rush.SetupNPCPerkLevels[PerkType.TwinPistolMomentum] = 1;
        rush.ExpectedActivatorStatusEffects = new[] { typeof(MomentumStatusEffect) };
        cases.Add(rush);

        // Rank II is cast the way NPC and droid AI casts it, at the enemy's own location; the dash must
        // pass beside the enemy and still strike it.
        var aiRush = MovementAttack(FeatType.PistolRush2);
        aiRush.TargetDistanceMeters = 3.5f;
        aiRush.MinimumActivatorTravelDistanceAfterImpact = 3f;
        aiRush.SetupNPCPerkLevels[PerkType.TwinPistolMomentum] = 1;
        aiRush.ExpectedActivatorStatusEffects = new[] { typeof(MomentumStatusEffect) };
        cases.Add(aiRush);

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

    private static AbilityBehaviorCase MovementAttack(FeatType feat)
    {
        var attack = Attack(feat);
        attack.TargetIsStationary = true;
        attack.SearchForSafeMovementLane = true;
        return attack;
    }
}
