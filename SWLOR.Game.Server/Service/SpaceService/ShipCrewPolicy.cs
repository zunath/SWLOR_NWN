using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.SkillService;
namespace SWLOR.Game.Server.Service.SpaceService
{
    public enum ShipCrewStation { Weapons, Systems, SurveyIndustry }
    public static class ShipCrewPolicy
    {
        public static ShipCrewStation? Station(SkillType skill)=>skill switch
        {
            SkillType.Gunnery=>ShipCrewStation.Weapons,
            SkillType.ShipSystems=>ShipCrewStation.Systems,
            SkillType.Astrometrics or SkillType.SpaceIndustry=>ShipCrewStation.SurveyIndustry,
            _=>null
        };
        public static bool CanOperate(string actor,string pilot,SkillType skill,IReadOnlyDictionary<ShipCrewStation,string> active)=>
            Station(skill) is ShipCrewStation station && active.TryGetValue(station,out var assigned)?actor==assigned:actor==pilot;
        public static IReadOnlyDictionary<SkillType,int> Skills(IReadOnlyDictionary<SkillType,int> pilot,
            IReadOnlyDictionary<ShipCrewStation,IReadOnlyDictionary<SkillType,int>> operators)=>pilot.ToDictionary(x=>x.Key,x=>
                Math.Clamp(Station(x.Key) is ShipCrewStation station && operators.TryGetValue(station,out var ranks)?ranks.GetValueOrDefault(x.Key):x.Value,0,50));
        public static void Assign(ShipStatus status,ShipCrewStation station,string id)
        {
            if(string.IsNullOrWhiteSpace(id))throw new InvalidOperationException("Choose a real station operator.");
            if(status.Crew.Any(x=>x.Value==id&&x.Key!=station))throw new InvalidOperationException("One operator can occupy only one station.");
            if(status.Crew.TryGetValue(station,out var occupant)&&occupant!=id)throw new InvalidOperationException("That station is occupied.");
            status.Crew[station]=id;
        }
    }
}
