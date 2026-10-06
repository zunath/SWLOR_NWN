using System.Text.Json;
using System.Text.Json.Serialization;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Service.PerkService;

internal static class CraftingSimulation
{
    private static readonly CraftActionType[] Actions = Enum.GetValues<CraftActionType>();
    private sealed record Scenario(string Name, int Rank, int Level, int Cft, int Control, int CP, int Penalty = 0);
    private sealed record Result(string Scenario, string Profile, string Build, string Policy, int Seeds,
        double CompletionPercent, double ExpectedQualityPercent, double SuccessfulQualityPercent, double MeanActions,
        int MaxActions, int DistinctFirstTenSequences, double XPQualityFactorPerAction);
    private static CraftSession Step(CraftSession state, CraftActionType action, int roll = 1) =>
        CraftActionEvaluator.Resolve(state, new(state.Id, state.ActionCount, action), () => roll).Session;
    private static double Quality(CraftSession state) => state.Quality / (double)state.MaxQuality;
    private static IReadOnlyDictionary<StatType,int> Stats(string skill, string build)
    {
        IPerkListDefinition definition = skill switch
        {
            "Smithery" => new SmitheryCraftingPerkDefinition(),
            "Engineering" => new EngineeringCraftingPerkDefinition(),
            "Fabrication" => new FabricationCraftingPerkDefinition(),
            "Agriculture" => new AgricultureCraftingPerkDefinition(),
            _ => new EspionageCraftingPerkDefinition()
        };
        var perks = definition.BuildPerks().Values.ToArray();
        var chosen = build == "None" ? Array.Empty<PerkDetail>() : build == "LineA" ? perks.Take(1) : build == "LineB" ? perks.Skip(1).Take(1) : perks;
        return chosen.SelectMany(perk => perk.PerkLevels.Values.Last().StatBonuses).ToDictionary(bonus => bonus.Stat, bonus => bonus.Calculate(0));
    }
    // This conservative reserve uses no opportunity cards. It proves base completion remains affordable.
    private static (int CP, int Durability)? CompletionReserve(CraftSession s)
    {
        if (s.Status == CraftSessionStatus.Succeeded) return (0,0);
        if (s.Status != CraftSessionStatus.Active) return null;
        var p = CraftWorkEvaluator.UnconditionedProgress(s);
        if (p <= 0) return null;
        var required = s.MaxProgress - s.Progress;
        var basicDur = s.Profile == CraftProfile.Sturdy ? 8 : 10;
        var carefulDur = s.Profile == CraftProfile.Sturdy ? 4 : 5;
        (int CP, int Durability)? best = null;
        for (var careful = 0; careful <= (s.SkillRank >= 30 ? Math.Min(s.CP/10, (int)Math.Ceiling(required / (p*1.2))) : 0); careful++)
        {
            var basic = Math.Max(0,(int)Math.Ceiling((required - careful*Math.Floor(p*1.2))/p));
            var dur = basic*basicDur + careful*carefulDur;
            var repairs = Math.Max(0,(int)Math.Ceiling((dur - s.Durability) / 30d));
            if (repairs > 0 && s.SkillRank < 10) continue;
            var cost = careful*10 + repairs*10;
            if (cost > s.CP) continue;
            if (best == null || cost*3 + dur < best.Value.CP*3 + best.Value.Durability) best = (cost,dur);
        }
        return best;
    }
    private static double Score(CraftSession s)
    {
        if (s.Status == CraftSessionStatus.Succeeded) return Quality(s) - s.ActionCount*0.0001;
        var reserve=CompletionReserve(s); if (reserve == null) return -100;
        var cp=s.CP-reserve.Value.CP;
        var dur=s.Durability-reserve.Value.Durability;
        var touches=0;
        for (var k=1;k<=100;k++)
        {
            var repairs=Math.Max(0,(int)Math.Ceiling((k*10-dur)/30d));
            if (k*3+repairs*10>cp || repairs>0 && s.SkillRank<10) break;
            touches=k;
        }
        var possible=touches*CraftWorkEvaluator.UnconditionedQuality(s);
        if (s.Buff(CraftBuffType.MuscleMemory) is {} muscle) possible += (int)(CraftWorkEvaluator.UnconditionedQuality(s)*muscle.Magnitude/100d);
        var attainable=Math.Min(1,Quality(s)+possible/(double)s.MaxQuality);
        return Quality(s)*0.35 + attainable*0.65 + s.Progress/(double)s.MaxProgress*0.005 - s.ActionCount*0.0001;
    }
    private static CraftActionType Choose(CraftSession real, bool forecast, bool risk = false)
    {
        var visible=Enumerable.Repeat(CraftCondition.Normal,real.ConditionIndex+10).ToArray();
        visible[real.ConditionIndex]=real.CurrentCondition;
        if (forecast)
            for (var i=1;i<=real.ForecastLength && real.ConditionIndex+i<real.Conditions.Count;i++) visible[real.ConditionIndex+i]=real.Conditions[real.ConditionIndex+i];
        var s=real with { Conditions=visible };
        var best=double.NegativeInfinity; var chosen=CraftActionType.BasicSynthesis;
        if (real.Quality >= real.MaxQuality)
        {
            var finishing=Actions.Select(a => (Action:a, Preview:CraftActionEvaluator.Preview(real,a)))
                .Where(x => x.Preview.IsAvailable && x.Preview.SuccessChance==100 && x.Preview.FinishesOnSuccess).OrderBy(x => x.Preview.CPCost).FirstOrDefault();
            if (finishing.Preview != null) return finishing.Action;
        }
        foreach (var a in Actions)
        {
            var preview=CraftActionEvaluator.Preview(s,a);
            if (!preview.IsAvailable || preview.SuccessChance<100 && !risk) continue;
            var next=Step(s,a);var score=Score(next);
            if (next.Status==CraftSessionStatus.Active)
            {
                var lookahead=double.NegativeInfinity;
                foreach(var second in Actions)
                {
                    var p=CraftActionEvaluator.Preview(next,second);
                    if(p.IsAvailable && p.SuccessChance==100) lookahead=Math.Max(lookahead,Score(Step(next,second)));
                }
                if(lookahead>-100) score=score*0.1+lookahead*0.9;
            }
            if (preview.SuccessChance < 100) score = score*preview.SuccessChance/100d + Score(Step(s,a,100))*(100-preview.SuccessChance)/100d;
            if(score>best){best=score;chosen=a;}
        }
        return chosen;
    }
    private static CraftSession Run(CraftSession initial, string policy, IReadOnlyList<CraftActionType> fixedActions, int seed, out string trace)
    {
        var s=initial;var rolls=new Random(seed ^ 173991);var sequence=new List<CraftActionType>();var cursor=0;
        while(s.Status==CraftSessionStatus.Active && s.ActionCount<160)
        {
            var a=policy=="Fixed" ? cursor<fixedActions.Count ? fixedActions[cursor++] : CraftActionType.BasicSynthesis : Choose(s,policy=="Forecast" || policy=="Risk aware",policy=="Risk aware");
            var preview=CraftActionEvaluator.Preview(s,a);
            if(!preview.IsAvailable)
            {
                if(policy=="Fixed" && cursor<fixedActions.Count)continue;
                s=s with {Status=CraftSessionStatus.Aborted};break;
            }
            sequence.Add(a);s=Step(s,a,rolls.Next(1,101));
            if(s.CP<0 || s.Durability<0 || s.CP>s.MaxCP || s.Durability>s.MaxDurability) throw new InvalidOperationException("Resource bounds failed");
        }
        if(s.ActionCount>=160) throw new InvalidOperationException("Policy exceeded the finite action budget");
        trace=string.Join(',',sequence.Take(10));return s;
    }
    private static CraftSession New(Scenario fixture, CraftProfile profile, string skill, string build, int seed)
    {
        if (skill.StartsWith("Espionage")) fixture = fixture with { Cft = 0, Control = 0, CP = 0 };
        var chart=new RecipeLevelChart().GetByLevel(fixture.Level);var random=new Random(seed);
        return profile==CraftProfile.Legacy ? CraftSession.CreateLegacy(fixture.Rank,fixture.Level,chart,fixture.Cft,fixture.Control,fixture.CP,fixture.Penalty)
            : CraftSession.Create(fixture.Rank,fixture.Level,chart,fixture.Cft,fixture.Control,fixture.CP,profile,
                skill=="Espionage Trap" ? CraftTechnique.TrapAssembly : skill=="Espionage Poison" ? CraftTechnique.PoisonMixing : CraftTechnique.None, Stats(skill,build),random.Next,fixture.Penalty);
    }
    private static IReadOnlyList<CraftActionType> TrainFixed(Scenario scenario, CraftProfile profile, string skill, string build)
    {
        // Bounded search: neutral-state adaptive templates and explicit touch/repair rotations.
        var candidates=new List<IReadOnlyList<CraftActionType>>();
        for(var touches=0;touches<=20;touches++)
        {
            var actions=new List<CraftActionType>();
            for(var i=0;i<touches;i++) {if(i>0 && i%5==0)actions.Add(CraftActionType.MastersMend); actions.Add(CraftActionType.BasicTouch);}
            actions.Add(CraftActionType.MastersMend);
            actions.AddRange(Enumerable.Repeat(CraftActionType.BasicSynthesis,20));candidates.Add(actions);
        }
        if (profile == CraftProfile.Legacy) candidates.Add(new[] { CraftActionType.BasicTouch,CraftActionType.BasicTouch,CraftActionType.BasicTouch,CraftActionType.BasicTouch,CraftActionType.BasicTouch,CraftActionType.BasicTouch,
            CraftActionType.MastersMend,CraftActionType.WasteNot,CraftActionType.BasicTouch,CraftActionType.BasicTouch,CraftActionType.BasicTouch,CraftActionType.BasicTouch,
            CraftActionType.WasteNot,CraftActionType.BasicTouch,CraftActionType.BasicTouch,CraftActionType.BasicTouch,CraftActionType.BasicTouch,
            CraftActionType.WasteNot,CraftActionType.SteadyHand,CraftActionType.RapidSynthesis,CraftActionType.SteadyHand,CraftActionType.CarefulSynthesis });
        for(var seed=-10;seed<0;seed++)
        {
            var s=New(scenario,profile,skill,build,seed);var actions=new List<CraftActionType>();
            while(s.Status==CraftSessionStatus.Active && actions.Count<80)
            {var a=Choose(s,false);if(!CraftActionEvaluator.Preview(s,a).IsAvailable)break;actions.Add(a);s=Step(s,a);}
            candidates.Add(actions);
        }
        double best=-1;IReadOnlyList<CraftActionType> winner=candidates[0];
        foreach(var candidate in candidates)
        {
            var score=0d;
            for(var seed=-16;seed<0;seed++)
            {var final=Run(New(scenario,profile,skill,build,seed),"Fixed",candidate,seed,out _);if(final.Status==CraftSessionStatus.Succeeded)score+=Quality(final)+0.01;}
            if(score>best){best=score;winner=candidate;}
        }
        return winner;
    }
    public static void Run(string[] args)
    {
        var seeds=args.Length>2?int.Parse(args[2]):100;
        var output=Path.GetFullPath(args.Length>1?args[1]:"design/testing/crafting-policy-results.json");
        var scenarios=new[]{ new Scenario("Starter",5,5,0,0,0),new Scenario("Repair unlock",10,10,0,0,10),
            new Scenario("Rank 25",25,25,20,20,25),new Scenario("Baseline gear",50,50,30,29,37),
            new Scenario("Researched gear",50,50,80,279,37),new Scenario("Three rank deficit",50,53,80,279,37),
            new Scenario("Enhanced recipe",50,50,80,279,37,160) };
        if (args.Length > 3) scenarios = scenarios.Where(s => s.Name.Contains(args[3], StringComparison.OrdinalIgnoreCase)).ToArray();
        var results=new List<Result>();
        var rotations=new List<object>();
        foreach(var fixture in scenarios)
        foreach(var profile in new[]{CraftProfile.Legacy,CraftProfile.Sturdy,CraftProfile.Delicate,CraftProfile.Calibrated})
        foreach(var skill in profile==CraftProfile.Legacy ? new[]{"Legacy"} : profile==CraftProfile.Sturdy ? new[]{"Smithery","Fabrication"} : profile==CraftProfile.Delicate ? new[]{"Agriculture","Espionage Poison"} : new[]{"Engineering","Espionage Trap"})
        foreach(var build in fixture.Rank==50 && profile!=CraftProfile.Legacy ? new[]{"None","LineA","LineB","Full"}:new[]{"None"})
        {
            var fixedActions=TrainFixed(fixture,profile,skill,build);
            rotations.Add(new { Scenario=fixture.Name, Profile=profile.ToString(), Skill=skill, Build=build, Actions=fixedActions });
            foreach(var policy in profile==CraftProfile.Legacy ? new[]{"Fixed"} : new[]{"Fixed","Current condition","Forecast","Risk aware"})
            {
                var states=new List<CraftSession>();var traces=new HashSet<string>();
                for(var seed=10000;seed<10000+seeds;seed++)
                {var final=Run(New(fixture,profile,skill,build,seed),policy,fixedActions,seed,out var trace);states.Add(final);traces.Add(trace);}
                var successes=states.Where(s=>s.Status==CraftSessionStatus.Succeeded).ToArray();
                results.Add(new(fixture.Name,profile.ToString(),skill+" "+build,policy,seeds,successes.Length*100d/seeds,
                    states.Average(s=>s.Status==CraftSessionStatus.Succeeded?Quality(s)*100:0),successes.Length>0?successes.Average(s=>Quality(s)*100):0,
                    states.Average(s=>s.ActionCount),states.Max(s=>s.ActionCount),traces.Count,
                    states.Average(s=>s.Status==CraftSessionStatus.Succeeded?(1+Quality(s))/Math.Max(1,s.ActionCount):0)));
            }
            Console.WriteLine($"Simulated {fixture.Name}, {profile}, {skill} {build}: {seeds} paired evaluation seeds.");
        }
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        var options=new JsonSerializerOptions{WriteIndented=true};options.Converters.Add(new JsonStringEnumConverter());
        File.WriteAllText(output,JsonSerializer.Serialize(new { SeedsPerCase=seeds, TrainingSeeds="-16 to -1",EvaluationSeeds="10000 onward",
            Limitations="Bounded fixed search, not an optimality proof. Policies never inspect hidden cards. Per-action XP proxy excludes human timing and vendor/property transfer economics. Human playtesting remains a rollout gate.", Rotations=rotations, Results=results },options));
        Console.WriteLine($"Wrote {results.Count} policy results to {output}");
    }
}
