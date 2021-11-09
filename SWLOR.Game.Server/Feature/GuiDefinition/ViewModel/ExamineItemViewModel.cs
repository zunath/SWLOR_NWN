using System;
using System.Text;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
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
            var item = GetItem();
            WindowTitle = GetName(item);
            Description = GetDescription(item);

            ItemProperties = Item.BuildItemPropertyString(item);
        }

        public Action OnCloseWindow() => () =>
        {
            var item = GetItem();
            DestroyObject(item);
        };

        
    }
}
