using System;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    public class _33_MoveBeastElementalPuritiesToResistances : ServerMigrationBase, IServerMigration
    {
        public int Version => 33;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostDatabaseLoad;

        public void Migrate()
        {
            var beastCount = MigrateEntities<Beast>();
            var incubationJobCount = MigrateEntities<IncubationJob>();

            Log.Write(
                LogGroup.Migration,
                $"Moved beast elemental purities from defenses to resistances for {beastCount} beasts and {incubationJobCount} incubation jobs.");
        }

        private static int MigrateEntities<TEntity>()
            where TEntity : EntityBase
        {
            var query = new DBQuery<TEntity>();
            var count = (int)DB.SearchCount(query);
            var entities = DB.SearchRawJson(query.AddPaging(count, 0));
            var migratedCount = 0;

            foreach (var rawEntity in entities)
            {
                var jObject = JObject.Parse(rawEntity);
                if (!MigratePurities(jObject))
                    continue;

                var entity = jObject.ToObject<TEntity>();
                DB.Set(entity);
                migratedCount++;
            }

            return migratedCount;
        }

        private static bool MigratePurities(JObject entity)
        {
            var migrated = false;
            var defensePurities = GetOrCreateObject(entity, nameof(Beast.DefensePurities), ref migrated);
            var resistancePurities = GetOrCreateObject(entity, nameof(Beast.ResistancePurities), ref migrated);

            migrated |= NormalizeDefensePurity(defensePurities, CombatDamageType.Physical);
            migrated |= NormalizeDefensePurity(defensePurities, CombatDamageType.Force);

            migrated |= MoveDefensePurityToResistance(defensePurities, resistancePurities, CombatDamageType.Fire, ResistanceType.Fire);
            migrated |= MoveDefensePurityToResistance(defensePurities, resistancePurities, CombatDamageType.Poison, ResistanceType.Poison);
            migrated |= MoveDefensePurityToResistance(defensePurities, resistancePurities, CombatDamageType.Electrical, ResistanceType.Electrical);
            migrated |= MoveDefensePurityToResistance(defensePurities, resistancePurities, CombatDamageType.Ice, ResistanceType.Ice);

            foreach (var resistanceType in Resistance.GetAllResistanceTypes())
            {
                migrated |= NormalizeResistancePurity(resistancePurities, resistanceType);
            }

            return migrated;
        }

        private static JObject GetOrCreateObject(JObject entity, string propertyName, ref bool migrated)
        {
            if (entity[propertyName] is JObject existing)
                return existing;

            var created = new JObject();
            entity[propertyName] = created;
            migrated = true;

            return created;
        }

        private static bool NormalizeDefensePurity(JObject defensePurities, CombatDamageType type)
        {
            var migrated = false;
            var targetKey = type.ToString();
            var numericKey = ((int)type).ToString();
            var targetToken = defensePurities[targetKey];
            var numericToken = defensePurities[numericKey];

            if (targetToken == null && numericToken != null)
            {
                defensePurities[targetKey] = numericToken.DeepClone();
                migrated = true;
            }
            else if (targetToken != null && numericToken != null)
            {
                defensePurities[targetKey] = Math.Max(GetInt(targetToken), GetInt(numericToken));
                migrated = true;
            }

            if (numericToken != null)
            {
                defensePurities.Remove(numericKey);
                migrated = true;
            }

            if (defensePurities[targetKey] == null)
            {
                defensePurities[targetKey] = 0;
                migrated = true;
            }

            return migrated;
        }

        private static bool MoveDefensePurityToResistance(
            JObject defensePurities,
            JObject resistancePurities,
            CombatDamageType damageType,
            ResistanceType resistanceType)
        {
            var migrated = false;
            var nameKey = damageType.ToString();
            var numericKey = ((int)damageType).ToString();
            var sourceToken = defensePurities[nameKey] ?? defensePurities[numericKey];

            if (sourceToken != null)
            {
                migrated |= MergeResistancePurity(resistancePurities, resistanceType, GetInt(sourceToken));
            }

            if (defensePurities.Remove(nameKey))
                migrated = true;

            if (defensePurities.Remove(numericKey))
                migrated = true;

            return migrated;
        }

        private static bool NormalizeResistancePurity(JObject resistancePurities, ResistanceType type)
        {
            var migrated = false;
            var targetKey = type.ToString();
            var numericKey = ((int)type).ToString();
            var targetToken = resistancePurities[targetKey];
            var numericToken = resistancePurities[numericKey];

            if (targetToken == null && numericToken != null)
            {
                resistancePurities[targetKey] = numericToken.DeepClone();
                migrated = true;
            }
            else if (targetToken != null && numericToken != null)
            {
                resistancePurities[targetKey] = Math.Max(GetInt(targetToken), GetInt(numericToken));
                migrated = true;
            }

            if (numericToken != null)
            {
                resistancePurities.Remove(numericKey);
                migrated = true;
            }

            if (resistancePurities[targetKey] == null)
            {
                resistancePurities[targetKey] = 0;
                migrated = true;
            }

            return migrated;
        }

        private static bool MergeResistancePurity(JObject resistancePurities, ResistanceType type, int value)
        {
            var key = type.ToString();
            var existingValue = GetInt(resistancePurities[key]);
            var newValue = Math.Max(existingValue, value);

            if (resistancePurities[key] != null && existingValue == newValue)
                return false;

            resistancePurities[key] = newValue;
            return true;
        }

        private static int GetInt(JToken token)
        {
            return int.TryParse(token?.ToString(), out var value)
                ? value
                : 0;
        }
    }
}
