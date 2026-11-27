using System.Collections.Generic;
using NWN.Native.API;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using Player = SWLOR.Game.Server.Entity.Player;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// Movement, maximum HP and food HP, attributes, NPC loading and regeneration: part 2 of the stat split (until then
    /// members of <c>IStatService</c>).
    /// </summary>
    public interface ICreatureStatService
    {
        /// <summary>
        /// When a player enters the server, reapply HP and temporary stats.
        /// </summary>
        void ApplyPlayerStats();

        /// <summary>
        /// The engine keeps a player's local variables across a relog until the server resets,
        /// but the forced walk set through NWNX belongs to the old creature object.
        /// Reapply it so a player who logged out walking still walks.
        /// </summary>
        void RestoreWalkMode(uint player);

        /// <summary>
        /// After a player's status effects are reassociated,
        /// adjust any food HP if necessary.
        /// </summary>
        void ReapplyFoodHP();

        void ReapplyFoodHP(uint player);

        /// <summary>
        /// Increases or decreases a player's HP by a specified amount.
        /// There is a cap of 255 HP per NWN level. Players are auto-leveled to 40 by default, so this
        /// gives 255 * 40 = 10,200 HP maximum. If the player's HP would go over this amount, it will be set to 10,200.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="player">The player to adjust</param>
        /// <param name="adjustBy">The amount to adjust by.</param>
        void AdjustPlayerMaxHP(Player entity, uint player, int adjustBy);

        /// <summary>
        /// Sets a player's stored maximum HP to the sum of its sources: the base, the HP bonuses on equipped items,
        /// and active food. Equipment and food add and remove their HP as changes, so one missed, failed or repeated
        /// change leaves the record off for good. A record below the native floor of one HP per level then hides
        /// gear HP until enough is worn to climb past that floor.
        /// </summary>
        /// <param name="player">The player to reconcile</param>
        /// <returns>True if the record was corrected</returns>
        bool RefreshPlayerMaxHP(uint player);

        int GetEquippedHPBonus(uint creature);

        bool IsWalkModeEnabled(uint creature);

        void ClearWalkModeBeforeDMPossessionChange();

        bool ToggleWalkMode(uint player);

        void ApplyCreatureMovementRate(uint creature);

        /// <summary>
        /// Out-of-combat movement speed depends on the combat state, which has no event of its own, so a player
        /// holding that stat has their movement rate re-applied on the heartbeat that sees the state change.
        /// </summary>
        void RefreshOutOfCombatMovementRate();

        void ForgetOutOfCombatMovementState();

        float GetMovementSpeedMultiplier(uint creature);

        /// <summary>
        /// Calculates a player's stat based on their skill bonuses, upgrades, etc. and applies the changes to one ability score.
        /// </summary>
        /// <param name="entity">The player entity</param>
        /// <param name="player">The player object</param>
        /// <param name="ability">The ability score to apply to.</param>
        void ApplyPlayerStat(Player entity, uint player, AbilityType ability);

        void RestorePlayerAttributes(uint player);

        void RestorePlayerAttributes(Player entity, Action<AbilityType, int> setAbilityScore);

        /// <summary>
        /// Stores an NPC's STM and FP as local variables.
        /// Also load their HP per their skin, if specified.
        /// </summary>
        void LoadNPCStats();

        void LoadNPCStats(uint self);

        /// <summary>
        /// Sets an NPC's final maximum HP after accounting for the native NWN bonuses
        /// derived from Constitution (SWLOR Vitality), Toughness, and similar rules.
        /// ObjectPlugin.SetMaxHitPoints writes the engine's base HP, not its final maximum.
        /// </summary>
        /// <param name="creature">The NPC whose HP budget is being applied.</param>
        /// <param name="desiredMaxHitPoints">The final maximum HP the NPC should have.</param>
        /// <param name="restoreToFull">If true, restore current HP to the final maximum.</param>
        void SetNPCMaxHitPoints(uint creature, int desiredMaxHitPoints, bool restoreToFull = false);

        /// <summary>
        /// Restores an NPC's STM and FP.
        /// </summary>
        /// <param name="outOfCombatRegen">Also heals a tenth of maximum HP while the NPC is out of combat with nobody on its enmity table.</param>
        /// <param name="enmity">Who the NPC hates most; read only when <paramref name="outOfCombatRegen"/> is true, so other callers pass null.</param>
        void RestoreNPCStats(bool outOfCombatRegen, IEnmityTargetQuery enmity);

        /// <summary>
        /// Restores a beast's FP and STM. Each heartbeat restores 1 + WIL/10 FP and 1 + MGT/10 STM.
        /// STM regeneration remains active in and out of combat, but cannot begin until six seconds
        /// after the beast last spent STM.
        /// </summary>
        void RestoreBeastStats();

        bool IsNaturalStaminaRegenerationAvailable(long availableAtTicks, long currentTicks);

        /// <summary>
        /// Amount of FP or STM a beast restores per heartbeat, scaled by the governing attribute
        /// (WIL for FP, MGT for STM).
        /// </summary>
        int GetBeastNaturalRegenAmount(int attribute);
    }
}
