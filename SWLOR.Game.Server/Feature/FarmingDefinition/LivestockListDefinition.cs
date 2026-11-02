using System.Collections.Generic;
using SWLOR.Game.Server.Service.LivestockService;

namespace SWLOR.Game.Server.Feature.FarmingDefinition
{
    /// <summary>
    /// The seven animals of the Player City Revamp in five tiers. Each is raised from an existing creature whose meat cooking
    /// already uses, at the tier of its level in the Design Bible's World NPCs tab: Warocas 3, Valley Cairnmog 10, Gimpassa
    /// 12, K'lor'slug 22, Aradile 25, Womp Rat 35 and Plains Thune 46. The young animal items (<c>livestock_*</c>) are
    /// content for the farm items slice and are listed as missing until it lands.
    /// </summary>
    public class LivestockListDefinition : ILivestockListDefinition
    {
        private readonly LivestockBuilder _builder = new();

        public Dictionary<LivestockType, LivestockDefinition> BuildLivestock()
        {
            TierOne();
            TierTwo();
            TierThree();
            TierFour();
            TierFive();

            return _builder.Build();
        }

        private void TierOne()
        {
            _builder.Create(LivestockType.Warocas).Name("Warocas").Tier(1)
                .SourceCreature("warocas").Young("livestock_waro")
                .Meat("warocas_meat").SecondProduct("waro_leg");
        }

        private void TierTwo()
        {
            _builder.Create(LivestockType.ValleyCairnmog).Name("Valley Cairnmog").Tier(2)
                .SourceCreature("valley_cairnmog").Young("livestock_cairn")
                .Meat("cairnmog_meat");
            _builder.Create(LivestockType.Gimpassa).Name("Gimpassa").Tier(2)
                .SourceCreature("ww_gimpassa").Young("livestock_gimp")
                .Meat("gimp_meat");
        }

        private void TierThree()
        {
            _builder.Create(LivestockType.Klorslug).Name("K'lor'slug").Tier(3)
                .SourceCreature("korr_klorslug").Young("livestock_klor")
                .Meat("klorslug_meat");
            _builder.Create(LivestockType.Aradile).Name("Aradile").Tier(3)
                .SourceCreature("mc_aradile").Young("livestock_arad")
                .Meat("aradile_meat");
        }

        private void TierFour()
        {
            _builder.Create(LivestockType.Womprat).Name("Womp Rat").Tier(4)
                .SourceCreature("womprat").Young("livestock_womp")
                .Meat("wompratmeat").SecondProduct("womp_innards");
        }

        private void TierFive()
        {
            _builder.Create(LivestockType.Thune).Name("Thune").Tier(5)
                .SourceCreature("pthune").Young("livestock_thune")
                .Meat("thune_meat").SecondProduct("milk").CollectExtra("milk");
        }
    }
}
