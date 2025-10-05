namespace SWLOR.Shared.Domain.Associate
{
    public class AssociateScriptName
    {
        // Associate events
        public const string OnAssociateAddBefore = "asso_add_bef";
        public const string OnAssociateAddAfter = "asso_add_aft";
        public const string OnAssociateRemoveBefore = "asso_rem_bef";
        public const string OnAssociateRemoveAfter = "asso_rem_aft";
        public const string OnAssociateStateEffect = "assoc_stateffect";

        // Beast events
        public const string OnBeastBlocked = "beast_blocked";
        public const string OnBeastRoundEnd = "beast_roundend";
        public const string OnBeastConversation = "beast_convers";
        public const string OnBeastDamaged = "beast_damaged";
        public const string OnBeastDeath = "beast_death";
        public const string OnBeastDisturbed = "beast_disturbed";
        public const string OnBeastHeartbeat = "beast_hb";
        public const string OnBeastPerception = "beast_perception";
        public const string OnBeastAttacked = "beast_attacked";
        public const string OnBeastRest = "beast_rest";
        public const string OnBeastSpawn = "beast_spawn";
        public const string OnBeastSpellCast = "beast_spellcast";
        public const string OnBeastUserDefined = "beast_userdef";
        public const string OnBeastTerminate = "beast_term";

        // Droid events
        public const string OnDroidAssociateUsed = "droid_ass_used";
        public const string OnDroidBlocked = "droid_blocked";
        public const string OnDroidRoundEnd = "droid_roundend";
        public const string OnDroidConversation = "droid_convers";
        public const string OnDroidDamaged = "droid_damaged";
        public const string OnDroidDeath = "droid_death";
        public const string OnDroidDisturbed = "droid_disturbed";
        public const string OnDroidHeartbeat = "droid_hb";
        public const string OnDroidPerception = "droid_perception";
        public const string OnDroidAttacked = "droid_attacked";
        public const string OnDroidRest = "droid_rest";
        public const string OnDroidSpawn = "droid_spawn";
        public const string OnDroidSpellCast = "droid_spellcast";
        public const string OnDroidUserDefined = "droid_userdef";

        // Associate-related events
        public const string OnIncubatorTerminal = "incubator_term";
        public const string OnDNAExtractUsed = "dna_extract_used";
    }
}
