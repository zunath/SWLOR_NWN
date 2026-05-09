using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Vibroblade
{
    public class VibrobladeActiveAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureWeapon(builder.Create(FeatType.ShieldBash1, PerkType.ShieldBash).Name("Shield Bash I").Level(1), SkillType.Vibroblade, 12, 3, 12, SavingThrow.Will, typeof(DazedStatusEffect), 4);
            ConfigureWeapon(builder.Create(FeatType.ShieldBash2, PerkType.ShieldBash).Name("Shield Bash II").Level(2), SkillType.Vibroblade, 24, 6, 14, SavingThrow.Will, typeof(DazedStatusEffect), 6);
            ConfigureToggle(builder.Create(FeatType.DefensiveStance1, PerkType.DefensiveStance).Name("Defensive Stance I").Level(1), typeof(DefensiveStanceStatusEffect));
            ConfigurePartyStatus(builder.Create(FeatType.ShieldWall1, PerkType.ShieldWall).Name("Shield Wall").Level(1), typeof(ShieldWallStatusEffect), 60f, 15, true);
            ConfigureWeapon(builder.Create(FeatType.ShieldBash3, PerkType.ShieldBash).Name("Shield Bash III").Level(3), SkillType.Vibroblade, 36, 3, 16, SavingThrow.Will, typeof(StunnedStatusEffect), 8);
            ConfigureToggle(builder.Create(FeatType.DefensiveStance2, PerkType.DefensiveStance).Name("Defensive Stance II").Level(2), typeof(DefensiveStanceStatusEffect));
            ConfigureSelfStatus(builder.Create(FeatType.Invincible1, PerkType.Invincible).Name("Invincible").Level(1), typeof(InvincibleStatusEffect), 30f, 12);
            ConfigureWeapon(builder.Create(FeatType.HackingBlade1, PerkType.HackingBlade).Name("Hacking Blade I").Level(1), SkillType.Vibroblade, 8, 30, 10, SavingThrow.Fortitude, typeof(BleedStatusEffect), 4);
            ConfigureCastedTarget(builder.Create(FeatType.RiotBlade1, PerkType.RiotBlade).Name("Riot Blade I").Level(1), SkillType.Vibroblade, 15, 4);
            ConfigureToggle(builder.Create(FeatType.BerserkerStance1, PerkType.BerserkerStance).Name("Berserker Stance I").Level(1), typeof(BerserkerStanceStatusEffect));
            ConfigureCastedTarget(builder.Create(FeatType.RiotBlade2, PerkType.RiotBlade).Name("Riot Blade II").Level(2), SkillType.Vibroblade, 30, 6);
            ConfigureWeapon(builder.Create(FeatType.HackingBlade2, PerkType.HackingBlade).Name("Hacking Blade II").Level(2), SkillType.Vibroblade, 18, 60, 15, SavingThrow.Fortitude, typeof(BleedStatusEffect), 6);
            ConfigureCastedTarget(builder.Create(FeatType.RiotBlade3, PerkType.RiotBlade).Name("Riot Blade III").Level(3), SkillType.Vibroblade, 45, 8);
            ConfigureWeapon(builder.Create(FeatType.HackingBlade3, PerkType.HackingBlade).Name("Hacking Blade III").Level(3), SkillType.Vibroblade, 28, 60, 20, SavingThrow.Fortitude, typeof(BleedStatusEffect), 8);
            ConfigureToggle(builder.Create(FeatType.BerserkerStance2, PerkType.BerserkerStance).Name("Berserker Stance II").Level(2), typeof(BerserkerStanceStatusEffect));


            return builder.Build();
        }
    }
}
