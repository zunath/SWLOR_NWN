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

        // Stat change events
        public const string OnCharacterMaxHPChanged = "char_maxhp_chg";
        public const string OnCharacterMaxFPChanged = "char_maxfp_chg";
        public const string OnCharacterMaxSTMChanged = "char_maxstm_chg";
        public const string OnCharacterHPRegenChanged = "char_hpregen_chg";
        public const string OnCharacterFPRegenChanged = "char_fpregen_chg";
        public const string OnCharacterSTMRegenChanged = "char_stmregen_chg";
        public const string OnCharacterRecastReductionChanged = "char_recastred_chg";
        public const string OnCharacterDefenseChanged = "char_defense_chg";
        public const string OnCharacterEvasionChanged = "char_evasion_chg";
        public const string OnCharacterAccuracyChanged = "char_accuracy_chg";
        public const string OnCharacterAttackChanged = "char_attack_chg";
        public const string OnCharacterForceAttackChanged = "char_forceatk_chg";
        public const string OnCharacterMightChanged = "char_might_chg";
        public const string OnCharacterPerceptionChanged = "char_percept_chg";
        public const string OnCharacterVitalityChanged = "char_vitality_chg";
        public const string OnCharacterAgilityChanged = "char_agility_chg";
        public const string OnCharacterWillpowerChanged = "char_willpower_chg";
        public const string OnCharacterSocialChanged = "char_social_chg";
        public const string OnCharacterShieldDeflectionChanged = "char_shlddefl_chg";
        public const string OnCharacterAttackDeflectionChanged = "char_atkdefl_chg";
        public const string OnCharacterCriticalRateChanged = "char_critrate_chg";
        public const string OnCharacterEnmityChanged = "char_enmity_chg";
        public const string OnCharacterHasteChanged = "char_haste_chg";
        public const string OnCharacterSlowChanged = "char_slow_chg";
        public const string OnCharacterDamageReductionChanged = "char_dmgeduct_chg";
        public const string OnCharacterForceDefenseChanged = "char_forcedef_chg";
        public const string OnCharacterQueuedDMGBonusChanged = "char_queuedmg_chg";
        public const string OnCharacterParalysisChanged = "char_paralysis_chg";
        public const string OnCharacterAccuracyModifierChanged = "char_accmod_chg";
        public const string OnCharacterRecastReductionModifierChanged = "char_recastmod_chg";
        public const string OnCharacterDefenseBypassModifierChanged = "char_defbypass_chg";
        public const string OnCharacterHealingModifierChanged = "char_healmod_chg";
        public const string OnCharacterFPRestoreOnHitChanged = "char_fprestore_chg";
        public const string OnCharacterDefenseModifierChanged = "char_defmod_chg";
        public const string OnCharacterForceDefenseModifierChanged = "char_forcedefmod_chg";
        public const string OnCharacterExtraAttackModifierChanged = "char_extraatkmod_chg";
        public const string OnCharacterAttackModifierChanged = "char_atkmod_chg";
        public const string OnCharacterForceAttackModifierChanged = "char_forceatkmod_chg";
        public const string OnCharacterEvasionModifierChanged = "char_evasmod_chg";
        public const string OnCharacterXPModifierChanged = "char_xpmod_chg";
        public const string OnCharacterPoisonResistChanged = "char_poisonres_chg";
        public const string OnCharacterLevelChanged = "char_level_chg";
        public const string OnCharacterControlSmitheryChanged = "char_ctrl_smith_chg";
        public const string OnCharacterControlFabricationChanged = "char_ctrl_fab_chg";
        public const string OnCharacterControlEngineeringChanged = "char_ctrl_eng_chg";
        public const string OnCharacterControlAgricultureChanged = "char_ctrl_agri_chg";
        public const string OnCharacterCraftsmanshipSmitheryChanged = "char_craft_smith_chg";
        public const string OnCharacterCraftsmanshipFabricationChanged = "char_craft_fab_chg";
        public const string OnCharacterCraftsmanshipEngineeringChanged = "char_craft_eng_chg";
        public const string OnCharacterCraftsmanshipAgricultureChanged = "char_craft_agri_chg";
        public const string OnCharacterCPSmitheryChanged = "char_cp_smith_chg";
        public const string OnCharacterCPFabricationChanged = "char_cp_fab_chg";
        public const string OnCharacterCPEngineeringChanged = "char_cp_eng_chg";
        public const string OnCharacterCPAgricultureChanged = "char_cp_agri_chg";
    }
}
