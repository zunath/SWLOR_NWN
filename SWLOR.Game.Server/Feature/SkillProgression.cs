using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using Skill = SWLOR.Game.Server.Service.Skill;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature
{
    public static class SkillProgression
    {
        /// <summary>
        /// If a player is missing any skills in their DB record, they will be added here.
        /// </summary>
        [NWNEventHandler("mod_enter")]
        public static void AddMissingSkills()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            foreach (var skill in Skill.GetAllSkills())
            {
                if (!dbPlayer.Skills.ContainsKey(skill.Key))
                {
                    dbPlayer.Skills[skill.Key] = new PlayerSkill();
                }
            }

            DB.Set(playerId, dbPlayer);
        }
    }
}
