using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.FishingService
{
    public class FishingLocationBuilder
    {
        private readonly Dictionary<FishingLocationType, FishingLocationDetail> _locations = new();
        private FishingLocationDetail _activeDetail;
        private FishDetail _activeFishDetail;

        public FishingLocationBuilder Create(FishingLocationType type)
        {
            _activeDetail = new FishingLocationDetail();
            _locations.Add(type, _activeDetail);

            return this;
        }

        public FishingLocationBuilder AddFish()
        {
            _activeFishDetail = new FishDetail();

            return this;
        }

        public Dictionary<FishingLocationType, FishingLocationDetail> Build()
        {
            return _locations;
        }
    }
}
