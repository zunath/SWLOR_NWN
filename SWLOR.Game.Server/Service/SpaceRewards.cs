using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    public static partial class Space
    {
        private const string CreditReceiptVariable = "SPACE_REWARD_PAID";
        private static string EncounterId(ShipStatus status) => status?.EncounterProfile == null ? null : "space-encounter-" + status.FlightId;
        private static void RegisterSpaceEncounter(uint creature, ShipStatus status)
        {
            var profile = SpaceEncounterCatalog.Default.Profiles[status.EncounterProfile];
            DB.Set(new SpaceEncounter { Id = EncounterId(status), AreaResref = GetResRef(GetArea(creature)), Profile = profile.Id,
                ActivityId = string.IsNullOrWhiteSpace(GetLocalString(creature, "SPACE_ACTIVITY_ID")) ? null : GetLocalString(creature, "SPACE_ACTIVITY_ID"),
                Contributions = new() { RemainingDamage = profile.Hull + profile.Shield } });
        }
        private static void TouchOperatingClock(uint player, DateTime now, double seconds)
        {
            if (!GetIsPC(player) || GetIsDM(player)) return;
            var record = DB.Get<Player>(GetObjectUUID(player));
            record.SpaceExperience.Touch(now, now.AddSeconds(Math.Clamp(seconds, 0, 10)));
            DB.Set(record);
        }
        private static void RecordSpaceDamage(uint source, uint target, ShipStatus sourceStatus, ShipStatus targetStatus, double shield, double hull)
        {
            if (source == target || !(GetIsEnemy(source, target) || GetIsEnemy(target, source))) return;
            if (GetIsPC(source) && EncounterId(targetStatus) is string targetId)
            {
                var encounter = DB.Get<SpaceEncounter>(targetId);
                if (encounter != null && !encounter.Completed)
                { encounter.Contributions.CreditDamage(GetObjectUUID(source), shield + hull); DB.Set(encounter); }
            }
            if (GetIsPC(target) && EncounterId(sourceStatus) is string sourceId)
            {
                var encounter = DB.Get<SpaceEncounter>(sourceId);
                if (encounter == null || encounter.Completed) return;
                var playerId = GetObjectUUID(target);
                encounter.Contributions.Credit(playerId, SkillType.Piloting, shield + hull);
                if (!targetStatus.HostileDamageDebt.TryGetValue(sourceId, out var debt)) targetStatus.HostileDamageDebt[sourceId] = debt = new();
                debt.Shield += shield; debt.Hull += hull;
                TouchOperatingClock(target, DateTime.UtcNow, SpaceEncounterCatalog.Default.Profiles[sourceStatus.EncounterProfile].Cycle);
                DB.Set(encounter);
            }
        }
        private static void RecordSpaceEvasion(uint source, uint target, ShipStatus sourceStatus, double avoided)
        {
            if (!GetIsPC(target) || GetIsDM(target) || EncounterId(sourceStatus) is not string id) return;
            var encounter = DB.Get<SpaceEncounter>(id);
            if (encounter == null || encounter.Completed) return;
            encounter.Contributions.Credit(GetObjectUUID(target), SkillType.Piloting, avoided);
            TouchOperatingClock(target, DateTime.UtcNow, SpaceEncounterCatalog.Default.Profiles[sourceStatus.EncounterProfile].Cycle);
            DB.Set(encounter);
        }
        private static void RecordSpaceRecovery(uint source, uint target, ShipStatus status, ShipResource pool, double actual)
        {
            if (!GetIsObjectValid(source) || !GetIsPC(source) || GetIsDM(source) || actual <= 0 || pool == ShipResource.Capacitor) return;
            var remaining = actual;
            foreach (var pair in status.HostileDamageDebt.OrderBy(x => x.Key, StringComparer.Ordinal).ToArray())
            {
                var encounter = DB.Get<SpaceEncounter>(pair.Key);
                if (encounter == null || encounter.Completed) { status.HostileDamageDebt.Remove(pair.Key); continue; }
                var recovered = pair.Value.Recover(pool, remaining);
                if (recovered > 0)
                { encounter.Contributions.Credit(GetObjectUUID(source), SkillType.ShipSystems, recovered); DB.Set(encounter); remaining -= recovered; }
                if (pair.Value.Hull + pair.Value.Shield <= 1e-9) status.HostileDamageDebt.Remove(pair.Key);
                if (remaining <= 1e-9) break;
            }
        }
        private static void RecordSpaceEnergy(uint source, uint recipient, double paid, double actual)
        {
            if (!GetIsPC(source) || !GetIsPC(recipient) || source == recipient || actual <= 0) return;
            var target = GetShipStatus(recipient);
            foreach (var id in target.HostileDamageDebt.Keys.OrderBy(x => x, StringComparer.Ordinal).ToArray())
            {
                var encounter = DB.Get<SpaceEncounter>(id);
                if (encounter == null || encounter.Completed) continue;
                encounter.Contributions.CreditEnergy(GetObjectUUID(source), GetObjectUUID(recipient), paid, actual);
                DB.Set(encounter); break;
            }
        }
        [NWNEventHandler(ScriptName.OnCreatureDeathBefore)]
        public static void CompleteSpaceEncounter()
        {
            var creature = OBJECT_SELF;
            var status = GetShipStatus(creature);
            if (EncounterId(status) is not string id || status.Hull > 0) return;
            var encounter = DB.Get<SpaceEncounter>(id);
            if (encounter == null || encounter.Completed) return;
            var profile = SpaceEncounterCatalog.Default.Profiles[encounter.Profile];
            // Freeze contribution before any reward is delivered. Stable entitlement IDs recover interrupted settlement.
            var position = GetPosition(creature); encounter.X = position.X; encounter.Y = position.Y; encounter.Z = position.Z;
            encounter.Completed = true; encounter.CompletedAt = DateTime.UtcNow;
            DB.Set(encounter);
            CreateEncounterWreck(encounter, profile);
            CreateEncounterRewards(encounter, profile);
        }
        private static void CreateEncounterRewards(SpaceEncounter encounter, SpaceEncounterProfile profile)
        {
            if (encounter.RewardJournalComplete) return;
            if (encounter.ActivityId != null) { encounter.RewardJournalComplete = true; DB.Set(encounter); return; }
            var ledger = encounter.Contributions;
            var credits = ledger.Allocate(profile.RewardCredits);
            var skillPools = new Dictionary<SkillType, int> { [SkillType.Piloting] = (int)Math.Floor(profile.Experience * .25),
                [SkillType.Gunnery] = (int)Math.Floor(profile.Experience * .65), [SkillType.ShipSystems] = (int)Math.Floor(profile.Experience * .10) };
            var experience = skillPools.ToDictionary(x => x.Key, x => ledger.Allocate(x.Key == SkillType.ShipSystems && ledger.Participants.Values.Any(p => p.EnergyPoints > 0) ? x.Value - (int)Math.Floor(x.Value * .1) : x.Value, x.Key));
            var energyPool = ledger.Participants.Values.Any(p => p.EnergyPoints > 0) ? (int)Math.Floor(skillPools[SkillType.ShipSystems] * .1) : 0;
            var energy = ledger.Allocate(energyPool, SkillType.ShipSystems, true);
            foreach (var playerId in ledger.Participants.Keys)
            {
                var rewardId = encounter.Id + "/" + playerId;
                if (DB.Get<SpaceReward>(rewardId) != null) continue;
                DB.Set(new SpaceReward { Id = rewardId, PlayerId = playerId, Credits = credits.GetValueOrDefault(playerId),
                    Experience = experience.ToDictionary(x => x.Key, x => x.Value.GetValueOrDefault(playerId) + (x.Key == SkillType.ShipSystems ? energy.GetValueOrDefault(playerId) : 0)) });
            }
            encounter.RewardJournalComplete = true; DB.Set(encounter);
        }
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void LoadSpaceRewards()
        {
            // Complete journals whose event was interrupted after freezing the finite pool.
            foreach (var encounter in DB.Search(new DBQuery<SpaceEncounter>().AddFieldSearch(nameof(SpaceEncounter.Completed), true).AddFieldSearch(nameof(SpaceEncounter.RewardJournalComplete), false)).ToArray())
            {
                var profile = SpaceEncounterCatalog.Default.Profiles[encounter.Profile];
                CreateEncounterWreck(encounter, profile);
                CreateEncounterRewards(encounter, profile);
            }
            Scheduler.ScheduleRepeating(ProcessSpaceRewards, TimeSpan.FromSeconds(5));
        }
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void RecoverSpaceRewardsOnLogin()
        { var player = GetEnteringObject(); if (GetIsPC(player) && !GetIsDM(player)) SettleSpaceRewards(player); }
        private static void ProcessSpaceRewards()
        {
            foreach (var encounter in DB.Search(new DBQuery<SpaceEncounter>().AddFieldSearch(nameof(SpaceEncounter.Completed), true).AddFieldSearch(nameof(SpaceEncounter.RewardJournalComplete), false)).ToArray())
            {
                var profile = SpaceEncounterCatalog.Default.Profiles[encounter.Profile];
                CreateEncounterWreck(encounter, profile); CreateEncounterRewards(encounter, profile);
            }
            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
                if (!GetIsDM(player) && !GetIsDead(player)) SettleSpaceRewards(player);
        }
        private static void SettleSpaceRewards(uint player)
        {
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDead(player)) return;
            var playerId = GetObjectUUID(player);
            var query = new DBQuery<SpaceReward>().AddFieldSearch(nameof(SpaceReward.PlayerId), playerId, false)
                .AddFieldSearch(nameof(SpaceReward.Settled), false);
            foreach (var reward in DB.Search(query).OrderBy(x => x.DateCreated).Take(8).ToArray())
            {
                if (!reward.CreditsSettled)
                {
                    if (reward.Credits > 0 && GetLocalString(player, CreditReceiptVariable) != reward.Id)
                    {
                        GiveGoldToCreature(player, reward.Credits);
                        SetLocalString(player, CreditReceiptVariable, reward.Id);
                        ExportSingleCharacter(player);
                    }
                    reward.CreditsSettled = true; DB.Set(reward);
                }
                foreach (var (skill, amount) in reward.Experience.Where(x => x.Value > 0))
                    Skill.GiveSkillXP(player, skill, amount, applyHenchmanPenalty: false, receiptId: reward.Id + "/" + skill,
                        settleBaseXP: (record, requested) => record.SpaceExperience.ClaimOperating(requested));
                var settled = DB.Get<Player>(playerId).SkillXPReceipts;
                if (reward.Experience.Where(x => x.Value > 0).All(x => settled.ContainsKey(reward.Id + "/" + x.Key)))
                { reward.Settled = true; DB.Set(reward); }
            }
        }
    }
}
