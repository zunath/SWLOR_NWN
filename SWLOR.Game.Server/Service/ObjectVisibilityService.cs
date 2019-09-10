using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWNX;


using static NWN._;

namespace SWLOR.Game.Server.Service
{
    public static class ObjectVisibilityService
    {
        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleEnter>(message => OnModuleEnter());
            MessageHub.Instance.Subscribe<OnModuleLoad>(message => OnModuleLoad());
        }

        private static void OnModuleLoad()
        {
            foreach (var area in NWModule.Get().Areas)
            {
                NWObject obj = _.GetFirstObjectInArea(area);
                while (obj.IsValid)
                {
                    string visibilityObjectID = obj.GetLocalString("VISIBILITY_OBJECT_ID");
                    if (!string.IsNullOrWhiteSpace(visibilityObjectID))
                    {
                        AppCache.VisibilityObjects.Add(visibilityObjectID, obj);
                    }

                    obj = _.GetNextObjectInArea(area);
                }
            }
        }


        private static void OnModuleEnter()
        {
            NWPlayer player = _.GetEnteringObject();
            if (!player.IsPlayer) return;
            
            var visibilities = DataService.PCObjectVisibility.GetAllByPlayerID(player.GlobalID).ToList();

            // Apply visibilities for player
            foreach (var visibility in visibilities)
            {
                if (!AppCache.VisibilityObjects.ContainsKey(visibility.VisibilityObjectID)) continue;

                var obj = AppCache.VisibilityObjects[visibility.VisibilityObjectID];

                if (visibility.IsVisible)
                    NWNXVisibility.SetVisibilityOverride(player, obj, VisibilityType.Visible);
                else
                    NWNXVisibility.SetVisibilityOverride(player, obj, VisibilityType.Hidden);
            }

            // Hide any objects which are hidden by default, as long as player doesn't have an override already.
            foreach (var visibilityObject in AppCache.VisibilityObjects)
            {
                string visibilityObjectID = visibilityObject.Value.GetLocalString("VISIBILITY_OBJECT_ID");
                var matchingVisibility = visibilities.SingleOrDefault(x => x.PlayerID == player.GlobalID && x.VisibilityObjectID.ToString() == visibilityObjectID);
                if (visibilityObject.Value.GetLocalInt("VISIBILITY_HIDDEN_DEFAULT") == TRUE && matchingVisibility == null)
                {
                    NWNXVisibility.SetVisibilityOverride(player, visibilityObject.Value, VisibilityType.Hidden);
                }
            }

        }

        public static void ApplyVisibilityForObject(NWObject target)
        {
            string visibilityObjectID = target.GetLocalString("VISIBILITY_OBJECT_ID");
            if (string.IsNullOrWhiteSpace(visibilityObjectID)) return;
            
            if (!AppCache.VisibilityObjects.ContainsKey(visibilityObjectID))
            {
                AppCache.VisibilityObjects.Add(visibilityObjectID, target);
            }
            else
            {
                AppCache.VisibilityObjects[visibilityObjectID] = target;
            }
            
            var players = NWModule.Get().Players.ToList();
            var concatPlayerIDs = players.Select(x => x.GlobalID);
            var pcVisibilities = DataService.PCObjectVisibility.GetAllByPlayerIDsAndVisibilityObjectID(concatPlayerIDs, visibilityObjectID).ToList();

            foreach (var player in players)
            {
                var visibility = pcVisibilities.SingleOrDefault(x => x.PlayerID == player.GlobalID);

                if (visibility == null)
                {
                    if(target.GetLocalInt("VISIBILITY_HIDDEN_DEFAULT") == TRUE)
                        NWNXVisibility.SetVisibilityOverride(player, target, VisibilityType.Hidden);
                    continue;
                }

                if(visibility.IsVisible)
                    NWNXVisibility.SetVisibilityOverride(player, target, VisibilityType.Visible);
                else
                    NWNXVisibility.SetVisibilityOverride(player, target, VisibilityType.Hidden);
            }
        }

        public static void AdjustVisibility(NWPlayer player, NWObject target, bool isVisible)
        {
            if (!player.IsPlayer) return;
            if (target.IsPlayer || target.IsDM) return;

            string visibilityObjectID = target.GetLocalString("VISIBILITY_OBJECT_ID");
            if (string.IsNullOrWhiteSpace(visibilityObjectID))
            {
                target.AssignCommand(() =>
                {
                    _.SpeakString("Unable to locate VISIBILITY_OBJECT_ID variable. Need this in order to adjust visibility. Notify an admin if you see this message.");
                });
                return;
            }

            var visibility = DataService.PCObjectVisibility.GetByPlayerIDAndVisibilityObjectIDOrDefault(player.GlobalID, visibilityObjectID);
            DatabaseActionType action = DatabaseActionType.Update;

            if (visibility == null)
            {
                visibility = new PCObjectVisibility
                {
                    PlayerID = player.GlobalID,
                    VisibilityObjectID = visibilityObjectID
                };
                action = DatabaseActionType.Insert;
            }

            visibility.IsVisible = isVisible;
            DataService.SubmitDataChange(visibility, action);

            if (visibility.IsVisible)
                NWNXVisibility.SetVisibilityOverride(player, target, VisibilityType.Visible);
            else
                NWNXVisibility.SetVisibilityOverride(player, target, VisibilityType.Hidden);
        }

        public static void AdjustVisibility(NWPlayer player, string targetGUID, bool isVisible)
        {
            var obj = AppCache.VisibilityObjects.Single(x => x.Key == targetGUID);
            AdjustVisibility(player, obj.Value, isVisible);
        }
    }
}
