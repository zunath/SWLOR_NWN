namespace SWLOR.Game.Server.Service.MasteryService
{
    /// <summary>
    /// Identifies which business rule a MasteryRuleViolation came from. Every value except
    /// OffLimit is a warning staff may override with a reason - see MasteryRules.ValidateRequest.
    /// </summary>
    public enum MasteryRuleType
    {
        /// <summary>Hard-blocking. The mastery is OffLimit and cannot be requested.</summary>
        OffLimit = 0,

        /// <summary>The character already holds a different Rare mastery.</summary>
        RareConflict = 1,

        /// <summary>The character already holds a different mastery at tier 5.</summary>
        Tier5Conflict = 2,

        /// <summary>The requested tier is not exactly the mastery's current tier + 1.</summary>
        TierProgression = 3,

        /// <summary>The character is younger than the minimum age to request masteries.</summary>
        CharacterAge = 4,

        /// <summary>The character does not meet the associated skill's rank requirement.</summary>
        SkillRank = 5,

        /// <summary>The character's training queue is already at the 3-entry cap.</summary>
        QueueFull = 6,

        /// <summary>Granting this request would exceed the 17-level total cap.</summary>
        LevelCap = 7,

        /// <summary>Hard-blocking. The requested tier is outside the valid 1-5 range.</summary>
        InvalidTier = 8
    }
}
