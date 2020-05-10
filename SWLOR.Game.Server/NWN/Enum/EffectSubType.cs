using System;

namespace SWLOR.Game.Server.NWN.Enum
{
    [Flags]
    public enum EffectSubType
    {
        Magical = 8,
        Supernatural = 16,
        Extraordinary = 24,
        Mask = 0x18
    }
}