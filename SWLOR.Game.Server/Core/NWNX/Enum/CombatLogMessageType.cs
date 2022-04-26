namespace SWLOR.Game.Server.Core.NWNX.Enum
{
    public enum CombatLogMessageType
    {
        SimpleAdjective = 1,
        SimpleDamage = 2,
        ComplexDamage = 3,
        ComplexDeath = 4,
        ComplexAttack = 5,
        SpecialAttack = 6,
        SavingThrow = 7,
        CastSpell = 8,
        UseSkill = 9,
        SpellResistance = 10,

        Feedback = 11, // NOTE: This hides ALL feedback messages, to hide individual messages use NWNX_Feedback_SetFeedbackMessageHidden()
        Counterspell = 12,
        TouchAttack = 13,
        Initiative = 14,
        DispelMagic = 15,
        Polymorph = 17,
        FeedbackString = 18,
        Vibrate = 19,
        UnlockAchievement = 20,
        PostAurString = 22, // PostString messages
        EnterTargetingMode = 23


        // 1  -> Simple_Adjective: <charname> : <adjective described by strref>
        // 2  -> Simple_Damage: <charname> damaged : <amount>
        // 3  -> Complex_Damage: <charname> damages <charname> : <amount>
        // 4  -> Complex_Death: <charname> killed <charname>
        // 5  -> Complex_Attack: <charname> attacks <charname> : *hit* / *miss* / *parried* : (<attack roll> + <attack mod> = <modified total>)
        // 6  -> Special_Attack: <charname> attempts <special attack> on <charname> : *success* / *failure* : (<attack roll> + <attack mod> = <modified roll>)
        // 7  -> Saving_Throw: <charname> : <saving throw type> : *success* / *failure* : (<saving throw roll> + <saving throw modifier> = <modified total>)
        // 8  -> Cast_Spell: <charname> casts <spell name> : Spellcraft check *failure* / *success*
        // 9  -> Use_Skill: <charname> : <skill name> : *success* / *failure* : (<skill roll> + <skill modifier> = <modified total> vs <DC> )
        // 10 -> Spell_Resistance: <charname> : Spell Resistance <SR value> : *success* / *failure*
        // 11 -> Feedback: Reason skill/feat/ability failed.
        // 12 -> Counterspell: <charname> casts <spell name> : *spell countered by* : <charname> casting <spell name>
        // 13 -> TouchAttack: <charname> attempts <melee/ranged touch attack> on <charname> : *hit/miss/critical* : (<attack roll> + <attack mod> = <modified roll>)
        // 14 -> Initiative: <charname> : Initiative Roll : <total> : (<roll> + <modifier> = <total>)
        // 15 -> Dispel_Magic: Dispel Magic : <charname> : <spell name>, <spell name>, <spell name>...
        // 17 -> Unused, probably
        // 18 -> Same as 11, maybe. Might be unused too
        // 19 -> Unused
        // 20 -> Unused
    }
}