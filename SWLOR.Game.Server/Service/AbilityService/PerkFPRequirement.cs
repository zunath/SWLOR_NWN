using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.AbilityService
{
    /// <summary>
    /// Adds an FP requirement to activate a perk.
    /// </summary>
    public class PerkFPRequirement : IAbilityActivationRequirement
    {
        private readonly int _requiredFP;

        public PerkFPRequirement(int requiredFP)
        {
            _requiredFP = requiredFP;
        }

        public string CheckRequirements(uint player)
        {
            // DMs are assumed to be able to activate.
            if (GetIsDM(player)) return string.Empty;

            if (GetIsPC(player))
            {
                // Force Attunement reduces FP costs to zero.
                if (StatusEffect.HasStatusEffect(player, StatusEffectType.ForceAttunement)) return string.Empty;

                var playerId = GetObjectUUID(player);
                var dbPlayer = DB.Get<Player>(playerId);

                if (dbPlayer.FP >= _requiredFP) return string.Empty;

                return $"Not enough FP. (Required: {_requiredFP})";
            }
            else
            {
                var fp = GetLocalInt(player, "FP");
                if (fp >= _requiredFP) return string.Empty;
                return $"Not enough FP. (Required: {_requiredFP})";
            }
        }

        public void AfterActivationAction(uint player)
        {
            if (GetIsDM(player)) return;

            // Force Attunement reduces FP costs to zero.
            if (StatusEffect.HasStatusEffect(player, StatusEffectType.ForceAttunement)) return;

            if (GetIsPC(player))
            {
                var playerId = GetObjectUUID(player);
                var dbPlayer = DB.Get<Player>(playerId);
                Stat.ReduceMP(dbPlayer, _requiredFP);

                DB.Set(playerId, dbPlayer);
            }
            else
            {
                var fp = GetLocalInt(player, "FP");
                fp -= _requiredFP;
                if (fp < 0)
                    fp = 0;

                SetLocalInt(player, "FP", fp);
            }
        }
    }
}
