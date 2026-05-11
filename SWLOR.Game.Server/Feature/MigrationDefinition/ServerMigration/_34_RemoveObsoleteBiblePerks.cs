using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    public class _34_RemoveObsoleteBiblePerks : ServerMigrationBase, IServerMigration
    {
        private static readonly Dictionary<PerkType, int[]> PlayerRemovedPerks = new()
        {
            { PerkType.DemolitionExpert, new[] { 1, 2, 3 } },
            { PerkType.FlashbangGrenade, new[] { 2, 3, 3 } },
            { PerkType.KoltoGrenade, new[] { 2, 3, 3 } },
            { PerkType.KoltoBomb, new[] { 2, 3, 3 } },
            { PerkType.IncendiaryBomb, new[] { 2, 3, 3 } },
            { PerkType.GasBomb, new[] { 2, 3, 3 } },
            { PerkType.StealthGenerator, new[] { 2, 3, 3 } },

            { PerkType.RangedHealing, new[] { 2, 3, 4, 5 } },
            { PerkType.FrugalMedic, new[] { 1, 2, 2 } },
            { PerkType.KoltoRecovery, new[] { 3, 4, 5 } },
            { PerkType.StasisField, new[] { 2, 3, 4 } },
            { PerkType.CombatEnhancement, new[] { 3, 3, 4 } },

            { PerkType.ForceHeal, new[] { 2, 2, 2, 3, 3 } },
            { PerkType.ForceBurst, new[] { 2, 2, 3, 3 } },
            { PerkType.Disturbance, new[] { 2, 2, 2 } },
            { PerkType.ForceValor, new[] { 2, 3 } },
            { PerkType.ThrowRock, new[] { 1, 2, 2, 2, 3 } },
            { PerkType.BurstOfSpeed, new[] { 2, 2 } },
            { PerkType.ThrowLightsaber, new[] { 2, 2, 2 } },
            { PerkType.ForceStun, new[] { 2, 2, 3 } },
            { PerkType.BattleInsight, new[] { 2, 2 } },
            { PerkType.ForceMind, new[] { 3, 4 } },
            { PerkType.Premonition, new[] { 2, 2 } },
            { PerkType.ForceInspiration, new[] { 2, 3, 4 } },
        };

        private static readonly Dictionary<PerkType, (int MaxLevel, int[] PricesByLevel)> PlayerTrimmedPerks = new()
        {
            { PerkType.IonGrenade, (2, new[] { 2, 3, 3 }) },
            { PerkType.AdhesiveGrenade, (2, new[] { 2, 3, 3 }) },
            { PerkType.MedKit, (4, new[] { 1, 2, 3, 4, 4 }) },
            { PerkType.Resuscitation, (2, new[] { 4, 4, 4 }) },
            { PerkType.Shielding, (3, new[] { 2, 3, 3, 4 }) },
        };

        private static readonly Dictionary<PerkType, int[]> BeastRemovedPerks = new()
        {
            { PerkType.FlameBreath, new[] { 2, 2, 2, 3, 3 } },
            { PerkType.ShockingSlash, new[] { 1, 1, 1, 2, 2 } },
            { PerkType.DiseasedTouch, new[] { 2, 2, 2, 2, 2 } },
            { PerkType.Clip, new[] { 2, 2, 2, 2, 2 } },
            { PerkType.SpinningClaw, new[] { 2, 2, 2, 2, 2 } },
            { PerkType.BeastSpeed, new[] { 3, 3, 3 } },
            { PerkType.BolsterArmor, new[] { 1, 1, 1, 2, 2 } },
        };

        private static readonly Dictionary<PerkType, (int MaxLevel, int[] PricesByLevel)> BeastTrimmedPerks = new()
        {
            { PerkType.Bite, (3, new[] { 1, 1, 1, 2, 2 }) },
            { PerkType.Claw, (3, new[] { 1, 1, 1, 2, 2 }) },
            { PerkType.BolsterAttack, (3, new[] { 1, 1, 1, 2, 2 }) },
            { PerkType.Hasten, (2, new[] { 4, 4, 4 }) },
            { PerkType.PoisonBreath, (3, new[] { 1, 1, 1, 2, 2 }) },
            { PerkType.IceBreath, (3, new[] { 1, 1, 1, 2, 2 }) },
            { PerkType.EvasiveManeuver, (3, new[] { 1, 1, 1, 2, 2 }) },
            { PerkType.Assault, (3, new[] { 1, 1, 1, 2, 2 }) },
            { PerkType.ForceTouch, (3, new[] { 1, 1, 1, 2, 2 }) },
            { PerkType.Innervate, (3, new[] { 1, 1, 1, 2, 2 }) },
            { PerkType.Anger, (2, new[] { 1, 1, 1, 2, 2 }) },
            { PerkType.FocusAttention, (3, new[] { 2, 2, 2, 3, 3 }) },
        };

        private static readonly HashSet<RecastGroup> ObsoleteRecastGroups = new()
        {
            RecastGroup.BurstOfSpeed,
            RecastGroup.ForceHeal,
            RecastGroup.ThrowLightsaber,
            RecastGroup.ForceStun,
            RecastGroup.BattleInsight,
            RecastGroup.ForceBurst,
            RecastGroup.ForceMind,
            RecastGroup.KoltoRecovery,
            RecastGroup.StasisField,
            RecastGroup.CombatEnhancement,
            RecastGroup.StealthGenerator,
            RecastGroup.Premonition,
            RecastGroup.Disturbance,
            RecastGroup.ForceValor,
            RecastGroup.ThrowRock,
            RecastGroup.ForceInspiration,
            RecastGroup.FlashbangGrenade,
            RecastGroup.KoltoGrenade,
            RecastGroup.KoltoBomb,
            RecastGroup.IncendiaryBomb,
            RecastGroup.GasBomb,
            RecastGroup.DiseasedTouch,
            RecastGroup.Clip,
            RecastGroup.SpinningClaw,
            RecastGroup.FlameBreath,
            RecastGroup.ShockingSlash,
            RecastGroup.BolsterArmor,
        };

        public int Version => 34;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostDatabaseLoad;

        public void Migrate()
        {
            var (playersMigrated, playerSpRefunded) = MigratePlayers();
            var (beastsMigrated, beastSpRefunded) = MigrateBeasts();

            Log.Write(
                LogGroup.Migration,
                $"Removed obsolete Bible perks from {playersMigrated} players and {beastsMigrated} beasts. Refunded {playerSpRefunded} player SP and {beastSpRefunded} beast SP.");
        }

        private static (int EntityCount, int SpRefunded) MigratePlayers()
        {
            var query = new DBQuery<Player>();
            var playerCount = (int)DB.SearchCount(query);
            var players = DB.Search(query.AddPaging(playerCount, 0));
            var migratedCount = 0;
            var totalRefund = 0;

            foreach (var player in players)
            {
                var refund = CleanPerks(player.Perks, PlayerRemovedPerks, PlayerTrimmedPerks, out var changed);
                changed |= RemoveUnlockedPerks(player);
                changed |= RemoveRecastTimes(player);

                if (refund > 0)
                {
                    player.UnallocatedSP += refund;
                    totalRefund += refund;
                    changed = true;
                }

                if (!changed)
                    continue;

                DB.Set(player);
                migratedCount++;
            }

            return (migratedCount, totalRefund);
        }

        private static (int EntityCount, int SpRefunded) MigrateBeasts()
        {
            var query = new DBQuery<Beast>();
            var beastCount = (int)DB.SearchCount(query);
            var beasts = DB.Search(query.AddPaging(beastCount, 0));
            var migratedCount = 0;
            var totalRefund = 0;

            foreach (var beast in beasts)
            {
                var refund = CleanPerks(beast.Perks, BeastRemovedPerks, BeastTrimmedPerks, out var changed);

                if (refund > 0)
                {
                    beast.UnallocatedSP += refund;
                    totalRefund += refund;
                    changed = true;
                }

                if (!changed)
                    continue;

                DB.Set(beast);
                migratedCount++;
            }

            return (migratedCount, totalRefund);
        }

        private static int CleanPerks(
            Dictionary<PerkType, int> perks,
            Dictionary<PerkType, int[]> removedPerks,
            Dictionary<PerkType, (int MaxLevel, int[] PricesByLevel)> trimmedPerks,
            out bool changed)
        {
            changed = false;
            if (perks == null)
                return 0;

            var refund = 0;

            foreach (var (perkType, pricesByLevel) in removedPerks)
            {
                if (!perks.TryGetValue(perkType, out var purchasedLevel))
                    continue;

                refund += CalculateRefund(pricesByLevel, 1, purchasedLevel);
                perks.Remove(perkType);
                changed = true;
            }

            foreach (var (perkType, trim) in trimmedPerks)
            {
                if (!perks.TryGetValue(perkType, out var purchasedLevel) ||
                    purchasedLevel <= trim.MaxLevel)
                    continue;

                refund += CalculateRefund(trim.PricesByLevel, trim.MaxLevel + 1, purchasedLevel);
                perks[perkType] = trim.MaxLevel;
                changed = true;
            }

            return refund;
        }

        private static int CalculateRefund(int[] pricesByLevel, int fromLevel, int purchasedLevel)
        {
            var refund = 0;
            var maxLevel = purchasedLevel > pricesByLevel.Length
                ? pricesByLevel.Length
                : purchasedLevel;

            for (var level = fromLevel; level <= maxLevel; level++)
            {
                refund += pricesByLevel[level - 1];
            }

            return refund;
        }

        private static bool RemoveUnlockedPerks(Player player)
        {
            if (player.UnlockedPerks == null)
                return false;

            var changed = false;
            foreach (var perkType in PlayerRemovedPerks.Keys)
            {
                changed |= player.UnlockedPerks.Remove(perkType);
            }

            return changed;
        }

        private static bool RemoveRecastTimes(Player player)
        {
            if (player.RecastTimes == null)
                return false;

            var changed = false;
            foreach (var recastGroup in ObsoleteRecastGroups)
            {
                changed |= player.RecastTimes.Remove(recastGroup);
            }

            return changed;
        }
    }
}
