using System;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Service
{
    public static class ShipResearch
    {
        private const string Paid = "SHIP_RESEARCH_PAID";
        private const string Output = "SHIP_RESEARCH_OUTPUT";
        public static void Start(uint player, ResearchJob job, uint blueprint, int credits)
        {
            job.IsShipResearch = true; job.CreditCost = credits;
            job.InputItemId = GetIsObjectValid(blueprint) ? GetObjectUUID(blueprint) : null;
            job.Success = Random.D100(1) <= ShipResearchPolicy.SuccessChance() * 100;
            DB.Set(job);
            SettleInput(player, job);
        }
        private static uint Find(uint player, string identity, bool result)
        {
            if (string.IsNullOrEmpty(identity)) return OBJECT_INVALID;
            for (var item = GetFirstItemInInventory(player); GetIsObjectValid(item); item = GetNextItemInInventory(player))
                if (result ? GetLocalString(item, Output) == identity : GetObjectUUID(item) == identity) return item;
            return OBJECT_INVALID;
        }
        private static void SettleInput(uint player, ResearchJob job)
        {
            if (job.InputSettled || job.PlayerId != GetObjectUUID(player)) return;
            if (GetLocalString(player, Paid) != job.Id)
            {
                if (GetGold(player) < job.CreditCost) throw new InvalidOperationException("The research attempt needs its recorded payment.");
                TakeGoldFromCreature(job.CreditCost, player, true);
                SetLocalString(player, Paid, job.Id); ExportSingleCharacter(player);
            }
            var input = Find(player, job.InputItemId, false);
            if (GetIsObjectValid(input)) DestroyObject(input);
            var playerId = job.PlayerId; var jobId = job.Id;
            DelayCommand(.1f, () =>
            {
                if (!GetIsObjectValid(player) || GetObjectUUID(player) != playerId || GetIsObjectValid(Find(player, job.InputItemId, false))) return;
                ExportSingleCharacter(player);
                var saved = DB.Get<ResearchJob>(jobId);
                if (saved == null) return;
                saved.InputSettled = true; DB.Set(saved);
            });
        }
        public static void Deliver(uint player, ResearchJob job, bool cancel = false)
        {
            if (job.PlayerId != GetObjectUUID(player) || !job.InputSettled || (!cancel && DateTime.UtcNow < job.DateCompleted))
                throw new InvalidOperationException("The research attempt is not ready to settle.");
            if (!job.OutputPrepared)
            {
                job.Cancelled = cancel;
                uint item = OBJECT_INVALID;
                var existing = !string.IsNullOrEmpty(job.SerializedItem);
                if (existing) item = ObjectPlugin.Deserialize(job.SerializedItem);
                else if (job.Success && !cancel) item = CreateItemOnObject("blueprint", GetObjectByTag("TEMP_ITEM_STORAGE"));
                if ((existing || (job.Success && !cancel)) && !GetIsObjectValid(item)) throw new InvalidOperationException("The saved research result could not be created; the attempt remains recoverable.");
                if (GetIsObjectValid(item))
                {
                    if (!cancel)
                    {
                        var details = Craft.GetBlueprintDetails(item); details.Recipe = job.Recipe;
                        ShipResearchPolicy.Upgrade(details, job.Success, Random.D3(1) + Perk.GetPerkLevel(player, PerkType.ScientificNetworking), Random.Next(2), Random.D10(1));
                        SetName(item, $"Blueprint: {Cache.GetItemNameByResref(Craft.GetRecipe(job.Recipe).Resref)}");
                        Craft.SetBlueprintDetails(item, details);
                    }
                    SetLocalString(item, Output, job.Id);
                    job.SerializedOutput = ObjectPlugin.Serialize(item); DestroyObject(item);
                }
                job.OutputPrepared = true; DB.Set(job);
            }
            if (!string.IsNullOrEmpty(job.SerializedOutput) && !GetIsObjectValid(Find(player, job.Id, true)))
            {
                var item = ObjectPlugin.Deserialize(job.SerializedOutput); ObjectPlugin.AcquireItem(player, item);
                if (!GetIsObjectValid(item) || GetItemPossessor(item) != player)
                { if (GetIsObjectValid(item)) DestroyObject(item); throw new InvalidOperationException("Make room in your inventory for the research result."); }
            }
            ExportSingleCharacter(player);
            job.OutputSettled = true; DB.Set(job);
            SendMessageToPC(player, job.Cancelled ? "Research cancelled; the original blueprint was returned." : job.Success ? "Ship blueprint research succeeded." : "Ship blueprint research failed; an existing blueprint retains its level.");
            DB.Delete<ResearchJob>(job.Id);
        }
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void RecoverOnLogin()
        {
            var player = GetEnteringObject(); if (!GetIsPC(player) || GetIsDM(player)) return;
            foreach (var job in DB.Search(new DBQuery<ResearchJob>().AddFieldSearch(nameof(ResearchJob.PlayerId), GetObjectUUID(player), false)))
            {
                if (!job.IsShipResearch) continue;
                try { if (job.OutputPrepared) Deliver(player, job, job.Cancelled); else SettleInput(player, job); }
                catch (InvalidOperationException ex) { SendMessageToPC(player, ex.Message); }
            }
        }
    }
}
