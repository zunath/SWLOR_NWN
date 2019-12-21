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

            NWNXCore.NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nFeat);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nBaseItem);

            NWNXCore.NWNX_CallFunction(NWNX_Weapon, sFunc);
        }

        public static void SetEpicWeaponFocusFeat(int nBaseItem, int nFeat)
        {
            string sFunc = "SetEpicWeaponFocusFeat";

            NWNXCore.NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nFeat);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nBaseItem);

            NWNXCore.NWNX_CallFunction(NWNX_Weapon, sFunc);
        }

        public static void SetGreaterWeaponFocusFeat(int nBaseItem, int nFeat)
        {
            string sFunc = "SetGreaterWeaponFocusFeat";

            NWNXCore.NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nFeat);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nBaseItem);

            NWNXCore.NWNX_CallFunction(NWNX_Weapon, sFunc);
        }

        public static void SetWeaponFinesseSize(int nBaseItem, int nSize)
        {
            string sFunc = "SetWeaponFinesseSize";

            NWNXCore.NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nSize);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nBaseItem);

            NWNXCore.NWNX_CallFunction(NWNX_Weapon, sFunc);
        }

        public static void SetWeaponUnarmed(int nBaseItem)
        {
            string sFunc = "SetWeaponUnarmed";

            NWNXCore.NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nBaseItem);

            NWNXCore.NWNX_CallFunction(NWNX_Weapon, sFunc);
        }

        public static void SetWeaponIsMonkWeapon(int nBaseItem)
        {
            string sFunc = "SetWeaponIsMonkWeapon";

            NWNXCore.NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nBaseItem);

            NWNXCore.NWNX_CallFunction(NWNX_Weapon, sFunc);
        }

        public static void SetWeaponImprovedCriticalFeat(int nBaseItem, int nFeat)
        {
            string sFunc = "SetWeaponImprovedCriticalFeat";

            NWNXCore.NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nFeat);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nBaseItem);

            NWNXCore.NWNX_CallFunction(NWNX_Weapon, sFunc);
        }

        public static void SetWeaponSpecializationFeat(int nBaseItem, int nFeat)
        {
            string sFunc = "SetWeaponSpecializationFeat";

            NWNXCore.NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nFeat);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nBaseItem);

            NWNXCore.NWNX_CallFunction(NWNX_Weapon, sFunc);
        }

        public static void SetGreaterWeaponSpecializationFeat(int nBaseItem, int nFeat)
        {
            string sFunc = "SetGreaterWeaponSpecializationFeat";

            NWNXCore.NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nFeat);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nBaseItem);

            NWNXCore.NWNX_CallFunction(NWNX_Weapon, sFunc);
        }

        public static void SetEpicWeaponSpecializationFeat(int nBaseItem, int nFeat)
        {
            string sFunc = "SetEpicWeaponSpecializationFeat";

            NWNXCore.NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nFeat);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nBaseItem);

            NWNXCore.NWNX_CallFunction(NWNX_Weapon, sFunc);
        }

        public static void SetEpicWeaponOverwhelmingCriticalFeat(int nBaseItem, int nFeat)
        {
            string sFunc = "SetEpicWeaponOverwhelmingCriticalFeat";

            NWNXCore.NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nFeat);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nBaseItem);

            NWNXCore.NWNX_CallFunction(NWNX_Weapon, sFunc);
        }

        public static void SetEpicWeaponDevastatingCriticalFeat(int nBaseItem, int nFeat)
        {
            string sFunc = "SetEpicWeaponDevastatingCriticalFeat";

            NWNXCore.NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nFeat);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nBaseItem);

            NWNXCore.NWNX_CallFunction(NWNX_Weapon, sFunc);
        }

        public static void SetWeaponOfChoiceFeat(int nBaseItem, int nFeat)
        {
            string sFunc = "SetWeaponOfChoiceFeat";

            NWNXCore.NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nFeat);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nBaseItem);

            NWNXCore.NWNX_CallFunction(NWNX_Weapon, sFunc);
        }

        public static void SetOption(int nOption, int nVal)
        {
            string sFunc = "SetOption";

            NWNXCore.NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nVal);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Weapon, sFunc, nOption);

            NWNXCore.NWNX_CallFunction(NWNX_Weapon, sFunc);
        }

        public static void SetDevastatingCriticalEventScript(string sScript)
        {
            string sFunc = "SetDevastatingCriticalEventScript";

            NWNXCore.NWNX_PushArgumentString(NWNX_Weapon, sFunc, sScript);

            NWNXCore.NWNX_CallFunction(NWNX_Weapon, sFunc);
        }

        public static void BypassDevastatingCritical()
        {
            string sFunc = "SetEventData";

            NWNXCore.NWNX_PushArgumentInt(NWNX_Weapon, sFunc, 1);
            NWNXCore.NWNX_PushArgumentInt(NWNX_Weapon, sFunc, NWNX_WEAPON_SETDATA_DC_BYPASS);

            NWNXCore.NWNX_CallFunction(NWNX_Weapon, sFunc);
        }
        
        public static DevastatingCriticalData GetDevastatingCriticalEventData()
        {
            string sFunc = "GetEventData";
            DevastatingCriticalData data = new DevastatingCriticalData();

            NWNXCore.NWNX_PushArgumentInt(NWNX_Weapon, sFunc, NWNX_WEAPON_GETDATA_DC);
            NWNXCore.NWNX_CallFunction(NWNX_Weapon, sFunc);

            data.Weapon = NWNXCore.NWNX_GetReturnValueObject(NWNX_Weapon, sFunc);
            data.Target = NWNXCore.NWNX_GetReturnValueObject(NWNX_Weapon, sFunc);
            data.Damage = NWNXCore.NWNX_GetReturnValueInt(NWNX_Weapon, sFunc);

            return data;
        }

    }
}
