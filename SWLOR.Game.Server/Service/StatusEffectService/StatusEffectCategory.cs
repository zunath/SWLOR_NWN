namespace SWLOR.Game.Server.Service.StatusEffectService
{
    [Flags]
    public enum StatusEffectCategory
    {
        None = 0,
        Debuff = 1,
        Control = 2,
        Bleeding = 4
    }
}
