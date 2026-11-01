using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    /// <summary>
    /// The Researcher line of Fabrication: ten purchases at Fabrication 5/10/15/20/25/30/35/40/45/50 for 1/1/2/2/3/3/3/3/3/4 SP, each
    /// a stat adjustment the research Experiment reads. The rank-20 purchase is the style, a choice of two through
    /// <see cref="PerkBuilder.RequirementCannotHavePerk"/>, and the owned style is always active.
    /// </summary>
    public class FabricationResearcherPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            FieldNotes();
            EfficientAnalysis();
            Methodical();
            Intuitive();
            BroaderHypotheses();
            SteadyHands();
            PeerReview();
            SecondOpinion();
            Breakthrough();

            return _builder.Build();
        }

        private void FieldNotes()
        {
            _builder.Create(PerkCategoryType.Fabrication, PerkType.FieldNotes)
                .Name("Field Notes")
                .Icon("ife_rsrch")

                .AddPerkLevel()
                .Description("In a research experiment, every Test has 10 more points of chance to find evidence.")
                .Price(1)
                .RequirementSkill(SkillType.Fabrication, 5)
                .IncreasesStat(StatType.ExperimentTestChancePercent, 10)

                .AddPerkLevel()
                .Description("In a research experiment, every Test has 20 more points of chance to find evidence.")
                .Price(2)
                .RequirementSkill(SkillType.Fabrication, 15)
                .IncreasesStat(StatType.ExperimentTestChancePercent, 20);
        }

        private void SteadyHands()
        {
            _builder.Create(PerkCategoryType.Fabrication, PerkType.SteadyHands)
                .Name("Steady Hands")
                .Icon("ife_observe")

                .AddPerkLevel()
                .Description("In a research experiment, Refine Method costs 5 less CP, to a minimum of 1 CP.")
                .Price(3)
                .RequirementSkill(SkillType.Fabrication, 35)
                .IncreasesStat(StatType.ExperimentRefineCpReduction, 5);
        }

        private void BroaderHypotheses()
        {
            _builder.Create(PerkCategoryType.Fabrication, PerkType.BroaderHypotheses)
                .Name("Broader Hypotheses")
                .Icon("ife_rsrch_proj")

                .AddPerkLevel()
                .Description("Every research experiment has 1 more trial.")
                .Price(3)
                .RequirementSkill(SkillType.Fabrication, 25)
                .IncreasesStat(StatType.ExperimentTrials, 1);
        }

        private void Methodical()
        {
            _builder.Create(PerkCategoryType.Fabrication, PerkType.Methodical)
                .Name("Methodical")
                .Icon("ife_sci_net")

                .AddPerkLevel()
                .Description("Your research style, always active: every research experiment has 2 more trials, and every Test has 15 fewer points of chance. Cannot be purchased with Intuitive.")
                .Price(2)
                .RequirementSkill(SkillType.Fabrication, 20)
                .RequirementCannotHavePerk(PerkType.Intuitive)
                .IncreasesStat(StatType.ExperimentTrials, 2)
                .IncreasesStat(StatType.ExperimentTestChancePercent, -15);
        }

        private void Intuitive()
        {
            _builder.Create(PerkCategoryType.Fabrication, PerkType.Intuitive)
                .Name("Intuitive")
                .Icon("ife_tiny_bulb")

                .AddPerkLevel()
                .Description("Your research style, always active: every Test has 20 more points of chance, and every research experiment has 1 fewer trial. Cannot be purchased with Methodical.")
                .Price(2)
                .RequirementSkill(SkillType.Fabrication, 20)
                .RequirementCannotHavePerk(PerkType.Methodical)
                .IncreasesStat(StatType.ExperimentTestChancePercent, 20)
                .IncreasesStat(StatType.ExperimentTrials, -1);
        }

        private void PeerReview()
        {
            _builder.Create(PerkCategoryType.Fabrication, PerkType.PeerReview)
                .Name("Peer Review")
                .Icon("ife_copycat_bulb")

                .AddPerkLevel()
                .Description("Adds Peer Review to a research experiment where evidence decides between a credit reduction and a time reduction: once per experiment, for 25 CP and no trial, move half of one candidate's evidence (rounded up) to the other.")
                .Price(3)
                .RequirementSkill(SkillType.Fabrication, 40)
                .IncreasesStat(StatType.ExperimentPeerReviewUnlocked, 1);
        }

        private void EfficientAnalysis()
        {
            _builder.Create(PerkCategoryType.Fabrication, PerkType.EfficientAnalysis)
                .Name("Efficient Analysis")
                .Icon("ife_min_light")

                .AddPerkLevel()
                .Description("In a research experiment, Analyze cuts 6% of the job's time for each piece of spare evidence instead of 5%. The cap of 25% stays.")
                .Price(1)
                .RequirementSkill(SkillType.Fabrication, 10)
                .IncreasesStat(StatType.ExperimentTimeCutPerEvidencePercent, 1)

                .AddPerkLevel()
                .Description("In a research experiment, Analyze cuts 7% of the job's time for each piece of spare evidence instead of 5%. The cap of 25% stays.")
                .Price(3)
                .RequirementSkill(SkillType.Fabrication, 30)
                .IncreasesStat(StatType.ExperimentTimeCutPerEvidencePercent, 2);
        }

        private void SecondOpinion()
        {
            _builder.Create(PerkCategoryType.Fabrication, PerkType.SecondOpinion)
                .Name("Second Opinion")
                .Icon("ife_prune")

                .AddPerkLevel()
                .Description("In a research experiment, Refine Method raises the chance of your next Test by 20 points instead of 15.")
                .Price(3)
                .RequirementSkill(SkillType.Fabrication, 45)
                .IncreasesStat(StatType.ExperimentRefineChancePercent, 5);
        }

        private void Breakthrough()
        {
            _builder.Create(PerkCategoryType.Fabrication, PerkType.Breakthrough)
                .Name("Breakthrough")
                .Icon("ife_spark")

                .AddPerkLevel()
                .Description("Adds Breakthrough to a research experiment: once per experiment, free and without a trial, your next Test succeeds automatically, and for the rest of the experiment each piece of spare evidence cuts twice as much of the job's time, within the cap of 25%. It grants no extra bonuses.")
                .Price(4)
                .RequirementSkill(SkillType.Fabrication, 50)
                .IncreasesStat(StatType.ExperimentBreakthroughUnlocked, 1);
        }
    }
}
