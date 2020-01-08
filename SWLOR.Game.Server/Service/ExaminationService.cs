using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.GameObject;


namespace SWLOR.Game.Server.Service
{
    public static class ExaminationService
    {
        public static bool OnModuleExamine(NWPlayer examiner, NWObject target)
        {
            string backupDescription = target.GetLocalString("BACKUP_DESCRIPTION");

            if (!string.IsNullOrWhiteSpace(backupDescription))
            {
                target.UnidentifiedDescription = backupDescription;
            }
            if (!examiner.IsDM || !target.IsPlayer || target.IsDM) return false;

            backupDescription = target.IdentifiedDescription;
            target.SetLocalString("BACKUP_DESCRIPTION", backupDescription);
            var dbPlayer = DataService.Player.GetByID(target.GlobalID);
            NWArea area = NWModule.Get().Areas.Single(x => x.Resref == dbPlayer.RespawnAreaResref);
            string respawnAreaName = area.Name;

            StringBuilder description =
                new StringBuilder(
                    ColorTokenService.Green("ID: ") + target.GlobalID + "\n" +
                    ColorTokenService.Green("Character Name: ") + target.Name + "\n" +
                    ColorTokenService.Green("Respawn Area: ") + respawnAreaName + "\n" +
                    ColorTokenService.Green("Skill Points: ") + dbPlayer.TotalSPAcquired + " (Unallocated: " + dbPlayer.UnallocatedSP + ")" + "\n" +
                    ColorTokenService.Green("FP: ") + dbPlayer.CurrentFP + " / " + dbPlayer.MaxFP + "\n" +
                    ColorTokenService.Green("Skill Levels: ") + "\n\n");

            foreach (var pcSkill in dbPlayer.Skills)
            {
                var skill = SkillService.GetSkill(pcSkill.Key);
                description.Append(skill.Name).Append(" rank ").Append(pcSkill.Value.Rank).AppendLine();
            }

            description.Append("\n\n").Append(ColorTokenService.Green("Perks: ")).Append("\n\n");

            foreach (var pcPerk in dbPlayer.Perks)
            {
                var perk = PerkService.GetPerkHandler(pcPerk.Key);
                description.Append(perk.Name).Append(" Lvl. ").Append(pcPerk.Value).AppendLine();
            }
            
            description.Append("\n\n").Append(ColorTokenService.Green("Description: \n\n")).Append(backupDescription).AppendLine();
            target.UnidentifiedDescription = description.ToString();

            return true;
        }

    }
}
