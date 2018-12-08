using System.Collections.Generic;
using System.Linq;
using System.Text;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.GameObject;

using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Service
{
    public class ExaminationService: IExaminationService
    {
        private readonly IDataService _data;
        private readonly INWScript _;
        private readonly IColorTokenService _color;
        private readonly ISkillService _skill;

        public ExaminationService(
            IDataService data, 
            INWScript script, 
            IColorTokenService color,
            ISkillService skill)
        {
            _data = data;
            _ = script;
            _color = color;
            _skill = skill;
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
            Player playerEntity = _data.Single<Player>(x => x.ID == target.GlobalID);
            NWArea area = NWModule.Get().Areas.Single(x => x.Resref == playerEntity.RespawnAreaResref);
            string respawnAreaName = area.Name;

            StringBuilder description =
                new StringBuilder(
                    _color.Green("ID: ") + target.GlobalID + "\n" +
                    _color.Green("Character Name: ") + target.Name + "\n" +
                    _color.Green("Respawn Area: ") + respawnAreaName + "\n" +
                    _color.Green("Skill Points: ") + playerEntity.TotalSPAcquired + " (Unallocated: " + playerEntity.UnallocatedSP + ")" + "\n" +
                    _color.Green("FP: ") + playerEntity.CurrentFP + " / " + playerEntity.MaxFP + "\n" +
                    _color.Green("Skill Levels: ") + "\n\n");

            List<PCSkill> pcSkills = _skill.GetAllPCSkills(target.Object);

            foreach (PCSkill pcSkill in pcSkills)
            {
                Skill skill = _skill.GetSkill(pcSkill.SkillID);
                description.Append(skill.Name).Append(" rank ").Append(pcSkill.Rank).AppendLine();
            }

            description.Append("\n\n").Append(_color.Green("Perks: ")).Append("\n\n");
            
            var pcPerks = _data.Where<PCPerk>(x => x.PlayerID == target.GlobalID);
            
            foreach (PCPerk pcPerk in pcPerks)
            {
                var perk = _data.Get<Data.Entity.Perk>(pcPerk.PerkID);
                description.Append(perk.Name).Append(" Lvl. ").Append(pcPerk.PerkLevel).AppendLine();
            }
            
            description.Append("\n\n").Append(_color.Green("Description: \n\n")).Append(backupDescription).AppendLine();
            target.UnidentifiedDescription = description.ToString();

            return true;
        }

    }
}
