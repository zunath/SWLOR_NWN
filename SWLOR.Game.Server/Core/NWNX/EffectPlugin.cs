using System.Numerics;
using SWLOR.Game.Server.Core.NWNX.Enum;

namespace SWLOR.Game.Server.Core.NWNX
{
    public static class EffectPlugin
    {
        private const string PLUGIN_NAME = "NWNX_Effect";

        private static EffectUnpacked ResolveUnpack(bool bLink = true)
        {

            var newEffect = new EffectUnpacked();

            newEffect.sItemProp = NWNCore.NativeFunctions.nwnxPopString();
            newEffect.Tag = NWNCore.NativeFunctions.nwnxPopString();

            newEffect.vParam1 = new Vector3
            {
                Z = NWNCore.NativeFunctions.nwnxPopFloat(),
                Y = NWNCore.NativeFunctions.nwnxPopFloat(),
                X = NWNCore.NativeFunctions.nwnxPopFloat(),
            };
            newEffect.vParam0 = new Vector3
            {
                Z = NWNCore.NativeFunctions.nwnxPopFloat(),
                Y = NWNCore.NativeFunctions.nwnxPopFloat(),
                X = NWNCore.NativeFunctions.nwnxPopFloat(),
            };

            newEffect.oParam3 = NWNCore.NativeFunctions.nwnxPopObject();
            newEffect.oParam2 = NWNCore.NativeFunctions.nwnxPopObject();
            newEffect.oParam1 = NWNCore.NativeFunctions.nwnxPopObject();
            newEffect.oParam0 = NWNCore.NativeFunctions.nwnxPopObject();

            newEffect.sParam5 = NWNCore.NativeFunctions.nwnxPopString();
            newEffect.sParam4 = NWNCore.NativeFunctions.nwnxPopString();
            newEffect.sParam3 = NWNCore.NativeFunctions.nwnxPopString();
            newEffect.sParam2 = NWNCore.NativeFunctions.nwnxPopString();
            newEffect.sParam1 = NWNCore.NativeFunctions.nwnxPopString();
            newEffect.sParam0 = NWNCore.NativeFunctions.nwnxPopString();

            newEffect.fParam3 = NWNCore.NativeFunctions.nwnxPopFloat();
            newEffect.fParam2 = NWNCore.NativeFunctions.nwnxPopFloat();
            newEffect.fParam1 = NWNCore.NativeFunctions.nwnxPopFloat();
            newEffect.fParam0 = NWNCore.NativeFunctions.nwnxPopFloat();

            newEffect.nParam7 = NWNCore.NativeFunctions.nwnxPopInt();
            newEffect.nParam6 = NWNCore.NativeFunctions.nwnxPopInt();
            newEffect.nParam5 = NWNCore.NativeFunctions.nwnxPopInt();
            newEffect.nParam4 = NWNCore.NativeFunctions.nwnxPopInt();
            newEffect.nParam3 = NWNCore.NativeFunctions.nwnxPopInt();
            newEffect.nParam2 = NWNCore.NativeFunctions.nwnxPopInt();
            newEffect.nParam1 = NWNCore.NativeFunctions.nwnxPopInt();
            newEffect.nParam0 = NWNCore.NativeFunctions.nwnxPopInt();

            newEffect.NumIntegers = NWNCore.NativeFunctions.nwnxPopInt();

            if (bLink)
            {
                newEffect.LinkRightValid = NWNCore.NativeFunctions.nwnxPopInt() == 1;
                newEffect.LinkRight = new Core.Effect(NWNCore.NativeFunctions.nwnxPopEffect());
                newEffect.LinkLeftValid = NWNCore.NativeFunctions.nwnxPopInt() == 1;
                newEffect.LinkLeft = new Core.Effect(NWNCore.NativeFunctions.nwnxPopEffect());
            }
            else
            {
                newEffect.LinkRightValid = false;
                newEffect.LinkLeftValid = false;
            }

            newEffect.CasterLevel = NWNCore.NativeFunctions.nwnxPopInt();
            newEffect.ShowIcon = NWNCore.NativeFunctions.nwnxPopInt();
            newEffect.Expose = NWNCore.NativeFunctions.nwnxPopInt();
            newEffect.SpellId = NWNCore.NativeFunctions.nwnxPopInt();
            newEffect.Creator = NWNCore.NativeFunctions.nwnxPopObject();

            newEffect.ExpiryTimeOfDay = NWNCore.NativeFunctions.nwnxPopInt();
            newEffect.ExpiryCalendarDay = NWNCore.NativeFunctions.nwnxPopInt();
            newEffect.Duration = NWNCore.NativeFunctions.nwnxPopFloat();

            newEffect.SubType = NWNCore.NativeFunctions.nwnxPopInt();
            newEffect.Type = NWNCore.NativeFunctions.nwnxPopInt();
            newEffect.EffectId = NWNCore.NativeFunctions.nwnxPopInt();

            return newEffect;
        }

