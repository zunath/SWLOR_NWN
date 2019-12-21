using NWN;

namespace SWLOR.Game.Server.NWNX
{
    public static class NWNXEffect
    {
        private const string NWNX_Effect = "NWNX_Effect";

        /// <summary>
        /// Convert native effect type to unpacked structure
        /// </summary>
        /// <param name="effect"></param>
        /// <returns></returns>
        public static EffectUnpacked UnpackEffect(Effect effect)
        {
            string sFunc = "UnpackEffect";

            NWNXCore.NWNX_PushArgumentEffect(NWNX_Effect, sFunc, effect);
            NWNXCore.NWNX_CallFunction(NWNX_Effect, sFunc);

            EffectUnpacked n = new EffectUnpacked
            {
                Tag = NWNXCore.NWNX_GetReturnValueString(NWNX_Effect, sFunc),
                oParam3 = NWNXCore.NWNX_GetReturnValueObject(NWNX_Effect, sFunc),
                oParam2 = NWNXCore.NWNX_GetReturnValueObject(NWNX_Effect, sFunc),
                oParam1 = NWNXCore.NWNX_GetReturnValueObject(NWNX_Effect, sFunc),
                oParam0 = NWNXCore.NWNX_GetReturnValueObject(NWNX_Effect, sFunc),
                sParam5 = NWNXCore.NWNX_GetReturnValueString(NWNX_Effect, sFunc),
                sParam4 = NWNXCore.NWNX_GetReturnValueString(NWNX_Effect, sFunc),
                sParam3 = NWNXCore.NWNX_GetReturnValueString(NWNX_Effect, sFunc),
                sParam2 = NWNXCore.NWNX_GetReturnValueString(NWNX_Effect, sFunc),
                sParam1 = NWNXCore.NWNX_GetReturnValueString(NWNX_Effect, sFunc),
                sParam0 = NWNXCore.NWNX_GetReturnValueString(NWNX_Effect, sFunc),
                fParam3 = NWNXCore.NWNX_GetReturnValueFloat(NWNX_Effect, sFunc),
                fParam2 = NWNXCore.NWNX_GetReturnValueFloat(NWNX_Effect, sFunc),
                fParam1 = NWNXCore.NWNX_GetReturnValueFloat(NWNX_Effect, sFunc),
                fParam0 = NWNXCore.NWNX_GetReturnValueFloat(NWNX_Effect, sFunc),
                nParam7 = NWNXCore.NWNX_GetReturnValueInt(NWNX_Effect, sFunc),
                nParam6 = NWNXCore.NWNX_GetReturnValueInt(NWNX_Effect, sFunc),
                nParam5 = NWNXCore.NWNX_GetReturnValueInt(NWNX_Effect, sFunc),
                nParam4 = NWNXCore.NWNX_GetReturnValueInt(NWNX_Effect, sFunc),
                nParam3 = NWNXCore.NWNX_GetReturnValueInt(NWNX_Effect, sFunc),
                nParam2 = NWNXCore.NWNX_GetReturnValueInt(NWNX_Effect, sFunc),
                nParam1 = NWNXCore.NWNX_GetReturnValueInt(NWNX_Effect, sFunc),
                nParam0 = NWNXCore.NWNX_GetReturnValueInt(NWNX_Effect, sFunc),
                NumIntegers = NWNXCore.NWNX_GetReturnValueInt(NWNX_Effect, sFunc),
                LinkRightValid = NWNXCore.NWNX_GetReturnValueInt(NWNX_Effect, sFunc),
                LinkRight = NWNXCore.NWNX_GetReturnValueEffect(NWNX_Effect, sFunc),
                LinkLeftValid = NWNXCore.NWNX_GetReturnValueInt(NWNX_Effect, sFunc),
                LinkLeft = NWNXCore.NWNX_GetReturnValueEffect(NWNX_Effect, sFunc),
                CasterLevel = NWNXCore.NWNX_GetReturnValueInt(NWNX_Effect, sFunc),
                ShowIcon = NWNXCore.NWNX_GetReturnValueInt(NWNX_Effect, sFunc),
                Expose = NWNXCore.NWNX_GetReturnValueInt(NWNX_Effect, sFunc),
                SpellID = NWNXCore.NWNX_GetReturnValueInt(NWNX_Effect, sFunc),
                Creator = NWNXCore.NWNX_GetReturnValueObject(NWNX_Effect, sFunc),
                ExpiryTimeOfDay = NWNXCore.NWNX_GetReturnValueInt(NWNX_Effect, sFunc),
                ExpiryCalendarDay = NWNXCore.NWNX_GetReturnValueInt(NWNX_Effect, sFunc),
                Duration = NWNXCore.NWNX_GetReturnValueFloat(NWNX_Effect, sFunc),
                SubType = NWNXCore.NWNX_GetReturnValueInt(NWNX_Effect, sFunc),
                Type = NWNXCore.NWNX_GetReturnValueInt(NWNX_Effect, sFunc),
                EffectID = NWNXCore.NWNX_GetReturnValueInt(NWNX_Effect, sFunc)
            };
            
            return n;
        }

        /// <summary>
        /// Convert unpacked effect structure to native type
        /// </summary>
        /// <param name="effect"></param>
        /// <returns></returns>
        public static Effect PackEffect(EffectUnpacked effect)
        {
            string sFunc = "PackEffect";

            NWNXCore.NWNX_PushArgumentInt(NWNX_Effect, sFunc, effect.EffectID);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Effect, sFunc, effect.Type);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Effect, sFunc, effect.SubType);

