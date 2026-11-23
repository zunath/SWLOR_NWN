namespace SWLOR.Game.Server.Enumeration
{
    /// <summary>
    /// Limits a skill or perk line to one body frame. None leaves it open to every character.
    /// </summary>
    public enum BodyFrameRestriction
    {
        None = 0,
        HumanoidOnly = 1,
        AstromechOnly = 2
    }
}
