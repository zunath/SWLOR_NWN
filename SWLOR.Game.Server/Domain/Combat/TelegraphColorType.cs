namespace SWLOR.Game.Server.Domain.Combat
{
    public enum TelegraphColorType
    {
        // These values are a two-bit protocol shared with sw_shader/fsfbpostpr.shd.
        Hostile = 0,
        Self = 1,
        Beneficial = 2,
        PartyMember = 3,
    }
}
