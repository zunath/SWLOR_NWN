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

        public FishingLocationBuilder AddFish(
            FishType fishType, 
            FishingRodType rodType, 
            FishingBaitType baitType)
        {
            _activeFishDetail = new FishDetail
            {
                Type = fishType,
                Frequency = 1,
                TimeOfDay = FishTimeOfDayType.All
            };

            _activeDetail.AddFish(rodType, baitType, _activeFishDetail);

            return this;
        }

        public FishingLocationBuilder Frequency(int frequency)
        {
            _activeFishDetail.Frequency = frequency;

            return this;
        }

        public FishingLocationBuilder DaytimeOnly()
        {
            _activeFishDetail.TimeOfDay = FishTimeOfDayType.Daytime;

            return this;
        }

        public FishingLocationBuilder NighttimeOnly()
        {
            _activeFishDetail.TimeOfDay = FishTimeOfDayType.Nighttime;

            return this;
        }

        public Dictionary<FishingLocationType, FishingLocationDetail> Build()
        {
            return _locations;
        }
    }
}
