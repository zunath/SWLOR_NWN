namespace SWLOR.Game.Server.NWNX
{
    public enum AdministrationOption
    {
        AllKillable = 0,  // TRUE/FALSE
        NonPartyKillable = 1,  // TRUE/FALSE
        RequireResurrection = 2,  // TRUE/FALSE
        LoseStolenItems = 3,  // TRUE/FALSE
        LoseItems = 4,  // TRUE/FALSE
        LoseExp = 5,  // TRUE/FALSE
        LoseGold = 6,  // TRUE/FALSE
        LoseGoldNum = 7,
        LoseExpNum = 8,
        LoseItemsNum = 9,
        PvpSetting = 10, // 0 = No PVP, 1 = Party PVP, 2 = Full PVP
        PauseAndPlay = 11, // TRUE/FALSE
        OnePartyOnly = 12, // TRUE/FALSE
        EnforceLegalCharacters = 13, // TRUE/FALSE
        ItemLevelRestrictions = 14, // TRUE/FALSE
        CDKeyBanListAllowList = 15, // TRUE/FALSE
        DisallowShouting = 16, // TRUE/FALSE
        ShowDMJoinedMessage = 17, // TRUE/FALSE
        BackupSavedCharacters = 18, // TRUE/FALSE
        AutoFailSaveOn1 = 19, // TRUE/FALSE
        ValidateSpells = 20, // TRUE/FALSE
        ExamineEffects = 21, // TRUE/FALSE
        ExamineChallengeRating = 22, // TRUE/FALSE
        UseMaxHitpoints = 23, // TRUE/FALSE
        RestoreSpellUses = 24, // TRUE/FALSE
        ResetEncounterSpawnPool = 25, // TRUE/FALSE
        HideHitpointsGained = 26, // TRUE/FALSE
    }
}
