
using SWLOR.Game.Server.Entity;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.AbilityService
{
    /// <summary>
    /// Adds a stamina requirement to activate a perk.
    /// </summary>
    public class AbilityStaminaRequirement : IAbilityActivationRequirement
    {
        private readonly int _requiredSTM;

        public AbilityStaminaRequirement(int requiredSTM)
        {
            _requiredSTM = requiredSTM;
        }

        public string CheckRequirements(uint player)
        {
            // DMs are assumed to be able to activate.
            if (GetIsDM(player)) return string.Empty;

            if (GetIsPC(player))
            {
                var playerId = GetObjectUUID(player);
                var dbPlayer = DB.Get<Player>(playerId);

                if (dbPlayer.Stamina >= _requiredSTM) return string.Empty;
                return $"Not enough stamina. (Required: {_requiredSTM})";
            }
            else
            {
                var stm = GetLocalInt(player, "STAMINA");
                if (stm >= _requiredSTM) return string.Empty;
                return $"Not enough stamina. (Required: {_requiredSTM})";
            }
        }

        public void AfterActivationAction(uint player)
        {
            if (GetIsDM(player)) return;

            if (GetIsPC(player))
            {
                var playerId = GetObjectUUID(player);
                var dbPlayer = DB.Get<Player>(playerId);
                Stat.ReduceStamina(dbPlayer, _requiredSTM);

                DB.Set(playerId, dbPlayer);
            }
            else
            {
                var stm = GetLocalInt(player, "STAMINA");
                stm -= _requiredSTM;
                if (stm < 0)
                    stm = 0;

                SetLocalInt(player, "STAMINA", stm);
            }
        }
    }
}