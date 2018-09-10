using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.GameObject;

using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Service
{
    public class ExaminationService: IExaminationService
    {
        private readonly IDataContext _db;
        private readonly INWScript _;
        private readonly IColorTokenService _color;

        public ExaminationService(IDataContext db, INWScript script, IColorTokenService color)
        {
            _db = db;
            _ = script;
            _color = color;
        }

        public bool OnModuleExamine(NWPlayer examiner, NWObject target)
        {
            string backupDescription = target.GetLocalString("BACKUP_DESCRIPTION");

            if (!string.IsNullOrWhiteSpace(backupDescription))
            {
                target.UnidentifiedDescription = backupDescription;
            }

            if (!examiner.IsDM || !target.IsPlayer || target.IsDM) return false;

            backupDescription = target.IdentifiedDescription;
            target.SetLocalString("BACKUP_DESCRIPTION", backupDescription);
            
            PlayerCharacter playerEntity = _db.PlayerCharacters.Single(x => x.PlayerID == target.GlobalID);
            string respawnAreaName = _.GetName(_.GetObjectByTag(playerEntity.RespawnAreaTag));

            StringBuilder description =
                new StringBuilder(
                    _color.Green("ID: ") + target.GlobalID + "\n" +
                    _color.Green("Character Name: ") + target.Name + "\n" +
                    _color.Green("Respawn Area: ") + respawnAreaName + "\n" +
                    _color.Green("Skill Points: ") + playerEntity.TotalSPAcquired + " (Unallocated: " + playerEntity.UnallocatedSP + ")" + "\n" +
                    _color.Green("FP: ") + playerEntity.CurrentFP + " / " + playerEntity.MaxFP + "\n" +
                    _color.Green("Skill Levels: ") + "\n\n");

            List<PCSkill> pcSkills = _db.PCSkills.Where(x => x.PlayerID == target.GlobalID && x.Skill.IsActive).ToList();

            foreach (PCSkill skill in pcSkills)
            {
                description.Append(skill.Skill.Name).Append(" rank ").Append(skill.Rank).AppendLine();
            }

            description.Append("\n\n").Append(_color.Green("Perks: ")).Append("\n\n");

            List<PCPerkHeader> pcPerks = _db.StoredProcedure<PCPerkHeader>("GetPCPerksForMenuHeader",
                new SqlParameter("PlayerID", target.GlobalID));

            foreach (PCPerkHeader perk in pcPerks)
            {
                description.Append(perk.Name).Append(" Lvl. ").Append(perk.Level).AppendLine();
            }
            
            description.Append("\n\n").Append(_color.Green("Description: \n\n")).Append(backupDescription).AppendLine();
            target.UnidentifiedDescription = description.ToString();
            
            return true;
        }

    }
}
