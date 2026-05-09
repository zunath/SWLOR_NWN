using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Pistol
{
    public class PistolActiveAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureCastedTarget(builder.Create(FeatType.QuickDraw1, PerkType.QuickDraw).Name("Quick Draw I").Level(1), SkillType.Pistol, 12, 3);
            ConfigureMultiHit(builder.Create(FeatType.DoubleShot1, PerkType.DoubleShot).Name("Double Shot I").Level(1), SkillType.Pistol, 7, 2, 5);
            ConfigureCastedTarget(builder.Create(FeatType.QuickDraw2, PerkType.QuickDraw).Name("Quick Draw II").Level(2), SkillType.Pistol, 24, 5);
            ConfigureMultiHit(builder.Create(FeatType.DoubleShot2, PerkType.DoubleShot).Name("Double Shot II").Level(2), SkillType.Pistol, 15, 2, 6);
            ConfigureToggle(builder.Create(FeatType.GunfighterStance1, PerkType.GunfighterStance).Name("Gunfighter Stance").Level(1), typeof(GunfighterStanceStatusEffect));
            ConfigureCastedTarget(builder.Create(FeatType.QuickDraw3, PerkType.QuickDraw).Name("Quick Draw III").Level(3), SkillType.Pistol, 36, 7);
            ConfigureMultiHit(builder.Create(FeatType.DoubleShot3, PerkType.DoubleShot).Name("Double Shot III").Level(3), SkillType.Pistol, 24, 2, 8);
            ConfigureCastedTarget(builder.Create(FeatType.QuickDraw4, PerkType.QuickDraw).Name("Quick Draw IV").Level(4), SkillType.Pistol, 50, 10, extraDamageWhenLowHp: 20);
            ConfigureSelfStatus(builder.Create(FeatType.GunslingerFocus1, PerkType.GunslingerFocus).Name("Gunslinger Focus").Level(1), typeof(GunslingerFocusStatusEffect), 20f, 6);
            ConfigureSelfStatus(builder.Create(FeatType.SnapRoll1, PerkType.SnapRoll).Name("Snap Roll I").Level(1), typeof(SnapRollStatusEffect), 6f, 25, activator => Enmity.ModifyEnmityOnAll(activator, -150));
            ConfigureToggle(builder.Create(FeatType.SkirmisherStance1, PerkType.SkirmisherStance).Name("Skirmisher Stance").Level(1), typeof(SkirmisherStanceStatusEffect));
            ConfigureTargetedInterrupt(builder.Create(FeatType.InterruptingShot1, PerkType.InterruptingShot).Name("Interrupting Shot I").Level(1), SkillType.Pistol, 0, 12, 12, SavingThrow.Will, typeof(FoggyMindStatusEffect), 6, FoggyMind(2));
            ConfigureSelfStatus(builder.Create(FeatType.SnapRoll2, PerkType.SnapRoll).Name("Snap Roll II").Level(2), typeof(SnapRollStatusEffect), 8f, 35, activator => Enmity.ModifyEnmityOnAll(activator, -250));
            ConfigureTargetedInterrupt(builder.Create(FeatType.InterruptingShot2, PerkType.InterruptingShot).Name("Interrupting Shot II").Level(2), SkillType.Pistol, 20, 20, 16, SavingThrow.Will, typeof(FoggyMindStatusEffect), 8, FoggyMind(2));


            return builder.Build();
        }

        private static void ConfigureTargetedInterrupt(
            AbilityBuilder ability,
            SkillType skill,
            int baseDamage,
            int duration,
            int savingThrowDc,
            SavingThrow savingThrow,
            Type statusEffect,
            int stamina,
            Func<IStatusEffect> statusEffectFactory = null)
        {
            ability.HasActivationDelay(0f)
                .RequiresTarget()
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    AssignCommand(target, () => ClearAllActions());
                    Ability.ApplyCombatImpact(activator, target, targetLocation, skill, baseDamage, duration, savingThrowDc, savingThrow, statusEffect, false, statusEffectFactory: statusEffectFactory);
                })
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth();

            if (stamina > 0)
                ability.RequirementStamina(stamina);
        }
    }
}
