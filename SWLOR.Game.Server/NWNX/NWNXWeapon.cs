using static SWLOR.Game.Server.NWNX.NWNXCore;

namespace SWLOR.Game.Server.NWNX
{
    public static class NWNXWeapon
    {
        const string NWNX_Weapon = "NWNX_Weapon";
        // Otpions constants to be used with NWNX_Weapon_SetOption function
        const int NWNX_WEAPON_OPT_GRTFOCUS_AB_BONUS = 0; // Greater Focus AB bonus
        const int NWNX_WEAPON_OPT_GRTSPEC_DAM_BONUS = 1; // Greater Spec. DAM bonus

        // Get Event Data Constants
        const int NWNX_WEAPON_GETDATA_DC = 0; // Get Devastating Critical Data

        // Set Event Data Constants
        const int NWNX_WEAPON_SETDATA_DC_BYPASS = 0; // Set Devastating Critical Bypass
        
        public static void SetWeaponFocusFeat(int nBaseItem, int nFeat)
        {
            string sFunc = "SetWeaponFocusFeat";

            NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nFeat);
            NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nBaseItem);

            NWNX_CallFunction(NWNX_Weapon, sFunc);
        }

        public static void SetEpicWeaponFocusFeat(int nBaseItem, int nFeat)
        {
            string sFunc = "SetEpicWeaponFocusFeat";

            NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nFeat);
            NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nBaseItem);

            NWNX_CallFunction(NWNX_Weapon, sFunc);
        }

        public static void SetGreaterWeaponFocusFeat(int nBaseItem, int nFeat)
        {
            string sFunc = "SetGreaterWeaponFocusFeat";

            NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nFeat);
            NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nBaseItem);

            NWNX_CallFunction(NWNX_Weapon, sFunc);
        }

        public static void SetWeaponFinesseSize(int nBaseItem, int nSize)
        {
            string sFunc = "SetWeaponFinesseSize";

            NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nSize);
            NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nBaseItem);

            NWNX_CallFunction(NWNX_Weapon, sFunc);
        }

        public static void SetWeaponUnarmed(int nBaseItem)
        {
            string sFunc = "SetWeaponUnarmed";

            NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nBaseItem);

            NWNX_CallFunction(NWNX_Weapon, sFunc);
        }

        public static void SetWeaponIsMonkWeapon(int nBaseItem)
        {
            string sFunc = "SetWeaponIsMonkWeapon";

            NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nBaseItem);

            NWNX_CallFunction(NWNX_Weapon, sFunc);
        }

        public static void SetWeaponImprovedCriticalFeat(int nBaseItem, int nFeat)
        {
            string sFunc = "SetWeaponImprovedCriticalFeat";

            NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nFeat);
            NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nBaseItem);

            NWNX_CallFunction(NWNX_Weapon, sFunc);
        }

        public static void SetWeaponSpecializationFeat(int nBaseItem, int nFeat)
        {
            string sFunc = "SetWeaponSpecializationFeat";

            NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nFeat);
            NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nBaseItem);

            NWNX_CallFunction(NWNX_Weapon, sFunc);
        }

        public static void SetGreaterWeaponSpecializationFeat(int nBaseItem, int nFeat)
        {
            string sFunc = "SetGreaterWeaponSpecializationFeat";

            NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nFeat);
            NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nBaseItem);

            NWNX_CallFunction(NWNX_Weapon, sFunc);
        }

        public static void SetEpicWeaponSpecializationFeat(int nBaseItem, int nFeat)
        {
            string sFunc = "SetEpicWeaponSpecializationFeat";

            NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nFeat);
            NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nBaseItem);

            NWNX_CallFunction(NWNX_Weapon, sFunc);
        }

        public static void SetEpicWeaponOverwhelmingCriticalFeat(int nBaseItem, int nFeat)
        {
            string sFunc = "SetEpicWeaponOverwhelmingCriticalFeat";

            NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nFeat);
            NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nBaseItem);

            NWNX_CallFunction(NWNX_Weapon, sFunc);
        }

        public static void SetEpicWeaponDevastatingCriticalFeat(int nBaseItem, int nFeat)
        {
            string sFunc = "SetEpicWeaponDevastatingCriticalFeat";

            NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nFeat);
            NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nBaseItem);

            NWNX_CallFunction(NWNX_Weapon, sFunc);
        }

        public static void SetWeaponOfChoiceFeat(int nBaseItem, int nFeat)
        {
            string sFunc = "SetWeaponOfChoiceFeat";

            NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nFeat);
            NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nBaseItem);

            NWNX_CallFunction(NWNX_Weapon, sFunc);
        }

        public static void SetOption(int nOption, int nVal)
        {
            string sFunc = "SetOption";

            NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nVal);
            NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nOption);

            NWNX_CallFunction(NWNX_Weapon, sFunc);
        }

        public static void SetDevastatingCriticalEventScript(string sScript)
        {
            string sFunc = "SetDevastatingCriticalEventScript";

            NWNX_PushArgumentString(NWNX_Weapon, sFunc, sScript);

            NWNX_CallFunction(NWNX_Weapon, sFunc);
        }

        public static void BypassDevastatingCritical()
        {
            string sFunc = "SetEventData";

            NWNX_PushArgumentInt(NWNX_Weapon, sFunc, 1);
            NWNX_PushArgumentInt(NWNX_Weapon, sFunc, NWNX_WEAPON_SETDATA_DC_BYPASS);

            NWNX_CallFunction(NWNX_Weapon, sFunc);
        }
        
        public static DevastatingCriticalData GetDevastatingCriticalEventData()
        {
            string sFunc = "GetEventData";
            DevastatingCriticalData data = new DevastatingCriticalData();

            NWNX_PushArgumentInt(NWNX_Weapon, sFunc, NWNX_WEAPON_GETDATA_DC);
            NWNX_CallFunction(NWNX_Weapon, sFunc);

            data.Weapon = NWNX_GetReturnValueObject(NWNX_Weapon, sFunc);
            data.Target = NWNX_GetReturnValueObject(NWNX_Weapon, sFunc);
            data.Damage = NWNX_GetReturnValueInt(NWNX_Weapon, sFunc);

            return data;
        }

    }
}
