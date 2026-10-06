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
        private static readonly DateTime SpaceSessionStartedAt=DateTime.UtcNow;
        private static IEnumerable<SpaceEncounter> EncounterJournals(bool complete,bool? journal=null)
        {
            for(var offset=0;;offset+=100)
            {
                var query=new DBQuery<SpaceEncounter>().AddFieldSearch(nameof(SpaceEncounter.Completed),complete).OrderBy(nameof(SpaceEncounter.Id)).AddPaging(100,offset);
                if(journal.HasValue)query.AddFieldSearch(nameof(SpaceEncounter.RewardJournalComplete),journal.Value);
                var rows=DB.Search(query).ToArray();foreach(var row in rows)yield return row;if(rows.Length<100)yield break;
            }
        }
        private static void CleanSpaceJournals()
        {
            var before=DateTime.UtcNow.AddDays(-1);
            foreach(var encounter in EncounterJournals(true,true).Where(x=>x.CompletedAt<before).Take(100).ToArray())
            {
                var ids=encounter.Contributions.Participants.Keys.Select(x=>encounter.Id+"/"+x).ToArray();
                var rewards=ids.Select(DB.Get<SpaceReward>).Where(x=>x!=null).ToArray();
                if(rewards.Any(x=>!x.Settled))continue;
                foreach(var reward in rewards)DeleteSettledReward(reward);
                DB.Delete<SpaceEncounter>(encounter.Id);
            }
            for(var offset=0;;offset+=100)
            {
                var rows=DB.Search(new DBQuery<SpaceContract>().AddFieldSearch(nameof(SpaceContract.Closed),true).OrderBy(nameof(SpaceContract.Id)).AddPaging(100,offset)).ToArray();
                foreach(var contract in rows.Where(x=>x.ExpiresAt<before).ToArray())
                {
                    var rewards=contract.Participants.Select(x=>DB.Get<SpaceReward>("space-contract/"+contract.Id+"/"+x)).Where(x=>x!=null).ToArray();
                    if(rewards.Any(x=>!x.Settled))continue;
                    foreach(var reward in rewards)DeleteSettledReward(reward);
                    DB.Delete<SpaceContract>(contract.Id);
                }
                if(rows.Length<100)break;
            }
            foreach(var ship in ShipEquipmentTransfers.AllShips())
            {
                var stale=ship.Status.HostileDamageDebt.Keys.Where(x=>DB.Get<SpaceEncounter>(x)==null).ToArray();
                if(stale.Length==0)continue;foreach(var id in stale)ship.Status.HostileDamageDebt.Remove(id);DB.Set(ship);
            }
        }
        private static void DeleteSettledReward(SpaceReward reward)
        {
            var player=DB.Get<Player>(reward.PlayerId);
            if(player!=null)
            {
                foreach(var skill in reward.Experience.Keys)player.SkillXPReceipts.Remove(reward.Id+"/"+skill);
                player.SpaceEconomy.Receipts.Remove(reward.Id);DB.Set(player);
            }
            DB.Delete<SpaceReward>(reward.Id);
        }
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
        private static void RecordSpaceDamage(uint source, uint target, ShipStatus sourceStatus, ShipStatus targetStatus, double shield, double hull,string creditOperatorId=null)
        {
            if (source == target || !(GetIsEnemy(source, target) || GetIsEnemy(target, source))) return;
            if(creditOperatorId==null&&GetIsPC(source))creditOperatorId=GetObjectUUID(SkillOperator(source,SkillType.Gunnery));
            if (creditOperatorId!=null && EncounterId(targetStatus) is string targetId)
            {
                var encounter = DB.Get<SpaceEncounter>(targetId);
                if (encounter != null && !encounter.Completed)
                { encounter.Contributions.CreditDamage(creditOperatorId, shield + hull); DB.Set(encounter); }
            }
            if ((GetIsPC(target) || !string.IsNullOrEmpty(GetLocalString(target,"SPACE_PROXY_SHIP")) || !string.IsNullOrEmpty(GetLocalString(target,"SPACE_RESCUE_CONTRACT"))) && EncounterId(sourceStatus) is string sourceId)
            {
                var encounter = DB.Get<SpaceEncounter>(sourceId);
                if (encounter == null || encounter.Completed) return;
                var playerId = GetObjectUUID(target);
                if(GetIsPC(target))encounter.Contributions.Credit(playerId, SkillType.Piloting, shield + hull);
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
            if(actual-remaining>0)TouchOperatingClock(source,DateTime.UtcNow,1);
            var rescueContract=GetLocalString(target,"SPACE_RESCUE_CONTRACT");
            if(!string.IsNullOrEmpty(rescueContract))
            {
                var contract=DB.Get<SpaceContract>(rescueContract);
                if(contract?.State==SpaceContractState.Active&&contract.ParticipantShip(GetObjectUUID(source))!=null)
                {contract.RescueRecovery=Math.Min(SpaceActivityCatalog.Default.Profiles[contract.Profile].Rescue,contract.RescueRecovery+actual-remaining);DB.Set(contract);}
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
            foreach (var encounter in EncounterJournals(true,false).ToArray())
            {
                var profile = SpaceEncounterCatalog.Default.Profiles[encounter.Profile];
                CreateEncounterWreck(encounter, profile);
                CreateEncounterRewards(encounter, profile);
            }
            foreach(var encounter in EncounterJournals(false).Where(x=>x.DateCreated<SpaceSessionStartedAt).ToArray())DB.Delete<SpaceEncounter>(encounter.Id);
            Scheduler.ScheduleRepeating(ProcessSpaceRewards, TimeSpan.FromSeconds(5));
            Scheduler.ScheduleRepeating(CleanSpaceJournals,TimeSpan.FromHours(1));
        }
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void RecoverSpaceRewardsOnLogin()
        { var player = GetEnteringObject(); if (GetIsPC(player) && !GetIsDM(player)) SettleSpaceRewards(player); }
        private static void ProcessSpaceRewards()
        {
            foreach (var encounter in EncounterJournals(true,false).ToArray())
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
                if (!reward.ReputationSettled)
                {
                    var record=DB.Get<Player>(playerId);
                    record.SpaceEconomy.AwardReputation(reward.Id,reward.Reputation,reward.DateCreated);
                    DB.Set(record);reward.ReputationSettled=true;DB.Set(reward);
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
