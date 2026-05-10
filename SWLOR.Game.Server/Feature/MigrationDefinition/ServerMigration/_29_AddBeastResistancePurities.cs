using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    public class _29_AddBeastResistancePurities : ServerMigrationBase, IServerMigration
    {
        private const string SavingThrowPuritiesKey = "SavingThrowPurities";
        private const string SavingThrowWillKey = "Will";
        private const string SavingThrowReflexKey = "Reflex";
        private const string SavingThrowFortitudeKey = "Fortitude";
        private const string SavingThrowWillValue = "3";
        private const string SavingThrowReflexValue = "2";
        private const string SavingThrowFortitudeValue = "1";

        public int Version => 29;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostDatabaseLoad;

        public void Migrate()
        {
            var beastCount = MigrateEntities<Beast>();
            var incubationJobCount = MigrateEntities<IncubationJob>();

            Log.Write(LogGroup.Migration, $"Added beast resistance purities for {beastCount} beasts and {incubationJobCount} incubation jobs.");
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
                if (!AddResistancePurities(jObject))
                    continue;

                var entity = jObject.ToObject<TEntity>();
                DB.Set(entity);
                migratedCount++;
            }

            return migratedCount;
        }

        private static bool AddResistancePurities(JObject entity)
        {
            var migrated = false;
            var defensePurities = entity[nameof(Beast.DefensePurities)] as JObject;
            var savingThrowPurities = entity[SavingThrowPuritiesKey] as JObject;
            var resistancePurities = entity[nameof(Beast.ResistancePurities)] as JObject;

            if (resistancePurities == null)
            {
                resistancePurities = new JObject();
                entity[nameof(Beast.ResistancePurities)] = resistancePurities;
                migrated = true;
            }

            migrated |= AddResistancePurity(resistancePurities, ResistanceType.Fire, GetToken(defensePurities, CombatDamageType.Fire));
            migrated |= AddResistancePurity(resistancePurities, ResistanceType.Poison, GetToken(defensePurities, CombatDamageType.Poison));
            migrated |= AddResistancePurity(resistancePurities, ResistanceType.Electrical, GetToken(defensePurities, CombatDamageType.Electrical));
            migrated |= AddResistancePurity(resistancePurities, ResistanceType.Ice, GetToken(defensePurities, CombatDamageType.Ice));
            migrated |= AddResistancePurity(resistancePurities, ResistanceType.Mind, GetToken(savingThrowPurities, SavingThrowWillKey, SavingThrowWillValue));
            migrated |= AddResistancePurity(resistancePurities, ResistanceType.Mobility, GetToken(savingThrowPurities, SavingThrowReflexKey, SavingThrowReflexValue));
            migrated |= AddResistancePurity(resistancePurities, ResistanceType.Trauma, GetToken(savingThrowPurities, SavingThrowFortitudeKey, SavingThrowFortitudeValue));
            migrated |= AddResistancePurity(resistancePurities, ResistanceType.Disruption, GetToken(defensePurities, CombatDamageType.Force));

            return migrated;
        }

        private static bool AddResistancePurity(JObject resistancePurities, ResistanceType type, JToken legacyToken)
        {
            var key = type.ToString();
            if (resistancePurities[key] != null)
                return false;

            resistancePurities[key] = legacyToken?.DeepClone() ?? 0;
            return true;
        }

        private static JToken GetToken(JObject obj, CombatDamageType type)
        {
            return obj?[type.ToString()] ?? obj?[((int)type).ToString()];
        }

        private static JToken GetToken(JObject obj, string nameKey, string numericKey)
        {
            return obj?[nameKey] ?? obj?[numericKey];
        }
    }
}