        private static void ResolvePack(EffectUnpacked e, bool bReplace = false)
        {
            if (!bReplace)
                NWNCore.NativeFunctions.nwnxPushInt(e.Type);

            NWNCore.NativeFunctions.nwnxPushInt(e.SubType);

            NWNCore.NativeFunctions.nwnxPushFloat(e.Duration);
            NWNCore.NativeFunctions.nwnxPushInt(e.ExpiryCalendarDay);
            NWNCore.NativeFunctions.nwnxPushInt(e.ExpiryTimeOfDay);

            NWNCore.NativeFunctions.nwnxPushObject(e.Creator);
            NWNCore.NativeFunctions.nwnxPushInt(e.SpellId);
            NWNCore.NativeFunctions.nwnxPushInt(e.Expose);
            NWNCore.NativeFunctions.nwnxPushInt(e.ShowIcon);
            NWNCore.NativeFunctions.nwnxPushInt(e.CasterLevel);

            if (!bReplace)
            {
                NWNCore.NativeFunctions.nwnxPushEffect(e.LinkLeft);
                NWNCore.NativeFunctions.nwnxPushInt(e.LinkLeftValid ? 1 : 0);
                NWNCore.NativeFunctions.nwnxPushEffect(e.LinkRight);
                NWNCore.NativeFunctions.nwnxPushInt(e.LinkRightValid ? 1 : 0);
            }

            NWNCore.NativeFunctions.nwnxPushInt(e.NumIntegers);
            NWNCore.NativeFunctions.nwnxPushInt(e.nParam0);
            NWNCore.NativeFunctions.nwnxPushInt(e.nParam1);
            NWNCore.NativeFunctions.nwnxPushInt(e.nParam2);
            NWNCore.NativeFunctions.nwnxPushInt(e.nParam3);
            NWNCore.NativeFunctions.nwnxPushInt(e.nParam4);
            NWNCore.NativeFunctions.nwnxPushInt(e.nParam5);
            NWNCore.NativeFunctions.nwnxPushInt(e.nParam6);
            NWNCore.NativeFunctions.nwnxPushInt(e.nParam7);
            NWNCore.NativeFunctions.nwnxPushFloat(e.fParam0);
            NWNCore.NativeFunctions.nwnxPushFloat(e.fParam1);
            NWNCore.NativeFunctions.nwnxPushFloat(e.fParam2);
            NWNCore.NativeFunctions.nwnxPushFloat(e.fParam3);
            NWNCore.NativeFunctions.nwnxPushString(e.sParam0);
            NWNCore.NativeFunctions.nwnxPushString(e.sParam1);
            NWNCore.NativeFunctions.nwnxPushString(e.sParam2);
            NWNCore.NativeFunctions.nwnxPushString(e.sParam3);
            NWNCore.NativeFunctions.nwnxPushString(e.sParam4);
            NWNCore.NativeFunctions.nwnxPushString(e.sParam5);
            NWNCore.NativeFunctions.nwnxPushObject(e.oParam0);
            NWNCore.NativeFunctions.nwnxPushObject(e.oParam1);
            NWNCore.NativeFunctions.nwnxPushObject(e.oParam2);
            NWNCore.NativeFunctions.nwnxPushObject(e.oParam3);

            NWNCore.NativeFunctions.nwnxPushFloat(e.vParam0.X);
            NWNCore.NativeFunctions.nwnxPushFloat(e.vParam0.Y);
            NWNCore.NativeFunctions.nwnxPushFloat(e.vParam0.Z);

            NWNCore.NativeFunctions.nwnxPushFloat(e.vParam1.X);
            NWNCore.NativeFunctions.nwnxPushFloat(e.vParam1.Y);
            NWNCore.NativeFunctions.nwnxPushFloat(e.vParam1.Z);

            NWNCore.NativeFunctions.nwnxPushString(e.Tag);

            NWNCore.NativeFunctions.nwnxPushString(e.sItemProp);
        }

