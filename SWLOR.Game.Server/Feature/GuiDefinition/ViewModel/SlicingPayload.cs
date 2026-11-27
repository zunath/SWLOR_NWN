using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.Crafting;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public sealed class SlicingPayload : GuiPayloadBase
    {
        public uint Target { get; }
        public SlicingSourceType Source { get; }
        public int Tier { get; }

        public SlicingPayload(uint target, SlicingSourceType source, int tier)
        {
            Target = target;
            Source = source;
            Tier = tier;
        }
    }
}
