using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Area;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DialogService;
using Object = SWLOR.Game.Server.Core.NWNX.Object;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class PlayerHouseFurnitureDialog: DialogBase
    {
        private class Model
        {
            public Model()
            {
                GridTiles = new List<uint>();
                Placeable = OBJECT_INVALID;
            }

            public List<uint> GridTiles { get; set; }
            public bool HasBeenPlaced { get; set; }
            public uint Item { get; set; }
            public uint Placeable { get; set; }
            public FurnitureType FurnitureType { get; set; }
            public Location TargetLocation { get; set; }
            public string OwnerUUID { get; set; }
        }

        private const string MainPageId = "MAIN_PAGE";
        private const string MovePageId = "MOVE_PAGE";
        private const string RotatePageId = "ROTATE_PAGE";

        public override PlayerDialog SetUp(uint player)
        {
            var builder = new DialogBuilder()
                .WithDataModel(new Model())
                .AddInitializationAction(Initialize)
                .AddEndAction(ClearPlacementGrid)
                .AddPage(MainPageId, MainPageInit)
                .AddPage(MovePageId, MovePageInit)
                .AddPage(RotatePageId, RotatePageInit);

            return builder.Build();
        }

        private void Initialize()
        {
            LoadPlacementGrid();
            PopulateDataModel();
        }

        private void PopulateDataModel()
        {
            // There are two ways to enter this conversation.
            //   1.) Use a furniture item.
            //   2.) Use the property tool feat.
            // Depending on the entry method, the data model gets populated differently.

            var player = GetPC();
            var area = GetArea(player);
            var model = GetDataModel<Model>();

            var item = GetLocalObject(player, "TEMP_FURNITURE_OBJECT");

            // Used furniture item.
            if (GetIsObjectValid(item))
            {
                model.Item = item;
                model.HasBeenPlaced = false;
            }
            // Used property tool.
            else
            {
                model.Item = OBJECT_INVALID;
                model.HasBeenPlaced = true;
                model.Placeable = GetLocalObject(player, "TEMP_FURNITURE_PLACEABLE");
            }

            model.FurnitureType = (FurnitureType)GetLocalInt(player, "TEMP_FURNITURE_TYPE_ID");
            model.OwnerUUID = GetLocalString(area, "HOUSING_OWNER_PLAYER_UUID");
            model.TargetLocation = GetLocalLocation(player, "TEMP_FURNITURE_LOCATION");

            // Wipe temp variables.
            DeleteLocalInt(player, "TEMP_FURNITURE_TYPE_ID");
            DeleteLocalObject(player, "TEMP_FURNITURE_OBJECT");
            DeleteLocalLocation(player, "TEMP_FURNITURE_LOCATION");
            DeleteLocalObject(player, "TEMP_FURNITURE_PLACEABLE");
        }

        private void MainPageInit(DialogPage page)
        {
            var model = GetDataModel<Model>();
            var dbHouse = DB.Get<PlayerHouse>(model.OwnerUUID);
            var houseDetail = Housing.GetHouseTypeDetail(dbHouse.HouseType);
            var furnitureDetail = Housing.GetFurnitureDetail(model.FurnitureType);

            page.Header = ColorToken.Green("Furniture: ") + furnitureDetail.Name + "\n" +
                          ColorToken.Green("Furniture Limit: ") + dbHouse.Furnitures.Count + " / " + houseDetail.FurnitureLimit + "\n\n" +
                          "Please select an action to take.";

            // Furniture has been placed. Only give options to adjust positioning/rotation
            if (model.HasBeenPlaced)
            {
                page.AddResponse("Pick Up", PickUpFurniture);
                page.AddResponse("Move", () => ChangePage(MovePageId));
                page.AddResponse("Rotate", () => ChangePage(RotatePageId));
            }
            // Furniture hasn't been placed yet.
            else
            {
                page.AddResponse("Place", PlaceFurniture);
            }
        }

        private void PlaceFurniture()
        {
            var player = GetPC();
            var model = GetDataModel<Model>();

            // Other players might have changed the state of the house since the conversation started.
            // Check everything again to make sure the furniture can still be placed.
            if (!Housing.CanPlaceFurniture(player, model.Item))
            {
                return;
            }

            var furnitureDetail = Housing.GetFurnitureDetail(model.FurnitureType);
            model.Placeable = CreateObject(ObjectType.Placeable, furnitureDetail.Resref, model.TargetLocation);
            DestroyObject(model.Item);
            model.HasBeenPlaced = true;

            // Persist the new furniture to the DB.
            var position = GetPositionFromLocation(model.TargetLocation);
            var dbHouse = DB.Get<PlayerHouse>(model.OwnerUUID);

            var furnitureId = Guid.NewGuid().ToString();
            dbHouse.Furnitures[furnitureId] = new PlayerHouseFurniture
            {
                FurnitureType = model.FurnitureType,
                Orientation = 0.0f,
                X = position.X,
                Y = position.Y,
                Z = position.Z
            };

            DB.Set(model.OwnerUUID, dbHouse);

            // Add an Id pointer to the placeable.
            SetLocalString(model.Placeable, "HOUSING_FURNITURE_ID", furnitureId);
        }

        private void PickUpFurniture()
        {
            var player = GetPC();
            var model = GetDataModel<Model>();
            var dbHouse = DB.Get<PlayerHouse>(model.OwnerUUID);

            var furnitureId = GetLocalString(model.Placeable, "HOUSING_FURNITURE_ID");
            var furnitureType = dbHouse.Furnitures[furnitureId].FurnitureType;
            var paddedId = ((int) furnitureType).ToString().PadLeft(4, '0');
            var itemResref = $"furniture_{paddedId}";

            model.HasBeenPlaced = false;
            model.Item = CreateItemOnObject(itemResref, player); 

            DestroyObject(model.Placeable);
            model.Placeable = OBJECT_INVALID;

            dbHouse.Furnitures.Remove(furnitureId);
            DB.Set(model.OwnerUUID, dbHouse);
        }

        private void MovePageInit(DialogPage page)
        {
            var player = GetPC();
            var model = GetDataModel<Model>();

            void Move(float x, float y, float z)
            {
                var position = GetPosition(model.Placeable);
                position.X += x;
                position.Y += y;
                position.Z += z;

                if (position.Z > 10f)
                    position.Z = 10f;
                else if (position.Z < -10f)
                    position.Z = -10f;

                Object.SetPosition(model.Placeable, position);

                var furnitureId = GetLocalString(model.Placeable, "HOUSING_FURNITURE_ID");
                var dbHouse = DB.Get<PlayerHouse>(model.OwnerUUID);
                dbHouse.Furnitures[furnitureId].X = position.X;
                dbHouse.Furnitures[furnitureId].Y = position.Y;
                dbHouse.Furnitures[furnitureId].Z = position.Z;

                DB.Set(model.OwnerUUID, dbHouse);
            }

            page.Header = ColorToken.Green("Move Furniture") + "\n\n";

            page.AddResponse("Move North", () => Move(0f, 0.2f, 0f));
            page.AddResponse("Move South", () => Move(0f, -0.2f, 0f));
            page.AddResponse("Move East", () => Move(0.2f, 0f, 0f));
            page.AddResponse("Move West", () => Move(-0.2f, 0f, 0f));
            page.AddResponse("Move Up", () => Move(0f, 0f, 0.1f));
            page.AddResponse("Move Down", () => Move(0f, 0f, -0.1f));
        }

        private void RotatePageInit(DialogPage page)
        {
            var model = GetDataModel<Model>();

            void Rotate(float degrees, bool set = false)
            {
                var facing = GetFacing(model.Placeable);
                if (set)
                {
                    facing = degrees;
                    AssignCommand(model.Placeable, () => SetFacing(facing));
                }
                else
                {
                    facing += degrees;
                    AssignCommand(model.Placeable, () => SetFacing(facing));
                }

                var dbHouse = DB.Get<PlayerHouse>(model.OwnerUUID);
                var furnitureId = GetLocalString(model.Placeable, "HOUSING_FURNITURE_ID");

                dbHouse.Furnitures[furnitureId].Orientation = facing;
                DB.Set(model.OwnerUUID, dbHouse);
            }

            page.Header = ColorToken.Green("Rotate Furniture") + "\n\n" +
                          ColorToken.Green("Current Facing: ") + GetFacing(model.Placeable) + " degrees";

            page.AddResponse("Face North", () => Rotate(90f, true));
            page.AddResponse("Face South", () => Rotate(270f, true));
            page.AddResponse("Face East", () => Rotate(0f, true));
            page.AddResponse("Face West", () => Rotate(180f, true));

            page.AddResponse("20 degrees", () => Rotate(20f));
            page.AddResponse("30 degrees", () => Rotate(30f));
            page.AddResponse("45 degrees", () => Rotate(45f));
            page.AddResponse("60 degrees", () => Rotate(60f));
            page.AddResponse("75 degrees", () => Rotate(75f));
            page.AddResponse("90 degrees", () => Rotate(90f));
            page.AddResponse("180 degrees", () => Rotate(180f));
        }

        /// <summary>
        /// Spawns a placement grid which is visible only to the player currently in this conversation.
        /// The grid is useful for lining up furniture positioning.
        /// </summary>
        private void LoadPlacementGrid()
        {
            var player = GetPC();
            var area = GetArea(OBJECT_SELF);
            var areaWidth = GetAreaSize(Dimension.Width, area);
            var areaHeight = GetAreaSize(Dimension.Height, area);
            var model = GetDataModel<Model>();

            var position = Vector3(5.0f, 0.0f, 0.1f);

            for (var i = 0; i <= areaHeight; i++)
            {
                position.Y = -5.0f;
                for (var j = 0; j <= areaWidth; j++)
                {
                    position.Y += 10.0f;

                    var location = Location(area, position, 0.0f);
                    var tile = CreateObject(ObjectType.Placeable, "plc_invisobj", location);
                    SetPlotFlag(tile, true);
                    ApplyEffectToObject(DurationType.Permanent, EffectVisualEffect(VisualEffect.Vfx_Placement_Grid), tile);

                    Visibility.SetVisibilityOverride(OBJECT_INVALID, tile, VisibilityType.Hidden);
                    Visibility.SetVisibilityOverride(player, tile, VisibilityType.Visible);
                    model.GridTiles.Add(tile);
                }

                position.X += 10.0f;
            }
        }

        /// <summary>
        /// Removes the placement grid placeables and effects from the area.
        /// </summary>
        private void ClearPlacementGrid()
        {
            var model = GetDataModel<Model>();

            foreach (var tile in model.GridTiles)
            {
                DestroyObject(tile);
            }

            model.GridTiles.Clear();
        }

    }
}
