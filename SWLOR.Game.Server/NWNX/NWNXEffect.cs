using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN;
using static SWLOR.Game.Server.NWNX.NWNXCore;

namespace SWLOR.Game.Server.NWNX
{
    public static class NWNXEffect
    {
        private const string PLUGIN_NAME = "NWNX_Effect";

        // Convert native effect type to unpacked structure
        public static EffectUnpacked UnpackEffect(Effect effect)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetCurrentNodeType");
            Internal.NativeFunctions.nwnxPushEffect(effect.Handle);
            Internal.NativeFunctions.nwnxCallFunction();

            var newEffect = new EffectUnpacked
            {
                Tag = Internal.NativeFunctions.nwnxPopString(),
                oParam3 = Internal.NativeFunctions.nwnxPopObject(),
                oParam2 = Internal.NativeFunctions.nwnxPopObject(),
                oParam1 = Internal.NativeFunctions.nwnxPopObject(),
                oParam0 = Internal.NativeFunctions.nwnxPopObject(),
                sParam5 = Internal.NativeFunctions.nwnxPopString(),
                sParam4 = Internal.NativeFunctions.nwnxPopString(),
                sParam3 = Internal.NativeFunctions.nwnxPopString(),
                sParam2 = Internal.NativeFunctions.nwnxPopString(),
                sParam1 = Internal.NativeFunctions.nwnxPopString(),
                sParam0 = Internal.NativeFunctions.nwnxPopString(),
                fParam3 = Internal.NativeFunctions.nwnxPopFloat(),
                fParam2 = Internal.NativeFunctions.nwnxPopFloat(),
                fParam1 = Internal.NativeFunctions.nwnxPopFloat(),
                fParam0 = Internal.NativeFunctions.nwnxPopFloat(),
                nParam7 = Internal.NativeFunctions.nwnxPopInt(),
                nParam6 = Internal.NativeFunctions.nwnxPopInt(),
                nParam5 = Internal.NativeFunctions.nwnxPopInt(),
                nParam4 = Internal.NativeFunctions.nwnxPopInt(),
                nParam3 = Internal.NativeFunctions.nwnxPopInt(),
                nParam2 = Internal.NativeFunctions.nwnxPopInt(),
                nParam1 = Internal.NativeFunctions.nwnxPopInt(),
                nParam0 = Internal.NativeFunctions.nwnxPopInt(),
                NumIntegers = Internal.NativeFunctions.nwnxPopInt(),
                LinkRightValid = Internal.NativeFunctions.nwnxPopInt(),
                LinkRight = new Effect(Internal.NativeFunctions.nwnxPopEffect()),
                LinkLeftValid = Internal.NativeFunctions.nwnxPopInt(),
                LinkLeft = new Effect(Internal.NativeFunctions.nwnxPopEffect()),
                CasterLevel = Internal.NativeFunctions.nwnxPopInt(),
                ShowIcon = Internal.NativeFunctions.nwnxPopInt(),
                Expose = Internal.NativeFunctions.nwnxPopInt(),
                SpellID = Internal.NativeFunctions.nwnxPopInt(),
                Creator = Internal.NativeFunctions.nwnxPopObject(),
                ExpiryTimeOfDay = Internal.NativeFunctions.nwnxPopInt(),
                ExpiryCalendarDay = Internal.NativeFunctions.nwnxPopInt(),
                Duration = Internal.NativeFunctions.nwnxPopFloat(),
                SubType = Internal.NativeFunctions.nwnxPopInt(),
                Type = Internal.NativeFunctions.nwnxPopInt(),
                EffectID = Internal.NativeFunctions.nwnxPopInt()
            };
            return newEffect;
        }

