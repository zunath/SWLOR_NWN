using System.Collections.Generic;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;
using SWLOR.Game.Server.ValueObject.Dialog;
using static NWN.NWScript;
using BaseStructureType = SWLOR.Game.Server.Enumeration.BaseStructureType;

namespace SWLOR.Game.Server.Conversation
{
    public class PlaceStructure : ConversationBase
    {
        private readonly IDataContext _db;
        private readonly IBaseService _base;
        private readonly IColorTokenService _color;

        public PlaceStructure(
            INWScript script,
            IDialogService dialog,
            IDataContext db,
            IBaseService @base,
            IColorTokenService color)
            : base(script, dialog)
        {
            _db = db;
            _base = @base;
            _color = color;
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");
            DialogPage mainPage = new DialogPage(string.Empty,
                "Place Structure",
                "Rotate",
                "Preview");

            DialogPage rotatePage = new DialogPage(string.Empty,
                "East",
                "North",
                "West",
                "South",
                "20 degrees",
                "30 degrees",
                "45 degrees",
                "60 degrees",
                "75 degrees",
                "90 degrees",
                "180 degrees",
                "Back");

            dialog.AddPage("MainPage", mainPage);
            dialog.AddPage("RotatePage", rotatePage);
            return dialog;
        }

        public override void Initialize()
        {
            LoadMainPage();
        }

        private void LoadMainPage()
        {
            var data = _base.GetPlayerTempData(GetPC());
            PCBase pcBase = _db.PCBases.Single(x => x.PCBaseID == data.PCBaseID);
            BaseStructure structure = _db.BaseStructures.Single(x => x.BaseStructureID == data.StructureID);
            var tower = _base.GetBaseControlTower(data.PCBaseID);
            bool canPlaceStructure = true;
            bool isPlacingTower = structure.BaseStructureTypeID == (int) BaseStructureType.ControlTower;

            double towerPower = tower?.BaseStructure.Power ?? 0.0f;
            double towerCPU = tower?.BaseStructure.CPU ?? 0.0f;
            double newPower = pcBase.Power + structure.Power;
            double newCPU = pcBase.CPU + structure.CPU;
            string insufficientPower = newPower > towerPower && !isPlacingTower ? _color.Red(" (INSUFFICIENT POWER)") : string.Empty;
            string insufficientCPU = newCPU > towerCPU && !isPlacingTower ? _color.Red(" (INSUFFICIENT CPU)") : string.Empty;

            string header = _color.Green("Structure: ") + structure.Name + "\n";

            if (isPlacingTower)
            {
                header += _color.Green("Available Power: ") + structure.Power + "\n";
                header += _color.Green("Available CPU: ") + structure.CPU + "\n";
            }
            else
            {
                header += _color.Green("Base Power: ") + pcBase.Power + " / " +  towerPower + "\n";
                header += _color.Green("Base CPU: ") + pcBase.CPU + " / " + towerCPU + "\n";
                header += _color.Green("Required Power: ") + structure.Power + insufficientPower + "\n";
                header += _color.Green("Required CPU: ") + structure.CPU + insufficientCPU + "\n";
            }

            if (!isPlacingTower && (newPower > towerPower || newCPU > towerCPU))
            {
                canPlaceStructure = false;
                header += "\nOne or more requirements not met. Cannot place structure.";
            }

            SetPageHeader("MainPage", header);

            SetResponseVisible("MainPage", 1, canPlaceStructure);
            SetResponseVisible("MainPage", 2, canPlaceStructure);
            SetResponseVisible("MainPage", 3, canPlaceStructure);
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            switch (pageName)
            {
                case "MainPage":
                    MainResponses(responseID);
                    break;
                case "RotatePage":
                    RotateResponses(responseID);
                    break;
            }
        }

        private void MainResponses(int responseID)
        {
            
            switch (responseID)
            {
                case 1: // Place Structure
                    DoPlaceStructure();
                    break;
                case 2: // Rotate
                    LoadRotatePage();
                    ChangePage("RotatePage");
                    break;
                case 3: // Preview
                    Preview();
                    break;
            }
        }

        private void Preview()
        {
            var data = _base.GetPlayerTempData(GetPC());
            if (data.IsPreviewing) return;

            data.IsPreviewing = true;
            var structure = _db.BaseStructures.Single(x => x.BaseStructureID == data.StructureID);
            var plc = NWPlaceable.Wrap(_.CreateObject(OBJECT_TYPE_PLACEABLE, structure.PlaceableResref, data.TargetLocation));
            plc.IsUseable = false;
            plc.Destroy(6.0f);
            plc.DelayCommand(() => { data.IsPreviewing = false; }, 6.1f);
            _.ApplyEffectToObject(DURATION_TYPE_PERMANENT, _.EffectVisualEffect(VFX_DUR_AURA_GREEN), plc.Object);
        }

