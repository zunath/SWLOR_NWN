using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
namespace SWLOR.Game.Server.Service.SpaceService
{
    public sealed class SpaceTradeItem
    {
        public string Resref {get;set;}
        public string Name {get;set;}
        public int Reference {get;set;}
        public int Resale {get;set;}
        public bool Vendor {get;set;}
        public bool Ammunition {get;set;}
        public int Engineering {get;set;}
    }
    public sealed class SpaceEconomyCatalog
    {
        public sealed class LegacyRefund
        {
            public string Resref {get;set;}
            public double Fraction {get;set;}
            public Dictionary<string,int> Components {get;set;}
        }
        private sealed class Data
        {
            public List<SpaceTradeItem> Items {get;set;}
            [JsonProperty("legacy_refunds")] public List<LegacyRefund> LegacyRefunds {get;set;}
            [JsonProperty("ore_bids")] public Dictionary<string,int> OreBids {get;set;}
        }
        private static readonly Lazy<SpaceEconomyCatalog> Instance=new(()=>
        {
            using var stream=typeof(SpaceEconomyCatalog).Assembly.GetManifestResourceStream("SWLOR.SpaceEconomy.json")??throw new InvalidDataException("Missing ship economy data.");
            using var reader=new StreamReader(stream);var data=JsonConvert.DeserializeObject<Data>(reader.ReadToEnd());
            if(data?.Items==null||data.OreBids==null||data.Items.Any(x=>x.Reference<=0||x.Resale<0||x.Resale>x.Reference*.25||x.Resref.Length>16))throw new InvalidDataException("Invalid ship economy data.");
            return new(){Items=data.Items.ToDictionary(x=>x.Resref),OreBids=data.OreBids,LegacyRefunds=data.LegacyRefunds.ToDictionary(x=>x.Resref)};
        });
        public static SpaceEconomyCatalog Default=>Instance.Value;
        public IReadOnlyDictionary<string,SpaceTradeItem> Items {get;private init;}
        public IReadOnlyDictionary<string,int> OreBids {get;private init;}
        public IReadOnlyDictionary<string,LegacyRefund> LegacyRefunds {get;private init;}
        public static int SupplyPrice(SpaceTradeItem item,int quantity,double ammunitionDiscount=0)=>checked((int)Math.Ceiling(item.Reference*quantity*(1-(item.Ammunition?Math.Clamp(ammunitionDiscount,0,.25):0))));
    }
}
