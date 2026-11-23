using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;
using static SWLOR.Game.Server.Tests.Support.CombatTranscriptBench;

namespace SWLOR.Game.Server.Tests.Domain.Companions;

/// <summary>
/// The ordered call-and-draw transcripts of the beast incubation code, taken before it is split: using an incubator (the perk gate, the
/// property gate, another player's job still running and finished, the window for the player's own job and for an empty incubator) and
/// finishing a job into an egg (<c>CreateBeastEgg</c>: the mutation roll with its seeded draws, the field note, the item properties of every
/// purity, the vendor bonus and the job removal), plus the egg created by beast type. The real container hands out the beast mastery
/// service over the whole beast catalog; the neighbours it calls are recording stand-ins, the engine records its writes in order and every
/// random draw is written with its bounds. Regenerate with <c>SWLOR_UPDATE_SNAPSHOTS=1</c> only for a deliberate change.
/// </summary>
[TestFixture]
[NonParallelizable]
public class BeastIncubationTranscriptTests
{
    private const uint Incubator = 90;
    private const uint Egg = 800;

    private static readonly Type[] Leaves =
    {
        typeof(IIncubationFieldNoteService), typeof(IPropertyService), typeof(IAIService), typeof(ICombatRecorderService),
        typeof(ICompanionControlService), typeof(IGuiService), typeof(IItemService), typeof(IPerkService), typeof(IResistanceService),
        typeof(IStatService), typeof(ICreatureStatService), typeof(IStatusEffectApplicationService), typeof(ICombatDamageCatalogService)
    };

    private sealed class Scene : IDisposable
    {
        private readonly List<IDisposable> _scopes = new();
        private uint _nextEgg = Egg;

        public Scene()
        {
            Bench = new CombatTranscriptBench(Leaves);
            Bench.NameCreature(Incubator, "Incubator");
            Bench.Db.OnSet = entity => Bench.Lines.Add($"DB.Set {entity.GetType().Name} {entity.Id}");
            _scopes.Add(ItemPluginRuntime.Override(CombatTranscriptBench.EngineProxy.Wrap<IItemPlugin>(new FakeItemPlugin(), Bench)));

            Bench.Engine.SetReturn("GetLastUsedBy", Attacker);
            Bench.Engine.SetReturn("OBJECT_SELF", Incubator);
            Bench.Engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! is not (0 or 0x7F000000u or Gone));
            Bench.Engine.SetReturnHandler("CreateItemOnObject", _ =>
            {
                var egg = _nextEgg++;
                Bench.NameCreature(egg, "Egg" + (egg - Egg + 1));
                return egg;
            });

            Bench.Leaf<IPerkService>().Script(nameof(IPerkService.GetPerkLevel), _ => DnaManipulationLevel);
            Bench.Leaf<IPropertyService>().Script(nameof(IPropertyService.GetPropertyId), _ => PropertyId);
            Bench.Leaf<IResistanceService>().Script(nameof(IResistanceService.IsValidResistanceType), args => (ResistanceType)args[0]! != ResistanceType.Invalid);
            Bench.Leaf<IResistanceService>().Script(nameof(IResistanceService.GetAllResistanceTypes), _ => new[] { ResistanceType.Fire, ResistanceType.Poison, ResistanceType.Mind });
            Bench.Leaf<ICombatDamageCatalogService>().Script(nameof(ICombatDamageCatalogService.GetDefenseDamageTypes), _ =>
                Enum.GetValues<CombatDamageType>().Where(type => type.IsDefenseDamageType()).Take(3).ToArray());
        }

        public CombatTranscriptBench Bench { get; }
        public int DnaManipulationLevel { get; set; } = 1;
        public string PropertyId { get; set; } = "property-1";

        /// <summary>The beast catalog (the stay-part).</summary>
        public IBeastMasteryService Beasts => Bench.Get<IBeastMasteryService>();

        /// <summary>The incubator and egg code: the service that holds it, so a split changes this one line.</summary>
        public IBeastMasteryService Incubation => Bench.Get<IBeastMasteryService>();

