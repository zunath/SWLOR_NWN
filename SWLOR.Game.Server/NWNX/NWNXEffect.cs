using NWN;
using SWLOR.Game.Server.GameObject;
using static SWLOR.Game.Server.NWNX.NWNXCore;

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

            NWNX_PushArgumentEffect(NWNX_Effect, sFunc, effect);
            NWNX_CallFunction(NWNX_Effect, sFunc);

            EffectUnpacked n = new EffectUnpacked
            {
                Tag = NWNX_GetReturnValueString(NWNX_Effect, sFunc),
                oParam3 = NWNX_GetReturnValueObject(NWNX_Effect, sFunc),
                oParam2 = NWNX_GetReturnValueObject(NWNX_Effect, sFunc),
                oParam1 = NWNX_GetReturnValueObject(NWNX_Effect, sFunc),
                oParam0 = NWNX_GetReturnValueObject(NWNX_Effect, sFunc),
                sParam5 = NWNX_GetReturnValueString(NWNX_Effect, sFunc),
                sParam4 = NWNX_GetReturnValueString(NWNX_Effect, sFunc),
                sParam3 = NWNX_GetReturnValueString(NWNX_Effect, sFunc),
                sParam2 = NWNX_GetReturnValueString(NWNX_Effect, sFunc),
                sParam1 = NWNX_GetReturnValueString(NWNX_Effect, sFunc),
                sParam0 = NWNX_GetReturnValueString(NWNX_Effect, sFunc),
                fParam3 = NWNX_GetReturnValueFloat(NWNX_Effect, sFunc),
                fParam2 = NWNX_GetReturnValueFloat(NWNX_Effect, sFunc),
                fParam1 = NWNX_GetReturnValueFloat(NWNX_Effect, sFunc),
                fParam0 = NWNX_GetReturnValueFloat(NWNX_Effect, sFunc),
                nParam7 = NWNX_GetReturnValueInt(NWNX_Effect, sFunc),
                nParam6 = NWNX_GetReturnValueInt(NWNX_Effect, sFunc),
                nParam5 = NWNX_GetReturnValueInt(NWNX_Effect, sFunc),
                nParam4 = NWNX_GetReturnValueInt(NWNX_Effect, sFunc),
                nParam3 = NWNX_GetReturnValueInt(NWNX_Effect, sFunc),
                nParam2 = NWNX_GetReturnValueInt(NWNX_Effect, sFunc),
                nParam1 = NWNX_GetReturnValueInt(NWNX_Effect, sFunc),
                nParam0 = NWNX_GetReturnValueInt(NWNX_Effect, sFunc),
                NumIntegers = NWNX_GetReturnValueInt(NWNX_Effect, sFunc),
                LinkRightValid = NWNX_GetReturnValueInt(NWNX_Effect, sFunc),
                LinkRight = NWNX_GetReturnValueEffect(NWNX_Effect, sFunc),
                LinkLeftValid = NWNX_GetReturnValueInt(NWNX_Effect, sFunc),
                LinkLeft = NWNX_GetReturnValueEffect(NWNX_Effect, sFunc),
                CasterLevel = NWNX_GetReturnValueInt(NWNX_Effect, sFunc),
                ShowIcon = NWNX_GetReturnValueInt(NWNX_Effect, sFunc),
                Expose = NWNX_GetReturnValueInt(NWNX_Effect, sFunc),
                SpellID = NWNX_GetReturnValueInt(NWNX_Effect, sFunc),
                Creator = NWNX_GetReturnValueObject(NWNX_Effect, sFunc),
                ExpiryTimeOfDay = NWNX_GetReturnValueInt(NWNX_Effect, sFunc),
                ExpiryCalendarDay = NWNX_GetReturnValueInt(NWNX_Effect, sFunc),
                Duration = NWNX_GetReturnValueFloat(NWNX_Effect, sFunc),
                SubType = NWNX_GetReturnValueInt(NWNX_Effect, sFunc),
                Type = NWNX_GetReturnValueInt(NWNX_Effect, sFunc),
                EffectID = NWNX_GetReturnValueInt(NWNX_Effect, sFunc)
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

            NWNX_PushArgumentInt(NWNX_Effect, sFunc, effect.EffectID);
            NWNX_PushArgumentInt(NWNX_Effect, sFunc, effect.Type);
            NWNX_PushArgumentInt(NWNX_Effect, sFunc, effect.SubType);

            NWNX_PushArgumentFloat(NWNX_Effect, sFunc, effect.Duration);
            NWNX_PushArgumentInt(NWNX_Effect, sFunc, effect.ExpiryCalendarDay);
            NWNX_PushArgumentInt(NWNX_Effect, sFunc, effect.ExpiryTimeOfDay);

            NWNX_PushArgumentObject(NWNX_Effect, sFunc, effect.Creator);
            NWNX_PushArgumentInt(NWNX_Effect, sFunc, effect.SpellID);
            NWNX_PushArgumentInt(NWNX_Effect, sFunc, effect.Expose);
            NWNX_PushArgumentInt(NWNX_Effect, sFunc, effect.ShowIcon);
            NWNX_PushArgumentInt(NWNX_Effect, sFunc, effect.CasterLevel);

            NWNX_PushArgumentEffect(NWNX_Effect, sFunc, effect.LinkLeft);
            NWNX_PushArgumentInt(NWNX_Effect, sFunc, effect.LinkLeftValid);
            NWNX_PushArgumentEffect(NWNX_Effect, sFunc, effect.LinkRight);
            NWNX_PushArgumentInt(NWNX_Effect, sFunc, effect.LinkRightValid);

            NWNX_PushArgumentInt(NWNX_Effect, sFunc, effect.NumIntegers);
            NWNX_PushArgumentInt(NWNX_Effect, sFunc, effect.nParam0);
            NWNX_PushArgumentInt(NWNX_Effect, sFunc, effect.nParam1);
            NWNX_PushArgumentInt(NWNX_Effect, sFunc, effect.nParam2);
            NWNX_PushArgumentInt(NWNX_Effect, sFunc, effect.nParam3);
            NWNX_PushArgumentInt(NWNX_Effect, sFunc, effect.nParam4);
            NWNX_PushArgumentInt(NWNX_Effect, sFunc, effect.nParam5);
            NWNX_PushArgumentInt(NWNX_Effect, sFunc, effect.nParam6);
            NWNX_PushArgumentInt(NWNX_Effect, sFunc, effect.nParam7);
            NWNX_PushArgumentFloat(NWNX_Effect, sFunc, effect.fParam0);
            NWNX_PushArgumentFloat(NWNX_Effect, sFunc, effect.fParam1);
            NWNX_PushArgumentFloat(NWNX_Effect, sFunc, effect.fParam2);
            NWNX_PushArgumentFloat(NWNX_Effect, sFunc, effect.fParam3);
            NWNX_PushArgumentString(NWNX_Effect, sFunc, effect.sParam0);
            NWNX_PushArgumentString(NWNX_Effect, sFunc, effect.sParam1);
            NWNX_PushArgumentString(NWNX_Effect, sFunc, effect.sParam2);
            NWNX_PushArgumentString(NWNX_Effect, sFunc, effect.sParam3);
            NWNX_PushArgumentString(NWNX_Effect, sFunc, effect.sParam4);
            NWNX_PushArgumentString(NWNX_Effect, sFunc, effect.sParam5);
            NWNX_PushArgumentObject(NWNX_Effect, sFunc, effect.oParam0);
            NWNX_PushArgumentObject(NWNX_Effect, sFunc, effect.oParam1);
            NWNX_PushArgumentObject(NWNX_Effect, sFunc, effect.oParam2);
            NWNX_PushArgumentObject(NWNX_Effect, sFunc, effect.oParam3);

            NWNX_PushArgumentString(NWNX_Effect, sFunc, effect.Tag);

            NWNX_CallFunction(NWNX_Effect, sFunc);
            return NWNX_GetReturnValueEffect(NWNX_Effect, sFunc);
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

            NWNX_PushArgumentString(NWNX_Effect, sFunc, data);
            NWNX_PushArgumentString(NWNX_Effect, sFunc, script);
            NWNX_PushArgumentEffect(NWNX_Effect, sFunc, e);

            NWNX_CallFunction(NWNX_Effect, sFunc);

            return NWNX_GetReturnValueEffect(NWNX_Effect, sFunc);
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

            NWNX_CallFunction(NWNX_Effect, sFunc);

            return NWNX_GetReturnValueString(NWNX_Effect, sFunc);
        }

        /// <summary>
        /// Get the effect creator of NWNX_Effect_SetEffectExpiredScript()
        /// THIS SHOULD ONLY BE CALLED FROM WITHIN A SCRIPT THAT WAS EXECUTED BY NWNX_Effect_SetEffectExpiredScript()
        /// </summary>
        /// <returns></returns>
        public static NWObject GetEffectExpiredCreator()
        {
            string sFunc = "GetEffectExpiredCreator";

            NWNX_CallFunction(NWNX_Effect, sFunc);

            return NWNX_GetReturnValueObject(NWNX_Effect, sFunc);
        }
    }
}
