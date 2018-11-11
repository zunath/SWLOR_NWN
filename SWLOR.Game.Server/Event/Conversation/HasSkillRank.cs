using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Event.Conversation
{
    public class HasSkillRank : IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly ISkillService _skill;

        public HasSkillRank(
            INWScript script,
            ISkillService skill)
        {
            _ = script;
            _skill = skill;
        }
        public bool Run(params object[] args)
        {
            int index = (int) args[0];
            string method = (string) args[1];
            NWPlayer player = _.GetPCSpeaker();
            NWObject talkTo = Object.OBJECT_SELF;

            int count = 1;
            string varName = "SKILL_" + index + "_REQ_";
            int skillID = talkTo.GetLocalInt(varName + count);
            bool displayNode = true;

            while(skillID > 0)
            {
                int requiredLevel = talkTo.GetLocalInt(varName + "LEVEL_" + count);
                bool meetsRequirement = _skill.GetPCSkillRank(player, skillID) >= requiredLevel;

                // OR = Any one of the listed skills can be met and the node will appear.
                if (method == "OR")
                {
                    if (meetsRequirement) return true;
                    else displayNode = false;
                }
                // AND = ALL listed skills must be met in order for the node to appear.
                else
                {
                    if (!meetsRequirement) return false;
                }

                count++;
                skillID = talkTo.GetLocalInt(varName + count);
            }

            

            return displayNode;
        }
    }
}
