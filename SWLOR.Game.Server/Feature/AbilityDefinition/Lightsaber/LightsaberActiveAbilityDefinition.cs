using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Lightsaber
{
    public class LightsaberActiveAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            builder.Create(FeatType.TauntingDeflection1, PerkType.TauntingDeflection)
                .Name("Taunting Deflection")
                .Level(1)
                .HasActivationDelay(0f)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    StatusEffect.ApplyStatusEffect(activator, activator, typeof(TauntingDeflectionStatusEffect), 30f);
                    Enmity.ModifyEnmityOnAll(activator, 850);
                })
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(6);

            ConfigurePartyStatus(builder.Create(FeatType.GuardiansInfluence1, PerkType.GuardiansInfluence).Name("Guardian's Influence").Level(1), typeof(DeflectingAuraStatusEffect), 60f, 15, false);
            ConfigureToggle(builder.Create(FeatType.ImpenetrableGuard1, PerkType.ImpenetrableGuard).Name("Impenetrable Guard").Level(1), typeof(ImpenetrableGuardStatusEffect));
            ConfigureSelfStatus(builder.Create(FeatType.GuardianMaster1, PerkType.GuardianMaster).Name("Guardian Master").Level(1), typeof(GuardiansWrathStatusEffect), 30f, 12);
            ConfigureToggle(builder.Create(FeatType.FerocityStance1, PerkType.FerocityStance).Name("Ferocity Stance").Level(1), typeof(FerocityStanceStatusEffect));
            ConfigureSelfStatus(builder.Create(FeatType.Centering1, PerkType.Centering).Name("Centering I").Level(1), typeof(CenteringStatusEffect), 30f, 10, activator => Enmity.ModifyEnmityOnAll(activator, -250));
            ConfigureToggle(builder.Create(FeatType.FocusedStance1, PerkType.FocusedStance).Name("Focused Stance").Level(1), typeof(FocusedStanceStatusEffect));
            ConfigurePartyStatus(builder.Create(FeatType.BrutalAssault1, PerkType.BrutalAssault).Name("Brutal Assault").Level(1), typeof(BrutalAssaultStatusEffect), 60f, 10, false);

            builder.Create(FeatType.SecondWind1, PerkType.SecondWind)
                .Name("Second Wind")
                .Level(1)
                .HasActivationDelay(0f)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    var percent = Math.Min(75, 50 + Math.Max(0, GetAbilityModifier(AbilityType.Might, activator)));
                    var amount = Math.Max(1, (int)Math.Ceiling(Stat.GetMaxStamina(activator) * (percent / 100f)));
                    Stat.RestoreStamina(activator, amount);
                })
                .IsCastedAbility()
                .BreaksStealth();

            builder.Create(FeatType.Purify1, PerkType.Purify)
                .Name("Purify")
                .Level(1)
                .HasActivationDelay(0f)
                .HasImpactAction((activator, target, level, targetLocation) => PurifyAndMirror(activator))
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementFP(5);

            ConfigureSelfStatus(builder.Create(FeatType.Centering2, PerkType.Centering).Name("Centering II").Level(2), typeof(CenteringStatusEffect), 30f, 20, activator => Enmity.ModifyEnmityOnAll(activator, -500));


            return builder.Build();
        }
    }
}
