using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Items;

namespace SWLOR.Game.Server.Feature.MigrationDefinition
{
    /// <summary>
    /// What a character gets after the login guard (<see cref="IMigrationService.AdmitLogin"/>) lets it in: the overflow
    /// storage reminder, then the base attributes and native feats its record holds, combat readiness, maximum hit
    /// points and equipment stats. It calls the Items and Character services, which sit above the migration service, so
    /// it is content that the migration service does not name.
    /// </summary>
    public class CharacterLoginRestoration
    {
        private IMigrationService Migration { get; }
        private IOverflowStorageService OverflowStorage { get; }
        private IPerkService Perk { get; }
        private ICreatureStatService CreatureStat { get; }
        private IStatRecordService StatRecord { get; }

        public CharacterLoginRestoration(IMigrationService migration, IOverflowStorageService overflowStorage, IPerkService perk, ICreatureStatService creatureStat, IStatRecordService statRecord)
        {
            Migration = migration;
            OverflowStorage = overflowStorage;
            Perk = perk;
            CreatureStat = creatureStat;
            StatRecord = statRecord;
        }

        [NWNEventHandler(ScriptName.OnCharacterInitAfter)]
        public void AfterCharacterInitialization()
        {
            var player = GetEnteringObject();
            if (!Migration.AdmitLogin(player))
                return;

            OverflowStorage.NotifyOnLogin(player);

            // Restore the base attributes and native feats the record holds, which saved characters may lack.
            RestorePlayerAttributes(player);
            RestorePlayerFeats(player);
            // Saved equipment does not emit equip events on login.
            RefreshCombatReadinessEquipment(player);
            RefreshPlayerMaxHP(player);
            RefreshPlayerEquipmentStats(player);
        }

        /// <summary>Restores the base attributes the record holds. The stat service reads the record and the engine.</summary>
        protected virtual void RestorePlayerAttributes(uint player)
        {
            CreatureStat.RestorePlayerAttributes(player);
        }

        /// <summary>Restores the native feats the record holds. The perk service reads the record and the engine.</summary>
        protected virtual void RestorePlayerFeats(uint player)
        {
            Perk.RestorePlayerFeats(player);
        }

        /// <summary>Reconciles combat readiness with the worn equipment. The stat service reads the record and the engine.</summary>
        protected virtual void RefreshCombatReadinessEquipment(uint player)
        {
            StatRecord.RefreshCombatReadinessEquipment(player);
        }

        /// <summary>Recomputes the maximum hit points. The stat service reads the record and the engine.</summary>
        protected virtual void RefreshPlayerMaxHP(uint player)
        {
            CreatureStat.RefreshPlayerMaxHP(player);
        }

        /// <summary>Recomputes the equipment stats on the record. The stat service reads the record and the engine.</summary>
        protected virtual void RefreshPlayerEquipmentStats(uint player)
        {
            StatRecord.RefreshPlayerEquipmentStats(player);
        }
    }
}
