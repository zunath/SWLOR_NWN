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
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetWeaponFocusFeat");
            NWNXPInvoke.NWNXPushInt((int)feat);
            NWNXPInvoke.NWNXPushInt((int)baseItem);
            NWNXPInvoke.NWNXCallFunction();
        }

        public static void SetEpicWeaponFocusFeat(BaseItem baseItem, FeatType feat)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetEpicWeaponFocusFeat");
            NWNXPInvoke.NWNXPushInt((int)feat);
            NWNXPInvoke.NWNXPushInt((int)baseItem);
            NWNXPInvoke.NWNXCallFunction();
        }

        public static void SetGreaterWeaponFocusFeat(BaseItem baseItem, FeatType feat)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetGreaterWeaponFocusFeat");
            NWNXPInvoke.NWNXPushInt((int)feat);
            NWNXPInvoke.NWNXPushInt((int)baseItem);
            NWNXPInvoke.NWNXCallFunction();
        }

        public static void SetWeaponFinesseSize(BaseItem baseItem, CreatureSize nCreatureSize)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetWeaponFinesseSize");
            NWNXPInvoke.NWNXPushInt((int)nCreatureSize);
            NWNXPInvoke.NWNXPushInt((int)baseItem);
            NWNXPInvoke.NWNXCallFunction();
        }

        public static void SetWeaponUnarmed(BaseItem baseItem)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetWeaponUnarmed");
            NWNXPInvoke.NWNXPushInt((int)baseItem);
            NWNXPInvoke.NWNXCallFunction();
        }

        public static void SetWeaponIsMonkWeapon(BaseItem baseItem)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetWeaponIsMonkWeapon");
            NWNXPInvoke.NWNXPushInt((int)baseItem);
            NWNXPInvoke.NWNXCallFunction();
        }

        public static void SetWeaponImprovedCriticalFeat(BaseItem baseItem, FeatType feat)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetWeaponImprovedCriticalFeat");
            NWNXPInvoke.NWNXPushInt((int)feat);
            NWNXPInvoke.NWNXPushInt((int)baseItem);
            NWNXPInvoke.NWNXCallFunction();
        }

        public static void SetWeaponSpecializationFeat(BaseItem baseItem, FeatType feat)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetWeaponSpecializationFeat");
            NWNXPInvoke.NWNXPushInt((int)feat);
            NWNXPInvoke.NWNXPushInt((int)baseItem);
            NWNXPInvoke.NWNXCallFunction();
        }

        public static void SetGreaterWeaponSpecializationFeat(BaseItem baseItem, FeatType feat)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetGreaterWeaponSpecializationFeat");
            NWNXPInvoke.NWNXPushInt((int)feat);
            NWNXPInvoke.NWNXPushInt((int)baseItem);
            NWNXPInvoke.NWNXCallFunction();
        }

        public static void SetEpicWeaponSpecializationFeat(BaseItem baseItem, FeatType feat)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetEpicWeaponSpecializationFeat");
            NWNXPInvoke.NWNXPushInt((int)feat);
            NWNXPInvoke.NWNXPushInt((int)baseItem);
            NWNXPInvoke.NWNXCallFunction();
        }

        public static void SetEpicWeaponOverwhelmingCriticalFeat(BaseItem baseItem, FeatType feat)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetEpicWeaponOverwhelmingCriticalFeat");
            NWNXPInvoke.NWNXPushInt((int)feat);
            NWNXPInvoke.NWNXPushInt((int)baseItem);
            NWNXPInvoke.NWNXCallFunction();
        }

        public static void SetEpicWeaponDevastatingCriticalFeat(BaseItem baseItem, FeatType feat)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetEpicWeaponDevastatingCriticalFeat");
            NWNXPInvoke.NWNXPushInt((int)feat);
            NWNXPInvoke.NWNXPushInt((int)baseItem);
            NWNXPInvoke.NWNXCallFunction();
        }

        public static void SetWeaponOfChoiceFeat(BaseItem baseItem, FeatType feat)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetWeaponOfChoiceFeat");
            NWNXPInvoke.NWNXPushInt((int)feat);
            NWNXPInvoke.NWNXPushInt((int)baseItem);
            NWNXPInvoke.NWNXCallFunction();
        }

        public static void SetOption(int nOption, int nVal)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetOption");
            NWNXPInvoke.NWNXPushInt(nVal);
            NWNXPInvoke.NWNXPushInt(nOption);
            NWNXPInvoke.NWNXCallFunction();
        }

        public static void SetDevastatingCriticalEventScript(string sScript)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetDevastatingCriticalEventScript");
            NWNCore.NativeFunctions.nwnxPushString(sScript);
            NWNXPInvoke.NWNXCallFunction();
        }

        public static void BypassDevastatingCritical()
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetEventData");
            NWNXPInvoke.NWNXPushInt(1);
            NWNXPInvoke.NWNXPushInt(DC_Bypass);
            NWNXPInvoke.NWNXCallFunction();
        }

        public static DevastatingCriticalData GetDevastatingCriticalEventData()
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetEventData");
            var data = new DevastatingCriticalData();
            NWNXPInvoke.NWNXPushInt(GetData_DC);
            NWNXPInvoke.NWNXCallFunction();
            data.Weapon = NWNCore.NativeFunctions.nwnxPopObject();
            data.Target = NWNCore.NativeFunctions.nwnxPopObject();
            data.Damage = NWNXPInvoke.NWNXPopInt();
            return data;
        }


        /// @brief Sets weapon to gain .5 strength bonus.
        /// @param oWeapon Should be a melee weapon.
        /// @param nEnable TRUE for bonus. FALSE to turn off bonus.
        /// @param bPersist whether the two hand state should persist to the gff file.
        public static void SetOneHalfStrength(uint oWeapon, bool nEnable, bool bPersist = false)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetOneHalfStrength");

            NWNXPInvoke.NWNXPushInt(bPersist ? 1 : 0);
            NWNXPInvoke.NWNXPushInt(nEnable ? 1 : 0);
            NWNXPInvoke.NWNXPushObject(oWeapon);

            NWNXPInvoke.NWNXCallFunction();
        }

        /// @brief Gets if the weapon is set to gain addition .5 strength bonus
        /// @param oWeapon the weapon
        /// @return FALSE/0 if weapon is not receiving the bonus. TRUE/1 if it does.
        public static int GetOneHalfStrength(uint oWeapon)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetOneHalfStrength");
            NWNXPInvoke.NWNXPushObject(oWeapon);
            NWNXPInvoke.NWNXCallFunction();

            return NWNXPInvoke.NWNXPopInt();
        }

        /// @brief Override the max attack distance of ranged weapons.
        /// @param nBaseItem The baseitem id.
        /// @param fMax The maximum attack distance. Default is 40.0f.
        /// @param fMaxPassive The maximum passive attack distance. Default is 20.0f. Seems to be used by the engine to determine a new nearby target when needed.
        /// @param fPreferred The preferred attack distance. See the PrefAttackDist column in baseitems.2da, default seems to be 30.0f for ranged weapons.
        /// @note fMaxPassive should probably be lower than fMax, half of fMax seems to be a good start. fPreferred should be at least ~0.5f lower than fMax.
        public static void SetMaxRangedAttackDistanceOverride(BaseItem nBaseItem, float fMax, float fMaxPassive, float fPreferred)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetMaxRangedAttackDistanceOverride");

            NWNCore.NativeFunctions.nwnxPushFloat(fPreferred);
            NWNCore.NativeFunctions.nwnxPushFloat(fMaxPassive);
            NWNCore.NativeFunctions.nwnxPushFloat(fMax);
            NWNXPInvoke.NWNXPushInt((int)nBaseItem);

            NWNXPInvoke.NWNXCallFunction();
        }
    }
}