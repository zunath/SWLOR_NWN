using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Core.NWNX
{
    public static class PlayerQuickBarSlot
    {
        /// <summary>
        /// Create an empty QBS of given type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
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
        /// Create a QBS for using an item
        /// </summary>
        /// <param name="oItem"></param>
        /// <param name="nPropertyID"></param>
        /// <returns></returns>
        public static QuickBarSlot UseItem(uint oItem, int nPropertyID)
        {
            var qbs = Empty(QuickBarSlotType.Item);

            qbs.Item = oItem;
            qbs.INTParam1 = nPropertyID;

            return qbs;
        }

        /// <summary>
        /// Create a QBS for equipping an item
        /// </summary>
        /// <param name="oItem"></param>
        /// <param name="oSecondaryItem"></param>
        /// <returns></returns>
        public static QuickBarSlot EquipItem(uint oItem, uint oSecondaryItem)
        {
            var qbs = Empty(QuickBarSlotType.Item);

            qbs.Item = oItem;
            qbs.SecondaryItem = oSecondaryItem;

            return qbs;
        }

        /// <summary>
        /// Create a QBS for casting a spell
        /// </summary>
        /// <param name="nSpell"></param>
        /// <param name="nClassIndex"></param>
        /// <param name="nMetamagic"></param>
        /// <param name="nDomainLevel"></param>
        /// <returns></returns>
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
        /// Create a QBS for using a skill
        /// </summary>
        /// <param name="nSkill"></param>
        /// <returns></returns>
        public static QuickBarSlot UseSkill(int nSkill)
        {
            var qbs = Empty(QuickBarSlotType.Skill);

            qbs.INTParam1 = nSkill;

            return qbs;
        }

        /// <summary>
        /// Create a QBS for using a feat
        /// </summary>
        /// <param name="nFeat"></param>
        /// <returns></returns>
        public static QuickBarSlot UseFeat(FeatType nFeat)
        {
            var qbs = Empty(QuickBarSlotType.Feat);

            qbs.INTParam1 = (int)nFeat;

            return qbs;
        }

        /// <summary>
        /// Create a QBS for starting a dialog
        /// </summary>
        /// <returns></returns>
        public static QuickBarSlot StartDialog()
        {
            return Empty(QuickBarSlotType.Dialog);
        }

        /// <summary>
        /// Create a QBS for attacking
        /// </summary>
        /// <returns></returns>
        public static QuickBarSlot Attack()
        {
            return Empty(QuickBarSlotType.Attack);
        }

        /// <summary>
        /// Create a QBS for emoting
        /// </summary>
        /// <param name="nEmote"></param>
        /// <returns></returns>
        public static QuickBarSlot Emote(int nEmote)
        {
            var qbs = Empty(QuickBarSlotType.Emote);

            qbs.INTParam1 = nEmote;

            return qbs;
        }

        /// <summary>
        /// Create a QBS for toggling a mode
        /// </summary>
        /// <param name="nMode"></param>
        /// <returns></returns>
        public static QuickBarSlot ToggleMode(int nMode)
        {
            var qbs = Empty(QuickBarSlotType.ModeToggle);

            qbs.INTParam1 = nMode;

            return qbs;
        }

        /// <summary>
        /// Create a QBS for examining
        /// </summary>
        /// <returns></returns>
        public static QuickBarSlot Examine()
        {
            return Empty(QuickBarSlotType.Examine);
        }

        /// <summary>
        /// Create a QBS for bartering
        /// </summary>
        /// <returns></returns>
        public static QuickBarSlot Barter()
        {
            return Empty(QuickBarSlotType.Barter);
        }

        /// <summary>
        /// Create a QBS for quickchat command
        /// </summary>
        /// <param name="nCommand"></param>
        /// <returns></returns>
        public static QuickBarSlot QuickChat(int nCommand)
        {
            var qbs = Empty(QuickBarSlotType.QuickChat);

            qbs.INTParam1 = nCommand;

            return qbs;
        }

        /// <summary>
        /// Create a QBS for possessing a familiar
        /// </summary>
        /// <returns></returns>
        public static QuickBarSlot PossessFamiliar()
        {
            return Empty(QuickBarSlotType.PossessFamiliar);
        }

        /// <summary>
        /// Create a QBS for casting a spell
        /// </summary>
        /// <param name="nSpell"></param>
        /// <param name="nCasterLevel"></param>
        /// <returns></returns>
        public static QuickBarSlot UseSpecialAbility(int nSpell, int nCasterLevel)
        {
            var qbs = Empty(QuickBarSlotType.Spell);

            qbs.INTParam1 = nSpell;
            qbs.DomainLevel = nCasterLevel;

            return qbs;
        }

        /// <summary>
        /// Create a QBS for running a command
        /// </summary>
        /// <param name="sCommandLabel"></param>
        /// <param name="sCommandLine"></param>
        /// <returns></returns>
        public static QuickBarSlot Command(string sCommandLabel, string sCommandLine)
        {
            var qbs = Empty(QuickBarSlotType.Command);

            qbs.CommandLabel = sCommandLabel;
            qbs.CommandLine = sCommandLine;

            return qbs;
        }
    }
}
