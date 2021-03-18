using System.Numerics;
using SWLOR.Game.Server.Core.NWNX.Enum;

namespace SWLOR.Game.Server.Core.NWNX
{
    public static class Effect
    {
        private const string PLUGIN_NAME = "NWNX_Effect";

        private static EffectUnpacked ResolveUnpack(bool bLink = true)
        {

            var newEffect = new EffectUnpacked();

            newEffect.sItemProp = Internal.NativeFunctions.nwnxPopString();
            newEffect.Tag = Internal.NativeFunctions.nwnxPopString();

            newEffect.vParam1 = new Vector3
            {
                Z = Internal.NativeFunctions.nwnxPopFloat(),
                Y = Internal.NativeFunctions.nwnxPopFloat(),
                X = Internal.NativeFunctions.nwnxPopFloat(),
            };
            newEffect.vParam0 = new Vector3
            {
                Z = Internal.NativeFunctions.nwnxPopFloat(),
                Y = Internal.NativeFunctions.nwnxPopFloat(),
                X = Internal.NativeFunctions.nwnxPopFloat(),
            };

            newEffect.oParam3 = Internal.NativeFunctions.nwnxPopObject();
            newEffect.oParam2 = Internal.NativeFunctions.nwnxPopObject();
            newEffect.oParam1 = Internal.NativeFunctions.nwnxPopObject();
            newEffect.oParam0 = Internal.NativeFunctions.nwnxPopObject();

            newEffect.sParam5 = Internal.NativeFunctions.nwnxPopString();
            newEffect.sParam4 = Internal.NativeFunctions.nwnxPopString();
            newEffect.sParam3 = Internal.NativeFunctions.nwnxPopString();
            newEffect.sParam2 = Internal.NativeFunctions.nwnxPopString();
            newEffect.sParam1 = Internal.NativeFunctions.nwnxPopString();
            newEffect.sParam0 = Internal.NativeFunctions.nwnxPopString();

            newEffect.fParam3 = Internal.NativeFunctions.nwnxPopFloat();
            newEffect.fParam2 = Internal.NativeFunctions.nwnxPopFloat();
            newEffect.fParam1 = Internal.NativeFunctions.nwnxPopFloat();
            newEffect.fParam0 = Internal.NativeFunctions.nwnxPopFloat();

            newEffect.nParam7 = Internal.NativeFunctions.nwnxPopInt();
            newEffect.nParam6 = Internal.NativeFunctions.nwnxPopInt();
            newEffect.nParam5 = Internal.NativeFunctions.nwnxPopInt();
            newEffect.nParam4 = Internal.NativeFunctions.nwnxPopInt();
            newEffect.nParam3 = Internal.NativeFunctions.nwnxPopInt();
            newEffect.nParam2 = Internal.NativeFunctions.nwnxPopInt();
            newEffect.nParam1 = Internal.NativeFunctions.nwnxPopInt();
            newEffect.nParam0 = Internal.NativeFunctions.nwnxPopInt();

            newEffect.NumIntegers = Internal.NativeFunctions.nwnxPopInt();

            if (bLink)
            {
                newEffect.LinkRightValid = Internal.NativeFunctions.nwnxPopInt() == 1;
                newEffect.LinkRight = new Core.Effect(Internal.NativeFunctions.nwnxPopEffect());
                newEffect.LinkLeftValid = Internal.NativeFunctions.nwnxPopInt() == 1;
                newEffect.LinkLeft = new Core.Effect(Internal.NativeFunctions.nwnxPopEffect());
            }
            else
            {
                newEffect.LinkRightValid = false;
                newEffect.LinkLeftValid = false;
            }

            newEffect.CasterLevel = Internal.NativeFunctions.nwnxPopInt();
            newEffect.ShowIcon = Internal.NativeFunctions.nwnxPopInt();
            newEffect.Expose = Internal.NativeFunctions.nwnxPopInt();
            newEffect.SpellId = Internal.NativeFunctions.nwnxPopInt();
            newEffect.Creator = Internal.NativeFunctions.nwnxPopObject();

            newEffect.ExpiryTimeOfDay = Internal.NativeFunctions.nwnxPopInt();
            newEffect.ExpiryCalendarDay = Internal.NativeFunctions.nwnxPopInt();
            newEffect.Duration = Internal.NativeFunctions.nwnxPopFloat();

            newEffect.SubType = Internal.NativeFunctions.nwnxPopInt();
            newEffect.Type = Internal.NativeFunctions.nwnxPopInt();
            newEffect.EffectId = Internal.NativeFunctions.nwnxPopInt();

            return newEffect;
        }

