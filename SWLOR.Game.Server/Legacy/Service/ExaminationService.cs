using System.Linq;
using System.Text;
using SWLOR.Game.Server.Legacy.GameObject;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Legacy.Service
{
    public static class ExaminationService
    {
        public static bool OnModuleExamine(NWPlayer examiner, NWObject target)
        {
            var backupDescription = target.GetLocalString("BACKUP_DESCRIPTION");

            if (!string.IsNullOrWhiteSpace(backupDescription))
            {
                target.UnidentifiedDescription = backupDescription;
            }
            if (!examiner.IsDM || !target.IsPlayer || target.IsDM) return false;

            backupDescription = target.IdentifiedDescription;
            target.SetLocalString("BACKUP_DESCRIPTION", backupDescription);
            var playerEntity = DataService.Player.GetByID(target.GlobalID);
            var area = NWModule.Get().Areas.Single(x => GetResRef(x) == playerEntity.RespawnAreaResref);
            var respawnAreaName = GetName(area);

            var description =
                new StringBuilder(
                    ColorTokenService.Green("ID: ") + target.GlobalID + "\n" +
                    ColorTokenService.Green("Character Name: ") + target.Name + "\n" +
                    ColorTokenService.Green("Respawn Area: ") + respawnAreaName + "\n" +
                    ColorTokenService.Green("Skill Points: ") + playerEntity.TotalSPAcquired + " (Unallocated: " + playerEntity.UnallocatedSP + ")" + "\n" +
                    ColorTokenService.Green("FP: ") + playerEntity.CurrentFP + " / " + playerEntity.MaxFP + "\n" +
                    ColorTokenService.Green("Skill Levels: ") + "\n\n");

            var pcSkills = SkillService.GetAllPCSkills(target.Object);

            foreach (var pcSkill in pcSkills)
            {
                var skill = SkillService.GetSkill(pcSkill.SkillID);
                description.Append(skill.Name).Append(" rank ").Append(pcSkill.Rank).AppendLine();
            }

            description.Append("\n\n").Append(ColorTokenService.Green("Perks: ")).Append("\n\n");

            var pcPerks = DataService.PCPerk.GetAllByPlayerID(target.GlobalID);
            
            foreach (var pcPerk in pcPerks)
            {
                var perk = DataService.Perk.GetByID(pcPerk.PerkID);
                description.Append(perk.Name).Append(" Lvl. ").Append(pcPerk.PerkLevel).AppendLine();
            }
            
            description.Append("\n\n").Append(ColorTokenService.Green("Description: \n\n")).Append(backupDescription).AppendLine();
            target.UnidentifiedDescription = description.ToString();            
            DelayCommand(0.1f, () => { SetDescription(target, backupDescription, false); });
            return true;
        }

    }
}
