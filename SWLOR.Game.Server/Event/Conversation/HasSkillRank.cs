﻿using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject;
using _ = SWLOR.Game.Server.NWScript._;

namespace SWLOR.Game.Server.Event.Conversation
{
    public static class HasSkillRank
    {
        public static bool Check(int index, string method)
        {
            NWPlayer player = _.GetPCSpeaker();
            NWObject talkTo = NWGameObject.OBJECT_SELF;

            int count = 1;
            string varName = "SKILL_" + index + "_REQ_";
            var skillID = (Enumeration.Skill)talkTo.GetLocalInt(varName + count);
            bool displayNode = true;

            while (skillID > 0)
            {
                int requiredLevel = talkTo.GetLocalInt(varName + "LEVEL_" + count);
                bool meetsRequirement = SkillService.GetPCSkillRank(player, skillID) >= requiredLevel;

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
                skillID = (Enumeration.Skill)talkTo.GetLocalInt(varName + count);
            }

            return displayNode;
        
        }
    }
}
