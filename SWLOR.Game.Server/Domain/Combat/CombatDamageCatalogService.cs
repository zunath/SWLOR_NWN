using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CombatService;
using static SWLOR.Game.Server.Domain.Combat.CombatRules;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The damage type tables of <c>Combat</c>, as this instance's own fields, and the login step that adds missing defense
    /// and resistance entries to a player. <c>CreateDefaultDefenseValues</c> is pure, so it is in <see cref="CombatRules"/>.
    /// </summary>
    public class CombatDamageCatalogService : ICombatDamageCatalogService
    {
        private readonly List<CombatDamageType> _allValidDamageTypes = new();
        private readonly List<CombatDamageType> _allDefenseDamageTypes = new();
        private bool _damageTypesCached;

        private readonly IDBService _db;
        private readonly IResistanceService _resistance;

        public CombatDamageCatalogService(IDBService db, IResistanceService resistance)
        {
            _db = db;
            _resistance = resistance;
        }

        /// <summary>
        /// Cache all valid character and defense damage types before module load.
        /// </summary>
        public void LoadDamageTypes()
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
        public void AddDamageTypeResistances()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var foundNewType = false;
            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
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
                dbPlayer.Resistances = _resistance.CreateDefaultResistanceValues();
            }

            foundNewType |= EnsureDefenseValues(dbPlayer.Defenses);

            foreach (var type in _resistance.GetAllResistanceTypes())
            {
                if (!dbPlayer.Resistances.ContainsKey(type))
                {
                    foundNewType = true;
                    dbPlayer.Resistances[type] = 0;
                }
            }

            if (foundNewType)
            {
                _db.Set(dbPlayer);
            }
        }

        /// <summary>
        /// Retrieves all valid damage types available in the system.
        /// </summary>
        /// <returns>A list of damage types</returns>
        public IReadOnlyList<CombatDamageType> GetAllDamageTypes()
        {
            EnsureDamageTypesCached();
            return _allValidDamageTypes;
        }

        /// <summary>
        /// Retrieves all damage types which use a defense rating.
        /// </summary>
        /// <returns>A list of defense damage types</returns>
        public IReadOnlyList<CombatDamageType> GetDefenseDamageTypes()
        {
            EnsureDamageTypesCached();
            return _allDefenseDamageTypes;
        }

        public bool EnsureDefenseValues(Dictionary<CombatDamageType, int> defenses, int defaultValue = 0)
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

        private void EnsureDamageTypesCached()
        {
            if (!_damageTypesCached)
            {
                LoadDamageTypes();
            }
        }
    }
}