        private void LoadRotatePage()
        {
            var data = _base.GetPlayerTempData(GetPC());
            float facing = _.GetFacingFromLocation(data.TargetLocation);
            string header = _color.Green("Current Direction: ") + facing;

            if (data.StructurePreview == null || !data.StructurePreview.IsValid)
            {
                var structure = _db.BaseStructures.Single(x => x.BaseStructureID == data.StructureID);
                data.StructurePreview = NWPlaceable.Wrap(_.CreateObject(OBJECT_TYPE_PLACEABLE, structure.PlaceableResref, data.TargetLocation));
                data.StructurePreview.IsUseable = false;
                _.ApplyEffectToObject(DURATION_TYPE_PERMANENT, _.EffectVisualEffect(VFX_DUR_AURA_GREEN), data.StructurePreview.Object);
            }

            SetPageHeader("RotatePage", header);
        }

        private void RotateResponses(int responseID)
        {
            var data = _base.GetPlayerTempData(GetPC());
            switch (responseID)
            {
                case 1: // East
                    DoRotate(0.0f, true);
                    break;
                case 2: // North
                    DoRotate(90.0f, true);
                    break;
                case 3: // West
                    DoRotate(180.0f, true);
                    break;
                case 4: // South
                    DoRotate(270.0f, true);
                    break;
                case 5: // Rotate 20
                    DoRotate(20.0f, false);
                    break;
                case 6: // Rotate 30
                    DoRotate(30.0f, false);
                    break;
                case 7: // Rotate 45
                    DoRotate(45.0f, false);
                    break;
                case 8: // Rotate 60
                    DoRotate(60.0f, false);
                    break;
                case 9: // Rotate 75
                    DoRotate(75.0f, false);
                    break;
                case 10: // Rotate 90
                    DoRotate(90.0f, false);
                    break;
                case 11: // Rotate 180
                    DoRotate(180.0f, false);
                    break;
                case 12: // Back
                    if (data.StructurePreview != null && data.StructurePreview.IsValid)
                    {
                        data.StructurePreview.Destroy();
                    }
                    ChangePage("MainPage");
                    break;
            }
        }

        private void DoRotate(float degrees, bool isSet)
        {
            var data = _base.GetPlayerTempData(GetPC());
            float facing = _.GetFacingFromLocation(data.TargetLocation);
            if (isSet)
            {
                facing = degrees;
            }
            else
            {
                facing += degrees;
            }

            while (facing > 360)
            {
                facing -= 360;
            }

            if (data.StructurePreview != null && data.StructurePreview.IsValid)
            {
                data.StructurePreview.Facing = facing;
            }

            data.TargetLocation = _.Location(_.GetAreaFromLocation(data.TargetLocation),
                _.GetPositionFromLocation(data.TargetLocation),
                facing);
            LoadRotatePage();
        }

        private void DoPlaceStructure()
        {
            var data = _base.GetPlayerTempData(GetPC());
            string canPlaceStructure = _base.CanPlaceStructure(GetPC(), data.StructureItem, data.TargetLocation, data.StructureID);

            if (!string.IsNullOrWhiteSpace(canPlaceStructure))
            {
                GetPC().SendMessage(canPlaceStructure);
                return;
            }

            var dbStructure = _db.BaseStructures.Single(x => x.BaseStructureID == data.StructureID);
            bool isPlacingTower = dbStructure.BaseStructureTypeID == (int)BaseStructureType.ControlTower;
            var position = _.GetPositionFromLocation(data.TargetLocation);
            var structure = new PCBaseStructure
            {
                BaseStructureID = data.StructureID,
                Durability = dbStructure.Durability + data.StructureItem.Durability,
                LocationOrientation = _.GetFacingFromLocation(data.TargetLocation),
                LocationX = position.m_X,
                LocationY = position.m_Y,
                LocationZ = position.m_Z,
                PCBaseID = data.PCBaseID
            };

            var pcBase = _db.PCBases.Single(x => x.PCBaseID == data.PCBaseID);
            if (isPlacingTower)
            {
                pcBase.Power = dbStructure.Power;
                pcBase.CPU = dbStructure.CPU;
            }
            else
            {
                pcBase.Power -= dbStructure.Power;
                pcBase.CPU -= dbStructure.CPU;
            }

            _db.PCBaseStructures.Add(structure);
            _db.SaveChanges();
            
            var plc = NWPlaceable.Wrap(_.CreateObject(OBJECT_TYPE_PLACEABLE, dbStructure.PlaceableResref, data.TargetLocation));
            plc.SetLocalInt("PC_BASE_STRUCTURE_ID", structure.PCBaseStructureID);
            List<AreaStructure> areaStructures = data.TargetArea.Data["BASE_SERVICE_STRUCTURES"];
            areaStructures.Add(new AreaStructure(data.PCBaseID, structure.PCBaseStructureID, plc));

            data.StructureItem.Destroy();
            EndConversation();
        }

        public override void EndDialog()
        {
            _base.ClearPlayerTempData(GetPC());
        }


    }
}
