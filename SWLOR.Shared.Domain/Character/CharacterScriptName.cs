namespace SWLOR.Shared.Domain.Character
{
    internal class CharacterScriptName
    {
        // Character events
        public const string OnPlayerInitialized = "char_init_after";
        public const string OnCharacterRebuild = "char_rebuild";
        public const string OnExitRebuild = "exit_rebuild";
        public const string OnExitSpending = "exit_spending";
        public const string OnBuyStatRebuild = "buy_stat_rebuild";

        // Player events
        public const string OnPlayerDamaged = "pc_damaged";
        public const string OnPlayerHeartbeat = "pc_heartbeat";
        public const string OnPlayerPerception = "pc_perception";
        public const string OnPlayerSpellCastAt = "pc_spellcastat";
        public const string OnPlayerAttacked = "pc_attacked";
        public const string OnPlayerDisturbed = "pc_disturb";
        public const string OnPlayerRoundEnd = "pc_roundend";
        public const string OnPlayerSpawn = "pc_spawn";
        public const string OnPlayerRested = "pc_rested";
        public const string OnPlayerDeath = "pc_death";
        public const string OnPlayerUserDefined = "pc_userdef";
        public const string OnPlayerBlocked = "pc_blocked";
        public const string OnPlayerFPAdjusted = "pc_fp_adjusted";
        public const string OnPlayerStaminaAdjusted = "pc_stm_adjusted";
        public const string OnPlayerShieldAdjusted = "pc_shld_adjusted";
        public const string OnPlayerHullAdjusted = "pc_hull_adjusted";
        public const string OnPlayerCapAdjusted = "pc_cap_adjusted";
        public const string OnPlayerTargetUpdated = "pc_target_upd";
        public const string OnPlayerCacheData = "pc_cache_data";
        public const string OnPlayerRestStarted = "rest_started";
        public const string OnRestTriggerEnter = "rest_trg_enter";
        public const string OnRestTriggerExit = "rest_trg_exit";
    }
}
