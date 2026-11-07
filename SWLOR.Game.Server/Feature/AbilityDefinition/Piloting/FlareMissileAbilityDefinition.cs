using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Piloting
{
    /// <summary>
    /// Flare Missile I-III: one launcher shot at the target that blinds it. Ported from the Throwing Flash Toss
    /// (damage, STM, cooldown and Blind duration are the ground's).
    /// </summary>
    public class FlareMissileAbilityDefinition : IAbilityListDefinition
    {
        private const float BlindSeconds = 15f;

        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Rank(_builder.Create(FeatType.OrdnanceFlareMissile1, PerkType.OrdnanceFlareMissile).Name("Flare Missile I").Level(1), 12, 4);
            Rank(_builder.Create(FeatType.OrdnanceFlareMissile2, PerkType.OrdnanceFlareMissile).Name("Flare Missile II").Level(2), 24, 8);
            Rank(_builder.Create(FeatType.OrdnanceFlareMissile3, PerkType.OrdnanceFlareMissile).Name("Flare Missile III").Level(3), 36, 10);

            return _builder.Build();
        }

        private static void Rank(AbilityBuilder ability, int bonusDmg, int stamina)
        {
            PilotingAbilityConfiguration.ConfigureTargetedShot(
                ability.HasRecastDelay(RecastGroup.OrdnanceFlareMissile, 24f),
                stamina,
                SpaceWeaponFilter.Launcher,
                (activator, target, _, _) =>
                {
                    var result = SpaceAbilities.FireWeapon(activator, target, SpaceWeaponFilter.Launcher, bonusDmg);
                    if (!result.Fired)
                    {
                        PilotingAbilityConfiguration.ReportFailure(activator, new[] { result });
                        return;
                    }

                    if (result.IsHit)
                        Space.ApplyStatusToShip(activator, target, typeof(BlindStatusEffect), BlindSeconds);
                });
        }
    }
}
