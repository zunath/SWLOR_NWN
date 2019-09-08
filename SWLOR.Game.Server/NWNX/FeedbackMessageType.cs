namespace SWLOR.Game.Server.NWNX
{
    public class FeedbackMessageType
    {

        // Feedback Messages
        // For use with NWNX_Feedback_SetFeedbackMessageHidden() and
        //              NWNX_Feedback_GetFeedbackMessageHidden()

        // Skill Feedback Messages
        public const int SkillCantUse = 0;
        public const int SkillCantUseTimer = 1;
        public const int SkillAnimalEmpathyValidTargets = 2;
        public const int SkillTauntValidTargets = 3;
        public const int SkillTauntTargetImmune = 223;
        public const int SkillPickpocketStoleItem = 4;
        public const int SkillPickpocketStoleGold = 5;
        public const int SkillPickpocketAttemptingToSteal = 46;
        public const int SkillPickpocketAttemptDetected = 150;
        public const int SkillPickpocketStoleItemTarget = 47;
        public const int SkillPickpocketStoleGoldTarget = 48;
        public const int SkillPickpocketTargetBroke = 57;
        public const int SkillHealTargetNotDispsnd = 55;
        public const int SkillHealValidTargets = 56;
        public const int SkillStealthInCombat = 60;

        // Miscellaneous Targetting Messages
        public const int TargetUnaware = 6;
        public const int ActionNotPossibleStatus = 7;
        public const int ActionNotPossiblePVP = 187;
        public const int ActionCantReachTarget = 218;
        public const int ActionNoLoot = 247;

        // Miscellaneous Feedback Messages
        public const int WeightTooEncumberedToRun = 8;
        public const int WeightTooEncumberedWalkSlow = 9;
        public const int WeightTooEncumberedCantPickup = 10;
        public const int StatsLevelUp = 11;
        public const int InventoryFull = 12;
        public const int ContainerFull = 212;
        public const int TrapTriggered = 82;
        public const int DamageHealed = 151;
        public const int ExperienceGained = 182;
        public const int ExperienceLost = 183;
        public const int JournalUpdated = 184; // Doesn't actually work, use:
                                                      // NWNX_Feedback_{Get/Set}JournalUpdatedMessageHidden()
        public const int BarterCancelled = 185;

        // Mode activation/deactivation Messages
        public const int DetectModeActivated = 83;
        public const int DetectModeDeactivated= 84;
        public const int StealthModeActivated= 85;
        public const int StealthModeDeactivated= 86;
        public const int ParryModeActivated= 87;
        public const int ParryModeDeactivated= 88;
        public const int PowerAttackModeActivated= 89;
        public const int PowerAttackModeDeactivated= 90;
        public const int ImprovedPowerAttackModeActivated = 91;
        public const int ImprovedPowerAttackModeDeactivated = 92;
        public const int RapidShotModeActivated= 166;
        public const int RapidShotModeDeactivated= 167;
        public const int FlurryOfBlowsModeActivated= 168;
        public const int FlurryOfBlowsModeDeactivated = 169;
        public const int ExpertiseModeActivated = 227;
        public const int ExpertiseModeDeactivated = 228;
        public const int ImprovedExpertiseModeActivated= 229;
        public const int ImprovedExpertiseModeDeactivated= 230;
        public const int DefensiveCastModeActivated = 231;
        public const int DefensiveCastModeDeactivated = 232;
        public const int ModeCannotUseWeapons= 188;
        public const int DirtyFightingModeActivated = 237;
        public const int DirtyFightingModeDeactivated = 238;

        public const int DefensiveStanceModeActivated = 252;
        public const int DefensiveStanceModeDeactivated = 253;

        // Equipping Feedback Messages
        public const int EquipSkillSpellModifiers = 71;
        public const int EquipUnidentified = 76;
        public const int EquipMonkAbilities = 77;
        public const int EquipInsufficientLevel= 98;
        public const int EquipProficiencies = 119;
        public const int EquipWeaponTooLarge = 120;
        public const int EquipWeaponTooSmall= 260;
        public const int EquipOneHandedWeapon = 121;
        public const int EquipTwoHandedWeapon = 122;
        public const int EquipWeaponSwappedOut = 123;
        public const int EquipOneChainWeapon = 124;
        public const int EquipNaturalACNoStack = 189;
        public const int EquipArmourACNoStack = 190;
        public const int EquipShieldACNoStack = 191;
        public const int EquipDeflectionACNoStack = 192;
        public const int EquipNoArmorCombat = 193;
        public const int EquipRangerAbilities = 200;
        public const int EquipAlignment = 207;
        public const int EquipClass = 208;
        public const int EquipRace = 209;
        public const int UnequipNoArmorCombat = 194;

        // Action Feedback Messages
        public const int ObjectLocked= 13;
        public const int ObjectNotLocked= 14;
        public const int ObjectSpecialKey= 15;
        public const int ObjectusedKey = 16;
        public const int RestExcitedCantRest= 17;
        public const int RestBeginningRest = 18;
        public const int RestFinishedRest = 19;
        public const int RestCancelRest = 20;
        public const int RestNotAllowedInArea = 54;
        public const int RestNotAllowedByPossessedFamiliar = 153;
        public const int RestNotAllowedEnemies = 186;
        public const int RestCantUnderThisEffect = 213;
        public const int CastLostTarget = 21;
        public const int CastCantCast = 22;
        public const int CastCntrSpellTargetLostTarget = 52;
        public const int CastArcaneSpellFailure = 61;
        public const int CastCntrSpellTargetArcaneSpellFailure = 118;
        public const int CastEntangleConcentrationFailure = 65;
        public const int CastCntrSpellTargetEntangleConcentrationFailure = 147;
        public const int CastSpellInterrupted = 72;
        public const int CastEffectSpellFailure = 236;
        public const int CastCantCastWhilePolymorphed = 107;
        public const int CantUseHands = 210;
        public const int CantUseMouth = 211;
        public const int CaseDefCastConcentrationFailure = 233;
        public const int CastDefCastConcentrationSuccess = 240;
        public const int UseItemCantUse = 23;
        public const int ConversationTooFar = 58;
        public const int ConversationBusy = 59;
        public const int ConversationInCombat = 152;
        public const int CharacterInTransit = 74;
        public const int CharacterOutTransit = 75;
        public const int UseItemNotEquipped = 244;
        public const int DropItemCantDrop = 245;
        public const int DropItemCantGive = 246;
        public const int ClientServerSpellMismatch = 259;

        // Combat feedback messages
        public const int CombatRunningOutOfAmmo = 24;
        public const int CombatOutOfAmmo = 25;
        public const int CombatHenchmanOutOfAmmo = 241;
        public const int CombatDamageImmunity = 62;
        public const int CombatSpellImmunity = 68;
        public const int CombatDamageResistance = 63;
        public const int CombatDamageResistanceRemaining = 66;
        public const int CombatDamageReduction = 64;
        public const int CombatDamageReductionRemaining = 67;
        public const int CombatSpellLevelAbsorption = 69;
        public const int CombatSpellLevelAbsorptionRemaining = 70;
        public const int CombatWeaponNotEffective = 117;
        public const int CombatEpicDodgeAttackEvaded = 234;
        public const int CombatMassiveDamage = 235;
        public const int CombatSavedVsMassiveDamage = 254;
        public const int CombatSavedVsDevastatingCritical = 257;

        // Feat Feedback Messages
        public const int FeatSapValidTargets = 26;
        public const int FeatKnockdownValidTargets = 27;
        public const int FeatImpKnockdownValidTargets = 28;
        public const int FeatCalledShotNoLegs = 29;
        public const int FeatCalledShotNoArms = 30;
        public const int FeatSmiteGoodTargetNotGood = 239;
        public const int FeatSmiteEvilTargetNotEvil = 53;
        public const int FeatQuiveringPalmHigherLevel = 73;
        public const int FeatKeenSenseDetect = 195;
        public const int FeatUseUnarmed = 198;
        public const int FeatUses = 199;
        public const int FeatUseWeaponOfChoice = 243;

        // Party Feedback Messages
        public const int PartyNewLeader = 31;
        public const int PartyMemberKicked = 32;
        public const int PartyKickedYou = 33;
        public const int PartyAlreadyConsidering = 34;
        public const int PartyAlreadyInvolved = 35;
        public const int PartySentInvitation = 36;
        public const int PartyReceivedInvitation = 37;
        public const int PartyJoined = 38;
        public const int PartyInvitationIgnored = 39;
        public const int PartyYouIgnoredInvitation = 40;
        public const int PartyInvitationRejected = 41;
        public const int PartyYouRejectedInvitation = 42;
        public const int PartyInvitationExpired = 43;
        public const int PartyLeftParty = 44;
        public const int PartyYouLeft = 45;
        public const int PartyHenchmanLimit = 49;
        public const int PartyCannotLeaveTheOneParty = 196;
        public const int PartyCannotKickFromTheOneParty = 197;
        public const int PartyYouInvitedNonSingleton = 202;
        public const int PVPReactionDislikesYou = 203;

        // Item Feedback Messages
        public const int ItemReceived = 50;
        public const int ItemLost = 51;
        public const int ItemEjected = 96;
        public const int ItemUseUnidentified = 97;
        public const int ItemGoldGained = 148;
        public const int ItemGoldLost = 149;

        // Spell Scroll Learning
        public const int LearnScrollNotScroll= 78;
        public const int LearnScrollCantLearnClass = 79;
        public const int LearnScrollCantLearnLevel = 80;
        public const int LearnScrollCantLearnAbility= 81;
        public const int LearnScrollCantLearnOpposition = 219;
        public const int LearnScrollCantLearnPossess = 220;
        public const int LearnScrollCantLearnKnown = 221;
        public const int LearnScrollCantLearnDivine = 224;
        public const int LearnScrollSuccess= 222;

        // Floaty text feedback
        public const int FloatyTextStrRef = 93;
        public const int FloatyTextString = 94;

        // store feedback
        public const int CannotSellPlotItem = 99;
        public const int CannotSellContainer = 100;
        public const int CannotSellItem = 101;
        public const int NotEnoughGold = 102;
        public const int TransactionSucceeded = 103;
        public const int PriceTooHigh = 248;
        public const int StoreNotEnoughGold = 249;
        public const int CannotSellStolenItem= 250;
        public const int CannotSellRestrictedItem = 251;

        // Portal control feedback
        public const int PortalTimedOut = 104;
        public const int PortalInvalid= 105;

        // Chat feedback
        public const int ChatTellPlayerNotFound = 106;

        // Alignment Feedback
        public const int AlignmentShift = 108;
        public const int AlignmentPartyShift = 111;
        public const int AlignmentChange = 109;
        public const int AlignmentRestrictedByClassLost = 110;
        public const int AlignmentRestrictedByClassGain = 115;
        public const int AlignmentRestrictedWarningLoss = 116;
        public const int AlignmentRestrictedWarningGain = 112;
        public const int AlignmentEpitomeGained = 113;
        public const int AlignmentEpitomeLost = 114;

        // Immunity Feedback
        public const int ImmunityDisease = 125;
        public const int ImmunityCriticalHit = 126;
        public const int ImmunityDeathMagic = 127;
        public const int ImmunityFear = 128;
        public const int ImmunityKnockdown = 129;
        public const int ImmunityParalysis = 130;
        public const int ImmunityNegativeLevel = 131;
        public const int ImmunityMindSpells = 132;
        public const int ImmunityPoison = 133;
        public const int ImmunitySneakAttack = 134;
        public const int ImmunitySleep = 135;
        public const int ImmunityDaze = 136;
        public const int ImmunityConfusion = 137;
        public const int ImmunityStun = 138;
        public const int ImmunityBlindness = 139;
        public const int ImmunityDeafness= 140;
        public const int ImmunityCurse = 141;
        public const int ImmunityCharm = 142;
        public const int ImmunityDominate = 143;
        public const int ImmunityEntangle = 144;
        public const int ImmunitySilence = 145;
        public const int ImmunitySlow = 146;

        // Associates
        public const int AssociateSummoned = 154;
        public const int AssociateUnsummoning = 155;
        public const int AssociateUnsummoningBecauseRest = 156;
        public const int AssociateUnsummoningBecauseDied = 157;
        public const int AssociateDominated = 158;
        public const int AssociateDominationEnded = 159;
        public const int AssociatePossessedCannotRecoverTrap = 170;
        public const int AssociatePossessedCannotBarter = 171;
        public const int AssociatePossessedCannotEquip = 172;
        public const int AssociatePossessedCannotRepositoryMove = 173;
        public const int AssociatePossessedCannotPickUp = 174;
        public const int AssociatePossessedCannotDrop = 175;
        public const int AssociatePossessedCannotUnequip = 176;
        public const int AssociatePossessedCannotRest = 177;
        public const int AssociatePossessedCannotDialogue = 178;
        public const int AssociatePossessedCannotGiveItem= 179;
        public const int AssociatePossessedCannotTakeItem = 180;
        public const int AssociatePossessedCannotUSE_CONTAINER = 181;

        public const int ScriptError = 160;
        public const int ActionListOverflow = 161;
        public const int EffectListOverflow = 162;
        public const int AIUpdateTimeOverflow = 163;
        public const int ActionListWipeOverflow = 164;
        public const int EffectListWipeOverflow = 165;
        public const int SendMessageToPC = 204;
        public const int SendMessageToPCStrRef = 242;

        // Misc GUI feedback
        public const int GuiOnlyPartyLeaderMayClick = 201;
        public const int Paused = 205;
        public const int Unpaused = 206;
        public const int RestYouMayNotAtThisTime = 214;
        public const int GuiCharExportRequestSent = 215;
        public const int GuiCharExportedSuccessfully = 216;
        public const int GuiErrorCharNotExported = 217;
        public const int CameraBG = 255;
        public const int CameraEq = 256;
        public const int CameraChaseCam = 258;

        public const int Saving = 225;
        public const int SaveComplete = 226;
    }
}
