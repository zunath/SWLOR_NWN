using SWLOR.NWN.API.NWNX.Enum;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWNX
{
    public static class PlayerQuickBarSlot
    {
        /// <summary>
        /// Creates an empty QuickBarSlot of the specified type.
        /// </summary>
        /// <param name="type">The type of QuickBarSlot to create</param>
        /// <returns>An empty QuickBarSlot with the specified type</returns>
        public static QuickBarSlot Empty(QuickBarSlotType type)
        {
            var qbs = new QuickBarSlot
            {
                ObjectType = type,
                Item = OBJECT_INVALID,
                SecondaryItem = OBJECT_INVALID,
                MultiClass = 0,
                Resref = "",
                CommandLabel = "",
                CommandLine = "",
                ToolTip = "",
                INTParam1 = 0,
                MetaType = 0,
                DomainLevel = 0,
                AssociateType = 0,
                Associate = OBJECT_INVALID
            };

            return qbs;
        }

        /// <summary>
        /// Creates a QuickBarSlot for using an item.
        /// </summary>
        /// <param name="oItem">The item to use</param>
        /// <param name="nPropertyID">The property ID of the item</param>
        /// <returns>A QuickBarSlot configured for item usage</returns>
        public static QuickBarSlot UseItem(uint oItem, int nPropertyID)
        {
            var qbs = Empty(QuickBarSlotType.Item);

            qbs.Item = oItem;
            qbs.INTParam1 = nPropertyID;

            return qbs;
        }

        /// <summary>
        /// Creates a QuickBarSlot for equipping an item.
        /// </summary>
        /// <param name="oItem">The primary item to equip</param>
        /// <param name="oSecondaryItem">The secondary item to equip (e.g., off-hand weapon)</param>
        /// <returns>A QuickBarSlot configured for item equipping</returns>
        public static QuickBarSlot EquipItem(uint oItem, uint oSecondaryItem)
        {
            var qbs = Empty(QuickBarSlotType.Item);

            qbs.Item = oItem;
            qbs.SecondaryItem = oSecondaryItem;

            return qbs;
        }

        /// <summary>
        /// Creates a QuickBarSlot for casting a spell.
        /// </summary>
        /// <param name="nSpell">The spell ID to cast</param>
        /// <param name="nClassIndex">The class index for the spell</param>
        /// <param name="nMetamagic">The metamagic type to apply</param>
        /// <param name="nDomainLevel">The domain level for the spell</param>
        /// <returns>A QuickBarSlot configured for spell casting</returns>
        public static QuickBarSlot CastSpell(int nSpell, int nClassIndex, int nMetamagic, int nDomainLevel)
        {
            var qbs = Empty(QuickBarSlotType.Spell);

            qbs.INTParam1 = nSpell;
            qbs.MultiClass = nClassIndex;
            qbs.MetaType = nMetamagic;
            qbs.DomainLevel = nDomainLevel;

            return qbs;
        }

        /// <summary>
        /// Creates a QuickBarSlot for using a skill.
        /// </summary>
        /// <param name="nSkill">The skill ID to use</param>
        /// <returns>A QuickBarSlot configured for skill usage</returns>
        public static QuickBarSlot UseSkill(int nSkill)
        {
            var qbs = Empty(QuickBarSlotType.Skill);

            qbs.INTParam1 = nSkill;

            return qbs;
        }

        /// <summary>
        /// Creates a QuickBarSlot for using a feat.
        /// </summary>
        /// <param name="nFeat">The feat type to use</param>
        /// <returns>A QuickBarSlot configured for feat usage</returns>
        public static QuickBarSlot UseFeat(FeatType nFeat)
        {
            var qbs = Empty(QuickBarSlotType.Feat);

            qbs.INTParam1 = (int)nFeat;

            return qbs;
        }

        /// <summary>
        /// Creates a QuickBarSlot for starting a dialog.
        /// </summary>
        /// <returns>A QuickBarSlot configured for dialog interaction</returns>
        public static QuickBarSlot StartDialog()
        {
            return Empty(QuickBarSlotType.Dialog);
        }

        /// <summary>
        /// Creates a QuickBarSlot for attacking.
        /// </summary>
        /// <returns>A QuickBarSlot configured for attack action</returns>
        public static QuickBarSlot Attack()
        {
            return Empty(QuickBarSlotType.Attack);
        }

        /// <summary>
        /// Creates a QuickBarSlot for emoting.
        /// </summary>
        /// <param name="nEmote">The emote ID to perform</param>
        /// <returns>A QuickBarSlot configured for emote action</returns>
        public static QuickBarSlot Emote(int nEmote)
        {
            var qbs = Empty(QuickBarSlotType.Emote);

            qbs.INTParam1 = nEmote;

            return qbs;
        }

        /// <summary>
        /// Creates a QuickBarSlot for toggling a mode.
        /// </summary>
        /// <param name="nMode">The mode ID to toggle</param>
        /// <returns>A QuickBarSlot configured for mode toggling</returns>
        public static QuickBarSlot ToggleMode(int nMode)
        {
            var qbs = Empty(QuickBarSlotType.ModeToggle);

            qbs.INTParam1 = nMode;

            return qbs;
        }

        /// <summary>
        /// Creates a QuickBarSlot for examining objects.
        /// </summary>
        /// <returns>A QuickBarSlot configured for examine action</returns>
        public static QuickBarSlot Examine()
        {
            return Empty(QuickBarSlotType.Examine);
        }

        /// <summary>
        /// Creates a QuickBarSlot for bartering with merchants.
        /// </summary>
        /// <returns>A QuickBarSlot configured for barter action</returns>
        public static QuickBarSlot Barter()
        {
            return Empty(QuickBarSlotType.Barter);
        }

        /// <summary>
        /// Creates a QuickBarSlot for quickchat commands.
        /// </summary>
        /// <param name="nCommand">The quickchat command ID</param>
        /// <returns>A QuickBarSlot configured for quickchat command</returns>
        public static QuickBarSlot QuickChat(int nCommand)
        {
            var qbs = Empty(QuickBarSlotType.QuickChat);

            qbs.INTParam1 = nCommand;

            return qbs;
        }

        /// <summary>
        /// Creates a QuickBarSlot for possessing a familiar.
        /// </summary>
        /// <returns>A QuickBarSlot configured for familiar possession</returns>
        public static QuickBarSlot PossessFamiliar()
        {
            return Empty(QuickBarSlotType.PossessFamiliar);
        }

        /// <summary>
        /// Creates a QuickBarSlot for using a special ability.
        /// </summary>
        /// <param name="nSpell">The spell/ability ID to use</param>
        /// <param name="nCasterLevel">The caster level for the ability</param>
        /// <returns>A QuickBarSlot configured for special ability usage</returns>
        public static QuickBarSlot UseSpecialAbility(int nSpell, int nCasterLevel)
        {
            var qbs = Empty(QuickBarSlotType.Spell);

            qbs.INTParam1 = nSpell;
            qbs.DomainLevel = nCasterLevel;

            return qbs;
        }

        /// <summary>
        /// Creates a QuickBarSlot for running a custom command.
        /// </summary>
        /// <param name="sCommandLabel">The display label for the command</param>
        /// <param name="sCommandLine">The command line to execute</param>
        /// <returns>A QuickBarSlot configured for custom command execution</returns>
        public static QuickBarSlot Command(string sCommandLabel, string sCommandLine)
        {
            var qbs = Empty(QuickBarSlotType.Command);

            qbs.CommandLabel = sCommandLabel;
            qbs.CommandLine = sCommandLine;

            return qbs;
        }
    }
}
