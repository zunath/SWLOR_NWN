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
        StaminaDrainTrigger = 128
    }
}
