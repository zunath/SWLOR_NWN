using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.GameObject;

using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Service
{
    public class PVPSanctuaryService: IPVPSanctuaryService
    {
        private readonly IDataContext _db;
        private readonly INWScript _;
        private readonly IColorTokenService _color;

        public PVPSanctuaryService(IDataContext db, INWScript script, IColorTokenService color)
        {
            _db = db;
            _ = script;
            _color = color;
        }

        public bool PlayerHasPVPSanctuary(NWPlayer player)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            if (player.Object == null) throw new ArgumentNullException(nameof(player.Object));

            PlayerCharacter pc = _db.PlayerCharacters.Single(x => x.PlayerID == player.GlobalID);
            DateTime now = DateTime.UtcNow;

            return !pc.IsSanctuaryOverrideEnabled && now <= pc.DateSanctuaryEnds;
        }

        public void SetPlayerPVPSanctuaryOverride(NWPlayer player, bool overrideStatus)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            if (player.Object == null) throw new ArgumentNullException(nameof(player.Object));

            PlayerCharacter pc = _db.PlayerCharacters.Single(x => x.PlayerID == player.GlobalID);
            pc.IsSanctuaryOverrideEnabled = overrideStatus;
            _db.SaveChanges();
        }

        public bool IsPVPAttackAllowed(NWPlayer attacker, NWPlayer target)
        {
            // Check for sanctuary if this attack is PC versus PC
            if (target.IsPlayer && attacker.IsPlayer)
            {
                // Either the attacker or target has sanctuary - prevent combat from happening
                if (PlayerHasPVPSanctuary(attacker))
                {
                    attacker.FloatingText(_color.Red("You are under the effects of PVP sanctuary and cannot engage in PVP. To disable this feature permanently refer to the 'Disable PVP Sanctuary' option in your rest menu."));
                    attacker.DelayAssignCommand(() => attacker.ClearAllActions(), 0.0f);
                    
                    return false;
                }
                else if (PlayerHasPVPSanctuary(target))
                {
                    attacker.FloatingText(_color.Red("Your target is under the effects of PVP sanctuary and cannot engage in PVP combat."));
                    attacker.DelayAssignCommand(() => attacker.ClearAllActions(), 0.0f);
                    return false;
                }
            }

            return true;
        }
    }
}
