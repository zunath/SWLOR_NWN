using SWLOR.Game.Server.Service.PvpCombatService;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// Player-vs-player combat: who is fighting, how long, and the healing cut and damage increase of a long fight
    /// (the steps are in <see cref="PvpCombatRules"/>).
    /// </summary>
    public interface IPvpCombatService
    {
        bool IsPlayer(uint creature);

        bool IsPlayerControlled(uint creature);

        /// <summary>The player a creature fights for: the player itself, or the master of a player's pet.</summary>
        uint GetControllingPlayer(uint creature);

        /// <summary>Whether a player or a player's pet is acting on a different player.</summary>
        bool IsPlayerAgainstPlayer(uint source, uint target);

        bool IsInPvpCombat(uint player);

        /// <summary>How long the player's current PvP combat has run, or 0 when they are not in one.</summary>
        double GetCombatSeconds(uint player);

        int GetHealingReductionPercent(uint player);

        int GetDamageIncreasePercent(uint player);

        /// <summary>
        /// Keeps both sides of a hit in PvP combat when a player or a player's pet hits another
        /// player or a player's pet.
        /// </summary>
        void RecordHit(uint attacker, uint target);

        /// <summary>
        /// Raises damage a player or a player's pet deals to another player by that player's damage
        /// ramp. The increase belongs to the player being hit, so it can't be carried over to NPCs or
        /// to players who just joined the fight. A landed hit also keeps both sides in PvP combat.
        /// </summary>
        int ApplyDamageIncrease(uint defender, uint attacker, int damage, bool isLandedAttack);

        /// <summary>
        /// Cuts every heal a player receives during a long PvP fight: ability heals, heals over time,
        /// lifesteal, consumables, rest and natural regeneration. FP and STM are not heals and are
        /// never touched.
        /// </summary>
        void ReduceHealing();

        void EndOnDeath();

        void ForgetExitingPlayer();

        /// <summary>
        /// Starts or moves the player's PvP combat clock to <paramref name="seconds"/> as of now, as if
        /// they had been fighting that long, with their last PvP hit <paramref name="secondsSinceLastHit"/>
        /// ago. For tests only.
        /// </summary>
        void SetCombatSecondsForTesting(uint player, double seconds, double secondsSinceLastHit = 0);

        /// <summary>Ends the player's PvP combat at once. For tests only.</summary>
        void EndForTesting(uint player);
    }
}
