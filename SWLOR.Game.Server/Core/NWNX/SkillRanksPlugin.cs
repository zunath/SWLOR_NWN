using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum;

namespace SWLOR.Game.Server.Core.NWNX
{
    public static class SkillRanksPlugin
    {
        private const string PLUGIN_NAME = "NWNX_SkillRanks";

        public static int GetSkillFeatCountForSkill(FeatType feat)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetSkillFeatCountForSkill");
            NWNCore.NativeFunctions.nwnxPushInt((int)feat);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        public static SkillFeat GetSkillFeatForSkillByIndex(FeatType feat, NWNSkillType skill)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetSkillFeatForSkillByIndex");
            NWNCore.NativeFunctions.nwnxPushInt((int)feat);
            NWNCore.NativeFunctions.nwnxPushInt((int)skill);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return new SkillFeat
            {
                skill = (int)skill,
                feat = NWNCore.NativeFunctions.nwnxPopInt(),
                modifier = NWNCore.NativeFunctions.nwnxPopInt(),
                focusFeat = NWNCore.NativeFunctions.nwnxPopInt(),
                classes = NWNCore.NativeFunctions.nwnxPopString(),
                classLevelMod = NWNCore.NativeFunctions.nwnxPopFloat(),
                areaFlagsRequired = NWNCore.NativeFunctions.nwnxPopInt(),
                areaFlagsForbidden = NWNCore.NativeFunctions.nwnxPopInt(),
                dayOrNight = NWNCore.NativeFunctions.nwnxPopInt(),
                bypassArmorCheckPenalty = NWNCore.NativeFunctions.nwnxPopInt(),
                keyAbilityMask = NWNCore.NativeFunctions.nwnxPopInt()
            };
        }

        public static SkillFeat GetSkillFeat(FeatType feat, NWNSkillType skill)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetSkillFeat");
            NWNCore.NativeFunctions.nwnxPushInt((int)feat);
            NWNCore.NativeFunctions.nwnxPushInt((int)skill);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return new SkillFeat
            {
                skill = (int)skill,
                feat = (int)feat,
                modifier = NWNCore.NativeFunctions.nwnxPopInt(),
                focusFeat = NWNCore.NativeFunctions.nwnxPopInt(),
                classes = NWNCore.NativeFunctions.nwnxPopString(),
                classLevelMod = NWNCore.NativeFunctions.nwnxPopFloat(),
                areaFlagsRequired = NWNCore.NativeFunctions.nwnxPopInt(),
                areaFlagsForbidden = NWNCore.NativeFunctions.nwnxPopInt(),
                dayOrNight = NWNCore.NativeFunctions.nwnxPopInt(),
                bypassArmorCheckPenalty = NWNCore.NativeFunctions.nwnxPopInt(),
                keyAbilityMask = NWNCore.NativeFunctions.nwnxPopInt()
            };
        }

        public static void SetSkillFeat(SkillFeat skillFeat, bool createIfNonExistent = false)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetSkillFeat");
            NWNCore.NativeFunctions.nwnxPushInt(createIfNonExistent ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushInt(skillFeat.keyAbilityMask);
            NWNCore.NativeFunctions.nwnxPushInt(skillFeat.bypassArmorCheckPenalty);
            NWNCore.NativeFunctions.nwnxPushInt(skillFeat.dayOrNight);
            NWNCore.NativeFunctions.nwnxPushInt(skillFeat.areaFlagsForbidden);
            NWNCore.NativeFunctions.nwnxPushInt(skillFeat.areaFlagsRequired);
            NWNCore.NativeFunctions.nwnxPushFloat(skillFeat.classLevelMod);
            // We only need to send the string from the point of the first set bit
            NWNCore.NativeFunctions.nwnxPushString(
                skillFeat.classes!.Substring(skillFeat.classes!.Length - skillFeat.classes!.IndexOf("1"),
                    skillFeat.classes.Length));
            NWNCore.NativeFunctions.nwnxPushInt(skillFeat.focusFeat);
            NWNCore.NativeFunctions.nwnxPushInt(skillFeat.modifier);
            NWNCore.NativeFunctions.nwnxPushInt(skillFeat.feat);
            NWNCore.NativeFunctions.nwnxPushInt(skillFeat.skill);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static void SetSkillFeatFocusModifier(int modifier, bool epicFocus = false)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetSkillFeatFocusModifier");
            NWNCore.NativeFunctions.nwnxPushInt(epicFocus ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushInt(modifier);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static int GetBlindnessPenalty()
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetBlindnessPenalty");
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        public static void SetBlindnessPenalty(int modifier)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetBlindnessPenalty");
            NWNCore.NativeFunctions.nwnxPushInt(modifier);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }


        public static int GetAreaModifier(uint area, NWNSkillType skill)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetAreaModifier");
            NWNCore.NativeFunctions.nwnxPushInt((int)skill);
            NWNCore.NativeFunctions.nwnxPushObject(area);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        public static void SetAreaModifier(uint area, NWNSkillType skill, int modifier)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetAreaModifier");
            NWNCore.NativeFunctions.nwnxPushInt(modifier);
            NWNCore.NativeFunctions.nwnxPushInt((int)skill);
            NWNCore.NativeFunctions.nwnxPushObject(area);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }
    }
}