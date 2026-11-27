using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// Weapon classes can require valid equipment without coupling shared attacks to their implementation. The logic that
    /// used to be static in <c>Service.WeaponAttackEligibility</c> is moved here unchanged; the rule table is this
    /// instance's own. <see cref="IsAllowed(BaseItem, uint, uint, WeaponAttackPhase)"/> runs on every attack, so it
    /// allocates nothing and reads the table directly.
    /// </summary>
    public class WeaponAttackEligibilityService : IWeaponAttackEligibilityService
    {
        private readonly Dictionary<BaseItem, Func<uint, uint, WeaponAttackPhase, bool>> _rules = new();

        public void RegisterRule(BaseItem type, Func<uint, uint, WeaponAttackPhase, bool> rule)
        {
            ArgumentNullException.ThrowIfNull(rule);
            _rules[type] = rule;
        }

        public bool IsAllowed(uint creature, uint weapon, WeaponAttackPhase phase) =>
            !GetIsObjectValid(weapon) || IsAllowed(GetBaseItemType(weapon), creature, weapon, phase);

        public bool IsAllowed(BaseItem type, uint creature, uint weapon, WeaponAttackPhase phase) =>
            !_rules.TryGetValue(type, out var rule) || rule(creature, weapon, phase);
    }
}
