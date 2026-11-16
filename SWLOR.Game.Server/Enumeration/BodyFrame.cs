namespace SWLOR.Game.Server.Enumeration
{
    /// <summary>
    /// The kind of body a character has. Humanoid is the part-based body every species but the
    /// astromech uses; systems that need limbs, a head or worn clothing check this before running.
    /// </summary>
    public enum BodyFrame
    {
        Humanoid = 0,
        Astromech = 1
    }
}
