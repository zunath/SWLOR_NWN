using System;
using SWLOR.Game.Server.Service.GuiService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class ManageStructuresViewModel: GuiViewModelBase<ManageStructuresViewModel, GuiPayloadBase>
    {
        private const int StructuresPerPage = 25;
        private int SelectedStructureIndex { get; set; }

        public GuiBindingList<string> StructureNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> StructureToggles
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public GuiBindingList<string> PageNumbers
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public int SelectedPageIndex
        {
            get => Get<int>();
            set => Set(value);
        }

        public string XPosition
        {
            get => Get<string>();
            set => Set(value);
        }

        public string YPosition
        {
            get => Get<string>();
            set => Set(value);
        }

        public string ZPosition
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Facing
        {
            get => Get<string>();
            set => Set(value);
        }

        public string StructureName
        {
            get => Get<string>();
            set => Set(value);
        }

        public string FurnitureCount
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsManagePropertyEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsOpenStorageEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsRetrieveStructureEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            SelectedStructureIndex = -1;


            WatchOnClient(model => model.SelectedPageIndex);
            WatchOnClient(model => model.StructureName);
            WatchOnClient(model => model.XPosition);
            WatchOnClient(model => model.YPosition);
            WatchOnClient(model => model.ZPosition);
        }

        public Action OnSelectStructure() => () =>
        {
            if (SelectedStructureIndex > -1)
                StructureToggles[SelectedStructureIndex] = false;

            SelectedStructureIndex = NuiGetEventArrayIndex();
            StructureToggles[SelectedStructureIndex] = true;
        };

        public Action OnPreviousPage() => () =>
        {

        };

        public Action OnNextPage() => () =>
        {

        };

        public Action OnManageProperty() => () =>
        {

        };

        public Action OnOpenStorage() => () =>
        {

        };

        public Action OnRetrieveStructure() => () =>
        {

        };

        public Action OnSaveChanges() => () =>
        {

        };

        public Action OnDiscardChanges() => () =>
        {

        };

    }
}
