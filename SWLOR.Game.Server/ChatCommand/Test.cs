using NWN;
using SWLOR.Game.Server.Bioware.Contracts;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.NWNX.Contracts;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Testing command.", CommandPermissionType.DM)]
    public class Test : IChatCommand
    {
        private readonly INWScript _;
        private readonly INWNXItemProperty _nwnxItemProperty;
        private readonly IBiowareXP2 _biowareXP2;

        public Test(INWScript script,
            INWNXItemProperty nwnxItemProperty,
            IBiowareXP2 biowareXP2)
        {
            _ = script;
            _nwnxItemProperty = nwnxItemProperty;
            _biowareXP2 = biowareXP2;
        }
        
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            NWItem item = target.Object;

            ItemProperty dummy = _.ItemPropertyQuality(NWScript.IP_CONST_QUALITY_UNKOWN);
            ItemPropertyUnpacked bonusIP = _nwnxItemProperty.UnpackIP(dummy);
            //bonusIP.Property = (int) CustomItemPropertyType.ComponentBonus;
            //bonusIP.SubType = (int) ComponentBonusType.HPUp;
            //bonusIP.CostTable = 62;
            //bonusIP.CostTableValue = 4;
            //bonusIP.Param1 = 255;
            //bonusIP.Param1Value = 0;
            //bonusIP.UsesPerDay = 255;
            //bonusIP.ChanceToAppear = 100;
            //bonusIP.IsUseable = true;
            //bonusIP.SpellID = -1;

            //ItemPropertyUnpacked bonusIP = new ItemPropertyUnpacked
            //{
            //    Property = (int)CustomItemPropertyType.ComponentBonus,
            //    SubType = (int)ComponentBonusType.HPUp,
            //    CostTable = 62,
            //    CostTableValue = 4,
            //    Param1 = 255,
            //    Param1Value = 0,
            //    UsesPerDay = 255,
            //    ChanceToAppear = 100,
            //    IsUseable = true,
            //    SpellID = -1
            //};
            ItemProperty ip = _nwnxItemProperty.PackIP(bonusIP);
            _biowareXP2.IPSafeAddItemProperty(item, ip, 0.0f, AddItemPropertyPolicy.ReplaceExisting, true, false);
            
            foreach (var prop in item.ItemProperties)
            {
                user.SendMessage("Prop = " + _.GetItemPropertyType(prop));
            }

        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            return string.Empty;
        }

        public bool RequiresTarget => true;
    }
}
