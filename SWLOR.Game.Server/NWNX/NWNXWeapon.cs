﻿using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.NWN.Enum;
using SWLOR.Game.Server.NWN.Enum.Creature;
using SWLOR.Game.Server.NWN.Enum.Item;

namespace SWLOR.Game.Server.NWNX
{
    public static class NWNXWeapon
    {
        private const string PLUGIN_NAME = "NWNX_Weapon";

        // Options constants to be used with NWNX_Weapon_SetOption function
        private const int GreaterFOcusABBonus = 0; // Greater Focus AB bonus
        private const int GreaterSpecialDamageBonus = 1; // Greater Spec. DAM bonus

        // Get Event Data Constants
        private const int GetData_DC = 0; // Get Devastating Critical Data

        // Set Event Data Constants
        private const int DC_Bypass = 0; // Set Devastating Critical Bypass

        public static void SetWeaponFocusFeat(BaseItem baseItem, Feat feat)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetWeaponFocusFeat");
            Internal.NativeFunctions.nwnxPushInt((int)feat);
            Internal.NativeFunctions.nwnxPushInt((int)baseItem);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static void SetEpicWeaponFocusFeat(BaseItem baseItem, Feat feat)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetEpicWeaponFocusFeat");
            Internal.NativeFunctions.nwnxPushInt((int)feat);
            Internal.NativeFunctions.nwnxPushInt((int)baseItem);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static void SetGreaterWeaponFocusFeat(BaseItem baseItem, Feat feat)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetGreaterWeaponFocusFeat");
            Internal.NativeFunctions.nwnxPushInt((int)feat);
            Internal.NativeFunctions.nwnxPushInt((int)baseItem);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static void SetWeaponFinesseSize(BaseItem baseItem, CreatureSize nCreatureSize)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetWeaponFinesseSize");
            Internal.NativeFunctions.nwnxPushInt((int)nCreatureSize);
            Internal.NativeFunctions.nwnxPushInt((int)baseItem);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static void SetWeaponUnarmed(BaseItem baseItem)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetWeaponUnarmed");
            Internal.NativeFunctions.nwnxPushInt((int)baseItem);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static void SetWeaponIsMonkWeapon(BaseItem baseItem)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetWeaponIsMonkWeapon");
            Internal.NativeFunctions.nwnxPushInt((int)baseItem);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static void SetWeaponImprovedCriticalFeat(BaseItem baseItem, Feat feat)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetWeaponImprovedCriticalFeat");
            Internal.NativeFunctions.nwnxPushInt((int)feat);
            Internal.NativeFunctions.nwnxPushInt((int)baseItem);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static void SetWeaponSpecializationFeat(BaseItem baseItem, Feat feat)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetWeaponSpecializationFeat");
            Internal.NativeFunctions.nwnxPushInt((int)feat);
            Internal.NativeFunctions.nwnxPushInt((int)baseItem);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static void SetGreaterWeaponSpecializationFeat(BaseItem baseItem, Feat feat)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetGreaterWeaponSpecializationFeat");
            Internal.NativeFunctions.nwnxPushInt((int)feat);
            Internal.NativeFunctions.nwnxPushInt((int)baseItem);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static void SetEpicWeaponSpecializationFeat(BaseItem baseItem, Feat feat)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetEpicWeaponSpecializationFeat");
            Internal.NativeFunctions.nwnxPushInt((int)feat);
            Internal.NativeFunctions.nwnxPushInt((int)baseItem);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static void SetEpicWeaponOverwhelmingCriticalFeat(BaseItem baseItem, Feat feat)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetEpicWeaponOverwhelmingCriticalFeat");
            Internal.NativeFunctions.nwnxPushInt((int)feat);
            Internal.NativeFunctions.nwnxPushInt((int)baseItem);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static void SetEpicWeaponDevastatingCriticalFeat(BaseItem baseItem, Feat feat)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetEpicWeaponDevastatingCriticalFeat");
            Internal.NativeFunctions.nwnxPushInt((int)feat);
            Internal.NativeFunctions.nwnxPushInt((int)baseItem);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static void SetWeaponOfChoiceFeat(BaseItem baseItem, Feat feat)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetWeaponOfChoiceFeat");
            Internal.NativeFunctions.nwnxPushInt((int)feat);
            Internal.NativeFunctions.nwnxPushInt((int)baseItem);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static void SetOption(int nOption, int nVal)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetOption");
            Internal.NativeFunctions.nwnxPushInt(nVal);
            Internal.NativeFunctions.nwnxPushInt(nOption);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static void SetDevastatingCriticalEventScript(string sScript)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetDevastatingCriticalEventScript");
            Internal.NativeFunctions.nwnxPushString(sScript);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static void BypassDevastatingCritical()
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetEventData");
            Internal.NativeFunctions.nwnxPushInt(1);
            Internal.NativeFunctions.nwnxPushInt(DC_Bypass);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static DevastatingCriticalData GetDevastatingCriticalEventData()
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetEventData");
            var data = new DevastatingCriticalData();
            Internal.NativeFunctions.nwnxPushInt(GetData_DC);
            Internal.NativeFunctions.nwnxCallFunction();
            data.Weapon = Internal.NativeFunctions.nwnxPopObject();
            data.Target = Internal.NativeFunctions.nwnxPopObject();
            data.Damage = Internal.NativeFunctions.nwnxPopInt();
            return data;
        }


        /// @brief Sets weapon to gain .5 strength bonus.
        /// @param oWeapon Should be a melee weapon.
        /// @param nEnable TRUE for bonus. FALSE to turn off bonus.
        /// @param bPersist whether the two hand state should persist to the gff file.
        public static void SetOneHalfStrength(uint oWeapon, bool nEnable, bool bPersist = false)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetOneHalfStrength");

            Internal.NativeFunctions.nwnxPushInt(bPersist ? 1 : 0);
            Internal.NativeFunctions.nwnxPushInt(nEnable ? 1 : 0);
            Internal.NativeFunctions.nwnxPushObject(oWeapon);

            Internal.NativeFunctions.nwnxCallFunction();
        }

        /// @brief Gets if the weapon is set to gain addition .5 strength bonus
        /// @param oWeapon the weapon
        /// @return FALSE/0 if weapon is not receiving the bonus. TRUE/1 if it does.
        public static int GetOneHalfStrength(uint oWeapon)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetOneHalfStrength");
            Internal.NativeFunctions.nwnxPushObject(oWeapon);
            Internal.NativeFunctions.nwnxCallFunction();

            return Internal.NativeFunctions.nwnxPopInt();

        }
    }
}
