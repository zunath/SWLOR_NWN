using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    /// <summary>
    /// The Angler line of Agriculture: ten purchases at Agriculture 5/10/15/20/25/30/35/40/45/50 for 1/1/2/2/3/3/3/3/3/4 SP, each a
    /// stat adjustment the Line reads. The rank-20 purchase is the style, a choice of two through
    /// <see cref="PerkBuilder.RequirementCannotHavePerk"/>, and the owned style is always active. A rare fish lets the fight perks
    /// through at half strength, and Trophy Catch is the only perk that works on one at full strength.
    /// </summary>
    public class AgricultureAnglerPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            StrongLine();
            HardSet();
            FishSense();
            PatientAngler();
            AggressiveAngler();
            TrophyCatch();

            return _builder.Build();
        }

        private void StrongLine()
        {
            _builder.Create(PerkCategoryType.Agriculture, PerkType.StrongLine)
                .Name("Strong Line")
                .Icon("ife_tackle_maste")

                .AddPerkLevel()
                .Description("In the Line, your fishing line holds 2 more Tension before it snaps. Against a rare fish, half as much, rounded down.")
                .Price(1)
                .RequirementSkill(SkillType.Agriculture, 5)
                .IncreasesStat(StatType.AnglerTensionCap, 2)

                .AddPerkLevel()
                .Description("In the Line, your fishing line holds 5 more Tension before it snaps. Against a rare fish, half as much, rounded down.")
                .Price(3)
                .RequirementSkill(SkillType.Agriculture, 25)
                .IncreasesStat(StatType.AnglerTensionCap, 5)

                .AddPerkLevel()
                .Description("In the Line, your fishing line holds 8 more Tension before it snaps. Against a rare fish, half as much, rounded down.")
                .Price(3)
                .RequirementSkill(SkillType.Agriculture, 40)
                .IncreasesStat(StatType.AnglerTensionCap, 8);
        }

        private void HardSet()
        {
            _builder.Create(PerkCategoryType.Agriculture, PerkType.HardSet)
                .Name("Hard Set")
                .Icon("ife_hook")

                .AddPerkLevel()
                .Description("In the Line, Set Hook drains 1 more Stamina from the fish. Against a rare fish, half as much, rounded down.")
                .Price(1)
                .RequirementSkill(SkillType.Agriculture, 10)
                .IncreasesStat(StatType.AnglerSetHookDrain, 1)

                .AddPerkLevel()
                .Description("In the Line, Set Hook drains 2 more Stamina from the fish. Against a rare fish, half as much, rounded down.")
                .Price(3)
                .RequirementSkill(SkillType.Agriculture, 30)
                .IncreasesStat(StatType.AnglerSetHookDrain, 2)

                .AddPerkLevel()
                .Description("In the Line, Set Hook drains 3 more Stamina from the fish. Against a rare fish, half as much, rounded down.")
                .Price(3)
                .RequirementSkill(SkillType.Agriculture, 45)
                .IncreasesStat(StatType.AnglerSetHookDrain, 3);
        }

        private void FishSense()
        {
            _builder.Create(PerkCategoryType.Agriculture, PerkType.FishSense)
                .Name("Fish Sense")
                .Icon("ife_fish_eyes")

                .AddPerkLevel()
                .Description("In the Line, the window shows the fish's next two moves on the first 2 turns of a fight. It does not work on a rare fish.")
                .Price(2)
                .RequirementSkill(SkillType.Agriculture, 15)
                .IncreasesStat(StatType.AnglerForecastTurns, 2)

                .AddPerkLevel()
                .Description("In the Line, the window shows the fish's next two moves on the first 4 turns of a fight. It does not work on a rare fish.")
                .Price(3)
                .RequirementSkill(SkillType.Agriculture, 35)
                .IncreasesStat(StatType.AnglerForecastTurns, 4);
        }

        private void PatientAngler()
        {
            _builder.Create(PerkCategoryType.Agriculture, PerkType.PatientAngler)
                .Name("Patient Angler")
                .Icon("ife_fish_whisper")

                .AddPerkLevel()
                .Description("Your Line style, always active: Give Line lets out 1 less Distance (3 instead of 4). Against a rare fish it does nothing. Cannot be purchased with Aggressive Angler.")
                .Price(2)
                .RequirementSkill(SkillType.Agriculture, 20)
                .RequirementCannotHavePerk(PerkType.AggressiveAngler)
                .IncreasesStat(StatType.AnglerGiveLineDistanceReduction, 1);
        }

        private void AggressiveAngler()
        {
            _builder.Create(PerkCategoryType.Agriculture, PerkType.AggressiveAngler)
                .Name("Aggressive Angler")
                .Icon("ife_snagging")

                .AddPerkLevel()
                .Description("Your Line style, always active: each Reel pulls the fish in 1 more Distance. Against a rare fish it does nothing. Cannot be purchased with Patient Angler.")
                .Price(2)
                .RequirementSkill(SkillType.Agriculture, 20)
                .RequirementCannotHavePerk(PerkType.PatientAngler)
                .IncreasesStat(StatType.AnglerReelDistance, 1);
        }

        private void TrophyCatch()
        {
            _builder.Create(PerkCategoryType.Agriculture, PerkType.TrophyCatch)
                .Name("Trophy Catch")
                .Icon("ife_laurel_goobb")

                .AddPerkLevel()
                .Description("In the Line, a rare fish fights as if it were 4 steps easier on the difficulty ladder, a little over half a level weaker. It is the only fishing perk that works on a rare fish at full strength.")
                .Price(4)
                .RequirementSkill(SkillType.Agriculture, 50)
                .IncreasesStat(StatType.AnglerRareFishRungs, 4);
        }
    }
}
