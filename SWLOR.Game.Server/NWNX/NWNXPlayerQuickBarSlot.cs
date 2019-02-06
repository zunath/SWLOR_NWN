using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.NWNX.Contracts;

namespace SWLOR.Game.Server.NWNX
{
    public class NWNXPlayerQuickBarSlot: NWNXBase, INWNXPlayerQuickBarSlot
    {
        public NWNXPlayerQuickBarSlot(INWScript script) 
            : base(script)
        {
        }
        
        /// <summary>
        /// Create an empty QBS of given type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public QuickBarSlot Empty(QuickBarSlotType type)
        {
            QuickBarSlot qbs = new QuickBarSlot
            {
                ObjectType = type,
                Item = (new Object()),
                SecondaryItem = (new Object()),
                MultiClass = 0,
                Resref = "",
                CommandLabel = "",
                CommandLine = "",
                ToolTip = "",
                INTParam1 = 0,
                MetaType = 0,
                DomainLevel = 0,
                AssociateType = 0,
                Associate = (new Object())
            };
            
            return qbs;
        }

        /// <summary>
        /// Create a QBS for using an item
        /// </summary>
        /// <param name="oItem"></param>
        /// <param name="nPropertyID"></param>
        /// <returns></returns>
        public QuickBarSlot UseItem(NWItem oItem, int nPropertyID)
        {
            QuickBarSlot qbs = Empty(QuickBarSlotType.Item);

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
        public QuickBarSlot EquipItem(NWItem oItem, NWItem oSecondaryItem)
        {
            QuickBarSlot qbs = Empty(QuickBarSlotType.Item);

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
        public QuickBarSlot CastSpell(int nSpell, int nClassIndex, int nMetamagic, int nDomainLevel)
        {
            QuickBarSlot qbs = Empty(QuickBarSlotType.Spell);

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
        public QuickBarSlot UseSkill(int nSkill)
        {
            QuickBarSlot qbs = Empty(QuickBarSlotType.Skill);

            qbs.INTParam1 = nSkill;

            return qbs;
        }

        /// <summary>
        /// Create a QBS for using a feat
        /// </summary>
        /// <param name="nFeat"></param>
        /// <returns></returns>
        public QuickBarSlot UseFeat(int nFeat)
        {
            QuickBarSlot qbs = Empty(QuickBarSlotType.Feat);

            qbs.INTParam1 = nFeat;

            return qbs;
        }

        /// <summary>
        /// Create a QBS for starting a dialog
        /// </summary>
        /// <returns></returns>
        public QuickBarSlot StartDialog()
        {
            return Empty(QuickBarSlotType.Dialog);
        }

        /// <summary>
        /// Create a QBS for attacking
        /// </summary>
        /// <returns></returns>
        public QuickBarSlot Attack()
        {
            return Empty(QuickBarSlotType.Attack);
        }

        /// <summary>
        /// Create a QBS for emoting
        /// </summary>
        /// <param name="nEmote"></param>
        /// <returns></returns>
        public QuickBarSlot Emote(int nEmote)
        {
            QuickBarSlot qbs = Empty(QuickBarSlotType.Emote);

            qbs.INTParam1 = nEmote;

            return qbs;
        }

        /// <summary>
        /// Create a QBS for toggling a mode
        /// </summary>
        /// <param name="nMode"></param>
        /// <returns></returns>
        public QuickBarSlot ToggleMode(int nMode)
        {
            QuickBarSlot qbs = Empty(QuickBarSlotType.ModeToggle);

            qbs.INTParam1 = nMode;

            return qbs;
        }

        /// <summary>
        /// Create a QBS for examining
        /// </summary>
        /// <returns></returns>
        public QuickBarSlot Examine()
        {
            return Empty(QuickBarSlotType.Examine);
        }

        /// <summary>
        /// Create a QBS for bartering
        /// </summary>
        /// <returns></returns>
        public QuickBarSlot Barter()
        {
            return Empty(QuickBarSlotType.Barter);
        }

        /// <summary>
        /// Create a QBS for quickchat command
        /// </summary>
        /// <param name="nCommand"></param>
        /// <returns></returns>
        public QuickBarSlot QuickChat(int nCommand)
        {
            QuickBarSlot qbs = Empty(QuickBarSlotType.QuickChat);

            qbs.INTParam1 = nCommand;

            return qbs;
        }

        /// <summary>
        /// Create a QBS for possessing a familiar
        /// </summary>
        /// <returns></returns>
        public QuickBarSlot PossessFamiliar()
        {
            return Empty(QuickBarSlotType.PossessFamiliar);
        }

        /// <summary>
        /// Create a QBS for casting a spell
        /// </summary>
        /// <param name="nSpell"></param>
        /// <param name="nCasterLevel"></param>
        /// <returns></returns>
        public QuickBarSlot UseSpecialAbility(int nSpell, int nCasterLevel)
        {
            QuickBarSlot qbs = Empty(QuickBarSlotType.Spell);

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
        public QuickBarSlot Command(string sCommandLabel, string sCommandLine)
        {
            QuickBarSlot qbs = Empty(QuickBarSlotType.Command);

            qbs.CommandLabel = sCommandLabel;
            qbs.CommandLine = sCommandLine;

            return qbs;
        }
}
}
