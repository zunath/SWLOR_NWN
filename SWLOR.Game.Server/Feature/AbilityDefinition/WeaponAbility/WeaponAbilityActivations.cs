using SWLOR.Game.Server.Core;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Composition;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.WeaponAbility
{
    /// <summary>
    /// The activation step of a weapon ability: the idle bonus snapshots, the queued opening attack and the toggled self status.
    /// </summary>
    public class WeaponAbilityActivations
    {
        private ICombatAbilityBonusService CombatAbilityBonus { get; }
        private ICombatActivityService CombatActivity { get; }
        private StatusAbilityEffects StatusAbilityEffects { get; }
        private WeaponAbilityDamageModifiers WeaponAbilityDamageModifiers { get; }

        public WeaponAbilityActivations(ICombatAbilityBonusService combatAbilityBonus, ICombatActivityService combatActivity, StatusAbilityEffects statusAbilityEffects, WeaponAbilityDamageModifiers weaponAbilityDamageModifiers)
        {
            CombatAbilityBonus = combatAbilityBonus;
            CombatActivity = combatActivity;
            StatusAbilityEffects = statusAbilityEffects;
            WeaponAbilityDamageModifiers = weaponAbilityDamageModifiers;
        }

        public bool ApplyActivation(
            WeaponAbilityProfile profile,
            uint activator,
            SkillType skill,
            bool isHostile,
            bool isFriendlyTarget,
            Type statusEffect)
        {
            WeaponAbilityDamageModifiers.PrepareActivationIdleBonuses(profile, activator);
            if (isHostile && !profile.IsQueuedWeaponAbility)
                CombatActivity.StoreAbilityActivationIdleBonuses(activator, skill);
            WeaponAbilityDamageModifiers.PrepareQueuedActivation(profile, activator, skill);
            if (profile.IsQueuedWeaponAbility)
                CombatAbilityBonus.PrepareQueuedWeaponAbilityOpeningAttackAtActivation(activator, skill);
            return isHostile || isFriendlyTarget || statusEffect == null || StatusAbilityEffects.ToggleSelfStatus(activator, statusEffect);
        }
    }
}
