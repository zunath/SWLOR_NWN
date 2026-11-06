using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Service.PvpCombatService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Player-vs-player combat. A player is in PvP combat from the first hit they deal to, or take
    /// from, another player or a player's pet until <see cref="PvpCombatRules.CombatWindowSeconds"/>
    /// pass without one. The logic lives in <see cref="IPvpCombatService"/>; the event handlers stay here and every
    /// member forwards to the container's instance. See <see cref="PvpCombatRules"/> for the steps.
    /// </summary>
    public static class PvpCombat
    {
        private static IPvpCombatService Instance => FacadeAccessor.Resolve<IPvpCombatService>();

        public static bool IsPlayer(uint creature) => Instance.IsPlayer(creature);

        public static bool IsPlayerControlled(uint creature) => Instance.IsPlayerControlled(creature);

        /// <summary>The player a creature fights for: the player itself, or the master of a player's pet.</summary>
        public static uint GetControllingPlayer(uint creature) => Instance.GetControllingPlayer(creature);

        /// <summary>Whether a player or a player's pet is acting on a different player.</summary>
        public static bool IsPlayerAgainstPlayer(uint source, uint target) => Instance.IsPlayerAgainstPlayer(source, target);

        public static bool IsInPvpCombat(uint player) => Instance.IsInPvpCombat(player);

        /// <summary>How long the player's current PvP combat has run, or 0 when they are not in one.</summary>
        public static double GetCombatSeconds(uint player) => Instance.GetCombatSeconds(player);

        public static int GetHealingReductionPercent(uint player) => Instance.GetHealingReductionPercent(player);

        public static int GetDamageIncreasePercent(uint player) => Instance.GetDamageIncreasePercent(player);

        /// <summary>
        /// Keeps both sides of a hit in PvP combat when a player or a player's pet hits another
        /// player or a player's pet.
        /// </summary>
        public static void RecordHit(uint attacker, uint target) => Instance.RecordHit(attacker, target);

        /// <summary>
        /// Raises damage a player or a player's pet deals to another player by that player's damage
        /// ramp. The increase belongs to the player being hit, so it can't be carried over to NPCs or
        /// to players who just joined the fight. A landed hit also keeps both sides in PvP combat.
        /// </summary>
        public static int ApplyDamageIncrease(uint defender, uint attacker, int damage, bool isLandedAttack) =>
            Instance.ApplyDamageIncrease(defender, attacker, damage, isLandedAttack);

        /// <summary>
        /// Cuts every heal a player receives during a long PvP fight: ability heals, heals over time,
        /// lifesteal, consumables, rest and natural regeneration. FP and STM are not heals and are
        /// never touched.
        /// </summary>
        [NWNEventHandler(ScriptName.OnHealBefore)]
        public static void ReduceHealing() => Instance.ReduceHealing();

        [NWNEventHandler(ScriptName.OnModuleDeath)]
        public static void EndOnDeath() => Instance.EndOnDeath();

        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void ForgetExitingPlayer() => Instance.ForgetExitingPlayer();

        /// <summary>
        /// Starts or moves the player's PvP combat clock to <paramref name="seconds"/> as of now, as if
        /// they had been fighting that long, with their last PvP hit <paramref name="secondsSinceLastHit"/>
        /// ago. For tests only.
        /// </summary>
        public static void SetCombatSecondsForTesting(uint player, double seconds, double secondsSinceLastHit = 0) =>
            Instance.SetCombatSecondsForTesting(player, seconds, secondsSinceLastHit);

        /// <summary>Ends the player's PvP combat at once. For tests only.</summary>
        public static void EndForTesting(uint player) => Instance.EndForTesting(player);
    }
}
