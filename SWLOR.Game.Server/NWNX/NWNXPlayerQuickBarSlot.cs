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


        // Create an empty QBS of given type
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

        // Create a QBS for using an item
        public QuickBarSlot UseItem(NWItem oItem, int nPropertyID)
        {
            QuickBarSlot qbs = Empty(QuickBarSlotType.Item);

            qbs.Item = oItem;
            qbs.INTParam1 = nPropertyID;

            return qbs;
        }

        // Create a QBS for equipping an item
        public QuickBarSlot EquipItem(NWItem oItem, NWItem oSecondaryItem)
        {
            QuickBarSlot qbs = Empty(QuickBarSlotType.Item);

            qbs.Item = oItem;
            qbs.SecondaryItem = oSecondaryItem;

            return qbs;
        }

        // Create a QBS for casting a spell
        public QuickBarSlot CastSpell(int nSpell, int nClassIndex, int nMetamagic, int nDomainLevel)
        {
            QuickBarSlot qbs = Empty(QuickBarSlotType.Spell);

            qbs.INTParam1 = nSpell;
            qbs.MultiClass = nClassIndex;
            qbs.MetaType = nMetamagic;
            qbs.DomainLevel = nDomainLevel;

            return qbs;
        }

        // Create a QBS for using a skill
        public QuickBarSlot UseSkill(int nSkill)
        {
            QuickBarSlot qbs = Empty(QuickBarSlotType.Skill);

            qbs.INTParam1 = nSkill;

            return qbs;
        }

        // Create a QBS for using a feat
        public QuickBarSlot UseFeat(int nFeat)
        {
            QuickBarSlot qbs = Empty(QuickBarSlotType.Feat);

            qbs.INTParam1 = nFeat;

            return qbs;
        }

        // Create a QBS for starting a dialog
        public QuickBarSlot StartDialog()
        {
            return Empty(QuickBarSlotType.Dialog);
        }

        // Create a QBS for attacking
        public QuickBarSlot Attack()
        {
            return Empty(QuickBarSlotType.Attack);
        }

        // Create a QBS for emoting
        public QuickBarSlot Emote(int nEmote)
        {
            QuickBarSlot qbs = Empty(QuickBarSlotType.Emote);

            qbs.INTParam1 = nEmote;

            return qbs;
        }

        // Create a QBS for toggling a mode
        public QuickBarSlot ToggleMode(int nMode)
        {
            QuickBarSlot qbs = Empty(QuickBarSlotType.ModeToggle);

            qbs.INTParam1 = nMode;

            return qbs;
        }

        // Create a QBS for examining
        public QuickBarSlot Examine()
        {
            return Empty(QuickBarSlotType.Examine);
        }

        // Create a QBS for bartering
        public QuickBarSlot Barter()
        {
            return Empty(QuickBarSlotType.Barter);
        }

        // Create a QBS for quickchat command
        public QuickBarSlot QuickChat(int nCommand)
        {
            QuickBarSlot qbs = Empty(QuickBarSlotType.QuickChat);

            qbs.INTParam1 = nCommand;

            return qbs;
        }

        // Create a QBS for possessing a familiar
        public QuickBarSlot PossessFamiliar()
        {
            return Empty(QuickBarSlotType.PossessFamiliar);
        }

        // Create a QBS for casting a spell
        public QuickBarSlot UseSpecialAbility(int nSpell, int nCasterLevel)
        {
            QuickBarSlot qbs = Empty(QuickBarSlotType.Spell);

            qbs.INTParam1 = nSpell;
            qbs.DomainLevel = nCasterLevel;

            return qbs;
        }
    }
}
