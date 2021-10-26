using System;
using System.Text;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.GuiService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class ExamineItemViewModel: GuiViewModelBase<ExamineItemViewModel, GuiPayloadBase>
    {
        public string WindowTitle
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Description
        {
            get => Get<string>();
            set => Set(value);
        }

        public string ItemProperties
        {
            get => Get<string>();
            set => Set(value);
        }

        private uint GetItem()
        {
            return GetLocalObject(Player, "EXAMINE_ITEM_WINDOW_TARGET");
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            var sb = new StringBuilder();
            var item = GetItem();
            WindowTitle = GetName(item);
            Description = GetDescription(item);

            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                BuildItemPropertyString(sb, ip);
                sb.Append("\n");
            }

            ItemProperties = sb.ToString();
        }

        public Action OnCloseWindow() => () =>
        {
            var item = GetItem();
            DestroyObject(item);
        };

        private void BuildItemPropertyString(StringBuilder sb, ItemProperty ip)
        {
            var typeId = (int)GetItemPropertyType(ip);
            var name = GetStringByStrRef(Convert.ToInt32(Get2DAString("itempropdef", "GameStrRef", typeId)));
            sb.Append(name);

            var subTypeId = GetItemPropertySubType(ip);
            if (subTypeId != -1)
            {
                var subTypeResref = Get2DAString("itempropdef", "SubTypeResRef", typeId);
                var strRefId = StringToInt(Get2DAString(subTypeResref, "Name", subTypeId));
                if (strRefId != 0)
                    sb.Append($" {GetStringByStrRef(strRefId)}");
                
            }

            var param1 = GetItemPropertyParam1(ip);
            if (param1 != -1)
            {
                var paramResref = Get2DAString("iprp_paramtable", "TableResRef", param1);
                var strRef = StringToInt(Get2DAString(paramResref, "Name", GetItemPropertyParam1Value(ip)));
                if (strRef != 0)
                    sb.Append($" {GetStringByStrRef(strRef)}");
            }

            var costTable = GetItemPropertyCostTable(ip);
            if (costTable != -1)
            {
                var costTableResref = Get2DAString("iprp_costtable", "Name", costTable);
                var strRef = StringToInt(Get2DAString(costTableResref, "Name", GetItemPropertyCostTableValue(ip)));
                if (strRef != 0)
                    sb.Append($" {GetStringByStrRef(strRef)}");
            }
        }
        
    }
}
