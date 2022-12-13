using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.FishingService
{
    internal class FishingLocationDefinition
    {
        private readonly Dictionary<FishingLocationType, FishingLocationDetail> _locations = new();

        public Dictionary<FishingLocationType, FishingLocationDetail> Create()
        {
            ViscaraCavernFish();
            ViscaraDeepwoodsFish();
            ViscaraEasternSwamplandsFish();
            ViscaraLakeFish();
            ViscaraLakeGroundsFish();
            ViscaraMountainValleyFish();
            ViscaraWesternSwamplandsFish();
            ViscaraWildlandsFish();
            ViscaraWildwoodsFish();

            MonCalaCoralIslesInnerFish();
            MonCalaCoralIslesOuterFish();
            MonCalaDacCitySurfaceFish();
            MonCalaSharptoothJungleSouthFish();
            MonCalaSharptoothJungleCavesFish();
            MonCalaSunkenhedgeSwampsFish();

            HutlarCloningTestSiteFish();
            HutlarQionFoothillsFish();
            HutlarQionTundraFish();
            HutlarQionValleyFish();

            TatooineBabySarlaccCaveFish();
            TatooineTuskenRaiderCaveMainFloorFish();

            DathomirDesertWestSideFish();
            DathomirGrottoCavernsFish();
            DathomirGrottosFish();
            DathomirMountainsFish();
            DathomirTribeVillageFish();

            return _locations;
        }

        private void AddFish(
            FishingLocationType locationType,
            FishingRodType rodType,
            FishingBaitType baitType,
            FishType fishType,
            int weight)
        {
            if (!_locations.ContainsKey(locationType))
                _locations[locationType] = new FishingLocationDetail();

            _locations[locationType].AddFish(rodType, baitType, fishType, weight);
        }

        private void ViscaraCavernFish()
        {
            const FishingLocationType LocationType = FishingLocationType.ViscaraCavern;

            AddFish(LocationType, FishingRodType.Bamboo, FishingBaitType.LittleWorm, FishType.BastionSweeper, 100);
        }

        private void ViscaraDeepwoodsFish()
        {

        }

        private void ViscaraEasternSwamplandsFish()
        {
        }

        private void ViscaraLakeFish()
        {
        }

        private void ViscaraLakeGroundsFish()
        {
        }

        private void ViscaraMountainValleyFish()
        {
        }

        private void ViscaraWesternSwamplandsFish()
        {
        }

        private void ViscaraWildlandsFish()
        {
        }

        private void ViscaraWildwoodsFish()
        {
        }

        private void MonCalaCoralIslesInnerFish()
        {
        }

        private void MonCalaCoralIslesOuterFish()
        {
        }

        private void MonCalaDacCitySurfaceFish()
        {
        }

        private void MonCalaSharptoothJungleSouthFish()
        {
        }

        private void MonCalaSharptoothJungleCavesFish()
        {
        }

        private void MonCalaSunkenhedgeSwampsFish()
        {
        }

        private void HutlarCloningTestSiteFish()
        {
        }

        private void HutlarQionFoothillsFish()
        {
        }

        private void HutlarQionTundraFish()
        {
        }

        private void HutlarQionValleyFish()
        {
        }

        private void TatooineBabySarlaccCaveFish()
        {
        }

        private void TatooineTuskenRaiderCaveMainFloorFish()
        {
        }

        private void DathomirDesertWestSideFish()
        {
        }

        private void DathomirGrottoCavernsFish()
        {
        }

        private void DathomirGrottosFish()
        {
        }

        private void DathomirMountainsFish()
        {
        }

        private void DathomirTribeVillageFish()
        {
        }

    }
}
