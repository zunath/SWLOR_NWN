using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Service
{
    public class ObjectVisibilityService : IObjectVisibilityService
    {
        private readonly INWScript _;
        private readonly IDataService _data;
        private readonly AppCache _appCache;
        private readonly INWNXPlayer _nwnxPlayer;

        public ObjectVisibilityService(
            INWScript script,
            IDataService data,
            AppCache appCache,
            INWNXPlayer nwnxPlayer)
        {
            _ = script;
            _data = data;
            _appCache = appCache;
            _nwnxPlayer = nwnxPlayer;
        }

        public void OnModuleLoad()
        {
            foreach (var area in NWModule.Get().Areas)
            {
                NWObject obj = _.GetFirstObjectInArea(area);
                while (obj.IsValid)
                {
                    string visibilityObjectID = obj.GetLocalString("VISIBILITY_OBJECT_ID");
                    if (!string.IsNullOrWhiteSpace(visibilityObjectID))
                    {
                        _appCache.VisibilityObjects.Add(visibilityObjectID, obj);
                    }

                    obj = _.GetNextObjectInArea(area);
                }
            }
        }


        public void OnClientEnter()
        {
            NWPlayer player = _.GetEnteringObject();
            if (!player.IsPlayer) return;

            var dbPlayer = _data.Single<PlayerCharacter>(x => x.PlayerID == player.GlobalID);

            // Apply visibilities for player
            foreach (var visibility in dbPlayer.PCObjectVisibilities)
            {
                if (!_appCache.VisibilityObjects.ContainsKey(visibility.VisibilityObjectID)) continue;

                var obj = _appCache.VisibilityObjects[visibility.VisibilityObjectID];

                if (visibility.IsVisible)
                    _nwnxPlayer.SetVisibilityOverride(player, obj, (int)PlayerVisibilityType.Visible);
                else
                    _nwnxPlayer.SetVisibilityOverride(player, obj, (int)PlayerVisibilityType.Hidden);
            }

            // Hide any objects which are hidden by default, as long as player doesn't have an override already.
            foreach (var visibilityObject in _appCache.VisibilityObjects)
            {
                string visibilityObjectID = visibilityObject.Value.GetLocalString("VISIBILITY_OBJECT_ID");
                var matchingVisibility = dbPlayer.PCObjectVisibilities.SingleOrDefault(x => x.PlayerID == player.GlobalID && x.VisibilityObjectID == visibilityObjectID);
                if (visibilityObject.Value.GetLocalInt("VISIBILITY_HIDDEN_DEFAULT") == TRUE && matchingVisibility == null)
                {
                    _nwnxPlayer.SetVisibilityOverride(player, visibilityObject.Value, (int)PlayerVisibilityType.Hidden);
                }
            }

        }

        public void ApplyVisibilityForObject(NWObject target)
        {
            string visibilityObjectID = target.GetLocalString("VISIBILITY_OBJECT_ID");
            if (string.IsNullOrWhiteSpace(visibilityObjectID)) return;

            if (!_appCache.VisibilityObjects.ContainsKey(visibilityObjectID))
            {
                _appCache.VisibilityObjects.Add(visibilityObjectID, target);
            }
            else
            {
                _appCache.VisibilityObjects[visibilityObjectID] = target;
            }
            
            var players = NWModule.Get().Players.ToList();
            var concatPlayerIDs = players.Select(x => x.GlobalID);
            var pcVisibilities = _data.Where<PCObjectVisibility>(x => concatPlayerIDs.Contains(x.PlayerID)).ToList();

            foreach (var player in players)
            {
                var visibility = pcVisibilities.SingleOrDefault(x => x.PlayerID == player.GlobalID && x.VisibilityObjectID == visibilityObjectID);

                if (visibility == null)
                {
                    if(target.GetLocalInt("VISIBILITY_HIDDEN_DEFAULT") == TRUE)
                        _nwnxPlayer.SetVisibilityOverride(player, target, (int)PlayerVisibilityType.Hidden);
                    continue;
                }

                if(visibility.IsVisible)
                    _nwnxPlayer.SetVisibilityOverride(player, target, (int)PlayerVisibilityType.Visible);
                else
                    _nwnxPlayer.SetVisibilityOverride(player, target, (int)PlayerVisibilityType.Hidden);
            }
        }

        public void AdjustVisibility(NWPlayer player, NWObject target, bool isVisible)
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

            var visibility = _data.SingleOrDefault<PCObjectVisibility>(x => x.PlayerID == player.GlobalID && x.VisibilityObjectID == visibilityObjectID);
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
            _data.SubmitDataChange(visibility, action);

            if (visibility.IsVisible)
                _nwnxPlayer.SetVisibilityOverride(player, target, (int)PlayerVisibilityType.Visible);
            else
                _nwnxPlayer.SetVisibilityOverride(player, target, (int)PlayerVisibilityType.Hidden);
        }

    }
}
