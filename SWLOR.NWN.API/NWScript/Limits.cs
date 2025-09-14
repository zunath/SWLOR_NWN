using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   Gets the attack bonus limit.
        ///   - The default value is 20.
        /// </summary>
        public static int GetAttackBonusLimit()
        {
            return global::NWN.Core.NWScript.GetAttackBonusLimit();
        }

        /// <summary>
        ///   Gets the damage bonus limit.
        ///   - The default value is 100.
        /// </summary>
        public static int GetDamageBonusLimit()
        {
            return global::NWN.Core.NWScript.GetDamageBonusLimit();
        }

        /// <summary>
        ///   Gets the saving throw bonus limit.
        ///   - The default value is 20.
        /// </summary>
        public static int GetSavingThrowBonusLimit()
        {
            return global::NWN.Core.NWScript.GetSavingThrowBonusLimit();
        }

        /// <summary>
        ///   Gets the ability bonus limit.
        ///   - The default value is 12.
        /// </summary>
        public static int GetAbilityBonusLimit()
        {
            return global::NWN.Core.NWScript.GetAbilityBonusLimit();
        }

        /// <summary>
        ///   Gets the ability penalty limit.
        ///   - The default value is 30.
        /// </summary>
        public static int GetAbilityPenaltyLimit()
        {
            return global::NWN.Core.NWScript.GetAbilityPenaltyLimit();
        }

        /// <summary>
        ///   Gets the skill bonus limit.
        ///   - The default value is 50.
        /// </summary>
        public static int GetSkillBonusLimit()
        {
            return global::NWN.Core.NWScript.GetSkillBonusLimit();
        }

        /// <summary>
        ///   Sets the attack bonus limit.
        ///   - The minimum value is 0.
        /// </summary>
        public static void SetAttackBonusLimit(int nNewLimit)
        {
            global::NWN.Core.NWScript.SetAttackBonusLimit(nNewLimit);
        }

        /// <summary>
        ///   Sets the damage bonus limit.
        ///   - The minimum value is 0.
        /// </summary>
        public static void SetDamageBonusLimit(int nNewLimit)
        {
            global::NWN.Core.NWScript.SetDamageBonusLimit(nNewLimit);
        }

        /// <summary>
        ///   Sets the saving throw bonus limit.
        ///   - The minimum value is 0.
        /// </summary>
        public static void SetSavingThrowBonusLimit(int nNewLimit)
        {
            global::NWN.Core.NWScript.SetSavingThrowBonusLimit(nNewLimit);
        }

        /// <summary>
        ///   Sets the ability bonus limit.
        ///   - The minimum value is 0.
        /// </summary>
        public static void SetAbilityBonusLimit(int nNewLimit)
        {
            global::NWN.Core.NWScript.SetAbilityBonusLimit(nNewLimit);
        }

        /// <summary>
        ///   Sets the ability penalty limit.
        ///   - The minimum value is 0.
        /// </summary>
        public static void SetAbilityPenaltyLimit(int nNewLimit)
        {
            global::NWN.Core.NWScript.SetAbilityPenaltyLimit(nNewLimit);
        }

        /// <summary>
        ///   Sets the skill bonus limit.
        ///   - The minimum value is 0.
        /// </summary>
        public static void SetSkillBonusLimit(int nNewLimit)
        {
            global::NWN.Core.NWScript.SetSkillBonusLimit(nNewLimit);
        }
    }
}
