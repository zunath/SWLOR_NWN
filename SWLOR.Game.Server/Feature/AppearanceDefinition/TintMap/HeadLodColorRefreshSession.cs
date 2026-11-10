using System;
using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap
{
    internal sealed class HeadLodColorRefreshSession
    {
        public uint Creature { get; init; }
        public string Uuid { get; init; }
        public string Model { get; init; }
        public AppearanceType Appearance { get; init; }
        public Gender Gender { get; init; }
        public int Head { get; init; }
        public IReadOnlyList<TintMapMaterialSelection> Selections { get; set; }
        public int Sequence { get; set; }
        public IDisposable Subscription { get; set; }
    }
}
