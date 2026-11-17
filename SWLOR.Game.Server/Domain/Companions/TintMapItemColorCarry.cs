using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Domain.Companions
{
    public sealed class TintMapItemColorCarry
    {
        public IReadOnlyDictionary<TintMapLayerType, IReadOnlyList<TintMapItemColorSource>> Sources { get; }

        public TintMapItemColorCarry(
            IReadOnlyDictionary<TintMapLayerType, IReadOnlyList<TintMapItemColorSource>> sources)
        {
            Sources = sources;
        }
    }
}
