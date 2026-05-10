using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    public class _28_SplitDefensesAndResistances : ServerMigrationBase, IServerMigration
    {
        public int Version => 28;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostDatabaseLoad;

        public void Migrate()
        {
            var query = new DBQuery<Player>();
            var count = (int)DB.SearchCount(query);
            var players = DB.SearchRawJson(query.AddPaging(count, 0));
            var migratedCount = 0;

            foreach (var rawPlayer in players)
            {
                var jObject = JObject.Parse(rawPlayer);
                if (!SplitDefensesAndResistances(jObject))
                    continue;

                var player = jObject.ToObject<Player>();
                DB.Set(player);
                migratedCount++;
            }

            Log.Write(LogGroup.Migration, $"Split player defenses and resistances for {migratedCount} players.");
        }

        private static bool SplitDefensesAndResistances(JObject player)
        {
            var migrated = false;
            var defenses = player[nameof(Player.Defenses)] as JObject;
            var resistances = player[nameof(Player.Resistances)] as JObject;

            if (defenses == null)
            {
                defenses = new JObject();
                player[nameof(Player.Defenses)] = defenses;
                migrated = true;
            }

            if (resistances == null)
            {
                resistances = new JObject();
                player[nameof(Player.Resistances)] = resistances;
                migrated = true;
            }

            migrated |= MoveDefenseValue(defenses, resistances, CombatDamageType.Physical);
            migrated |= MoveDefenseValue(defenses, resistances, CombatDamageType.Force);

            migrated |= MoveLegacyElementalDefense(defenses, resistances, CombatDamageType.Fire, ResistanceType.Fire);
            migrated |= MoveLegacyElementalDefense(defenses, resistances, CombatDamageType.Poison, ResistanceType.Poison);
            migrated |= MoveLegacyElementalDefense(defenses, resistances, CombatDamageType.Electrical, ResistanceType.Electrical);
            migrated |= MoveLegacyElementalDefense(defenses, resistances, CombatDamageType.Ice, ResistanceType.Ice);

            migrated |= NormalizeLegacyElementalResistance(resistances, CombatDamageType.Fire, ResistanceType.Fire);
            migrated |= NormalizeLegacyElementalResistance(resistances, CombatDamageType.Poison, ResistanceType.Poison);
            migrated |= NormalizeLegacyElementalResistance(resistances, CombatDamageType.Electrical, ResistanceType.Electrical);
            migrated |= NormalizeLegacyElementalResistance(resistances, CombatDamageType.Ice, ResistanceType.Ice);

            migrated |= RemoveResistanceKeys(resistances, CombatDamageType.Physical);
            migrated |= RemoveResistanceKeys(resistances, CombatDamageType.Force);

            foreach (var type in Enum.GetValues(typeof(CombatDamageType)).Cast<CombatDamageType>())
            {
                if (!type.IsDefenseDamageType())
                    continue;

                migrated |= NormalizeDefenseValue(defenses, type);

                if (defenses[type.ToString()] != null)
                    continue;

                defenses[type.ToString()] = 0;
                migrated = true;
            }

            foreach (var type in Resistance.GetAllResistanceTypes())
            {
                if (resistances[type.ToString()] != null)
                    continue;

                resistances[type.ToString()] = 0;
                migrated = true;
            }

            return migrated;
        }

        private static bool MoveDefenseValue(JObject defenses, JObject resistances, CombatDamageType type)
        {
            var migrated = false;
            var key = type.ToString();
            var resistanceToken = GetToken(resistances, key, (int)type);
            var defenseToken = GetToken(defenses, key, (int)type);

            if (defenseToken == null && resistanceToken != null)
            {
                defenses[key] = resistanceToken.DeepClone();
                migrated = true;
            }

            return migrated;
        }

        private static bool NormalizeDefenseValue(JObject defenses, CombatDamageType type)
        {
            var migrated = false;
            var key = type.ToString();
            var numericKey = ((int)type).ToString();
            var token = defenses[key] ?? defenses[numericKey];

            if (defenses[key] == null && token != null)
            {
                defenses[key] = token.DeepClone();
                migrated = true;
            }

            if (defenses.Remove(numericKey))
                migrated = true;

            return migrated;
        }

        private static bool MoveLegacyElementalDefense(
            JObject defenses,
            JObject resistances,
            CombatDamageType legacyType,
            ResistanceType resistanceType)
        {
            var migrated = false;
            var legacyToken = GetToken(defenses, legacyType.ToString(), (int)legacyType);
            var resistanceToken = resistances[resistanceType.ToString()] ??
                                  resistances[legacyType.ToString()] ??
                                  resistances[((int)legacyType).ToString()];

            if (resistances[resistanceType.ToString()] == null && resistanceToken != null)
            {
                resistances[resistanceType.ToString()] = resistanceToken.DeepClone();
                migrated = true;
            }
            else if (resistanceToken == null && legacyToken != null)
            {
                resistances[resistanceType.ToString()] = legacyToken.DeepClone();
                migrated = true;
            }

            foreach (var key in new[] { legacyType.ToString(), ((int)legacyType).ToString() })
            {
                if (defenses.Remove(key))
                    migrated = true;
            }

            return migrated;
        }

        private static bool NormalizeLegacyElementalResistance(
            JObject resistances,
            CombatDamageType legacyType,
            ResistanceType resistanceType)
        {
            var migrated = false;
            var key = resistanceType.ToString();
            var legacyNumericKey = ((int)legacyType).ToString();
            var token = resistances[key] ?? resistances[legacyType.ToString()] ?? resistances[legacyNumericKey];

            if (resistances[key] == null && token != null)
            {
                resistances[key] = token.DeepClone();
                migrated = true;
            }

            if (resistances.Remove(legacyNumericKey))
                migrated = true;

            return migrated;
        }

        private static bool RemoveResistanceKeys(JObject resistances, CombatDamageType type)
        {
            var migrated = false;

            foreach (var key in new[] { type.ToString(), ((int)type).ToString() })
            {
                if (resistances.Remove(key))
                    migrated = true;
            }

            return migrated;
        }

        private static JToken GetToken(JObject obj, string name, int value)
        {
            return obj[name] ?? obj[value.ToString()];
        }
    }
}
