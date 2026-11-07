namespace SWLOR.Game.Server.Service.CombatService
{
    /// <summary>
    /// The floating text that tells a player a one-use combat bonus is waiting to be spent. A bonus that is already armed stays
    /// silent when its trigger fires again, so a trigger that fires on every hit does not repeat the text.
    /// </summary>
    public static class ReadyCue
    {
        public static bool ShouldShow(bool isPlayer, bool wasArmed, int bonus)
        {
            return isPlayer && !wasArmed && bonus != 0;
        }

        public static string Damage(string bonusName, int damageBonus)
        {
            return $"{bonusName} Ready: +{damageBonus} DMG";
        }

        public static string StaminaDiscount(int staminaCostAdjustment)
        {
            return $"Next Ability -{Math.Abs(staminaCostAdjustment)} STM";
        }
    }
}
