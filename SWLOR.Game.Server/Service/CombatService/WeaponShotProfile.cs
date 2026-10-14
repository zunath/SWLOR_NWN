using System;
using System.Text;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Service.CombatService
{
    /// <summary>Immutable weapon values captured when a weapon action starts.</summary>
    public sealed record WeaponShotProfile(uint Weapon, SkillType Skill, int Damage,
        CombatDamageType DamageType, int Accuracy, int NativeAccuracy, AbilityType DamageAbility, string Properties)
    {
        public static WeaponShotProfile Capture(uint actor, uint weapon, SkillType skill,
            AbilityType damageAbilityOverride = AbilityType.Invalid)
        {
            var ability = damageAbilityOverride == AbilityType.Invalid
                ? Combat.GetWeaponDamageAbilityType(actor, GetBaseItemType(weapon)) : damageAbilityOverride;
            for (var property = GetFirstItemProperty(weapon); GetIsItemPropertyValid(property);
                 property = GetNextItemProperty(weapon))
                if (GetItemPropertyType(property) == ItemPropertyType.DamageStat)
                    ability = (AbilityType)GetItemPropertySubType(property);
            var accuracy = Stat.GetAccuracy(actor, weapon, AbilityType.Agility, skill);
            var powerAttackPenalty = GetActionMode(actor, ActionMode.PowerAttack) ? 5 :
                GetActionMode(actor, ActionMode.ImprovedPowerAttack) ? 10 : 0;
            return new WeaponShotProfile(weapon, skill, WeaponDamage.GetEffectiveDMG(actor, weapon),
                WeaponDamage.GetDamageType(weapon), accuracy, accuracy + powerAttackPenalty, ability, Fingerprint(weapon));
        }

        public bool IsUnchanged() => GetIsObjectValid(Weapon) && Properties == Fingerprint(Weapon);

        private static string Fingerprint(uint weapon)
        {
            var text = new StringBuilder().Append((int)GetBaseItemType(weapon));
            for (var property = GetFirstItemProperty(weapon); GetIsItemPropertyValid(property);
                 property = GetNextItemProperty(weapon))
                text.Append('|').Append((int)GetItemPropertyType(property)).Append(':')
                    .Append(GetItemPropertySubType(property)).Append(':')
                    .Append(GetItemPropertyCostTableValue(property));
            return text.ToString();
        }
    }
}