        private static void ResolvePack(EffectUnpacked e, bool bReplace = false)
        {
            if (!bReplace)
                Internal.NativeFunctions.nwnxPushInt(e.Type);

            Internal.NativeFunctions.nwnxPushInt(e.SubType);

            Internal.NativeFunctions.nwnxPushFloat(e.Duration);
            Internal.NativeFunctions.nwnxPushInt(e.ExpiryCalendarDay);
            Internal.NativeFunctions.nwnxPushInt(e.ExpiryTimeOfDay);

            Internal.NativeFunctions.nwnxPushObject(e.Creator);
            Internal.NativeFunctions.nwnxPushInt(e.SpellId);
            Internal.NativeFunctions.nwnxPushInt(e.Expose);
            Internal.NativeFunctions.nwnxPushInt(e.ShowIcon);
            Internal.NativeFunctions.nwnxPushInt(e.CasterLevel);

            if (!bReplace)
            {
                Internal.NativeFunctions.nwnxPushEffect(e.LinkLeft);
                Internal.NativeFunctions.nwnxPushInt(e.LinkLeftValid ? 1 : 0);
                Internal.NativeFunctions.nwnxPushEffect(e.LinkRight);
                Internal.NativeFunctions.nwnxPushInt(e.LinkRightValid ? 1 : 0);
            }

            Internal.NativeFunctions.nwnxPushInt(e.NumIntegers);
            Internal.NativeFunctions.nwnxPushInt(e.nParam0);
            Internal.NativeFunctions.nwnxPushInt(e.nParam1);
            Internal.NativeFunctions.nwnxPushInt(e.nParam2);
            Internal.NativeFunctions.nwnxPushInt(e.nParam3);
            Internal.NativeFunctions.nwnxPushInt(e.nParam4);
            Internal.NativeFunctions.nwnxPushInt(e.nParam5);
            Internal.NativeFunctions.nwnxPushInt(e.nParam6);
            Internal.NativeFunctions.nwnxPushInt(e.nParam7);
            Internal.NativeFunctions.nwnxPushFloat(e.fParam0);
            Internal.NativeFunctions.nwnxPushFloat(e.fParam1);
            Internal.NativeFunctions.nwnxPushFloat(e.fParam2);
            Internal.NativeFunctions.nwnxPushFloat(e.fParam3);
            Internal.NativeFunctions.nwnxPushString(e.sParam0);
            Internal.NativeFunctions.nwnxPushString(e.sParam1);
            Internal.NativeFunctions.nwnxPushString(e.sParam2);
            Internal.NativeFunctions.nwnxPushString(e.sParam3);
            Internal.NativeFunctions.nwnxPushString(e.sParam4);
            Internal.NativeFunctions.nwnxPushString(e.sParam5);
            Internal.NativeFunctions.nwnxPushObject(e.oParam0);
            Internal.NativeFunctions.nwnxPushObject(e.oParam1);
            Internal.NativeFunctions.nwnxPushObject(e.oParam2);
            Internal.NativeFunctions.nwnxPushObject(e.oParam3);

            Internal.NativeFunctions.nwnxPushFloat(e.vParam0.X);
            Internal.NativeFunctions.nwnxPushFloat(e.vParam0.Y);
            Internal.NativeFunctions.nwnxPushFloat(e.vParam0.Z);

            Internal.NativeFunctions.nwnxPushFloat(e.vParam1.X);
            Internal.NativeFunctions.nwnxPushFloat(e.vParam1.Y);
            Internal.NativeFunctions.nwnxPushFloat(e.vParam1.Z);

            Internal.NativeFunctions.nwnxPushString(e.Tag);

            Internal.NativeFunctions.nwnxPushString(e.sItemProp);
        }

        // Convert native effect type to unpacked structure
        public static EffectUnpacked UnpackEffect(Core.Effect effect)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "UnpackEffect");
            Internal.NativeFunctions.nwnxPushEffect(effect.Handle);
            Internal.NativeFunctions.nwnxCallFunction();

            return ResolveUnpack();
        }

        // Convert unpacked effect structure to native type
        public static Core.Effect PackEffect(EffectUnpacked effect)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "PackEffect");
            ResolvePack(effect);

            return new Core.Effect(Internal.NativeFunctions.nwnxPopEffect());
        }

        // Set a script with optional data that runs when an effect expires
        // Only works for TEMPORARY and PERMANENT effects applied to an object
        // Note: OBJECT_SELF in the script is the object the effect is applied to
        public static Core.Effect SetEffectExpiredScript(Core.Effect effect, string script, string data = "")
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetEffectExpiredScript");
            Internal.NativeFunctions.nwnxPushString(data);
            Internal.NativeFunctions.nwnxPushString(script);
            Internal.NativeFunctions.nwnxPushEffect(effect.Handle);
            Internal.NativeFunctions.nwnxCallFunction();
            return new Core.Effect(Internal.NativeFunctions.nwnxPopEffect());
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


        /// @brief replace an already applied effect on an object
        /// Only duration, subtype, tag and spell related fields can be overwritten.
        /// @note eNew and eOld need to have the same type.
        /// @return Number of internal effects updated.
        public static int ReplaceEffect(uint obj, Core.Effect eOld, Core.Effect eNew)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "ReplaceEffect");
            Internal.NativeFunctions.nwnxPushEffect(eNew);
            Internal.NativeFunctions.nwnxPushEffect(eOld);
            Internal.NativeFunctions.nwnxPushObject(obj);
            Internal.NativeFunctions.nwnxCallFunction();

            return Internal.NativeFunctions.nwnxPopInt();
        }

        public static int GetTrueEffectCount(uint oObject)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetTrueEffectCount");
            Internal.NativeFunctions.nwnxPushObject(oObject);
            Internal.NativeFunctions.nwnxCallFunction();

            return Internal.NativeFunctions.nwnxPopInt();
        }

        public static EffectUnpacked GetTrueEffect(uint oObject, int nIndex)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetTrueEffectCount");
            Internal.NativeFunctions.nwnxPushInt(nIndex);
            Internal.NativeFunctions.nwnxPushObject(oObject);
            Internal.NativeFunctions.nwnxCallFunction();

            return ResolveUnpack(false);
        }

        public static void ReplaceEffectByIndex(uint oObject, int nIndex, EffectUnpacked e)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "ReplaceEffectByIndex");
            
            ResolvePack(e, true);
            Internal.NativeFunctions.nwnxPushInt(nIndex);
            Internal.NativeFunctions.nwnxPushObject(oObject);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static int RemoveEffectById(uint oObject, string sID)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "RemoveEffectById");
            
            Internal.NativeFunctions.nwnxPushString(sID);
            Internal.NativeFunctions.nwnxPushObject(oObject);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }
}
}