        public void Dispose()
        {
            for (var index = _scopes.Count - 1; index >= 0; index--)
                _scopes[index].Dispose();
            Bench.Dispose();
        }
    }

    // The beast with the most mutations a job rich in enzymes qualifies for (the first of them, in the order of the enum), leaving out those that
    // wait for a day of the week: the same beast on every run of the catalog as it is.
    private static BeastType BeastWithOpenMutations(IBeastMasteryService beast) =>
        beast.GetAllBeastTypes()
            .OrderBy(type => (int)type)
            .Select(type => (Type: type, Open: beast.GetBeastDetail(type).PossibleMutations.Count(mutation => mutation.Weight > 0 && mutation.Requirements.All(requirement => requirement is MutationRequirementEnzyme or MutationRequirementMinimumChance))))
            .Where(entry => entry.Open > 0)
            .OrderByDescending(entry => entry.Open)
            .First()
            .Type;

    private static BeastType BeastWithoutMutations(IBeastMasteryService beast) =>
        beast.GetAllBeastTypes()
            .OrderBy(type => (int)type)
            .First(type => beast.GetBeastDetail(type).PossibleMutations.Count == 0);

    private static IncubationJob Job(BeastType dna, int mutationChance, bool suppressed = false) => new()
    {
        Id = "job-1",
        ParentPropertyId = "property-1",
        PlayerId = "uuid-Attacker",
        BeastDNAType = dna,
        MutationChance = mutationChance,
        IsMutationSuppressed = suppressed,
        AttackPurity = 400,
        AccuracyPurity = 500,
        EvasionPurity = 600,
        LearningPurity = 700,
        XPPenalty = 100,
        LyaseColors = Enum.GetValues<EnzymeColorType>().ToDictionary(color => color, _ => 99),
        IsomeraseColors = Enum.GetValues<EnzymeColorType>().ToDictionary(color => color, _ => 99),
        HydrolaseColors = Enum.GetValues<EnzymeColorType>().ToDictionary(color => color, _ => 99),
        DefensePurities = Enum.GetValues<CombatDamageType>().Where(type => type.IsDefenseDamageType()).Take(2).ToDictionary(type => type, type => 300),
        ResistancePurities = new Dictionary<ResistanceType, int> { [ResistanceType.Fire] = 800, [ResistanceType.Mind] = 250 }
    };

    [Test]
    public void UsingAnIncubatorProducesTheCapturedTranscript()
    {
        using var scene = new Scene();
        var bench = scene.Bench;
        var beast = scene.Beasts;
        var incubation = scene.Incubation;
        beast.CacheData();
        var dna = BeastWithoutMutations(beast);
        bench.Lines.Clear();

        bench.Step("the player lacks the DNA Manipulation perk", () =>
        {
            scene.DnaManipulationLevel = 0;
            incubation.UseIncubator();
            scene.DnaManipulationLevel = 1;
        });
        bench.Step("the incubator has no property", () =>
        {
            scene.PropertyId = " ";
            incubation.UseIncubator();
            scene.PropertyId = "property-1";
        });
        bench.Step("no job runs: the window opens for an empty incubator", () => incubation.UseIncubator());
        bench.Step("another player's job is still running", () =>
        {
            var job = Job(dna, 0);
            job.PlayerId = "uuid-Defender";
            job.DateCompleted = DateTime.UtcNow.AddDays(3).AddHours(12).AddMinutes(30).AddSeconds(30);
            bench.Db.With(job);
            incubation.UseIncubator();
        });
        bench.Step("another player's job has finished", () =>
        {
            var job = bench.Db.Get<IncubationJob>("job-1");
            job.DateCompleted = DateTime.UtcNow.AddDays(-1);
            bench.Db.With(job);
            incubation.UseIncubator();
        });
        bench.Step("the player's own job: the window opens with the job", () =>
        {
            var job = bench.Db.Get<IncubationJob>("job-1");
            job.PlayerId = "uuid-Attacker";
            bench.Db.With(job);
            incubation.UseIncubator();
        });

        AssertGolden("SWLOR.Game.Server.Tests/Fixtures/CompanionTranscripts/BeastUseIncubator.txt", bench.Text);
    }

    [Test]
    public void FinishingAJobIntoAnEggProducesTheCapturedTranscript()
    {
        using var scene = new Scene();
        var bench = scene.Bench;
        var beast = scene.Beasts;
        var incubation = scene.Incubation;
        beast.CacheData();
        var mutating = BeastWithOpenMutations(beast);
        var plain = BeastWithoutMutations(beast);
        bench.Step("the beasts of the scenario", () =>
            bench.Note("mutations: " + beast.GetBeastDetail(mutating).Name + " (" + beast.GetBeastDetail(mutating).PossibleMutations.Count + " mutations); plain: " + beast.GetBeastDetail(plain).Name));

        void Finish(string label, IncubationJob job) => bench.Step(label, () =>
        {
            bench.Db.With(job);
            incubation.CreateBeastEgg(job, Attacker);
            foreach (var deleted in bench.Db.Deleted.ToArray())
                bench.Lines.Add("DB.Deleted IncubationJob " + deleted);
            bench.Db.Deleted.Clear();
        });

        Finish("mutation suppressed: no roll, the DNA beast", Job(mutating, 1000, suppressed: true));
        Finish("no mutations possible: the chance is rolled, nothing to choose", Job(plain, 1000));
        Finish("a mutation chance of 0: one roll that fails", Job(mutating, 0));
        Finish("a mutation chance of 1000: the roll succeeds and a mutation is picked by weight", Job(mutating, 1000));
        Finish("a middle chance", Job(mutating, 500));
        Finish("a second middle chance", Job(mutating, 500));

        bench.Step("an egg by beast type", () => incubation.CreateBeastEgg(mutating, Attacker));
        bench.Step("the egg of a job with no resistance purities and no experience penalty", () =>
        {
            var job = Job(plain, 0);
            job.ResistancePurities = new Dictionary<ResistanceType, int>();
            job.XPPenalty = 0;
            bench.Db.With(job);
            incubation.CreateBeastEgg(job, Attacker);
        });

        AssertGolden("SWLOR.Game.Server.Tests/Fixtures/CompanionTranscripts/BeastCreateEgg.txt", bench.Text);
    }
}
