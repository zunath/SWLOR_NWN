namespace SWLOR.Shared.Domain.Combat
{
    internal class CombatScriptName
    {
        // Combat events
        public const string OnStartCombatRoundBefore = "comb_round_bef";
        public const string OnStartCombatRoundAfter = "comb_round_aft";
        public const string OnCastSpellBefore = "cast_spell_bef";
        public const string OnCastSpellAfter = "cast_spell_aft";
        public const string OnHealBefore = "heal_bef";
        public const string OnHealAfter = "heal_aft";
        public const string OnCombatModeOn = "combat_mode_on";
        public const string OnCombatModeOff = "combat_mode_off";
        public const string OnCombatAttackOfOpportunityBefore = "combat_aoo_bef";
        public const string OnCombatAttackOfOpportunityAfter = "combat_aoo_aft";
        public const string OnBroadcastAttackOfOpportunityBefore = "brdcast_aoo_bef";
        public const string OnBroadcastAttackOfOpportunityAfter = "brdcast_aoo_aft";
        public const string OnPlayerRestStarted = "rest_started";
        public const string OnCombatPointXPDistribute = "cp_xp_distribute";
        public const string OnPlaceableDeath = "plc_death";
        public const string OnPitfallTrap = "pitfalltrap";
        public const string OnObjectDestroyed = "object_destroyed";
        public const string OnEnmityAcquired = "enmity_acquired";
        public const string OnEnmityChanged = "enmity_changed";
        public const string OnIntervalPC6Seconds = "interval_pc_6s";
        public const string OnItemHit = "item_on_hit";
        public const string OnDealtDamage = "swlor_dmg_dealt";
    }
}