            NWNXCore.NWNX_PushArgumentFloat(NWNX_Effect, sFunc, effect.Duration);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Effect, sFunc, effect.ExpiryCalendarDay);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Effect, sFunc, effect.ExpiryTimeOfDay);

            NWNXCore.NWNX_PushArgumentObject(NWNX_Effect, sFunc, effect.Creator);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Effect, sFunc, effect.SpellID);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Effect, sFunc, effect.Expose);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Effect, sFunc, effect.ShowIcon);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Effect, sFunc, effect.CasterLevel);

            NWNXCore.NWNX_PushArgumentEffect(NWNX_Effect, sFunc, effect.LinkLeft);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Effect, sFunc, effect.LinkLeftValid);
            NWNXCore.NWNX_PushArgumentEffect(NWNX_Effect, sFunc, effect.LinkRight);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Effect, sFunc, effect.LinkRightValid);

            NWNXCore.NWNX_PushArgumentInt(NWNX_Effect, sFunc, effect.NumIntegers);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Effect, sFunc, effect.nParam0);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Effect, sFunc, effect.nParam1);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Effect, sFunc, effect.nParam2);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Effect, sFunc, effect.nParam3);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Effect, sFunc, effect.nParam4);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Effect, sFunc, effect.nParam5);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Effect, sFunc, effect.nParam6);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Effect, sFunc, effect.nParam7);
            NWNXCore.NWNX_PushArgumentFloat(NWNX_Effect, sFunc, effect.fParam0);
            NWNXCore.NWNX_PushArgumentFloat(NWNX_Effect, sFunc, effect.fParam1);
            NWNXCore.NWNX_PushArgumentFloat(NWNX_Effect, sFunc, effect.fParam2);
            NWNXCore.NWNX_PushArgumentFloat(NWNX_Effect, sFunc, effect.fParam3);
            NWNXCore.NWNX_PushArgumentString(NWNX_Effect, sFunc, effect.sParam0);
            NWNXCore.NWNX_PushArgumentString(NWNX_Effect, sFunc, effect.sParam1);
            NWNXCore.NWNX_PushArgumentString(NWNX_Effect, sFunc, effect.sParam2);
            NWNXCore.NWNX_PushArgumentString(NWNX_Effect, sFunc, effect.sParam3);
            NWNXCore.NWNX_PushArgumentString(NWNX_Effect, sFunc, effect.sParam4);
            NWNXCore.NWNX_PushArgumentString(NWNX_Effect, sFunc, effect.sParam5);
            NWNXCore.NWNX_PushArgumentObject(NWNX_Effect, sFunc, effect.oParam0);
            NWNXCore.NWNX_PushArgumentObject(NWNX_Effect, sFunc, effect.oParam1);
            NWNXCore.NWNX_PushArgumentObject(NWNX_Effect, sFunc, effect.oParam2);
            NWNXCore.NWNX_PushArgumentObject(NWNX_Effect, sFunc, effect.oParam3);

            NWNXCore.NWNX_PushArgumentString(NWNX_Effect, sFunc, effect.Tag);

            NWNXCore.NWNX_CallFunction(NWNX_Effect, sFunc);
            return NWNXCore.NWNX_GetReturnValueEffect(NWNX_Effect, sFunc);
        }

        /// <summary>
        /// Set a script with optional data that runs when an effect expires
        /// Only works for TEMPORARY and PERMANENT effects applied to an object
        /// Note: OBJECT_SELF in the script is the object the effect is applied to
        /// </summary>
        /// <param name="e"></param>
        /// <param name="script"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Effect SetEffectExpiredScript(Effect e, string script, string data = "")
        {
            string sFunc = "SetEffectExpiredScript";

            NWNXCore.NWNX_PushArgumentString(NWNX_Effect, sFunc, data);
            NWNXCore.NWNX_PushArgumentString(NWNX_Effect, sFunc, script);
            NWNXCore.NWNX_PushArgumentEffect(NWNX_Effect, sFunc, e);

            NWNXCore.NWNX_CallFunction(NWNX_Effect, sFunc);

            return NWNXCore.NWNX_GetReturnValueEffect(NWNX_Effect, sFunc);
        }

        /// <summary>
        /// Get the data set with NWNX_Effect_SetEffectExpiredScript()
        ///
        /// THIS SHOULD ONLY BE CALLED FROM WITHIN A SCRIPT THAT WAS EXECUTED BY SetEffectExpiredScript()
        /// </summary>
        /// <returns></returns>
        public static string GetEffectExpiredData()
        {
            string sFunc = "GetEffectExpiredData";

            NWNXCore.NWNX_CallFunction(NWNX_Effect, sFunc);

            return NWNXCore.NWNX_GetReturnValueString(NWNX_Effect, sFunc);
        }

        /// <summary>
        /// Get the effect creator of NWNX_Effect_SetEffectExpiredScript()
        /// THIS SHOULD ONLY BE CALLED FROM WITHIN A SCRIPT THAT WAS EXECUTED BY NWNX_Effect_SetEffectExpiredScript()
        /// </summary>
        /// <returns></returns>
        public static NWGameObject GetEffectExpiredCreator()
        {
            string sFunc = "GetEffectExpiredCreator";

            NWNXCore.NWNX_CallFunction(NWNX_Effect, sFunc);

            return NWNXCore.NWNX_GetReturnValueObject(NWNX_Effect, sFunc);
        }
    }
}
