using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWNX;


using static SWLOR.Game.Server.NWScript._;
using _ = SWLOR.Game.Server.NWScript._;

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
            Console.WriteLine("OPSVisibilityService -> OnModuleLoad");
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
            Console.WriteLine("OPSVisibilityService -> OnModuleLoad Complete");
        }


        private static void OnModuleEnter()
        {
            NWPlayer player = _.GetEnteringObject();
            if (!player.IsPlayer) return;

            var dbPlayer = DataService.Player.GetByID(player.GlobalID);
            var visibilities = dbPlayer.ObjectVisibilities;

            // Apply visibilities for player
            foreach (var visibility in visibilities)
            {
                if (!AppCache.VisibilityObjects.ContainsKey(visibility.Key)) continue;

                var obj = AppCache.VisibilityObjects[visibility.Key];

                if (visibility.Value)
                    NWNXVisibility.SetVisibilityOverride(player, obj, VisibilityType.Visible);
                else
                    NWNXVisibility.SetVisibilityOverride(player, obj, VisibilityType.Hidden);
            }

            // Hide any objects which are hidden by default, as long as player doesn't have an override already.
            foreach (var visibilityObject in AppCache.VisibilityObjects)
            {
                string visibilityObjectID = visibilityObject.Value.GetLocalString("VISIBILITY_OBJECT_ID");
                var matchingVisibility = dbPlayer.ObjectVisibilities.ContainsKey(visibilityObjectID) ?
                    (bool?)dbPlayer.ObjectVisibilities[visibilityObjectID] :
                    null;

                if (visibilityObject.Value.GetLocalBoolean("VISIBILITY_HIDDEN_DEFAULT") && matchingVisibility == null)
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
            
            foreach (var player in NWModule.Get().Players)
            {
                var dbPlayer = DataService.Player.GetByID(player.GlobalID);
                var visibility = dbPlayer.ObjectVisibilities.ContainsKey(visibilityObjectID) ? 
                    (bool?)dbPlayer.ObjectVisibilities[visibilityObjectID] : 
                    null;

                if (visibility == null)
                {
                    if(target.GetLocalBoolean("VISIBILITY_HIDDEN_DEFAULT"))
                        NWNXVisibility.SetVisibilityOverride(player, target, VisibilityType.Hidden);
                    continue;
                }

                if(visibility == true)
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

            var dbPlayer = DataService.Player.GetByID(player.GlobalID);
            
            dbPlayer.ObjectVisibilities[visibilityObjectID] = isVisible;
            DataService.Set(dbPlayer);

            if (isVisible)
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
