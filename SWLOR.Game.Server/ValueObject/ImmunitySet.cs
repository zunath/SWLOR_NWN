using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using static NWN._;

namespace SWLOR.Game.Server.ValueObject
{
    public class ImmunitySet: Dictionary<int, ImmunityDetails>
    {
        public ImmunitySet()
        {
            Add(IP_CONST_DAMAGETYPE_ACID, new ImmunityDetails("PENALTY_ORIGINAL_IMMUNITY_ACID"));
            Add((int)CustomItemPropertyDamageType.Ballistic, new ImmunityDetails("PENALTY_ORIGINAL_IMMUNITY_BALLISTIC"));
            Add(IP_CONST_DAMAGETYPE_BLUDGEONING, new ImmunityDetails("PENALTY_ORIGINAL_IMMUNITY_BLUDGEONING"));
            Add((int)CustomItemPropertyDamageType.Bullet, new ImmunityDetails("PENALTY_ORIGINAL_IMMUNITY_BULLET"));
            Add(IP_CONST_DAMAGETYPE_COLD, new ImmunityDetails("PENALTY_ORIGINAL_IMMUNITY_COLD"));
            Add(IP_CONST_DAMAGETYPE_DIVINE, new ImmunityDetails("PENALTY_ORIGINAL_IMMUNITY_DIVINE"));
            Add(IP_CONST_DAMAGETYPE_ELECTRICAL, new ImmunityDetails("PENALTY_ORIGINAL_IMMUNITY_ELECTRICAL"));
            Add((int)CustomItemPropertyDamageType.Energy, new ImmunityDetails("PENALTY_ORIGINAL_IMMUNITY_ENERGY"));
            Add(IP_CONST_DAMAGETYPE_FIRE, new ImmunityDetails("PENALTY_ORIGINAL_IMMUNITY_FIRE"));
            Add(IP_CONST_DAMAGETYPE_MAGICAL, new ImmunityDetails("PENALTY_ORIGINAL_IMMUNITY_MAGICAL"));
            Add(IP_CONST_DAMAGETYPE_NEGATIVE, new ImmunityDetails("PENALTY_ORIGINAL_IMMUNITY_NEGATIVE"));
            Add(IP_CONST_DAMAGETYPE_PIERCING, new ImmunityDetails("PENALTY_ORIGINAL_IMMUNITY_PIERCING"));
            Add(IP_CONST_DAMAGETYPE_POSITIVE, new ImmunityDetails("PENALTY_ORIGINAL_IMMUNITY_POSITIVE"));
            Add(IP_CONST_DAMAGETYPE_SLASHING, new ImmunityDetails("PENALTY_ORIGINAL_IMMUNITY_SLASHING"));
            Add(IP_CONST_DAMAGETYPE_SONIC, new ImmunityDetails("PENALTY_ORIGINAL_IMMUNITY_SONIC"));
        }
    }
}
