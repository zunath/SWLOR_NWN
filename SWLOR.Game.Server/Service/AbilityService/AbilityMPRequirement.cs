using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Service.AbilityService
{
    /// <summary>
    /// Adds an MP requirement to activate a perk.
    /// </summary>
    public class PerkMPRequirement : IAbilityActivationRequirement
    {
        private readonly int _requiredMP;

        public PerkMPRequirement(int requiredMP)
        {
            _requiredMP = requiredMP;
        }

        public string CheckRequirements(uint player)
        {
            // DMs are assumed to be able to activate.
            if (GetIsDM(player)) return string.Empty;

            if (GetIsPC(player))
            {
                // Manafont reduces MP costs to zero.
                if (StatusEffect.HasStatusEffect(player, StatusEffectType.Manafont)) return string.Empty;

                var playerId = GetObjectUUID(player);
                var dbPlayer = DB.Get<Player>(playerId);

                if (dbPlayer.MP >= _requiredMP) return string.Empty;

                return $"Not enough MP. (Required: {_requiredMP})";
            }
            else
            {
                var mp = GetLocalInt(player, "MP");
                if (mp >= _requiredMP) return string.Empty;
                return $"Not enough MP. (Required: {_requiredMP})";
            }
        }

        public void AfterActivationAction(uint player)
        {
            if (GetIsDM(player)) return;

            // Manafont reduces MP costs to zero.
            if (StatusEffect.HasStatusEffect(player, StatusEffectType.Manafont)) return;

            if (GetIsPC(player))
            {
                var playerId = GetObjectUUID(player);
                var dbPlayer = DB.Get<Player>(playerId);
                Stat.ReduceMP(dbPlayer, _requiredMP);

                DB.Set(playerId, dbPlayer);
            }
            else
            {
                var mp = GetLocalInt(player, "MP");
                mp -= _requiredMP;
                if (mp < 0)
                    mp = 0;

                SetLocalInt(player, "MP", mp);
            }
        }
    }
}
