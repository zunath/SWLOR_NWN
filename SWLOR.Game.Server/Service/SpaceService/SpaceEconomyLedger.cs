using System;
using System.Collections.Generic;
namespace SWLOR.Game.Server.Service.SpaceService
{
    public sealed class SpaceEconomyLedger
    {
        public int StarterRevision { get; set; }
        public int ClaimedStarterRevision { get; set; } = -1;
        public string StarterChoice { get; set; }
        public HashSet<string> StarterOutputs { get; set; } = new();
        public int ServiceVoucher { get; set; }
        public long Reputation { get; set; }
        public Dictionary<string, SpaceDailyEconomy> Days { get; set; } = new();
        public HashSet<string> Receipts { get; set; } = new();
        public SpaceTradeTransaction Pending { get; set; }
        public Dictionary<string,double> MaterialRecovery {get;set;} = new();
        public SpaceDailyEconomy Day(DateTime now)
        {
            var key=now.ToUniversalTime().ToString("yyyy-MM-dd");
            if (!Days.TryGetValue(key,out var day)) Days[key]=day=new();
            return day;
        }
        public int AwardReputation(string receipt, int requested, DateTime now)
        {
            if (!Receipts.Add(receipt)) return 0;
            var day=Day(now);var amount=Math.Clamp(requested,0,Math.Max(0,100-day.Reputation));
            day.Reputation+=amount;Reputation+=amount;return amount;
        }
        public int OreAvailable(DateTime now) => Math.Max(0,1000-Day(now).Ore);
    }
    public sealed class SpaceDailyEconomy
    {
        public int Ore {get;set;}
        public int Reputation {get;set;}
    }
    public sealed class SpaceTradeTransaction
    {
        public string Id {get;set;}
        public string Resref {get;set;}
        public string InputId {get;set;}
        public int Quantity {get;set;}
        public int InitialQuantity {get;set;}
        public int Credits {get;set;}
        public bool Purchase {get;set;}
        public bool OreCommission {get;set;}
        public bool MaterialRecovery {get;set;}
        public DateTime Day {get;set;}
    }
}
