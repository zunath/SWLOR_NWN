using System;

namespace SWLOR.Game.Server.Service.StatusEffectService
{
    [Flags]
    public enum StatusEffectCleanseType
    {
        None = 0,
        Purify = 1,
        TreatmentKit1 = 2,
        TreatmentKit2 = 4,
        SoothePet = 8,
    }
}
