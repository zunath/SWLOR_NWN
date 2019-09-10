using System;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;



namespace SWLOR.Game.Server.Service
{
    public static class PVPSanctuaryService
    {
        public static bool PlayerHasPVPSanctuary(NWPlayer player)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            if (player.Object == null) throw new ArgumentNullException(nameof(player.Object));

            Player pc = DataService.Player.GetByID(player.GlobalID);
            DateTime now = DateTime.UtcNow;

            return !pc.IsSanctuaryOverrideEnabled && now <= pc.DateSanctuaryEnds;
        }

        public static void SetPlayerPVPSanctuaryOverride(NWPlayer player, bool overrideStatus)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            if (player.Object == null) throw new ArgumentNullException(nameof(player.Object));

            Player pc = DataService.Player.GetByID(player.GlobalID);
            pc.IsSanctuaryOverrideEnabled = overrideStatus;
            DataService.SubmitDataChange(pc, DatabaseActionType.Update);
        }

        public static bool IsPVPAttackAllowed(NWPlayer attacker, NWPlayer target)
        {
            // Check for sanctuary if this attack is PC versus PC
            if (target.IsPlayer && attacker.IsPlayer)
            {
                // Either the attacker or target has sanctuary - prevent combat from happening
                if (PlayerHasPVPSanctuary(attacker))
                {
                    attacker.FloatingText(ColorTokenService.Red("You are under the effects of PVP sanctuary and cannot engage in PVP. To disable this feature permanently refer to the 'Disable PVP Sanctuary' option in your rest menu."));
                    attacker.DelayAssignCommand(() => attacker.ClearAllActions(), 0.0f);
                    
                    return false;
                }
                else if (PlayerHasPVPSanctuary(target))
                {
                    attacker.FloatingText(ColorTokenService.Red("Your target is under the effects of PVP sanctuary and cannot engage in PVP combat."));
                    attacker.DelayAssignCommand(() => attacker.ClearAllActions(), 0.0f);
                    return false;
                }
            }

            return true;
        }
    }
}
