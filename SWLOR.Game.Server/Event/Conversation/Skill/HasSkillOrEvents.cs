using System;
using System.Collections.Generic;
using System.Text;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Legacy;
using Profiler = SWLOR.Game.Server.ValueObject.Profiler;

namespace SWLOR.Game.Server.Event.Conversation.Skill
{
    public static class HasSkillOrEvents
    {
        [NWNEventHandler("has_skill_or_1")]
        public static int HasSkillOr1()
        {
            return Check(1, "OR") ? 1 : 0;
        }
        [NWNEventHandler("has_skill_or_2")]
        public static int HasSkillOr2()
        {
            return Check(2, "OR") ? 1 : 0;
        }
        [NWNEventHandler("has_skill_or_3")]
        public static int HasSkillOr3()
        {
            return Check(3, "OR") ? 1 : 0;
        }
        [NWNEventHandler("has_skill_or_4")]
        public static int HasSkillOr4()
        {
            return Check(4, "OR") ? 1 : 0;
        }
        [NWNEventHandler("has_skill_or_5")]
        public static int HasSkillOr5()
        {
            return Check(5, "OR") ? 1 : 0;
        }
        [NWNEventHandler("has_skill_or_6")]
        public static int HasSkillOr6()
        {
            return Check(6, "OR") ? 1 : 0;
        }
        [NWNEventHandler("has_skill_or_7")]
        public static int HasSkillOr7()
        {
            return Check(7, "OR") ? 1 : 0;
        }
        [NWNEventHandler("has_skill_or_8")]
        public static int HasSkillOr8()
        {
            return Check(8, "OR") ? 1 : 0;
        }
        [NWNEventHandler("has_skill_or_9")]
        public static int HasSkillOr9()
        {
            return Check(9, "OR") ? 1 : 0;
        }

        private static bool Check(int index, string method)
        {
            NWPlayer player = NWScript.GetPCSpeaker();
            NWObject talkTo = NWScript.OBJECT_SELF;

            var count = 1;
            var varName = "SKILL_" + index + "_REQ_";
            var skillID = talkTo.GetLocalInt(varName + count);
            var displayNode = true;

            while (skillID > 0)
            {
                var requiredLevel = talkTo.GetLocalInt(varName + "LEVEL_" + count);
                var meetsRequirement = SkillService.GetPCSkillRank(player, skillID) >= requiredLevel;

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
