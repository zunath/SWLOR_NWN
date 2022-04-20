using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Creature;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Core.NWNX
{
    public static class WeaponPlugin
    {
        private const string PLUGIN_NAME = "NWNX_Weapon";

        // Options constants to be used with NWNX_Weapon_SetOption function
        private const int GreaterFOcusABBonus = 0; // Greater Focus AB bonus
        private const int GreaterSpecialDamageBonus = 1; // Greater Spec. DAM bonus

        // Get Event Data Constants
        private const int GetData_DC = 0; // Get Devastating Critical Data

        // Set Event Data Constants
        private const int DC_Bypass = 0; // Set Devastating Critical Bypass

        public static void SetWeaponFocusFeat(BaseItem baseItem, FeatType feat)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetWeaponFocusFeat");
            NWNCore.NativeFunctions.nwnxPushInt((int)feat);
            NWNCore.NativeFunctions.nwnxPushInt((int)baseItem);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static void SetEpicWeaponFocusFeat(BaseItem baseItem, FeatType feat)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetEpicWeaponFocusFeat");
            NWNCore.NativeFunctions.nwnxPushInt((int)feat);
            NWNCore.NativeFunctions.nwnxPushInt((int)baseItem);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static void SetGreaterWeaponFocusFeat(BaseItem baseItem, FeatType feat)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetGreaterWeaponFocusFeat");
            NWNCore.NativeFunctions.nwnxPushInt((int)feat);
            NWNCore.NativeFunctions.nwnxPushInt((int)baseItem);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static void SetWeaponFinesseSize(BaseItem baseItem, CreatureSize nCreatureSize)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetWeaponFinesseSize");
            NWNCore.NativeFunctions.nwnxPushInt((int)nCreatureSize);
            NWNCore.NativeFunctions.nwnxPushInt((int)baseItem);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static void SetWeaponUnarmed(BaseItem baseItem)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetWeaponUnarmed");
            NWNCore.NativeFunctions.nwnxPushInt((int)baseItem);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static void SetWeaponIsMonkWeapon(BaseItem baseItem)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetWeaponIsMonkWeapon");
            NWNCore.NativeFunctions.nwnxPushInt((int)baseItem);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static void SetWeaponImprovedCriticalFeat(BaseItem baseItem, FeatType feat)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetWeaponImprovedCriticalFeat");
            NWNCore.NativeFunctions.nwnxPushInt((int)feat);
            NWNCore.NativeFunctions.nwnxPushInt((int)baseItem);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static void SetWeaponSpecializationFeat(BaseItem baseItem, FeatType feat)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetWeaponSpecializationFeat");
            NWNCore.NativeFunctions.nwnxPushInt((int)feat);
            NWNCore.NativeFunctions.nwnxPushInt((int)baseItem);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static void SetGreaterWeaponSpecializationFeat(BaseItem baseItem, FeatType feat)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetGreaterWeaponSpecializationFeat");
            NWNCore.NativeFunctions.nwnxPushInt((int)feat);
            NWNCore.NativeFunctions.nwnxPushInt((int)baseItem);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static void SetEpicWeaponSpecializationFeat(BaseItem baseItem, FeatType feat)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetEpicWeaponSpecializationFeat");
            NWNCore.NativeFunctions.nwnxPushInt((int)feat);
            NWNCore.NativeFunctions.nwnxPushInt((int)baseItem);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static void SetEpicWeaponOverwhelmingCriticalFeat(BaseItem baseItem, FeatType feat)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetEpicWeaponOverwhelmingCriticalFeat");
            NWNCore.NativeFunctions.nwnxPushInt((int)feat);
            NWNCore.NativeFunctions.nwnxPushInt((int)baseItem);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static void SetEpicWeaponDevastatingCriticalFeat(BaseItem baseItem, FeatType feat)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetEpicWeaponDevastatingCriticalFeat");
            NWNCore.NativeFunctions.nwnxPushInt((int)feat);
            NWNCore.NativeFunctions.nwnxPushInt((int)baseItem);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static void SetWeaponOfChoiceFeat(BaseItem baseItem, FeatType feat)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetWeaponOfChoiceFeat");
            NWNCore.NativeFunctions.nwnxPushInt((int)feat);
            NWNCore.NativeFunctions.nwnxPushInt((int)baseItem);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static void SetOption(int nOption, int nVal)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetOption");
            NWNCore.NativeFunctions.nwnxPushInt(nVal);
            NWNCore.NativeFunctions.nwnxPushInt(nOption);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static void SetDevastatingCriticalEventScript(string sScript)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetDevastatingCriticalEventScript");
            NWNCore.NativeFunctions.nwnxPushString(sScript);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static void BypassDevastatingCritical()
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetEventData");
            NWNCore.NativeFunctions.nwnxPushInt(1);
            NWNCore.NativeFunctions.nwnxPushInt(DC_Bypass);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static DevastatingCriticalData GetDevastatingCriticalEventData()
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetEventData");
            var data = new DevastatingCriticalData();
            NWNCore.NativeFunctions.nwnxPushInt(GetData_DC);
            NWNCore.NativeFunctions.nwnxCallFunction();
            data.Weapon = NWNCore.NativeFunctions.nwnxPopObject();
            data.Target = NWNCore.NativeFunctions.nwnxPopObject();
            data.Damage = NWNCore.NativeFunctions.nwnxPopInt();
            return data;
        }


        /// @brief Sets weapon to gain .5 strength bonus.
        /// @param oWeapon Should be a melee weapon.
        /// @param nEnable TRUE for bonus. FALSE to turn off bonus.
        /// @param bPersist whether the two hand state should persist to the gff file.
        public static void SetOneHalfStrength(uint oWeapon, bool nEnable, bool bPersist = false)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetOneHalfStrength");

            NWNCore.NativeFunctions.nwnxPushInt(bPersist ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushInt(nEnable ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushObject(oWeapon);

            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        /// @brief Gets if the weapon is set to gain addition .5 strength bonus
        /// @param oWeapon the weapon
        /// @return FALSE/0 if weapon is not receiving the bonus. TRUE/1 if it does.
        public static int GetOneHalfStrength(uint oWeapon)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetOneHalfStrength");
            NWNCore.NativeFunctions.nwnxPushObject(oWeapon);
            NWNCore.NativeFunctions.nwnxCallFunction();

            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        /// @brief Override the max attack distance of ranged weapons.
        /// @param nBaseItem The baseitem id.
        /// @param fMax The maximum attack distance. Default is 40.0f.
        /// @param fMaxPassive The maximum passive attack distance. Default is 20.0f. Seems to be used by the engine to determine a new nearby target when needed.
        /// @param fPreferred The preferred attack distance. See the PrefAttackDist column in baseitems.2da, default seems to be 30.0f for ranged weapons.
        /// @note fMaxPassive should probably be lower than fMax, half of fMax seems to be a good start. fPreferred should be at least ~0.5f lower than fMax.
        public static void SetMaxRangedAttackDistanceOverride(BaseItem nBaseItem, float fMax, float fMaxPassive, float fPreferred)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetMaxRangedAttackDistanceOverride");

            NWNCore.NativeFunctions.nwnxPushFloat(fPreferred);
            NWNCore.NativeFunctions.nwnxPushFloat(fMaxPassive);
            NWNCore.NativeFunctions.nwnxPushFloat(fMax);
            NWNCore.NativeFunctions.nwnxPushInt((int)nBaseItem);

            NWNCore.NativeFunctions.nwnxCallFunction();
        }
    }
}