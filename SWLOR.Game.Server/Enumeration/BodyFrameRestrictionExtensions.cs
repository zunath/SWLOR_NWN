namespace SWLOR.Game.Server.Enumeration
{
    public static class BodyFrameRestrictionExtensions
    {
        /// <summary>
        /// Whether a character with the given body frame may use something carrying this restriction.
        /// </summary>
        public static bool Allows(this BodyFrameRestriction restriction, BodyFrame frame)
        {
            return restriction switch
            {
                BodyFrameRestriction.HumanoidOnly => frame == BodyFrame.Humanoid,
                BodyFrameRestriction.AstromechOnly => frame == BodyFrame.Astromech,
                _ => true
            };
        }
    }
}
