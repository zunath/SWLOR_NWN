using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.TradeService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// NPC trade convoys (plan section 13) in the live spawn and death pipeline: a hauler loads its cargo and calls its
    /// escorts as it spawns, and its death spills the cargo into cargo pods.
    /// </summary>
    public static class ConvoyRaidEngineTests
    {
        private const int Tier = 2;

        [EngineTest("A convoy hauler spawns with cargo and escorts and spills the cargo when destroyed", Category = "TradeConvoy", TimeoutSeconds = 30f)]
        public static async Task DestroyedHaulerSpillsItsCargo(EngineTestContext ctx)
        {
            var area = ctx.Arena;
            var hauler = CreateObject(ObjectType.Creature, ConvoyRules.GetHaulerTag(Tier), ctx.GetArenaLocation(10f));
            var created = new List<uint>();
            try
            {
                ctx.Assert(GetIsObjectValid(hauler), "the hauler spawns");
                var cargo = Trade.GetPodContents(hauler);
                ctx.Assert(cargo.Count > 0, "the hauler carries commodities");
                ctx.Assert(cargo.Keys.All(x => TradeLookup.GetCommodity(x).Legality == CommodityLegality.Legal), "convoys carry legal goods");

                for (var obj = GetFirstObjectInArea(area); GetIsObjectValid(obj); obj = GetNextObjectInArea(area))
                {
                    if (GetTag(obj) == ConvoyRules.GetEscortTag(Tier))
                        created.Add(obj);
                }

                ctx.AssertEqual(ConvoyRules.GetEscortCount(Tier), created.Count, "the escorts fly with the hauler");

                await ctx.ExecuteInCreatureContextAsync(hauler, Trade.SpillConvoyCargo);
                var pods = new List<uint>();
                for (var obj = GetFirstObjectInArea(area); GetIsObjectValid(obj); obj = GetNextObjectInArea(area))
                {
                    if (GetResRef(obj) == Trade.CargoPodResref)
                        pods.Add(obj);
                }

                created.AddRange(pods);
                var spilled = pods.SelectMany(Trade.GetPodContents).GroupBy(x => x.Key).ToDictionary(x => x.Key, x => x.Sum(y => y.Value));
                ctx.AssertEqual(cargo.Count, spilled.Count, "every commodity spilled");
                foreach (var (commodity, units) in cargo)
                    ctx.AssertEqual(units, spilled[commodity], $"all the {commodity} spilled");
                ctx.AssertEqual(0, Trade.GetPodContents(hauler).Count, "the hauler's hold is empty");
            }
            finally
            {
                foreach (var obj in created.Append(hauler).Where(GetIsObjectValid))
                    DestroyObject(obj);
            }
        }
    }
}