        // Convert native effect type to unpacked structure
        public static EffectUnpacked UnpackEffect(Core.Effect effect)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "UnpackEffect");
            NWNCore.NativeFunctions.nwnxPushEffect(effect.Handle);
            NWNCore.NativeFunctions.nwnxCallFunction();

            return ResolveUnpack();
        }

        // Convert unpacked effect structure to native type
        public static Core.Effect PackEffect(EffectUnpacked effect)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "PackEffect");
            ResolvePack(effect);

            return new Core.Effect(NWNCore.NativeFunctions.nwnxPopEffect());
        }

        /// @brief replace an already applied effect on an object
        /// Only duration, subtype, tag and spell related fields can be overwritten.
        /// @note eNew and eOld need to have the same type.
        /// @return Number of internal effects updated.
        public static int ReplaceEffect(uint obj, Core.Effect eOld, Core.Effect eNew)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "ReplaceEffect");
            NWNCore.NativeFunctions.nwnxPushEffect(eNew);
            NWNCore.NativeFunctions.nwnxPushEffect(eOld);
            NWNCore.NativeFunctions.nwnxPushObject(obj);
            NWNCore.NativeFunctions.nwnxCallFunction();

            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        /// @brief Gets the true effect count
        /// @param oObject The object to get the count of.
        /// @return the number of effects (item properties and other non-exposed effects included)
        public static int GetTrueEffectCount(uint oObject)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetTrueEffectCount");
            NWNCore.NativeFunctions.nwnxPushObject(oObject);
            NWNCore.NativeFunctions.nwnxCallFunction();

            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        /// @brief Gets a specific effect on an object. This can grab effects normally hidden from developers, such as item properties.
        /// @param oObject The object with the effect
        /// @param nIndex The point in the array to retrieve (0 to GetTrueEffectCount())
        /// @return A constructed NWNX_EffectUnpacked.
        public static EffectUnpacked GetTrueEffect(uint oObject, int nIndex)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetTrueEffectCount");
            NWNCore.NativeFunctions.nwnxPushInt(nIndex);
            NWNCore.NativeFunctions.nwnxPushObject(oObject);
            NWNCore.NativeFunctions.nwnxCallFunction();

            return ResolveUnpack(false);
        }

        /// Replaces an already applied effect with another.
        /// oObject The object with the effect to replace
        /// nIndex The array element to be replaced
        /// e The unpacked effect to replace it with.
        /// Cannot replace an effect with a different type or ID.
        public static void ReplaceEffectByIndex(uint oObject, int nIndex, EffectUnpacked e)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "ReplaceEffectByIndex");

            ResolvePack(e, true);
            NWNCore.NativeFunctions.nwnxPushInt(nIndex);
            NWNCore.NativeFunctions.nwnxPushObject(oObject);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        /// Removes effect by ID
        /// oObject The object to remove the effect from
        /// sID The id of the effect, can be retrieved by unpacking effects.
        /// FALSE/0 on failure TRUE/1 on success.
        public static int RemoveEffectById(uint oObject, string sID)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "RemoveEffectById");

            NWNCore.NativeFunctions.nwnxPushString(sID);
            NWNCore.NativeFunctions.nwnxPushObject(oObject);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        /// Applies an effect, bypassing any processing done by ApplyEffectToObject
        /// eEffect The effect to be applied.
        /// oObject The object to apply it to.
        public static void Apply(Effect eEffect, uint oObject)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "Apply");

            NWNCore.NativeFunctions.nwnxPushObject(oObject);
            NWNCore.NativeFunctions.nwnxPushEffect(eEffect);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }
    }
}