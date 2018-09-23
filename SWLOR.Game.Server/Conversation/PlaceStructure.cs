using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
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
        private readonly IAreaService _area;

        public PlaceStructure(
            INWScript script,
            IDialogService dialog,
            IDataContext db,
            IBaseService @base,
            IColorTokenService color,
            IAreaService area)
            : base(script, dialog)
        {
            _db = db;
            _base = @base;
            _color = color;
            _area = area;
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");
            DialogPage mainPage = new DialogPage(string.Empty,
                "Place Structure",
                "Rotate",
                "Preview",
                "Change Exterior Style",
                "Change Interior Style");

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

            DialogPage stylePage = new DialogPage();

            dialog.AddPage("MainPage", mainPage);
            dialog.AddPage("RotatePage", rotatePage);
            dialog.AddPage("StylePage", stylePage);
            return dialog;
        }

        public override void Initialize()
        {
            LoadMainPage();
        }

        private void LoadMainPage()
        {
            var data = _base.GetPlayerTempData(GetPC());
            BaseStructure structure = _db.BaseStructures.Single(x => x.BaseStructureID == data.StructureID);
            var tower = _base.GetBaseControlTower(data.PCBaseID);
            bool canPlaceStructure = true;
            bool isPlacingTower = structure.BaseStructureTypeID == (int) BaseStructureType.ControlTower;
            bool isPlacingBuilding = structure.BaseStructureTypeID == (int) BaseStructureType.Building;
            bool canChangeBuildingStyles = isPlacingBuilding && data.StructureItem.GetLocalInt("STRUCTURE_BUILDING_INITIALIZED") == FALSE;

            double powerInUse = _base.GetPowerInUse(data.PCBaseID);
            double cpuInUse = _base.GetCPUInUse(data.PCBaseID);

            double towerPower = tower != null ? tower.BaseStructure.Power + (tower.StructureBonus * 3) : 0.0f;
            double towerCPU = tower != null ? tower.BaseStructure.CPU + (tower.StructureBonus * 2) : 0.0f;
            double newPower = powerInUse + structure.Power;
            double newCPU = cpuInUse + structure.CPU;

            string insufficientPower = newPower > towerPower && !isPlacingTower ? _color.Red(" (INSUFFICIENT POWER)") : string.Empty;
            string insufficientCPU = newCPU > towerCPU && !isPlacingTower ? _color.Red(" (INSUFFICIENT CPU)") : string.Empty;

            string header = _color.Green("Structure: ") + structure.Name + "\n";

            if(data.ParentStructureID > 0) // Is a building
            {
                var buildingStructure = _db.PCBaseStructures.Single(x => x.PCBaseStructureID == data.ParentStructureID);
                header += _color.Green("Structure Limit: ") + buildingStructure.ChildStructures.Count + " / " + buildingStructure.BaseStructure.Storage + "\n";
            }
            else if (isPlacingTower)
            {
                header += _color.Green("Available Power: ") + structure.Power + (data.StructureItem.StructureBonus * 3) + "\n";
                header += _color.Green("Available CPU: ") + structure.CPU + (data.StructureItem.StructureBonus * 2) + "\n";
            }
            else
            {
                header += _color.Green("Base Power: ") + powerInUse + " / " +  towerPower + "\n";
                header += _color.Green("Base CPU: ") + cpuInUse + " / " + towerCPU + "\n";
                header += _color.Green("Required Power: ") + structure.Power + insufficientPower + "\n";
                header += _color.Green("Required CPU: ") + structure.CPU + insufficientCPU + "\n";
            }

            if (isPlacingBuilding)
            {
                int exteriorStyle = data.StructureItem.GetLocalInt("STRUCTURE_BUILDING_EXTERIOR_ID");
                int interiorStyle = data.StructureItem.GetLocalInt("STRUCTURE_BUILDING_INTERIOR_ID");
                var exterior = _db.BuildingStyles.Single(x => x.BuildingStyleID == exteriorStyle);
                var interior = _db.BuildingStyles.Single(x => x.BuildingStyleID == interiorStyle);

                header += _color.Green("Exterior Style: ") + exterior.Name + "\n";
                header += _color.Green("Interior Style: ") + interior.Name + "\n";
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
            SetResponseVisible("MainPage", 4, canPlaceStructure && canChangeBuildingStyles);
            SetResponseVisible("MainPage", 5, canPlaceStructure && canChangeBuildingStyles);
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
                case "StylePage":
                    StyleResponses(responseID);
                    break;
            }
        }

        private void MainResponses(int responseID)
        {
            var data = _base.GetPlayerTempData(GetPC());
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
                case 4: // Change Exterior Style
                    data.IsInteriorStyle = false;
                    LoadStylePage();
                    ChangePage("StylePage");
                    break;
                case 5: // Change Interior Style
                    data.IsInteriorStyle = true;
                    LoadStylePage();
                    ChangePage("StylePage");
                    break;
            }
        }

        private string GetPlaceableResref(BaseStructure structure)
        {
            var data = _base.GetPlayerTempData(GetPC());
            string resref = structure.PlaceableResref;

            if (string.IsNullOrWhiteSpace(resref) &&
                structure.BaseStructureTypeID == (int)BaseStructureType.Building)
            {
                int exteriorID = data.StructureItem.GetLocalInt("STRUCTURE_BUILDING_EXTERIOR_ID");
                var style = _db.BuildingStyles.Single(x => x.BuildingStyleID == exteriorID);

                resref = style.Resref;
            }

            return resref;
        }

        private void Preview()
        {
            var data = _base.GetPlayerTempData(GetPC());
            if (data.IsPreviewing) return;

            data.IsPreviewing = true;
            var structure = _db.BaseStructures.Single(x => x.BaseStructureID == data.StructureID);
            string resref = GetPlaceableResref(structure);

            NWPlaceable plc = (_.CreateObject(OBJECT_TYPE_PLACEABLE, resref, data.TargetLocation));
            plc.IsUseable = false;
            plc.Destroy(6.0f);
            _.DelayCommand(6.1f, () => { data.IsPreviewing = false; });
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
                string resref = GetPlaceableResref(structure);
                data.StructurePreview = (_.CreateObject(OBJECT_TYPE_PLACEABLE, resref, data.TargetLocation));
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

            var position = _.GetPositionFromLocation(data.TargetLocation);
            int? interiorStyleID = data.StructureItem.GetLocalInt("STRUCTURE_BUILDING_INTERIOR_ID");
            int? exteriorStyleID = data.StructureItem.GetLocalInt("STRUCTURE_BUILDING_EXTERIOR_ID");
            interiorStyleID = interiorStyleID <= 0 ? null : interiorStyleID;
            exteriorStyleID = exteriorStyleID <= 0 ? null : exteriorStyleID;

            var structure = new PCBaseStructure
            {
                BaseStructureID = data.StructureID,
                Durability = data.StructureItem.Durability,
                LocationOrientation = _.GetFacingFromLocation(data.TargetLocation),
                LocationX = position.m_X,
                LocationY = position.m_Y,
                LocationZ = position.m_Z,
                PCBaseID = data.PCBaseID,
                InteriorStyleID = interiorStyleID,
                ExteriorStyleID = exteriorStyleID,
                CustomName = string.Empty,
                ParentPCBaseStructureID = data.ParentStructureID,
                StructureBonus = data.StructureItem.StructureBonus
            };
            
            _db.PCBaseStructures.Add(structure);
            _db.SaveChanges();
            _base.SpawnStructure(data.TargetArea, structure.PCBaseStructureID);
            data.StructureItem.Destroy();
            EndConversation();
        }

        public override void EndDialog()
        {
            _base.ClearPlayerTempData(GetPC());
        }

        private void LoadStylePage()
        {
            var data = _base.GetPlayerTempData(GetPC());
            
            // Header
            int styleID = data.StructureItem.GetLocalInt("STRUCTURE_BUILDING_EXTERIOR_ID");
            if (data.IsInteriorStyle)
                styleID = data.StructureItem.GetLocalInt("STRUCTURE_BUILDING_INTERIOR_ID");

            var currentStyle = _db.BuildingStyles.Single(x => x.BuildingStyleID == styleID);
            string header = _color.Green("Building Style: ") + currentStyle.Name + "\n\n";
            header += "Change the style by selecting from the list below.";

            SetPageHeader("StylePage", header);
            // Responses
            ClearPageResponses("StylePage");

            if (data.IsInteriorStyle)
            {
                AddResponseToPage("StylePage", "Preview Interior", true, -2);
            }

            var styles = _db.BuildingStyles.Where(x => x.IsInterior == data.IsInteriorStyle && x.BaseStructureID == data.StructureID).ToList();
            foreach (var style in styles)
            {
                AddResponseToPage("StylePage", style.Name, true, style.BuildingStyleID);
            }

            AddResponseToPage("StylePage", "Back", true, -1);
        }

        private void StyleResponses(int responseID)
        {
            var data = _base.GetPlayerTempData(GetPC());
            var response = GetResponseByID("StylePage", responseID);
            int styleID = response.CustomData[string.Empty];

            if (styleID == -1)
            {
                ChangePage("MainPage");
                return;
            }
            else if (styleID == -2)
            {
                DoInteriorPreview();
                EndConversation();
                return;
            }

            if (data.IsInteriorStyle)
            {
                data.StructureItem.SetLocalInt("STRUCTURE_BUILDING_INTERIOR_ID", styleID);
            }
            else
            {
                data.StructureItem.SetLocalInt("STRUCTURE_BUILDING_EXTERIOR_ID", styleID);
                Preview();
            }

        }

        private void DoInteriorPreview()
        {
            var data = _base.GetPlayerTempData(GetPC());
            int styleID = data.StructureItem.GetLocalInt("STRUCTURE_BUILDING_INTERIOR_ID");
            var style = _db.BuildingStyles.Single(x => x.BuildingStyleID == styleID);
            var area = _area.CreateAreaInstance(style.Resref, "BUILDING PREVIEW: " + style.Name);
            area.SetLocalInt("IS_BUILDING_PREVIEW", TRUE);
            NWPlayer player = GetPC();
            
            _base.JumpPCToBuildingInterior(player, area);
        }
    }
}
