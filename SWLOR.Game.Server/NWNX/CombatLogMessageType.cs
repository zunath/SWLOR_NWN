namespace SWLOR.Game.Server.NWNX
{
    public class CombatLogMessageType
    {

        // CombatLog Messages
        // For use with NWNX_Feedback_SetCombatLogMessageHidden() and
        //              NWNX_Feedback_GetCombatLogMessageHidden()

        public const int SimpleAdjective     = 1;
        public const int SimpleDamage        = 2;
        public const int ComplexDamage       = 3;
        public const int ComplexDeath        = 4;
        public const int ComplexAttack       = 5;
        public const int SpecialAttack       = 6;
        public const int SavingThrow         = 7;
        public const int CastSpell           = 8;
        public const int UseSkill            = 9;
        public const int SpellResistance     = 10;
        public const int Feedback            = 11; // NOTE: This hides ALL feedback messages, to hide individual messages use NWNX_Feedback_SetFeedbackMessageHidden()
        public const int Counterspell        = 12;
        public const int TouchAttack         = 13;
        public const int Initiative          = 14;
        public const int DispelMagic         = 15;
        public const int Polymorph           = 17;
        public const int FeedbackString      = 18;
        public const int Vibrate             = 19;
        public const int UnlockAchievement   = 20;

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
        // 12 -> Counterspel: <charname> casts <spell name> : *spell countered by* : <charname> casting <spell name>
        // 13 -> TouchAttack: <charname> attempts <melee/ranged touch attack> on <charname> : *hit/miss/critical* : (<attack roll> + <attack mod> = <modified roll>)
        // 14 -> Initiative: <charname> : Initiative Roll : <total> : (<roll> + <modifier> = <total>)
        // 15 -> Dispel_Magic: Dispel Magic : <charname> : <spell name>, <spell name>, <spell name>...
        // 17 -> Unused, probably
        // 18 -> Same as 11, maybe. Might be unused too
        // 19 -> Unused
        // 20 -> Unused
        
    }
}
