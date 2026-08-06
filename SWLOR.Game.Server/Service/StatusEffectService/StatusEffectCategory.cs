namespace SWLOR.Game.Server.Service.StatusEffectService
{
    [Flags]
    public enum StatusEffectCategory
    {
        None = 0,
        Debuff = 1,
        Control = 2,
        Bleeding = 4,
        Buff = 8,
        Incapacitating = 16,
        Venom = 32,
        Infection = 64,
        StaminaDrainTrigger = 128,
        ForceDisruption = 256,

        /// <summary>
        /// A hard crowd-control effect (stun, daze, knockdown, immobilize, blind, sleep,
        /// confusion). While one is active, no different hard CC can land on the same target -
        /// the shared immunity gate reads this flag.
        /// </summary>
        HardCrowdControl = 512
    }
}
