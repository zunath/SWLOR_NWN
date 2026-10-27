namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Gets the attack bonus limit.
        /// The default value is 20.
        /// </summary>
        /// <returns>The attack bonus limit</returns>
        public static int GetAttackBonusLimit()
        {
            return NWScriptRuntime.Current.GetAttackBonusLimit();
        }

        /// <summary>
        /// Gets the damage bonus limit.
        /// The default value is 100.
        /// </summary>
        /// <returns>The damage bonus limit</returns>
        public static int GetDamageBonusLimit()
        {
            return NWScriptRuntime.Current.GetDamageBonusLimit();
        }

        /// <summary>
        /// Gets the saving throw bonus limit.
        /// The default value is 20.
        /// </summary>
        /// <returns>The saving throw bonus limit</returns>
        public static int GetSavingThrowBonusLimit()
        {
            return NWScriptRuntime.Current.GetSavingThrowBonusLimit();
        }

        /// <summary>
        /// Gets the ability bonus limit.
        /// The default value is 12.
        /// </summary>
        /// <returns>The ability bonus limit</returns>
        public static int GetAbilityBonusLimit()
        {
            return NWScriptRuntime.Current.GetAbilityBonusLimit();
        }

        /// <summary>
        /// Gets the ability penalty limit.
        /// The default value is 30.
        /// </summary>
        /// <returns>The ability penalty limit</returns>
        public static int GetAbilityPenaltyLimit()
        {
            return NWScriptRuntime.Current.GetAbilityPenaltyLimit();
        }

        /// <summary>
        /// Gets the skill bonus limit.
        /// The default value is 50.
        /// </summary>
        /// <returns>The skill bonus limit</returns>
        public static int GetSkillBonusLimit()
        {
            return NWScriptRuntime.Current.GetSkillBonusLimit();
        }

        /// <summary>
        /// Sets the attack bonus limit.
        /// The minimum value is 0.
        /// </summary>
        /// <param name="nNewLimit">The new attack bonus limit</param>
        public static void SetAttackBonusLimit(int nNewLimit)
        {
            NWScriptRuntime.Current.SetAttackBonusLimit(nNewLimit);
        }

        /// <summary>
        /// Sets the damage bonus limit.
        /// The minimum value is 0.
        /// </summary>
        /// <param name="nNewLimit">The new damage bonus limit</param>
        public static void SetDamageBonusLimit(int nNewLimit)
        {
            NWScriptRuntime.Current.SetDamageBonusLimit(nNewLimit);
        }

        /// <summary>
        /// Sets the saving throw bonus limit.
        /// The minimum value is 0.
        /// </summary>
        /// <param name="nNewLimit">The new saving throw bonus limit</param>
        public static void SetSavingThrowBonusLimit(int nNewLimit)
        {
            NWScriptRuntime.Current.SetSavingThrowBonusLimit(nNewLimit);
        }

        /// <summary>
        /// Sets the ability bonus limit.
        /// The minimum value is 0.
        /// </summary>
        /// <param name="nNewLimit">The new ability bonus limit</param>
        public static void SetAbilityBonusLimit(int nNewLimit)
        {
            NWScriptRuntime.Current.SetAbilityBonusLimit(nNewLimit);
        }

        /// <summary>
        /// Sets the ability penalty limit.
        /// The minimum value is 0.
        /// </summary>
        /// <param name="nNewLimit">The new ability penalty limit</param>
        public static void SetAbilityPenaltyLimit(int nNewLimit)
        {
            NWScriptRuntime.Current.SetAbilityPenaltyLimit(nNewLimit);
        }

        /// <summary>
        /// Sets the skill bonus limit.
        /// The minimum value is 0.
        /// </summary>
        /// <param name="nNewLimit">The new skill bonus limit</param>
        public static void SetSkillBonusLimit(int nNewLimit)
        {
            NWScriptRuntime.Current.SetSkillBonusLimit(nNewLimit);
        }
    }
}