        // Convert unpacked effect structure to native type
        public static Effect PackEffect(EffectUnpacked effect)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "PackEffect");
            Internal.NativeFunctions.nwnxPushInt(effect.EffectID);
            Internal.NativeFunctions.nwnxPushInt(effect.Type);
            Internal.NativeFunctions.nwnxPushInt(effect.SubType);
            Internal.NativeFunctions.nwnxPushFloat(effect.Duration);
            Internal.NativeFunctions.nwnxPushInt(effect.ExpiryCalendarDay);
            Internal.NativeFunctions.nwnxPushInt(effect.ExpiryTimeOfDay);
            Internal.NativeFunctions.nwnxPushObject(effect.Creator ?? Internal.OBJECT_INVALID);
            Internal.NativeFunctions.nwnxPushInt(effect.SpellID);
            Internal.NativeFunctions.nwnxPushInt(effect.Expose);
            Internal.NativeFunctions.nwnxPushInt(effect.ShowIcon);
            Internal.NativeFunctions.nwnxPushInt(effect.CasterLevel);
            Internal.NativeFunctions.nwnxPushEffect(effect.LinkLeft!.Handle);
            Internal.NativeFunctions.nwnxPushInt(effect.LinkLeftValid);
            Internal.NativeFunctions.nwnxPushEffect(effect.LinkRight!.Handle);
            Internal.NativeFunctions.nwnxPushInt(effect.LinkRightValid);
            Internal.NativeFunctions.nwnxPushInt(effect.NumIntegers);
            Internal.NativeFunctions.nwnxPushInt(effect.nParam0);
            Internal.NativeFunctions.nwnxPushInt(effect.nParam1);
            Internal.NativeFunctions.nwnxPushInt(effect.nParam2);
            Internal.NativeFunctions.nwnxPushInt(effect.nParam3);
            Internal.NativeFunctions.nwnxPushInt(effect.nParam4);
            Internal.NativeFunctions.nwnxPushInt(effect.nParam5);
            Internal.NativeFunctions.nwnxPushInt(effect.nParam6);
            Internal.NativeFunctions.nwnxPushInt(effect.nParam7);
            Internal.NativeFunctions.nwnxPushFloat(effect.fParam0);
            Internal.NativeFunctions.nwnxPushFloat(effect.fParam1);
            Internal.NativeFunctions.nwnxPushFloat(effect.fParam2);
            Internal.NativeFunctions.nwnxPushFloat(effect.fParam3);
            Internal.NativeFunctions.nwnxPushString(effect.sParam0!);
            Internal.NativeFunctions.nwnxPushString(effect.sParam1!);
            Internal.NativeFunctions.nwnxPushString(effect.sParam2!);
            Internal.NativeFunctions.nwnxPushString(effect.sParam3!);
            Internal.NativeFunctions.nwnxPushString(effect.sParam4!);
            Internal.NativeFunctions.nwnxPushString(effect.sParam5!);
            Internal.NativeFunctions.nwnxPushObject(effect.oParam0 ?? Internal.OBJECT_INVALID);
            Internal.NativeFunctions.nwnxPushObject(effect.oParam1 ?? Internal.OBJECT_INVALID);
            Internal.NativeFunctions.nwnxPushObject(effect.oParam2 ?? Internal.OBJECT_INVALID);
            Internal.NativeFunctions.nwnxPushObject(effect.oParam3 ?? Internal.OBJECT_INVALID);
            Internal.NativeFunctions.nwnxPushString(effect.Tag!);
            Internal.NativeFunctions.nwnxCallFunction();
            return new Effect(Internal.NativeFunctions.nwnxPopEffect());
        }

        // Set a script with optional data that runs when an effect expires
        // Only works for TEMPORARY and PERMANENT effects applied to an object
        // Note: OBJECT_SELF in the script is the object the effect is applied to
        public static Effect SetEffectExpiredScript(Effect effect, string script, string data = "")
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetEffectExpiredScript");
            Internal.NativeFunctions.nwnxPushString(data);
            Internal.NativeFunctions.nwnxPushString(script);
            Internal.NativeFunctions.nwnxPushEffect(effect.Handle);
            Internal.NativeFunctions.nwnxCallFunction();
            return new Effect(Internal.NativeFunctions.nwnxPopEffect());
        }

        // Get the data set with NWNX_Effect_SetEffectExpiredScript()
        // THIS SHOULD ONLY BE CALLED FROM WITHIN A SCRIPT THAT WAS EXECUTED BY SetEffectExpiredScript()    
        public static string GetEffectExpiredData()
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetEffectExpiredData");
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopString();
        }

        // Get the effect creator of NWNX_Effect_SetEffectExpiredScript()
        // THIS SHOULD ONLY BE CALLED FROM WITHIN A SCRIPT THAT WAS EXECUTED BY NWNX_Effect_SetEffectExpiredScript()
        public static uint GetEffectExpiredCreator()
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetEffectExpiredCreator");
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopObject();
        }
    }
}
