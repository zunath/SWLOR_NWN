using System.Collections.Generic;
using System.Linq;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.ActivityService;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatRecorderService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using ObjectPlugin = SWLOR.NWN.API.NWNX.ObjectPlugin;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;

namespace SWLOR.Game.Server.Service
{
    public static partial class Combat
    {
        private static readonly List<CombatDamageType> _allValidDamageTypes = new();
        private static readonly List<CombatDamageType> _allDefenseDamageTypes = new();
        private static bool _damageTypesCached;

        /// <summary>
        /// Cache all valid character and defense damage types before module load.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void LoadDamageTypes()
        {
            _allValidDamageTypes.Clear();
            _allDefenseDamageTypes.Clear();

            var allValues = Enum.GetValues(typeof(CombatDamageType)).Cast<CombatDamageType>();

            foreach (var type in allValues)
            {
                if (type.IsCharacterDamageType())
                    _allValidDamageTypes.Add(type);

                if (type.IsDefenseDamageType())
                    _allDefenseDamageTypes.Add(type);
            }

            _damageTypesCached = true;
        }

        /// <summary>
        /// When a player enters the server, apply any defense and resistance entries they don't already have.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void AddDamageTypeResistances()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var foundNewType = false;
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            if (dbPlayer == null)
                return;

            if (dbPlayer.Defenses == null)
            {
                foundNewType = true;
                dbPlayer.Defenses = CreateDefaultDefenseValues();
            }

            if (dbPlayer.Resistances == null)
            {
                foundNewType = true;
                dbPlayer.Resistances = Resistance.CreateDefaultResistanceValues();
            }

            foundNewType |= EnsureDefenseValues(dbPlayer.Defenses);

            foreach (var type in Resistance.GetAllResistanceTypes())
            {
                if (!dbPlayer.Resistances.ContainsKey(type))
                {
                    foundNewType = true;
                    dbPlayer.Resistances[type] = 0;
                }
            }

            if (foundNewType)
            {
                DB.Set(dbPlayer);
            }
        }

        /// <summary>
        /// Retrieves all valid damage types available in the system.
        /// </summary>
        /// <returns>A list of damage types</returns>
        public static IReadOnlyList<CombatDamageType> GetAllDamageTypes()
        {
            EnsureDamageTypesCached();
            return _allValidDamageTypes;
        }

        /// <summary>
        /// Retrieves all damage types which use a defense rating.
        /// </summary>
        /// <returns>A list of defense damage types</returns>
        public static IReadOnlyList<CombatDamageType> GetDefenseDamageTypes()
        {
            EnsureDamageTypesCached();
            return _allDefenseDamageTypes;
        }

        public static Dictionary<CombatDamageType, int> CreateDefaultDefenseValues(int defaultValue = 0)
        {
            return GetDefenseDamageTypes()
                .ToDictionary(type => type, _ => defaultValue);
        }

        public static bool EnsureDefenseValues(Dictionary<CombatDamageType, int> defenses, int defaultValue = 0)
        {
            if (defenses == null)
                throw new ArgumentNullException(nameof(defenses));

            var foundNewType = false;
            foreach (var type in GetDefenseDamageTypes())
            {
                if (defenses.ContainsKey(type))
                    continue;

                defenses[type] = defaultValue;
                foundNewType = true;
            }

            return foundNewType;
        }

        private static void EnsureDamageTypesCached()
        {
            if (!_damageTypesCached)
            {
                LoadDamageTypes();
            }
        }
    }
}
