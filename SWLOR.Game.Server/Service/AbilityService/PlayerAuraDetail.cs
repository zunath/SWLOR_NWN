using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Service.AbilityService
{
    public class PlayerAuraDetail
    {
        public Type StatusEffect { get; set; }
        public bool TargetsSelf { get; set; }
        public bool TargetsParty { get; set; }
        public bool TargetsEnemies { get; set; }
        public VisualEffect SuccessfulImpactVisualEffect { get; }
        /// <summary>
        /// Creatures already told they received this aura. Allies crossing the aura edge are not told again;
        /// the list ends with the aura.
        /// </summary>
        public HashSet<uint> AnnouncedTo { get; } = new();

        public PlayerAuraDetail(Type statusEffect, bool targetsSelf, bool targetsParty, bool targetsEnemies, VisualEffect successfulImpactVisualEffect = VisualEffect.None)
        {
            StatusEffect = statusEffect;
            TargetsSelf = targetsSelf;
            TargetsParty = targetsParty;
            TargetsEnemies = targetsEnemies;
            SuccessfulImpactVisualEffect = successfulImpactVisualEffect;
        }
    }
}
